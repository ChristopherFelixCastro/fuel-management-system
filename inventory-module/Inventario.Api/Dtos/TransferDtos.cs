namespace Inventario.Api.Dtos
{
    public class CreateTransferRequest
    {
        public Guid SourceTankId { get; set; }
        public Guid DestinationTankId { get; set; }
        public decimal Quantity { get; set; }
        public DateTime TransferredAt { get; set; }
        public string? Notes { get; set; }
    }

    public class TransferResponse
    {
        public Guid Id { get; set; }
        public Guid SourceTankId { get; set; }
        public Guid DestinationTankId { get; set; }
        public decimal Quantity { get; set; }
        public DateTime TransferredAt { get; set; }
    }
}