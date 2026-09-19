namespace Inventario.Api.Models
{
    public class MovimientoInventario
    {
        public Guid Id { get; set; }
        public Guid TanqueId { get; set; }
        public Guid RegistradoPorUsuarioId { get; set; }
        public string TipoMovimiento { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal SaldoAnterior { get; set; }
        public decimal SaldoPosterior { get; set; }
        public Guid? RecepcionId { get; set; }
        public Guid? DespachoId { get; set; }
        public Guid? TransferenciaId { get; set; }
        public Guid? AjusteId { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public string? Observaciones { get; set; }
    }
}