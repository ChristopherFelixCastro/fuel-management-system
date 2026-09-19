namespace Inventario.Api.Models
{
    public class Tanque
    {
        public Guid Id { get; set; }
        public Guid EstacionId { get; set; }
        public short TipoCombustibleId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal CapacidadMaxima { get; set; }
        public decimal StockActual { get; set; }
        public decimal NivelCritico { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }
}