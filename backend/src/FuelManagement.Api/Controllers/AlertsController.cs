using FuelManagement.Alerts.Dtos;
using FuelManagement.Alerts.Services;
using FuelManagement.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FuelManagement.Api.Controllers;

/// <summary>
/// Alertas operativas — listado, detalle y reconocimiento.
/// </summary>
[ApiController]
[Route("api/v1/alerts")]
[Produces("application/json")]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;

    public AlertsController(IAlertService alertService)
    {
        _alertService = alertService;
    }

    // --- GET /api/v1/alerts --------------------------------------------
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AlertDto>>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? estado,
        [FromQuery] string? severidad,
        [FromQuery] string? modulo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _alertService.GetPaginatedAsync(estado, severidad, modulo, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<AlertDto>>.Ok(result));
    }

    // --- GET /api/v1/alerts/{id} ---------------------------------------
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AlertDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _alertService.GetByIdAsync(id, ct);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail($"No se encontró la alerta con ID {id}"));

        return Ok(ApiResponse<AlertDto>.Ok(result));
    }

    // --- PATCH /api/v1/alerts/{id}/acknowledge ------------------------
    [HttpPatch("{id:guid}/acknowledge")]
    [ProducesResponseType(typeof(ApiResponse<AlertDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Acknowledge(Guid id, [FromBody] AcknowledgeAlertRequest? request, CancellationToken ct = default)
    {
        var result = await _alertService.AcknowledgeAsync(id, request, ct);
        return Ok(ApiResponse<AlertDto>.Ok(result, "Alerta reconocida exitosamente"));
    }
}
