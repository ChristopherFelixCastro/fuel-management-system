using FuelManagement.Reports.Services;
using Microsoft.AspNetCore.Mvc;

namespace FuelManagement.Api.Controllers;

/// <summary>
/// Exportaciones — genera archivos PDF, XLSX o CSV de los reportes operacionales.
/// </summary>
[ApiController]
[Route("api/v1/exports")]
public class ExportsController : ControllerBase
{
    private readonly IExportService _exportService;

    public ExportsController(IExportService exportService)
    {
        _exportService = exportService;
    }

    // --- GET /api/v1/exports/{reportType} ------------------------------
    [HttpGet("{reportType}")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Export(
        string reportType,
        [FromQuery] string format = "pdf",
        [FromQuery] Guid? tanqueId = null,
        [FromQuery] DateOnly? fechaDesde = null,
        [FromQuery] DateOnly? fechaHasta = null,
        [FromQuery] string? equipoCodigo = null,
        CancellationToken ct = default)
    {
        var (fileBytes, contentType, fileName) = await _exportService.ExportAsync(
            reportType, format, tanqueId, fechaDesde, fechaHasta, equipoCodigo, ct);

        return File(fileBytes, contentType, fileName);
    }
}
