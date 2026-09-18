using CombustibleAPI.Api.Middlewares;
using CombustibleAPI.Application.Dtos.Common;
using CombustibleAPI.Application.Dtos.Reports;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CombustibleAPI.Api.Controllers;

/// <summary>
/// Reportes operacionales de consumo, inventario y trazabilidad.
/// </summary>
[ApiController]
[Route("reports")]
[Authorize]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IReportExportService _reportExportService;

    public ReportsController(
        IReportService reportService,
        IReportExportService reportExportService)
    {
        _reportService = reportService;
        _reportExportService = reportExportService;
    }
    /// <summary>
    /// Obtiene el consumo diario de combustible agrupado por tanque.
    /// </summary>
    [HttpGet("consumption")]
    [Authorize(Roles =
        nameof(RolUsuario.ADMINISTRADOR) + "," +
        nameof(RolUsuario.SUPERVISOR) + "," +
        nameof(RolUsuario.AUDITOR))]
    [ProducesResponseType(
        typeof(ApiResponse<ReportPagedResponseDto<ConsumptionReportItemDto>>),
        200)]
    public async Task<IActionResult> GetConsumption(
        [FromQuery] ConsumptionReportFilterDto filter,
        CancellationToken ct)
    {
        var result = await _reportService.GetConsumptionAsync(filter, ct);

        return Ok(
            ApiResponse<ReportPagedResponseDto<ConsumptionReportItemDto>>.Ok(
                result,
                HttpContext.GetTraceId()));
    }

    /// <summary>
    /// Obtiene el estado actual del inventario por tanque.
    /// </summary>
    [HttpGet("inventory")]
    [Authorize(Roles =
        nameof(RolUsuario.ADMINISTRADOR) + "," +
        nameof(RolUsuario.SUPERVISOR) + "," +
        nameof(RolUsuario.AUDITOR))]
    [ProducesResponseType(
        typeof(ApiResponse<ReportPagedResponseDto<InventoryReportItemDto>>),
        200)]
    public async Task<IActionResult> GetInventory(
        [FromQuery] InventoryReportFilterDto filter,
        CancellationToken ct)
    {
        var result = await _reportService.GetInventoryAsync(filter, ct);

        return Ok(
            ApiResponse<ReportPagedResponseDto<InventoryReportItemDto>>.Ok(
                result,
                HttpContext.GetTraceId()));
    }

    /// <summary>
    /// Obtiene la trazabilidad de movimientos de inventario.
    /// </summary>
    [HttpGet("traceability")]
    [Authorize(Roles =
        nameof(RolUsuario.ADMINISTRADOR) + "," +
        nameof(RolUsuario.SUPERVISOR) + "," +
        nameof(RolUsuario.AUDITOR))]
    [ProducesResponseType(
        typeof(ApiResponse<ReportPagedResponseDto<TraceabilityReportItemDto>>),
        200)]
    public async Task<IActionResult> GetTraceability(
        [FromQuery] TraceabilityReportFilterDto filter,
        CancellationToken ct)
    {
        var result = await _reportService.GetTraceabilityAsync(filter, ct);

        return Ok(
            ApiResponse<ReportPagedResponseDto<TraceabilityReportItemDto>>.Ok(
                result,
                HttpContext.GetTraceId()));
    }

    /// <summary>
    /// Exporta el reporte de consumo en CSV, XLSX o PDF.
    /// </summary>
    [HttpGet("consumption/export")]
    [Authorize(Roles =
        nameof(RolUsuario.ADMINISTRADOR) + "," +
        nameof(RolUsuario.SUPERVISOR) + "," +
        nameof(RolUsuario.AUDITOR))]
    public async Task<IActionResult> ExportConsumption(
        [FromQuery] ConsumptionReportFilterDto filter,
        [FromQuery] string format = "pdf",
        CancellationToken ct = default)
    {
        var result = await _reportExportService.ExportConsumptionAsync(
            filter,
            format,
            ct);

        return File(
            result.Content,
            result.ContentType,
            result.FileName);
    }

    /// <summary>
    /// Exporta el reporte de inventario en CSV, XLSX o PDF.
    /// </summary>
    [HttpGet("inventory/export")]
    [Authorize(Roles =
        nameof(RolUsuario.ADMINISTRADOR) + "," +
        nameof(RolUsuario.SUPERVISOR) + "," +
        nameof(RolUsuario.AUDITOR))]
    public async Task<IActionResult> ExportInventory(
        [FromQuery] InventoryReportFilterDto filter,
        [FromQuery] string format = "pdf",
        CancellationToken ct = default)
    {
        var result = await _reportExportService.ExportInventoryAsync(
            filter,
            format,
            ct);

        return File(
            result.Content,
            result.ContentType,
            result.FileName);
    }

    /// <summary>
    /// Exporta el reporte de trazabilidad en CSV, XLSX o PDF.
    /// </summary>
    [HttpGet("traceability/export")]
    [Authorize(Roles =
        nameof(RolUsuario.ADMINISTRADOR) + "," +
        nameof(RolUsuario.SUPERVISOR) + "," +
        nameof(RolUsuario.AUDITOR))]
    public async Task<IActionResult> ExportTraceability(
        [FromQuery] TraceabilityReportFilterDto filter,
        [FromQuery] string format = "pdf",
        CancellationToken ct = default)
    {
        var result = await _reportExportService.ExportTraceabilityAsync(
            filter,
            format,
            ct);

        return File(
            result.Content,
            result.ContentType,
            result.FileName);
    }
}