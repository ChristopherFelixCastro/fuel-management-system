using FuelManagement.Reports.Dtos;
using FuelManagement.Reports.Services;
using FuelManagement.Shared.Contracts;
using FuelManagement.Shared.Domain;
using Microsoft.AspNetCore.Mvc;

namespace FuelManagement.Api.Controllers;

/// <summary>
/// Reportes operacionales — consumo, inventario y trazabilidad.
/// </summary>
[ApiController]
[Route("api/v1/reports")]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    // --- GET /api/v1/reports/consumption ------------------------------
    [HttpGet("consumption")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<VwConsumoDiario>>), 200)]
    public async Task<IActionResult> GetConsumption(
        [FromQuery] DateOnly? fechaDesde,
        [FromQuery] DateOnly? fechaHasta,
        [FromQuery] Guid? tanqueId,
        [FromQuery] string? equipoCodigo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var filter = new ConsumptionReportFilter
        {
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            TanqueId = tanqueId,
            EquipoCodigo = equipoCodigo
        };

        var result = await _reportService.GetConsumptionReportAsync(filter, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<VwConsumoDiario>>.Ok(result));
    }

    // --- GET /api/v1/reports/inventory --------------------------------
    [HttpGet("inventory")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<VwTanqueResumen>>), 200)]
    public async Task<IActionResult> GetInventory(
        [FromQuery] Guid? tanqueId,
        [FromQuery] string? combustibleTipo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var filter = new InventoryReportFilter
        {
            TanqueId = tanqueId,
            CombustibleTipo = combustibleTipo
        };

        var result = await _reportService.GetInventoryReportAsync(filter, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<VwTanqueResumen>>.Ok(result));
    }

    // --- GET /api/v1/reports/traceability -----------------------------
    [HttpGet("traceability")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<VwMovimientosTanque>>), 200)]
    public async Task<IActionResult> GetTraceability(
        [FromQuery] DateOnly? fechaDesde,
        [FromQuery] DateOnly? fechaHasta,
        [FromQuery] Guid? tanqueId,
        [FromQuery] string? tipoMovimiento,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var filter = new TraceabilityReportFilter
        {
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            TanqueId = tanqueId,
            TipoMovimiento = tipoMovimiento
        };

        var result = await _reportService.GetTraceabilityReportAsync(filter, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<VwMovimientosTanque>>.Ok(result));
    }
}
