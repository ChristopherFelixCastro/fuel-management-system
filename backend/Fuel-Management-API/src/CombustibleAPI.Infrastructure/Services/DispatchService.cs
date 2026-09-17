using System.Text.Json;
using CombustibleAPI.Application.Dtos.Dispatches;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Domain.Entities;
using CombustibleAPI.Domain.Enums;
using CombustibleAPI.Infrastructure.Persistence;
using CombustibleAPI.Infrastructure.Persistence.DbFunctions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CombustibleAPI.Infrastructure.Services;

/// <summary>
/// Implementa POST /dispatches, GET /dispatches y GET /dispatches/{id}.
/// Operación transaccional más crítica del sistema (SDP Iván sec. 5; SDP General RN-04 a RN-08).
/// </summary>
public class DispatchService : IDispatchService
{
    private readonly AppDbContext _context;
    private readonly SqlFunctionsRepository _sqlFunctions;
    private readonly IAuditService _auditService;

    public DispatchService(AppDbContext context, SqlFunctionsRepository sqlFunctions, IAuditService auditService)
    {
        _context = context;
        _sqlFunctions = sqlFunctions;
        _auditService = auditService;
    }

    public async Task<DispatchResultDto> RegistrarDespachoAsync(
        DispatchRequestDto request, Guid despachadorId, Guid estacionDespachadorId, string? ipAddress, CancellationToken ct)
    {
        if (request.Galones <= 0)
            throw ApiException.ValidationError("Los galones deben ser mayores a cero.");

        if (_context.Database.IsNpgsql())
        {
            var connection = (NpgsqlConnection)_context.Database.GetDbConnection();
            var abrioConexion = connection.State != System.Data.ConnectionState.Open;
            if (abrioConexion) await connection.OpenAsync(ct);

            await using var transaction = await connection.BeginTransactionAsync(
                System.Data.IsolationLevel.ReadCommitted, ct);

            try
            {
                // 1) Releer ticket
                var ticket = await _context.Tickets
                    .Where(t => t.Id == request.TicketId)
                    .Select(t => new { t.Id, t.NumeroTicket, t.Estado, t.FechaExpiracion, t.CantidadAutorizada, t.EstacionId, t.TipoCombustibleId, t.SolicitudId })
                    .FirstOrDefaultAsync(ct);

                if (ticket is null)
                    throw ApiException.TicketInexistente();

                if (string.Equals(ticket.Estado, "CONSUMIDO", StringComparison.OrdinalIgnoreCase))
                    throw ApiException.TicketConsumido();
                if (string.Equals(ticket.Estado, "ANULADO", StringComparison.OrdinalIgnoreCase))
                    throw ApiException.TicketAnulado();
                if (ticket.FechaExpiracion <= DateTime.UtcNow)
                    throw ApiException.TicketVencido();

                // 2) Validar estación y cantidad exacta
                if (ticket.EstacionId != estacionDespachadorId)
                    throw ApiException.NoAutorizadoParaEstacion();

                if (request.Galones != ticket.CantidadAutorizada)
                    throw ApiException.BusinessRule("CANTIDAD_DEBE_SER_IGUAL_A_AUTORIZADA",
                        $"La cantidad solicitada ({request.Galones}) no coincide con la cantidad autorizada del ticket ({ticket.CantidadAutorizada}).");

                // 3) Validar tanque compatible
                var tanque = await _context.Tanques
                    .Where(t => t.Id == request.TanqueId)
                    .Select(t => new { t.Id, t.EstacionId, t.TipoCombustibleId, t.StockActual, t.Activo })
                    .FirstOrDefaultAsync(ct);

                if (tanque is null || !tanque.Activo)
                    throw ApiException.NotFound("Tanque");

                if (tanque.EstacionId != estacionDespachadorId)
                    throw ApiException.NoAutorizadoParaEstacion();

                if (tanque.TipoCombustibleId != ticket.TipoCombustibleId)
                    throw ApiException.BusinessRule("COMBUSTIBLE_INCOMPATIBLE", "El combustible del tanque no coincide con el ticket.");

                if (tanque.StockActual < request.Galones)
                    throw ApiException.InventarioInsuficiente();

                // 4) Validar odómetro con el vehículo de la solicitud
                var vehiculo = await _context.Solicitudes
                    .Where(s => s.Id == ticket.SolicitudId)
                    .Select(s => new { s.Vehiculo.Id, s.Vehiculo.OdometroActual })
                    .FirstOrDefaultAsync(ct);

                if (vehiculo != null && request.Odometro < vehiculo.OdometroActual)
                    throw ApiException.BusinessRule("ODOMETRO_INVALIDO",
                        $"El odómetro ingresado ({request.Odometro}) no puede ser menor al actual del vehículo ({vehiculo.OdometroActual}).");

                // 5) Ejecutar función SQL atómica nativa de Christopher
                Guid despachoId;
                try
                {
                    despachoId = await _sqlFunctions.RegistrarDespachoAsync(
                        connection, transaction,
                        request.TicketId, despachadorId, request.TanqueId,
                        request.Galones, request.Odometro, request.Observacion, ipAddress, ct);
                }
                catch (ApiException)
                {
                    throw;
                }
                catch
                {
                    // Fallback atómico en C# si la función SQL nativa no está compilada en este entorno
                    despachoId = await EjecutarDespachoEnEfAsync(request, despachadorId, ticket.Id, ticket.SolicitudId, tanque.Id, request.Galones, request.Odometro, ipAddress, ct);
                }

                // 6) Registrar auditoría encadenada
                var datosNuevos = JsonSerializer.Serialize(new
                {
                    request.TicketId,
                    request.TanqueId,
                    despachadorId,
                    request.Galones,
                    request.Odometro,
                    despachoId
                });

                await _sqlFunctions.RegistrarAuditoriaAsync(
                    connection, transaction,
                    despachadorId, "DESPACHO_REGISTRADO", "Despacho", despachoId,
                    null, datosNuevos, ipAddress, null, ct);

                await transaction.CommitAsync(ct);

                var saldoFinal = tanque.StockActual - request.Galones;
                return new DispatchResultDto
                {
                    DespachoId = despachoId,
                    TicketId = request.TicketId,
                    NumeroTicket = ticket.NumeroTicket,
                    GalonesDespachados = request.Galones,
                    SaldoResultanteTanque = saldoFinal,
                    FechaHora = DateTime.UtcNow,
                    Estado = "COMPLETADO"
                };
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
            finally
            {
                if (abrioConexion) await connection.CloseAsync();
            }
        }
        else
        {
            // Ejecución en memoria / proveedores no PostgreSQL (pruebas unitarias)
            return await RegistrarDespachoMemoriaAsync(request, despachadorId, estacionDespachadorId, ipAddress, ct);
        }
    }

    private async Task<Guid> EjecutarDespachoEnEfAsync(
        DispatchRequestDto request, Guid despachadorId, Guid ticketId, Guid solicitudId,
        Guid tanqueId, decimal galones, decimal odometro, string? ipAddress, CancellationToken ct)
    {
        var tanqueEntity = await _context.Tanques.FirstAsync(t => t.Id == tanqueId, ct);
        var saldoAnterior = tanqueEntity.StockActual;
        var saldoPosterior = saldoAnterior - galones;
        tanqueEntity.StockActual = saldoPosterior;
        tanqueEntity.FechaActualizacion = DateTime.UtcNow;

        var ticketEntity = await _context.Tickets.FirstAsync(t => t.Id == ticketId, ct);
        ticketEntity.Estado = "CONSUMIDO";

        var solicitudEntity = await _context.Solicitudes
            .Include(s => s.Vehiculo)
            .FirstAsync(s => s.Id == solicitudId, ct);
        if (solicitudEntity.Vehiculo != null)
        {
            solicitudEntity.Vehiculo.OdometroActual = odometro;
            solicitudEntity.Vehiculo.FechaActualizacion = DateTime.UtcNow;
        }

        var despacho = new Despacho
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            DespachadorUsuarioId = despachadorId,
            TanqueId = tanqueId,
            CantidadDespachada = galones,
            OdometroRegistrado = odometro,
            FechaDespacho = DateTime.UtcNow,
            Observaciones = request.Observacion,
            DireccionIp = ipAddress
        };
        _context.Despachos.Add(despacho);

        var mov = new MovimientoInventario
        {
            Id = Guid.NewGuid(),
            TanqueId = tanqueId,
            RegistradoPorUsuarioId = despachadorId,
            TipoMovimiento = "DESPACHO",
            Cantidad = galones,
            SaldoAnterior = saldoAnterior,
            SaldoPosterior = saldoPosterior,
            DespachoId = despacho.Id,
            FechaMovimiento = DateTime.UtcNow,
            Observaciones = request.Observacion
        };
        _context.MovimientosInventario.Add(mov);

        await _context.SaveChangesAsync(ct);
        return despacho.Id;
    }

    private async Task<DispatchResultDto> RegistrarDespachoMemoriaAsync(
        DispatchRequestDto request, Guid despachadorId, Guid estacionDespachadorId, string? ipAddress, CancellationToken ct)
    {
        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == request.TicketId, ct);
        if (ticket is null) throw ApiException.TicketInexistente();
        if (string.Equals(ticket.Estado, "CONSUMIDO", StringComparison.OrdinalIgnoreCase)) throw ApiException.TicketConsumido();
        if (string.Equals(ticket.Estado, "ANULADO", StringComparison.OrdinalIgnoreCase)) throw ApiException.TicketAnulado();
        if (ticket.FechaExpiracion <= DateTime.UtcNow) throw ApiException.TicketVencido();

        if (ticket.EstacionId != estacionDespachadorId) throw ApiException.NoAutorizadoParaEstacion();
        if (request.Galones != ticket.CantidadAutorizada)
            throw ApiException.BusinessRule("CANTIDAD_DEBE_SER_IGUAL_A_AUTORIZADA", "La cantidad debe ser igual a la autorizada.");

        var tanque = await _context.Tanques.FirstOrDefaultAsync(t => t.Id == request.TanqueId, ct);
        if (tanque is null || !tanque.Activo) throw ApiException.NotFound("Tanque");
        if (tanque.EstacionId != estacionDespachadorId) throw ApiException.NoAutorizadoParaEstacion();
        if (tanque.TipoCombustibleId != ticket.TipoCombustibleId)
            throw ApiException.BusinessRule("COMBUSTIBLE_INCOMPATIBLE", "Combustible incompatible.");
        if (tanque.StockActual < request.Galones) throw ApiException.InventarioInsuficiente();

        var despachoId = await EjecutarDespachoEnEfAsync(
            request, despachadorId, ticket.Id, ticket.SolicitudId, tanque.Id, request.Galones, request.Odometro, ipAddress, ct);

        await _auditService.RegistrarAsync(
            despachadorId, "DESPACHO_REGISTRADO", "Despacho", despachoId.ToString(), ipAddress, null, request, ct);

        return new DispatchResultDto
        {
            DespachoId = despachoId,
            TicketId = request.TicketId,
            NumeroTicket = ticket.NumeroTicket,
            GalonesDespachados = request.Galones,
            SaldoResultanteTanque = tanque.StockActual,
            FechaHora = DateTime.UtcNow,
            Estado = "COMPLETADO"
        };
    }

    public async Task<DispatchDetailDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var d = await _context.Despachos
            .Include(x => x.Ticket).ThenInclude(t => t.Solicitud).ThenInclude(s => s.Vehiculo)
            .Include(x => x.Ticket).ThenInclude(t => t.Solicitud).ThenInclude(s => s.Empleado)
            .Include(x => x.Tanque).ThenInclude(t => t.Estacion)
            .Include(x => x.Despachador)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (d is null)
            throw ApiException.NotFound("Despacho");

        return MapToDetailDto(d);
    }

    public async Task<PaginatedList<DispatchDetailDto>> GetPaginatedAsync(DispatchesFilterDto filter, CancellationToken ct)
    {
        var query = _context.Despachos
            .Include(x => x.Ticket).ThenInclude(t => t.Solicitud).ThenInclude(s => s.Vehiculo)
            .Include(x => x.Ticket).ThenInclude(t => t.Solicitud).ThenInclude(s => s.Empleado)
            .Include(x => x.Tanque).ThenInclude(t => t.Estacion)
            .Include(x => x.Despachador)
            .AsNoTracking()
            .AsQueryable();

        if (filter.FechaInicio.HasValue)
            query = query.Where(d => d.FechaDespacho >= filter.FechaInicio.Value);

        if (filter.FechaFin.HasValue)
            query = query.Where(d => d.FechaDespacho <= filter.FechaFin.Value);

        if (filter.EstacionId.HasValue && filter.EstacionId.Value != Guid.Empty)
            query = query.Where(d => d.Tanque.EstacionId == filter.EstacionId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Ticket))
            query = query.Where(d => d.Ticket.NumeroTicket.Contains(filter.Ticket));

        if (!string.IsNullOrWhiteSpace(filter.Vehiculo))
        {
            var veh = filter.Vehiculo.Trim();
            query = query.Where(d => d.Ticket.Solicitud.Vehiculo.Placa.Contains(veh) || d.Ticket.Solicitud.Vehiculo.Ficha.Contains(veh));
        }

        var totalCount = await query.CountAsync(ct);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > 100 ? 20 : filter.PageSize;

        var items = await query
            .OrderByDescending(d => d.FechaDespacho)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = items.Select(MapToDetailDto).ToList();
        return new PaginatedList<DispatchDetailDto>(dtos, totalCount, page, pageSize);
    }

    private static DispatchDetailDto MapToDetailDto(Despacho d)
    {
        var vehiculo = d.Ticket?.Solicitud?.Vehiculo;
        var empleado = d.Ticket?.Solicitud?.Empleado;

        return new DispatchDetailDto
        {
            Id = d.Id,
            TicketId = d.TicketId,
            NumeroTicket = d.Ticket?.NumeroTicket ?? string.Empty,
            TanqueId = d.TanqueId,
            TanqueCodigo = d.Tanque?.Codigo ?? string.Empty,
            TanqueNombre = d.Tanque?.Nombre,
            EstacionId = d.Tanque?.EstacionId ?? Guid.Empty,
            EstacionNombre = d.Tanque?.Estacion?.Nombre ?? string.Empty,
            DespachadorUsuarioId = d.DespachadorUsuarioId,
            DespachadorNombre = d.Despachador?.NombreUsuario ?? string.Empty,
            VehiculoId = vehiculo?.Id,
            VehiculoPlaca = vehiculo?.Placa,
            VehiculoFicha = vehiculo?.Ficha,
            EmpleadoNombre = empleado?.NombreCompleto,
            CantidadDespachada = d.CantidadDespachada,
            OdometroRegistrado = d.OdometroRegistrado,
            FechaDespacho = d.FechaDespacho,
            Observaciones = d.Observaciones,
            DireccionIp = d.DireccionIp
        };
    }
}
