namespace Inventario.Api.Models
{
    public class Proveedor
    {
        public Guid Id { get; set; }
        public string Rnc { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? NombreComercial { get; set; }
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }
}