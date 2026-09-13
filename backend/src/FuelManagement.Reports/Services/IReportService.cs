using FuelManagement.Reports.Dtos;
using FuelManagement.Shared.Contracts;
using FuelManagement.Shared.Domain;

namespace FuelManagement.Reports.Services;

public interface IReportService
{
    Task<PagedResult<VwConsumoDiario>> GetConsumptionReportAsync(
        ConsumptionReportFilter filter,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    Task<List<VwConsumoDiario>> GetConsumptionDataAsync(
        ConsumptionReportFilter filter,
        CancellationToken ct = default);

    Task<PagedResult<VwTanqueResumen>> GetInventoryReportAsync(
        InventoryReportFilter filter,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    Task<List<VwTanqueResumen>> GetInventoryDataAsync(
        InventoryReportFilter filter,
        CancellationToken ct = default);

    Task<PagedResult<VwMovimientosTanque>> GetTraceabilityReportAsync(
        TraceabilityReportFilter filter,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    Task<List<VwMovimientosTanque>> GetTraceabilityDataAsync(
        TraceabilityReportFilter filter,
        CancellationToken ct = default);
}
