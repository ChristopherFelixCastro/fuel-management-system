namespace Inventario.Api.Models
{
    public class AjusteInventario
    {
        public Guid Id { get; set; }
        public Guid TanqueId { get; set; }
        public Guid ReportadoPorUsuarioId { get; set; }
        public Guid? RevisadoPorUsuarioId { get; set; }
        public string TipoAjuste { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string Estado { get; set; } = "PENDIENTE";
        public DateTime FechaReporte { get; set; }
        public DateTime? FechaRevision { get; set; }
        public string? MotivoRechazo { get; set; }
        public string? Observaciones { get; set; }
    }
}