using CombustibleAPI.Application.Dtos.Dispatches;
using CombustibleAPI.Application.Dtos.Requests;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Domain.Entities;
using CombustibleAPI.Infrastructure.Persistence;
using CombustibleAPI.Infrastructure.Persistence.DbFunctions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CombustibleAPI.Infrastructure.Services;

public sealed class RequestService : IRequestService
{
    private readonly AppDbContext _context;
    private readonly SqlFunctionsRepository _sql;
    private readonly IAuditService _audit;
    private readonly IQrCodeService _qrCodeService;

    public RequestService(
        AppDbContext context,
        SqlFunctionsRepository sql,
        IAuditService audit,
        IQrCodeService qrCodeService)
    {
        _context = context;
        _sql = sql;
        _audit = audit;
        _qrCodeService = qrCodeService;
    }

    public async Task<RequestResponseDto> CrearAsync(
        CreateRequestDto dto,
        Guid usuarioId,
        CancellationToken ct)
    {
        if (dto.CantidadSolicitada <= 0)
            throw ApiException.ValidationError(
                "La cantidad solicitada debe ser mayor a cero.");

        var empleado = await _context.Empleados
            .FirstOrDefaultAsync(x => x.Id == dto.EmpleadoId && x.Activo, ct)
            ?? throw ApiException.NotFound("Empleado");

        var vehiculo = await _context.Vehiculos
            .FirstOrDefaultAsync(x => x.Id == dto.VehiculoId && x.Activo, ct)
            ?? throw ApiException.NotFound("Vehículo");

        if (!await _context.Departamentos.AnyAsync(
                x => x.Id == dto.DepartamentoId && x.Activo, ct))
        {
            throw ApiException.NotFound("Departamento");
        }

        if (empleado.DepartamentoId != dto.DepartamentoId ||
            vehiculo.DepartamentoId != dto.DepartamentoId)
        {
            throw ApiException.BusinessRule(
                "DEPARTAMENTO_INCOHERENTE",
                "Empleado y vehículo deben pertenecer al departamento indicado.");
        }

        if (dto.CantidadSolicitada > vehiculo.CapacidadTanque)
        {
            throw ApiException.BusinessRule(
                "CANTIDAD_SUPERA_CAPACIDAD",
                "La cantidad solicitada supera la capacidad del tanque del vehículo.");
        }

        var solicitud = new Solicitud
        {
            Id = Guid.NewGuid(),
            EmpleadoId = empleado.Id,
            VehiculoId = vehiculo.Id,
            DepartamentoId = dto.DepartamentoId,
            CreadaPorUsuarioId = usuarioId,
            TipoSolicitud = string.IsNullOrWhiteSpace(dto.TipoSolicitud)
                ? "MANUAL"
                : dto.TipoSolicitud.Trim().ToUpperInvariant(),
            CantidadSolicitada = dto.CantidadSolicitada,
            Estado = "PENDIENTE",
            FechaSolicitud = DateTime.UtcNow,
            Observaciones = dto.Observaciones
        };

        _context.Solicitudes.Add(solicitud);
        await _context.SaveChangesAsync(ct);

        await _audit.RegistrarAsync(
            usuarioId,
            "SOLICITUD_CREADA",
            "Solicitud",
            solicitud.Id.ToString(),
            null,
            null,
            new
            {
                solicitud.CantidadSolicitada,
                solicitud.VehiculoId
            },
            ct);

        return await ObtenerInternoAsync(solicitud.Id, ct);
    }

    public async Task<PaginatedList<RequestResponseDto>> ListarAsync(
        RequestFilterDto f,
        Guid usuarioId,
        string rol,
        CancellationToken ct)
    {
        var q = ConsultaDetalle();

        if (string.Equals(
                rol,
                "SOLICITANTE",
                StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(x => x.CreadaPorUsuarioId == usuarioId);
        }
        else if (f.EmpleadoId.HasValue)
        {
            q = q.Where(x => x.EmpleadoId == f.EmpleadoId.Value);
        }

        if (!string.IsNullOrWhiteSpace(f.Estado))
            q = q.Where(x => x.Estado == f.Estado.Trim().ToUpper());

        if (f.VehiculoId.HasValue)
            q = q.Where(x => x.VehiculoId == f.VehiculoId.Value);

        if (f.DepartamentoId.HasValue)
            q = q.Where(x => x.DepartamentoId == f.DepartamentoId.Value);

        if (f.FechaInicio.HasValue)
            q = q.Where(x => x.FechaSolicitud >= f.FechaInicio.Value);

        if (f.FechaFin.HasValue)
            q = q.Where(x => x.FechaSolicitud <= f.FechaFin.Value);

        var total = await q.CountAsync(ct);

        var page = Math.Max(1, f.Page);
        var size = f.PageSize is < 1 or > 100
            ? 20
            : f.PageSize;

        var rows = await q
            .OrderByDescending(x => x.FechaSolicitud)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return new PaginatedList<RequestResponseDto>(
            rows.Select(Map).ToList(),
            total,
            page,
            size);
    }

    public async Task<RequestResponseDto> ObtenerAsync(
        Guid id,
        Guid usuarioId,
        string rol,
        CancellationToken ct)
    {
        var s = await ConsultaDetalle()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiException.NotFound("Solicitud");

        ExigirPropiedad(s, usuarioId, rol);

        return await ObtenerInternoAsync(id, ct);
    }

    public async Task<RequestResponseDto> ActualizarAsync(
        Guid id,
        UpdateRequestDto dto,
        Guid usuarioId,
        string rol,
        CancellationToken ct)
    {
        var s = await _context.Solicitudes
            .Include(x => x.Vehiculo)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiException.NotFound("Solicitud");

        ExigirPropiedad(s, usuarioId, rol);

        if (s.Estado != "PENDIENTE")
        {
            throw ApiException.BusinessRule(
                "SOLICITUD_NO_EDITABLE",
                "Solo una solicitud pendiente puede modificarse.");
        }

        if (dto.CantidadSolicitada.HasValue)
        {
            if (dto.CantidadSolicitada <= 0 ||
                dto.CantidadSolicitada > s.Vehiculo.CapacidadTanque)
            {
                throw ApiException.BusinessRule(
                    "CANTIDAD_INVALIDA",
                    "La cantidad debe ser positiva y no superar la capacidad del vehículo.");
            }

            s.CantidadSolicitada =
                dto.CantidadSolicitada.Value;
        }

        if (dto.Observaciones is not null)
            s.Observaciones = dto.Observaciones;

        await _context.SaveChangesAsync(ct);

        await _audit.RegistrarAsync(
            usuarioId,
            "SOLICITUD_ACTUALIZADA",
            "Solicitud",
            id.ToString(),
            null,
            null,
            dto,
            ct);

        return await ObtenerInternoAsync(id, ct);
    }

    public async Task<RequestResponseDto> AprobarAsync(
        Guid id,
        ApproveRequestDto dto,
        Guid revisorId,
        CancellationToken ct)
    {
        if (dto.CantidadAutorizada <= 0 ||
            dto.FechaExpiracion <= DateTime.UtcNow)
        {
            throw ApiException.ValidationError(
                "La cantidad autorizada debe ser positiva y el vencimiento futuro.");
        }

        if (!_context.Database.IsNpgsql())
        {
            var resultadoMemoria = await AprobarCoreAsync(
                id,
                dto,
                revisorId,
                null,
                null,
                ct);

            await _audit.RegistrarAsync(
                revisorId,
                "SOLICITUD_APROBADA",
                "Solicitud",
                id.ToString(),
                null,
                null,
                new
                {
                    resultadoMemoria.ticketId,
                    resultadoMemoria.numero,
                    dto.EstacionId,
                    dto.CantidadAutorizada
                },
                ct);

            return await ObtenerInternoAsync(id, ct);
        }

        var strategy =
            _context.Database.CreateExecutionStrategy();

        Guid ticketId = Guid.Empty;
        string numero = string.Empty;

        await strategy.ExecuteAsync(async () =>
        {
            var connection =
                (NpgsqlConnection)_context.Database.GetDbConnection();

            var opened =
                connection.State !=
                System.Data.ConnectionState.Open;

            if (opened)
                await connection.OpenAsync(ct);

            try
            {
                await using var tx =
                    await connection.BeginTransactionAsync(ct);

                try
                {
                    await _context.Database
                        .UseTransactionAsync(tx, ct);

                    var resultado =
                        await AprobarCoreAsync(
                            id,
                            dto,
                            revisorId,
                            connection,
                            tx,
                            ct);

                    ticketId = resultado.ticketId;
                    numero = resultado.numero;

                    await tx.CommitAsync(ct);
                }
                catch
                {
                    await tx.RollbackAsync(ct);
                    throw;
                }
                finally
                {
                    await _context.Database
                        .UseTransactionAsync(null, ct);
                }
            }
            finally
            {
                if (opened)
                    await connection.CloseAsync();
            }
        });

        // La aprobación ya fue confirmada y la transacción terminó.
        // AuditService puede administrar su propia transacción.
        await _audit.RegistrarAsync(
            revisorId,
            "SOLICITUD_APROBADA",
            "Solicitud",
            id.ToString(),
            null,
            null,
            new
            {
                ticketId,
                numero,
                dto.EstacionId,
                dto.CantidadAutorizada
            },
            ct);

        return await ObtenerInternoAsync(id, ct);
    }

    private async Task<(Guid ticketId, string numero)>
        AprobarCoreAsync(
            Guid id,
            ApproveRequestDto dto,
            Guid revisorId,
            NpgsqlConnection? connection,
            NpgsqlTransaction? tx,
            CancellationToken ct)
    {
        var s = await _context.Solicitudes
            .Include(x => x.Vehiculo)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiException.NotFound("Solicitud");

        if (s.Estado != "PENDIENTE")
        {
            throw ApiException.Conflict(
                "SOLICITUD_NO_PENDIENTE",
                "La solicitud ya fue revisada.");
        }

        if (await _context.Tickets
            .AnyAsync(x => x.SolicitudId == id, ct))
        {
            throw ApiException.Conflict(
                "TICKET_YA_EXISTE",
                "Ya existe un ticket para esta solicitud.");
        }

        if (dto.CantidadAutorizada > s.CantidadSolicitada ||
            dto.CantidadAutorizada >
            s.Vehiculo.CapacidadTanque)
        {
            throw ApiException.BusinessRule(
                "CANTIDAD_AUTORIZADA_INVALIDA",
                "La cantidad autorizada no puede exceder la solicitada ni la capacidad del vehículo.");
        }

        if (!await _context.Estaciones.AnyAsync(
                x => x.Id == dto.EstacionId && x.Activo,
                ct))
        {
            throw ApiException.NotFound("Estación");
        }

        var tipo = s.Vehiculo.TipoCombustibleId;

        decimal disponible;

        if (connection is not null)
        {
            disponible =
                (await _sql.ObtenerStockDisponibleAsync(
                    connection,
                    tx,
                    dto.EstacionId,
                    tipo,
                    ct)).stockDisponible;
        }
        else
        {
            var fisico = await _context.Tanques
                .Where(x =>
                    x.EstacionId == dto.EstacionId &&
                    x.TipoCombustibleId == tipo &&
                    x.Activo)
                .SumAsync(
                    x => (decimal?)x.StockActual,
                    ct) ?? 0m;

            var reservado = await _context.Tickets
                .Where(x =>
                    x.EstacionId == dto.EstacionId &&
                    x.TipoCombustibleId == tipo &&
                    (x.Estado == "CREADO" ||
                     x.Estado == "ENVIADO") &&
                    x.FechaExpiracion > DateTime.UtcNow)
                .SumAsync(
                    x => (decimal?)x.CantidadAutorizada,
                    ct) ?? 0m;

            disponible = fisico - reservado;
        }

        if (disponible < dto.CantidadAutorizada)
            throw ApiException.InventarioInsuficiente();

        var ticketId = Guid.NewGuid();

        // Genera los datos criptográficos del QR.
        // El token en claro NO se persiste en la base de datos.
        var qrSecurity =
            _qrCodeService.GenerateQrSecurityData(ticketId);

        var numero = connection is null
            ? $"COM-{DateTime.UtcNow.Year}-{(await _context.Tickets.CountAsync(ct) + 1):D6}"
            : await _sql.GenerarNumeroTicketAsync(
                connection,
                tx!,
                ct);

        _context.Tickets.Add(new Ticket
        {
            Id = ticketId,
            SolicitudId = id,
            EstacionId = dto.EstacionId,
            TipoCombustibleId = tipo,
            NumeroTicket = numero,
            CantidadAutorizada =
                dto.CantidadAutorizada,

            Estado = "CREADO",

            // Solo persistimos el hash del token y la firma HMAC.
            TokenQrHash = qrSecurity.TokenHash,
            FirmaQr = qrSecurity.Signature,

            FechaEmision = DateTime.UtcNow,
            FechaExpiracion = dto.FechaExpiracion
        });

        s.Estado = "APROBADA";
        s.CantidadAutorizada =
            dto.CantidadAutorizada;
        s.RevisadaPorUsuarioId = revisorId;
        s.FechaRevision = DateTime.UtcNow;
        s.FechaExpiracion = dto.FechaExpiracion;

        if (!string.IsNullOrWhiteSpace(dto.Observaciones))
            s.Observaciones = dto.Observaciones;

        await _context.SaveChangesAsync(ct);

        return (ticketId, numero);
    }

    public async Task<RequestResponseDto> RechazarAsync(
        Guid id,
        RejectRequestDto dto,
        Guid revisorId,
        CancellationToken ct)
    {
        var s = await _context.Solicitudes
            .FindAsync([id], ct)
            ?? throw ApiException.NotFound("Solicitud");

        if (s.Estado != "PENDIENTE")
        {
            throw ApiException.Conflict(
                "SOLICITUD_NO_PENDIENTE",
                "La solicitud ya fue revisada.");
        }

        s.Estado = "RECHAZADA";
        s.MotivoRechazo = dto.Motivo;
        s.RevisadaPorUsuarioId = revisorId;
        s.FechaRevision = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        await _audit.RegistrarAsync(
            revisorId,
            "SOLICITUD_RECHAZADA",
            "Solicitud",
            id.ToString(),
            null,
            null,
            dto,
            ct);

        return await ObtenerInternoAsync(id, ct);
    }

    public async Task<RequestResponseDto> CancelarAsync(
        Guid id,
        CancelRequestDto? dto,
        Guid usuarioId,
        string rol,
        CancellationToken ct)
    {
        var s = await _context.Solicitudes
            .FindAsync([id], ct)
            ?? throw ApiException.NotFound("Solicitud");

        ExigirPropiedad(s, usuarioId, rol);

        if (s.Estado != "PENDIENTE")
        {
            throw ApiException.BusinessRule(
                "SOLICITUD_NO_CANCELABLE",
                "Solo una solicitud pendiente puede cancelarse.");
        }

        s.Estado = "CANCELADA";
        s.MotivoCancelacion = dto?.Motivo;

        await _context.SaveChangesAsync(ct);

        await _audit.RegistrarAsync(
            usuarioId,
            "SOLICITUD_CANCELADA",
            "Solicitud",
            id.ToString(),
            null,
            null,
            dto,
            ct);

        return await ObtenerInternoAsync(id, ct);
    }

    private IQueryable<Solicitud> ConsultaDetalle()
    {
        return _context.Solicitudes
            .Include(x => x.Empleado)
            .Include(x => x.Vehiculo)
                .ThenInclude(x => x.TipoCombustible)
            .Include(x => x.Departamento)
            .Include(x => x.RevisadaPorUsuario)
            .Include(x => x.CreadaPorUsuario)
            .AsNoTracking();
    }

    private async Task<RequestResponseDto>
        ObtenerInternoAsync(
            Guid id,
            CancellationToken ct)
    {
        var solicitud = await ConsultaDetalle()
            .FirstAsync(x => x.Id == id, ct);

        var response = Map(solicitud);

        var ticket = await _context.Tickets
            .AsNoTracking()
            .Where(x => x.SolicitudId == id)
            .Select(x => new
            {
                x.Id,
                x.NumeroTicket
            })
            .FirstOrDefaultAsync(ct);

        if (ticket is not null)
        {
            response.TicketId = ticket.Id;
            response.NumeroTicket =
                ticket.NumeroTicket;
        }

        return response;
    }

    private static void ExigirPropiedad(
        Solicitud s,
        Guid usuarioId,
        string rol)
    {
        if (string.Equals(
                rol,
                "SOLICITANTE",
                StringComparison.OrdinalIgnoreCase) &&
            s.CreadaPorUsuarioId != usuarioId)
        {
            throw ApiException.Forbidden(
                "Un solicitante solo puede acceder a sus propias solicitudes.");
        }
    }

    private static RequestResponseDto Map(
        Solicitud s)
    {
        var t = s.Vehiculo.TipoCombustible;

        return new RequestResponseDto
        {
            Id = s.Id,
            EmpleadoId = s.EmpleadoId,
            EmpleadoNombre =
                s.Empleado.NombreCompleto,

            VehiculoId = s.VehiculoId,
            VehiculoPlaca = s.Vehiculo.Placa,
            VehiculoFicha = s.Vehiculo.Ficha,

            DepartamentoId = s.DepartamentoId,
            DepartamentoNombre =
                s.Departamento.Nombre,

            TipoCombustibleId =
                s.Vehiculo.TipoCombustibleId,
            TipoCombustible = t?.Nombre,

            CantidadSolicitada =
                s.CantidadSolicitada,
            CantidadAutorizada =
                s.CantidadAutorizada,

            Estado = s.Estado,
            TipoSolicitud = s.TipoSolicitud,
            FechaSolicitud = s.FechaSolicitud,
            FechaRevision = s.FechaRevision,

            MotivoRechazo = s.MotivoRechazo,
            MotivoCancelacion =
                s.MotivoCancelacion,
            Observaciones = s.Observaciones
        };
    }
}