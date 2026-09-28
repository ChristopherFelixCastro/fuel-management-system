namespace CombustibleAPI.Application.Dtos.PublicTickets;

public class PublicTicketDto
{
    public string NumeroTicket { get; set; } = default!;
    public string Empleado { get; set; } = default!;
    public string? CodigoEmpleado { get; set; }
    public string Vehiculo { get; set; } = default!;
    public string Placa { get; set; } = default!;
    public string Ficha { get; set; } = default!;
    public string TipoCombustible { get; set; } = default!;
    public decimal CantidadAutorizada { get; set; }
    public string Estacion { get; set; } = default!;
    public DateTime FechaExpiracion { get; set; }
    public string Estado { get; set; } = default!; // ACTIVO | CONSUMIDO | VENCIDO | ANULADO
    public string MensajeEstado { get; set; } = default!;
    public bool PermiteDespacho { get; set; }
}
