using CombustibleAPI.Application.Dtos.Closures;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Domain.Entities;
using CombustibleAPI.Infrastructure.Persistence;
using CombustibleAPI.Infrastructure.Persistence.DbFunctions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CombustibleAPI.Infrastructure.Services;

public class ClosureService : IClosureService
{
    private readonly AppDbContext _context;
    private readonly SqlFunctionsRepository _sqlFunctions;
    private readonly IAuditService _auditService;

    public ClosureService(AppDbContext context, SqlFunctionsRepository sqlFunctions, IAuditService auditService)
    {
        _context = context;
        _sqlFunctions = sqlFunctions;
        _auditService = auditService;
    }

    public async Task<ClosurePreviewDto> GetPreviewAsync(
    Guid tanqueId,
    DateOnly fecha,
    CancellationToken ct)
    {
        var tanque = await _context.Tanques
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tanqueId && t.Activo, ct);

        if (tanque is null)
            throw ApiException.NotFound("Tanque");

        var fechaInicio = DateTime.SpecifyKind(
            fecha.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);

        var fechaFin = DateTime.SpecifyKind(
            fecha.AddDays(1).ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);

        var movimientos = await _context.MovimientosInventario
            .AsNoTracking()
            .Where(m =>
            m.TanqueId == tanqueId &&
            m.FechaMovimiento >= fechaInicio &&
            m.FechaMovimiento < fechaFin)
            .OrderBy(m => m.FechaMovimiento)
            .ToListAsync(ct);

        var stockInicial =
            movimientos.FirstOrDefault()?.SaldoAnterior
            ?? tanque.StockActual;

        var recepciones = movimientos
            .Where(m => m.TipoMovimiento == "RECEPCION")
            .Sum(m => m.Cantidad);

        var transferenciasEntrada = movimientos
            .Where(m => m.TipoMovimiento == "TRANSFERENCIA_ENTRADA")
            .Sum(m => m.Cantidad);

        var transferenciasSalida = movimientos
            .Where(m => m.TipoMovimiento == "TRANSFERENCIA_SALIDA")
            .Sum(m => m.Cantidad);

        var despachos = movimientos
            .Where(m => m.TipoMovimiento == "DESPACHO")
            .Sum(m => m.Cantidad);

        var ajustesPositivos = movimientos
            .Where(m => m.TipoMovimiento == "AJUSTE_POSITIVO")
            .Sum(m => m.Cantidad);

        var ajustesNegativos = movimientos
            .Where(m => m.TipoMovimiento == "AJUSTE_NEGATIVO")
            .Sum(m => m.Cantidad);

        var stockTeorico =
            stockInicial
            + recepciones
            + transferenciasEntrada
            - transferenciasSalida
            - despachos
            + ajustesPositivos
            - ajustesNegativos;

        return new ClosurePreviewDto
        {
            TanqueId = tanqueId,
            Fecha = fecha,
            StockInicial = stockInicial,
            TotalRecepciones = recepciones,
            TotalTransferenciasEntrada = transferenciasEntrada,
            TotalTransferenciasSalida = transferenciasSalida,
            TotalDespachos = despachos,
            TotalAjustesPositivos = ajustesPositivos,
            TotalAjustesNegativos = ajustesNegativos,
            StockTeoricoFinal = stockTeorico
        };
    }

    public async Task<ClosurePagedResponseDto> GetClosuresAsync(
        ClosureFilterDto filter,
        CancellationToken ct)
    {
        var query = _context.CierresDiarios
            .Include(c => c.Tanque)
                .ThenInclude(t => t.Estacion)
            .Include(c => c.CreadoPorUsuario)
            .Include(c => c.RevisadoPorUsuario)
            .AsNoTracking()
            .AsQueryable();

        if (filter.FechaDesde.HasValue)
            query = query.Where(c =>
                c.FechaCierre >= filter.FechaDesde.Value);

        if (filter.FechaHasta.HasValue)
            query = query.Where(c =>
                c.FechaCierre <= filter.FechaHasta.Value);

        if (filter.TanqueId.HasValue)
            query = query.Where(c =>
                c.TanqueId == filter.TanqueId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Estado))
        {
            var estado = filter.Estado.Trim().ToUpperInvariant();

            query = query.Where(c =>
                c.Estado == estado);
        }

        var total = await query.CountAsync(ct);

        var cierres = await query
            .OrderByDescending(c => c.FechaCierre)
            .ThenByDescending(c => c.FechaCreacion)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return new ClosurePagedResponseDto
        {
            Items = cierres.Select(MapToResponseDto).ToList(),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public Task<ClosureResponseDto> GetClosureByIdAsync(
        Guid closureId,
        CancellationToken ct)
    {
        return ObtenerDetalleCierreAsync(closureId, ct);
    }

    public async Task<ClosureResponseDto> CreateDailyClosureAsync(CreateDailyClosureRequestDto request, Guid usuarioId, CancellationToken ct)
    {
        var tanque = await _context.Tanques
            .Include(t => t.Estacion)
            .FirstOrDefaultAsync(t => t.Id == request.TanqueId && t.Activo, ct);

        if (tanque is null)
            throw ApiException.NotFound("Tanque");

        Guid cierreId;

        if (_context.Database.IsNpgsql())
        {
            try
            {
                var connection = (NpgsqlConnection)_context.Database.GetDbConnection();
                var abrio = connection.State != System.Data.ConnectionState.Open;
                if (abrio) await connection.OpenAsync(ct);

                try
                {
                    cierreId = await _sqlFunctions.CrearCierreDiarioAsync(
                        connection, null,
                        request.TanqueId, usuarioId, request.Fecha, request.StockFisicoFinal,
                        request.MotivoDiferencia, request.Observaciones, ct);
                }
                finally
                {
                    if (abrio) await connection.CloseAsync();
                }
            }
            catch (ApiException)
            {
                throw;
            }
            catch
            {
                cierreId = await CrearCierreViaEfAsync(request, usuarioId, tanque, ct);
            }
        }
        else
        {
            cierreId = await CrearCierreViaEfAsync(request, usuarioId, tanque, ct);
        }

        await _auditService.RegistrarAsync(usuarioId, "CIERRE_DIARIO_CREADO", "CierreDiario", cierreId.ToString(), null, null, request, ct);

        return await ObtenerDetalleCierreAsync(cierreId, ct);
    }

    public async Task ApproveClosureAsync(Guid closureId, Guid supervisorId, CancellationToken ct)
    {
        var cierre = await _context.CierresDiarios.FirstOrDefaultAsync(c => c.Id == closureId, ct);
        if (cierre is null)
            throw ApiException.NotFound("Cierre diario");

        if (cierre.Estado != "PENDIENTE_APROBACION")
            throw ApiException.Conflict("El cierre ya fue revisado.");

        if (_context.Database.IsNpgsql())
        {
            try
            {
                var connection = (NpgsqlConnection)_context.Database.GetDbConnection();
                var abrio = connection.State != System.Data.ConnectionState.Open;
                if (abrio) await connection.OpenAsync(ct);

                try
                {
                    await _sqlFunctions.AprobarCierreAsync(connection, null, closureId, supervisorId, ct);
                }
                finally
                {
                    if (abrio) await connection.CloseAsync();
                }
            }
            catch (ApiException)
            {
                throw;
            }
            catch
            {
                cierre.Estado = "APROBADO";
                cierre.RevisadoPorUsuarioId = supervisorId;
                cierre.FechaRevision = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
            }
        }
        else
        {
            cierre.Estado = "APROBADO";
            cierre.RevisadoPorUsuarioId = supervisorId;
            cierre.FechaRevision = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }

        await _auditService.RegistrarAsync(supervisorId, "CIERRE_DIARIO_APROBADO", "CierreDiario", closureId.ToString(), null, null, null, ct);
    }

    public async Task RejectClosureAsync(Guid closureId, Guid supervisorId, string motivo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw ApiException.ValidationError("El motivo de rechazo es requerido.");

        var cierre = await _context.CierresDiarios.FirstOrDefaultAsync(c => c.Id == closureId, ct);
        if (cierre is null)
            throw ApiException.NotFound("Cierre diario");

        if (cierre.Estado != "PENDIENTE_APROBACION")
            throw ApiException.Conflict("El cierre ya fue revisado.");

        if (_context.Database.IsNpgsql())
        {
            try
            {
                var connection = (NpgsqlConnection)_context.Database.GetDbConnection();
                var abrio = connection.State != System.Data.ConnectionState.Open;
                if (abrio) await connection.OpenAsync(ct);

                try
                {
                    await _sqlFunctions.RechazarCierreAsync(connection, null, closureId, supervisorId, motivo, ct);
                }
                finally
                {
                    if (abrio) await connection.CloseAsync();
                }
            }
            catch (ApiException)
            {
                throw;
            }
            catch
            {
                cierre.Estado = "RECHAZADO";
                cierre.MotivoRechazo = motivo;
                cierre.RevisadoPorUsuarioId = supervisorId;
                cierre.FechaRevision = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
            }
        }
        else
        {
            cierre.Estado = "RECHAZADO";
            cierre.MotivoRechazo = motivo;
            cierre.RevisadoPorUsuarioId = supervisorId;
            cierre.FechaRevision = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }

        await _auditService.RegistrarAsync(supervisorId, "CIERRE_DIARIO_RECHAZADO", "CierreDiario", closureId.ToString(), null, null, new { motivo }, ct);
    }

    private async Task<Guid> CrearCierreViaEfAsync(CreateDailyClosureRequestDto request, Guid usuarioId, Tanque tanque, CancellationToken ct)
    {
                var fechaInicio = DateTime.SpecifyKind(
              request.Fecha.ToDateTime(TimeOnly.MinValue),
              DateTimeKind.Utc);

                var fechaFin = DateTime.SpecifyKind(
                    request.Fecha.AddDays(1).ToDateTime(TimeOnly.MinValue),
                    DateTimeKind.Utc);

        var movimientosDia = await _context.MovimientosInventario
            .Where(m =>
                m.TanqueId == request.TanqueId &&
                m.FechaMovimiento >= fechaInicio &&
                m.FechaMovimiento < fechaFin)
            .OrderBy(m => m.FechaMovimiento)
            .ToListAsync(ct);

        decimal stockInicial = movimientosDia.FirstOrDefault()?.SaldoAnterior ?? tanque.StockActual;
        decimal totalRecepciones = movimientosDia.Where(m => m.TipoMovimiento == "RECEPCION").Sum(m => m.Cantidad);
        decimal totalTransferenciasEntrada = movimientosDia.Where(m => m.TipoMovimiento == "TRANSFERENCIA_ENTRADA").Sum(m => m.Cantidad);
        decimal totalTransferenciasSalida = movimientosDia.Where(m => m.TipoMovimiento == "TRANSFERENCIA_SALIDA").Sum(m => m.Cantidad);
        decimal totalDespachos = movimientosDia.Where(m => m.TipoMovimiento == "DESPACHO").Sum(m => m.Cantidad);
        decimal totalAjustesPositivos = movimientosDia.Where(m => m.TipoMovimiento == "AJUSTE_POSITIVO").Sum(m => m.Cantidad);
        decimal totalAjustesNegativos = movimientosDia.Where(m => m.TipoMovimiento == "AJUSTE_NEGATIVO").Sum(m => m.Cantidad);

        decimal stockTeorico = stockInicial + totalRecepciones + totalTransferenciasEntrada - totalTransferenciasSalida - totalDespachos + totalAjustesPositivos - totalAjustesNegativos;
        decimal diferencia = request.StockFisicoFinal - stockTeorico;

        if (diferencia != 0 && string.IsNullOrWhiteSpace(request.MotivoDiferencia))
            throw ApiException.ValidationError("Se requiere motivo para la diferencia en el cierre.");

        var entity = new CierreDiario
        {
            Id = Guid.NewGuid(),
            TanqueId = request.TanqueId,
            CreadoPorUsuarioId = usuarioId,
            FechaCierre = request.Fecha,
            StockInicial = stockInicial,
            TotalRecepciones = totalRecepciones,
            TotalTransferenciasEntrada = totalTransferenciasEntrada,
            TotalTransferenciasSalida = totalTransferenciasSalida,
            TotalDespachos = totalDespachos,
            TotalAjustesPositivos = totalAjustesPositivos,
            TotalAjustesNegativos = totalAjustesNegativos,
            StockTeoricoFinal = stockTeorico,
            StockFisicoFinal = request.StockFisicoFinal,
            Diferencia = diferencia,
            Estado = "PENDIENTE_APROBACION",
            FechaCreacion = DateTime.UtcNow,
            MotivoDiferencia = request.MotivoDiferencia,
            Observaciones = request.Observaciones
        };

        _context.CierresDiarios.Add(entity);
        await _context.SaveChangesAsync(ct);
        return entity.Id;
    }

    private async Task<ClosureResponseDto> ObtenerDetalleCierreAsync(Guid id, CancellationToken ct)
    {
        var c = await _context.CierresDiarios
            .Include(x => x.Tanque).ThenInclude(t => t.Estacion)
            .Include(x => x.CreadoPorUsuario)
            .Include(x => x.RevisadoPorUsuario)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (c is null)
            throw ApiException.NotFound("Cierre diario");

        return new ClosureResponseDto
        {
            Id = c.Id,
            TanqueId = c.TanqueId,
            TanqueCodigo = c.Tanque?.Codigo,
            TanqueNombre = c.Tanque?.Nombre,
            EstacionId = c.Tanque?.EstacionId,
            EstacionNombre = c.Tanque?.Estacion?.Nombre,
            FechaCierre = c.FechaCierre,
            StockInicial = c.StockInicial,
            TotalRecepciones = c.TotalRecepciones,
            TotalTransferenciasEntrada = c.TotalTransferenciasEntrada,
            TotalTransferenciasSalida = c.TotalTransferenciasSalida,
            TotalDespachos = c.TotalDespachos,
            TotalAjustesPositivos = c.TotalAjustesPositivos,
            TotalAjustesNegativos = c.TotalAjustesNegativos,
            StockTeoricoFinal = c.StockTeoricoFinal,
            StockFisicoFinal = c.StockFisicoFinal,
            Diferencia = c.Diferencia,
            Estado = c.Estado,
            CreadoPorUsuarioId = c.CreadoPorUsuarioId,
            CreadoPor = c.CreadoPorUsuario?.NombreUsuario,
            RevisadoPorUsuarioId = c.RevisadoPorUsuarioId,
            RevisadoPor = c.RevisadoPorUsuario?.NombreUsuario,
            FechaCreacion = c.FechaCreacion,
            FechaRevision = c.FechaRevision,
            MotivoDiferencia = c.MotivoDiferencia,
            MotivoRechazo = c.MotivoRechazo,
            Observaciones = c.Observaciones
        };
    }
    private static ClosureResponseDto MapToResponseDto(CierreDiario c)
    {
        return new ClosureResponseDto
        {
            Id = c.Id,
            TanqueId = c.TanqueId,
            TanqueCodigo = c.Tanque?.Codigo,
            TanqueNombre = c.Tanque?.Nombre,
            EstacionId = c.Tanque?.EstacionId,
            EstacionNombre = c.Tanque?.Estacion?.Nombre,
            FechaCierre = c.FechaCierre,
            StockInicial = c.StockInicial,
            TotalRecepciones = c.TotalRecepciones,
            TotalTransferenciasEntrada = c.TotalTransferenciasEntrada,
            TotalTransferenciasSalida = c.TotalTransferenciasSalida,
            TotalDespachos = c.TotalDespachos,
            TotalAjustesPositivos = c.TotalAjustesPositivos,
            TotalAjustesNegativos = c.TotalAjustesNegativos,
            StockTeoricoFinal = c.StockTeoricoFinal,
            StockFisicoFinal = c.StockFisicoFinal,
            Diferencia = c.Diferencia,
            Estado = c.Estado,
            CreadoPorUsuarioId = c.CreadoPorUsuarioId,
            CreadoPor = c.CreadoPorUsuario?.NombreUsuario,
            RevisadoPorUsuarioId = c.RevisadoPorUsuarioId,
            RevisadoPor = c.RevisadoPorUsuario?.NombreUsuario,
            FechaCreacion = c.FechaCreacion,
            FechaRevision = c.FechaRevision,
            MotivoDiferencia = c.MotivoDiferencia,
            MotivoRechazo = c.MotivoRechazo,
            Observaciones = c.Observaciones
        };
    }
}
