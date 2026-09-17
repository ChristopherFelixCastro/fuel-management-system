using System.ComponentModel.DataAnnotations;

namespace CombustibleAPI.Application.Dtos.Tickets;

/// <summary>
/// La PWA envía el contenido crudo del QR sin interpretarlo (RN-06).
/// Puede ser el número de ticket (ej. COM-2026-000001), el ID (UUID) o el hash del token.
/// </summary>
public class ValidateTicketRequestDto
{
    [Required] public string QrPayload { get; set; } = default!;
}

public class TicketOficialDto
{
    public Guid TicketId { get; set; }
    public string NumeroTicket { get; set; } = default!;
    public string Ticket => NumeroTicket;
    public string Numero => NumeroTicket;

    public Guid EmpleadoId { get; set; }
    public string Empleado { get; set; } = default!;
    public string? CodigoEmpleado { get; set; }

    public Guid VehiculoId { get; set; }
    public string Vehiculo { get; set; } = default!;
    public string Placa { get; set; } = default!;
    public string Ficha { get; set; } = default!;

    public short TipoCombustibleId { get; set; }
    public string TipoCombustible { get; set; } = default!;
    public string? CombustibleCodigo { get; set; }

    public decimal CantidadAutorizada { get; set; }
    public decimal CantidadDisponible { get; set; }

    public DateTime FechaExpiracion { get; set; }
    public DateTime Vencimiento => FechaExpiracion;
    public DateTime VenceEn => FechaExpiracion;

    public string Estado { get; set; } = default!;
    public string EstadoEfectivo { get; set; } = default!;

    public Guid EstacionId { get; set; }
    public string? EstacionNombre { get; set; }

    public Guid DepartamentoId { get; set; }
    public string? DepartamentoNombre { get; set; }
}
