using FuelManagement.Closures.Dtos;
using FuelManagement.Shared.Contracts;
using FuelManagement.Shared.Data;
using FuelManagement.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FuelManagement.Closures.Services;

/// <summary>
/// Implementacion del servicio de cierres diarios.
/// Todas las operaciones de escritura se delegan a funciones PL/pgSQL via FromSqlRaw.
/// fn_registrar_auditoria se llama dentro de la misma conexion para garantizar atomicidad.
/// </summary>
public sealed class ClosureService : IClosureService
{
    private readonly FuelDbContext _db;

    public ClosureService(FuelDbContext db)
    {
        _db = db;
    }

    // ----------------------------------------------------------------
    // Preview
    // ----------------------------------------------------------------
    public async Task<ClosurePreviewDto> GetPreviewAsync(
        Guid tanqueId, DateOnly fecha, CancellationToken ct = default)
    {
        var rows = await _db.FnCalcCierreRows
            .FromSqlRaw("SELECT * FROM fn_calcular_cierre_diario({0}, {1})", tanqueId, fecha)
            .ToListAsync(ct);

        var row = rows.FirstOrDefault()
            ?? throw new KeyNotFoundException($"No se encontraron datos para el tanque {tanqueId} en la fecha {fecha}.");

        return new ClosurePreviewDto
        {
            TanqueId = tanqueId,
            Fecha = fecha,
            StockInicial = row.StockInicial,
            TotalRecepciones = row.TotalRecepciones,
            TotalTransferenciasEntrada = row.TotalTransferenciasEntrada,
            TotalTransferenciasSalida = row.TotalTransferenciasSalida,
            TotalDespachos = row.TotalDespachos,
            TotalAjustesPositivos = row.TotalAjustesPositivos,
            TotalAjustesNegativos = row.TotalAjustesNegativos,
            StockTeoricoFinal = row.StockTeoricoFinal,
        };
    }

    // ----------------------------------------------------------------
    // Create
    // ----------------------------------------------------------------
    public async Task<Guid> CreateAsync(
        CreateClosureRequest req, Guid usuarioId, CancellationToken ct = default)
    {
        // Llamar a la funcion PostgreSQL dentro de una transaccion
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var cierreId = await _db.Database
            .SqlQueryRaw<Guid>(
                "SELECT fn_crear_cierre_diario(@p_tanque_id,@p_usuario_id,@p_fecha,@p_stock_fisico_final,@p_motivo_diferencia,@p_observaciones) AS \"Value\"",
                new NpgsqlParameter { ParameterName = "@p_tanque_id", NpgsqlDbType = NpgsqlDbType.Uuid, Value = req.TanqueId },
                new NpgsqlParameter { ParameterName = "@p_usuario_id", NpgsqlDbType = NpgsqlDbType.Uuid, Value = usuarioId },
                new NpgsqlParameter { ParameterName = "@p_fecha", NpgsqlDbType = NpgsqlDbType.Date, Value = req.FechaCierre },
                new NpgsqlParameter { ParameterName = "@p_stock_fisico_final", NpgsqlDbType = NpgsqlDbType.Numeric, Value = req.StockFisicoFinal },
                new NpgsqlParameter { ParameterName = "@p_motivo_diferencia", NpgsqlDbType = NpgsqlDbType.Varchar, Value = (object?)req.MotivoDiferencia ?? DBNull.Value },
                new NpgsqlParameter { ParameterName = "@p_observaciones", NpgsqlDbType = NpgsqlDbType.Varchar, Value = (object?)req.Observaciones ?? DBNull.Value })
            .FirstAsync(ct);

        // Registrar auditoria
        await _db.Database.ExecuteSqlRawAsync(
            "SELECT fn_registrar_auditoria(@p_usuario_id,@p_accion,@p_entidad,@p_entidad_id,@p_datos_anteriores,@p_datos_nuevos,@p_direccion_ip,@p_user_agent)",
            AuditoriaParams(usuarioId, "CLOSURE_CREATED", "cierre_diario", cierreId));

        await tx.CommitAsync(ct);
        return cierreId;
    }

    // ----------------------------------------------------------------
    // Get All (paginado via vista vw_cierres_resumen)
    // ----------------------------------------------------------------
    public async Task<PagedResult<ClosureDto>> GetAllAsync(
        DateOnly? fechaDesde,
        DateOnly? fechaHasta,
        Guid? tanqueId,
        string? estado,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.VwCierresResumen.AsQueryable();

        if (fechaDesde.HasValue)
            query = query.Where(x => x.FechaCierre >= fechaDesde.Value);
        if (fechaHasta.HasValue)
            query = query.Where(x => x.FechaCierre <= fechaHasta.Value);
        if (tanqueId.HasValue)
            query = query.Where(x => x.TanqueId == tanqueId.Value);
        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(x => x.Estado == estado.ToUpper());

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.FechaCierre)
            .ThenByDescending(x => x.FechaCreacion)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapToDto(x))
            .ToListAsync(ct);

        return new PagedResult<ClosureDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    // ----------------------------------------------------------------
    // Get By Id
    // ----------------------------------------------------------------
    public async Task<ClosureDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var row = await _db.VwCierresResumen
            .Where(x => x.CierreId == id)
            .FirstOrDefaultAsync(ct);

        return row is null ? null : MapToDto(row);
    }

    // ----------------------------------------------------------------
    // Approve
    // ----------------------------------------------------------------
    public async Task ApproveAsync(
        Guid cierreId, Guid supervisorId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        await _db.Database.ExecuteSqlRawAsync(
            "SELECT fn_aprobar_cierre({0},{1})",
            cierreId,
            supervisorId);

        await _db.Database.ExecuteSqlRawAsync(
            "SELECT fn_registrar_auditoria(@p_usuario_id,@p_accion,@p_entidad,@p_entidad_id,@p_datos_anteriores,@p_datos_nuevos,@p_direccion_ip,@p_user_agent)",
            AuditoriaParams(supervisorId, "CLOSURE_APPROVED", "cierre_diario", cierreId));

        await tx.CommitAsync(ct);
    }

    // ----------------------------------------------------------------
    // Reject
    // ----------------------------------------------------------------
    public async Task RejectAsync(
        Guid cierreId, Guid supervisorId, string motivo, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        await _db.Database.ExecuteSqlRawAsync(
            "SELECT fn_rechazar_cierre({0},{1},{2})",
            cierreId,
            supervisorId,
            motivo);

        await _db.Database.ExecuteSqlRawAsync(
            "SELECT fn_registrar_auditoria(@p_usuario_id,@p_accion,@p_entidad,@p_entidad_id,@p_datos_anteriores,@p_datos_nuevos,@p_direccion_ip,@p_user_agent)",
            AuditoriaParams(supervisorId, "CLOSURE_REJECTED", "cierre_diario", cierreId));

        await tx.CommitAsync(ct);
    }

    // ----------------------------------------------------------------
    // PDF via QuestPDF
    // ----------------------------------------------------------------
    public async Task<byte[]> GeneratePdfAsync(Guid cierreId, CancellationToken ct = default)
    {
        var cierre = await _db.VwCierresResumen
            .Where(x => x.CierreId == cierreId)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Cierre {cierreId} no encontrado.");

        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("CIERRE DIARIO DE INVENTARIO")
                        .Bold().FontSize(16).AlignCenter();
                    col.Item().Text($"Fecha: {cierre.FechaCierre:dd/MM/yyyy}")
                        .AlignCenter();
                    col.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);
                });

                page.Content().Column(col =>
                {
                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Estacion: {cierre.EstacionNombre}").Bold();
                            c.Item().Text($"Tanque: {cierre.TanqueCodigo} — {cierre.TanqueNombre}");
                            c.Item().Text($"Combustible: {cierre.CombustibleNombre}");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Estado: {cierre.Estado}").Bold();
                            c.Item().Text($"Creado por: {cierre.CreadoPor}");
                            if (cierre.RevisadoPor is not null)
                                c.Item().Text($"Revisado por: {cierre.RevisadoPor}");
                        });
                    });

                    col.Item().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                        });

                        void AddRow(string label, decimal value, bool bold = false)
                        {
                            var text1 = table.Cell().Padding(4).Text(label);
                            if (bold) text1.Bold();
                            var text2 = table.Cell().Padding(4).AlignRight().Text(value.ToString("N2"));
                            if (bold) text2.Bold();
                        }

                        AddRow("Stock inicial", cierre.StockInicial);
                        AddRow("+ Recepciones", cierre.TotalRecepciones);
                        AddRow("+ Transferencias entrada", cierre.TotalTransferenciasEntrada);
                        AddRow("- Transferencias salida", cierre.TotalTransferenciasSalida);
                        AddRow("- Despachos", cierre.TotalDespachos);
                        AddRow("+ Ajustes positivos", cierre.TotalAjustesPositivos);
                        AddRow("- Ajustes negativos", cierre.TotalAjustesNegativos);
                        table.Cell().ColumnSpan(2).LineHorizontal(0.5f);
                        AddRow("Stock teorico final", cierre.StockTeoricoFinal, bold: true);
                        AddRow("Stock fisico final", cierre.StockFisicoFinal, bold: true);
                        AddRow("Diferencia", cierre.Diferencia, bold: true);
                    });

                    if (!string.IsNullOrEmpty(cierre.MotivoDiferencia))
                        col.Item().PaddingTop(10).Text($"Motivo de diferencia: {cierre.MotivoDiferencia}");
                    if (!string.IsNullOrEmpty(cierre.MotivoRechazo))
                        col.Item().PaddingTop(5).Text($"Motivo de rechazo: {cierre.MotivoRechazo}");
                    if (!string.IsNullOrEmpty(cierre.Observaciones))
                        col.Item().PaddingTop(5).Text($"Observaciones: {cierre.Observaciones}");
                });

                page.Footer().AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Pagina ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
            });
        }).GeneratePdf();
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------
    private static object[] AuditoriaParams(
        Guid usuarioId, string accion, string entidad, Guid entidadId,
        string? datosNuevos = null)
    {
        return
        [
            new NpgsqlParameter { ParameterName = "@p_usuario_id", NpgsqlDbType = NpgsqlDbType.Uuid, Value = usuarioId },
            new NpgsqlParameter { ParameterName = "@p_accion", NpgsqlDbType = NpgsqlDbType.Varchar, Value = accion },
            new NpgsqlParameter { ParameterName = "@p_entidad", NpgsqlDbType = NpgsqlDbType.Varchar, Value = entidad },
            new NpgsqlParameter { ParameterName = "@p_entidad_id", NpgsqlDbType = NpgsqlDbType.Uuid, Value = entidadId },
            new NpgsqlParameter { ParameterName = "@p_datos_anteriores", NpgsqlDbType = NpgsqlDbType.Jsonb, Value = DBNull.Value },
            new NpgsqlParameter { ParameterName = "@p_datos_nuevos", NpgsqlDbType = NpgsqlDbType.Jsonb, Value = (object?)datosNuevos ?? DBNull.Value },
            new NpgsqlParameter { ParameterName = "@p_direccion_ip", NpgsqlDbType = NpgsqlDbType.Varchar, Value = DBNull.Value },
            new NpgsqlParameter { ParameterName = "@p_user_agent", NpgsqlDbType = NpgsqlDbType.Varchar, Value = DBNull.Value },
        ];
    }

    private static ClosureDto MapToDto(VwCierresResumen x) => new()
    {
        Id = x.CierreId,
        TanqueId = x.TanqueId,
        TanqueCodigo = x.TanqueCodigo,
        TanqueNombre = x.TanqueNombre,
        EstacionId = x.EstacionId,
        EstacionCodigo = x.EstacionCodigo,
        EstacionNombre = x.EstacionNombre,
        CombustibleNombre = x.CombustibleNombre,
        FechaCierre = x.FechaCierre,
        Estado = x.Estado,
        StockInicial = x.StockInicial,
        TotalRecepciones = x.TotalRecepciones,
        TotalTransferenciasEntrada = x.TotalTransferenciasEntrada,
        TotalTransferenciasSalida = x.TotalTransferenciasSalida,
        TotalDespachos = x.TotalDespachos,
        TotalAjustesPositivos = x.TotalAjustesPositivos,
        TotalAjustesNegativos = x.TotalAjustesNegativos,
        StockTeoricoFinal = x.StockTeoricoFinal,
        StockFisicoFinal = x.StockFisicoFinal,
        Diferencia = x.Diferencia,
        CreadoPor = x.CreadoPor,
        RevisadoPor = x.RevisadoPor,
        FechaCreacion = x.FechaCreacion,
        FechaRevision = x.FechaRevision,
        MotivoDiferencia = x.MotivoDiferencia,
        MotivoRechazo = x.MotivoRechazo,
        Observaciones = x.Observaciones,
    };
}


