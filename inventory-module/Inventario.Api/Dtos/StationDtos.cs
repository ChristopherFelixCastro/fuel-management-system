namespace Inventario.Api.Dtos
{
    public class CreateStationRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public bool? IsActive { get; set; }
    }

    public class UpdateStationRequest
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
    }

    public class StationResponse
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public bool IsActive { get; set; }
    }
}