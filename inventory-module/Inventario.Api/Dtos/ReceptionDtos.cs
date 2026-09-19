namespace Inventario.Api.Dtos
{
    public class CreateReceptionRequest
    {
        public Guid SupplierId { get; set; }
        public Guid StationId { get; set; }
        public Guid TankId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
        public decimal Volume { get; set; }
        public string Rnc { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class ReceptionResponse
    {
        public Guid Id { get; set; }
        public Guid SupplierId { get; set; }
        public Guid TankId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal Volume { get; set; }
        public DateTime ReceivedAt { get; set; }
        public decimal TankQuantityAfter { get; set; }
    }
}