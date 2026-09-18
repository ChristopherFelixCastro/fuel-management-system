namespace FuelManagement.Reports.Services;

public interface IExportService
{
    Task<(byte[] FileBytes, string ContentType, string FileName)> ExportAsync(
        string reportType,
        string format,
        Guid? tanqueId,
        DateOnly? fechaDesde,
        DateOnly? fechaHasta,
        string? equipoCodigo,
        CancellationToken ct = default);
}
