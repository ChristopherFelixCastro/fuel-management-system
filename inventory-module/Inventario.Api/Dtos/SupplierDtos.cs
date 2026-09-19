namespace Inventario.Api.Dtos
{
    public class CreateSupplierRequest
    {
        public string Rnc { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool? IsActive { get; set; }
    }

    public class UpdateSupplierRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    public class SupplierResponse
    {
        public Guid Id { get; set; }
        public string Rnc { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
    }
}