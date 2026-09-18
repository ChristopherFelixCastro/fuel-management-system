using FuelManagement.Alerts.Dtos;
using FuelManagement.Shared.Contracts;

namespace FuelManagement.Alerts.Services;

public interface IAlertService
{
    Task<PagedResult<AlertDto>> GetPaginatedAsync(
        string? estado,
        string? severidad,
        string? modulo,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<AlertDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<AlertDto> AcknowledgeAsync(Guid id, AcknowledgeAlertRequest? request, CancellationToken ct = default);

    Task ScanAndCreateAlertsAsync(CancellationToken ct = default);
}
