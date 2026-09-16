using CombustibleAPI.Application.Dtos.Tickets;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CombustibleAPI.Infrastructure.Services;

/// <summary>
/// POST /tickets/validate. Regla de autoridad (RN-06/RN-07, SDP Gabriel sec. 4):
/// el QR nunca es la fuente de verdad. Esta clase consulta el estado oficial en BD y
/// devuelve el detalle completo del ticket: empleado, vehículo, placa, ficha, combustible,
/// cantidad autorizada, vencimiento y estado.
/// </summary>
public class TicketService : ITicketService
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;

    public TicketService(AppDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<TicketOficialDto> ValidarAsync(string qrPayload, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(qrPayload))
            throw ApiException.ValidationError("El payload del ticket/QR no puede estar vacío.");

        Guid.TryParse(qrPayload.Trim(), out var parsedId);

        var ticket = await _context.Tickets
            .Include(t => t.Solicitud).ThenInclude(s => s.Empleado)
            .Include(t => t.Solicitud).ThenInclude(s => s.Vehiculo)
            .Include(t => t.Solicitud).ThenInclude(s => s.Departamento)
            .Include(t => t.Estacion)
            .Include(t => t.TipoCombustible)
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.NumeroTicket == qrPayload.Trim() ||
                t.TokenQrHash == qrPayload.Trim() ||
                (parsedId != Guid.Empty && t.Id == parsedId), ct);

        if (ticket is null)
        {
            await _auditService.RegistrarAsync(null, "VALIDACION_TICKET_FALLIDA", "Ticket", qrPayload, null, null,
                new { motivo = "inexistente" }, ct);
            throw ApiException.TicketInexistente();
        }

        var ahora = DateTime.UtcNow;
        var estadoAlmacenado = ticket.Estado;
        string estadoEfectivo;

        if (string.Equals(estadoAlmacenado, "ANULADO", StringComparison.OrdinalIgnoreCase))
        {
            await _auditService.RegistrarAsync(null, "VALIDACION_TICKET_RECHAZADA", "Ticket", ticket.Id.ToString(), null, null,
                new { motivo = "anulado" }, ct);
            throw ApiException.TicketAnulado();
        }

        if (string.Equals(estadoAlmacenado, "CONSUMIDO", StringComparison.OrdinalIgnoreCase))
        {
            await _auditService.RegistrarAsync(null, "VALIDACION_TICKET_RECHAZADA", "Ticket", ticket.Id.ToString(), null, null,
                new { motivo = "consumido" }, ct);
            throw ApiException.TicketConsumido();
        }

        if (ticket.FechaExpiracion <= ahora)
        {
            await _auditService.RegistrarAsync(null, "VALIDACION_TICKET_RECHAZADA", "Ticket", ticket.Id.ToString(), null, null,
                new { motivo = "vencido" }, ct);
            throw ApiException.TicketVencido();
        }

        if (ticket.FechaExpiracion <= ahora.AddDays(2))
        {
            estadoEfectivo = "PROXIMO_A_VENCER";
        }
        else
        {
            estadoEfectivo = estadoAlmacenado;
        }

        var solicitud = ticket.Solicitud;
        var empleado = solicitud?.Empleado;
        var vehiculo = solicitud?.Vehiculo;
        var departamento = solicitud?.Departamento;

        await _auditService.RegistrarAsync(null, "VALIDACION_TICKET_EXITOSA", "Ticket", ticket.Id.ToString(), null, null,
            new { ticket.NumeroTicket, estadoEfectivo }, ct);

        return new TicketOficialDto
        {
            TicketId = ticket.Id,
            NumeroTicket = ticket.NumeroTicket,
            EmpleadoId = empleado?.Id ?? Guid.Empty,
            Empleado = empleado != null ? $"{empleado.Nombre} {empleado.Apellido}".Trim() : string.Empty,
            CodigoEmpleado = empleado?.CodigoEmpleado,
            VehiculoId = vehiculo?.Id ?? Guid.Empty,
            Vehiculo = vehiculo != null ? $"{vehiculo.Marca} {vehiculo.Modelo}".Trim() : string.Empty,
            Placa = vehiculo?.Placa ?? string.Empty,
            Ficha = vehiculo?.Ficha ?? string.Empty,
            TipoCombustibleId = ticket.TipoCombustibleId,
            TipoCombustible = ticket.TipoCombustible?.Nombre ?? string.Empty,
            CombustibleCodigo = ticket.TipoCombustible?.Codigo,
            CantidadAutorizada = ticket.CantidadAutorizada,
            CantidadDisponible = ticket.CantidadAutorizada,
            FechaExpiracion = ticket.FechaExpiracion,
            Estado = estadoAlmacenado,
            EstadoEfectivo = estadoEfectivo,
            EstacionId = ticket.EstacionId,
            EstacionNombre = ticket.Estacion?.Nombre,
            DepartamentoId = departamento?.Id ?? Guid.Empty,
            DepartamentoNombre = departamento?.Nombre
        };
    }
}
