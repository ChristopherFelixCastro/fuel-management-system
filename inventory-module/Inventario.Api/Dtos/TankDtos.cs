namespace Inventario.Api.Dtos
{
    public class CreateTankRequest
    {
        public Guid StationId { get; set; }
        public string Code { get; set; } = string.Empty;
        public short FuelTypeId { get; set; }
        public decimal MaxCapacity { get; set; }
        public decimal CurrentQuantity { get; set; }
        public decimal CriticalLevel { get; set; }
        public bool? IsActive { get; set; }
    }

    public class UpdateTankRequest
    {
        public decimal? MaxCapacity { get; set; }
        public decimal? CriticalLevel { get; set; }
    }

    public class TankResponse
    {
        public Guid Id { get; set; }
        public Guid StationId { get; set; }
        public string Code { get; set; } = string.Empty;
        public short FuelTypeId { get; set; }
        public decimal MaxCapacity { get; set; }
        public decimal CurrentQuantity { get; set; }
        public decimal CriticalLevel { get; set; }
        public bool IsActive { get; set; }
    }
}