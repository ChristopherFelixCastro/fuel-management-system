using CombustibleAPI.Api.Middlewares;
using CombustibleAPI.Application.Dtos.Alerts;
using CombustibleAPI.Application.Dtos.Common;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CombustibleAPI.Api.Controllers;

/// <summary>
/// Alertas operativas calculadas a partir del estado actual del sistema.
/// </summary>
[ApiController]
[Route("alerts")]
[Authorize]
[Produces("application/json")]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;

    public AlertsController(IAlertService alertService)
    {
        _alertService = alertService;
    }

    /// <summary>
    /// Obtiene los tanques activos cuyo stock alcanzó o cayó
    /// por debajo de su nivel crítico.
    /// </summary>
    [HttpGet("low-inventory")]
    [Authorize(Roles =
        nameof(RolUsuario.ADMINISTRADOR) + "," +
        nameof(RolUsuario.SUPERVISOR) + "," +
        nameof(RolUsuario.AUDITOR))]
    [ProducesResponseType(
        typeof(ApiResponse<LowInventoryAlertPagedResponseDto>),
        200)]
    public async Task<IActionResult> GetLowInventory(
        [FromQuery] LowInventoryAlertFilterDto filter,
        CancellationToken ct)
    {
        var result = await _alertService.GetLowInventoryAsync(filter, ct);

        return Ok(
            ApiResponse<LowInventoryAlertPagedResponseDto>.Ok(
                result,
                HttpContext.GetTraceId()));
    }
}