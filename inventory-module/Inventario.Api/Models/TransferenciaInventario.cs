namespace Inventario.Api.Models
{
    public class TransferenciaInventario
    {
        public Guid Id { get; set; }
        public Guid TanqueOrigenId { get; set; }
        public Guid TanqueDestinoId { get; set; }
        public Guid RegistradaPorUsuarioId { get; set; }
        public decimal Cantidad { get; set; }
        public DateTime FechaTransferencia { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}