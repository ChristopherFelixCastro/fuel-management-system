namespace Inventario.Api.Dtos
{
    public class InventoryItemResponse
    {
        public Guid TankId { get; set; }
        public Guid StationId { get; set; }
        public decimal PhysicalQuantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal CriticalLevel { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }

    public class InventoryMovementResponse
    {
        public Guid Id { get; set; }
        public Guid TankId { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal PreviousBalance { get; set; }
        public decimal NewBalance { get; set; }
        public DateTime OccurredAt { get; set; }
        public string? Notes { get; set; }
    }
}