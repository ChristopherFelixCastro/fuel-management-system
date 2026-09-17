namespace CombustibleAPI.Application.Dtos.Inventory;

public class TanqueCompatibleDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = default!;
    public string? Nombre { get; set; }
    public decimal CapacidadMaxima { get; set; }
    public decimal StockActual { get; set; }
    public decimal NivelCritico { get; set; }
    public bool Activo { get; set; }
}

public class AvailabilityResponseDto
{
    public Guid EstacionId { get; set; }
    public string? EstacionNombre { get; set; }
    public short TipoCombustibleId { get; set; }
    public string? CombustibleNombre { get; set; }
    public decimal StockFisico { get; set; }
    public decimal StockReservado { get; set; }
    public decimal StockDisponible { get; set; }
    public List<TanqueCompatibleDto> TanquesCompatibles { get; set; } = new();
}
