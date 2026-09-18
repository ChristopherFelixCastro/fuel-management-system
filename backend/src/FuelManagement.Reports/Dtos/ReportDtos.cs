using FuelManagement.Shared.Domain;

namespace FuelManagement.Reports.Dtos;

public sealed class ConsumptionReportFilter
{
    public DateOnly? FechaDesde { get; init; }
    public DateOnly? FechaHasta { get; init; }
    public Guid? TanqueId { get; init; }
    public string? EquipoCodigo { get; init; }
}

public sealed class InventoryReportFilter
{
    public Guid? TanqueId { get; init; }
    public string? CombustibleTipo { get; init; }
}

public sealed class TraceabilityReportFilter
{
    public DateOnly? FechaDesde { get; init; }
    public DateOnly? FechaHasta { get; init; }
    public Guid? TanqueId { get; init; }
    public string? TipoMovimiento { get; init; }
}
