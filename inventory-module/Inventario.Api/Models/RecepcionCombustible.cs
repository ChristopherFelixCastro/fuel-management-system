namespace Inventario.Api.Models
{
    public class RecepcionCombustible
    {
        public Guid Id { get; set; }
        public Guid ProveedorId { get; set; }
        public Guid TanqueId { get; set; }
        public Guid RegistradaPorUsuarioId { get; set; }
        public string NumeroFactura { get; set; } = string.Empty;
        public decimal CantidadRecibida { get; set; }
        public DateTime FechaRecepcion { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}