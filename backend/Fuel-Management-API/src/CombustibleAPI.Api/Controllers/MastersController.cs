using CombustibleAPI.Api.Middlewares;
using CombustibleAPI.Application.Dtos.Common;
using CombustibleAPI.Application.Dtos.Masters;
using CombustibleAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CombustibleAPI.Api.Controllers;

/// <summary>
/// Endpoints de catálogos maestros del sistema.
/// </summary>
[ApiController]
[Route("masters")]
[Authorize]
[Produces("application/json")]
public class MastersController : ControllerBase
{
  private readonly IMastersService _mastersService;
private readonly IAdminService _admin;

public MastersController(
    IMastersService mastersService,
    IAdminService admin)
{
    _mastersService = mastersService;
    _admin = admin;
}

    /// <summary>Listado de roles activos del sistema.</summary>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(ApiResponse<List<RolDto>>), 200)]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        var result = await _mastersService.GetRolesAsync(ct);
        return Ok(ApiResponse<List<RolDto>>.Ok(result, HttpContext.GetTraceId()));
    }

    /// <summary>Listado de estaciones de combustible activas.</summary>
    [HttpGet("estaciones")]
    [ProducesResponseType(typeof(ApiResponse<List<EstacionDto>>), 200)]
    public async Task<IActionResult> GetEstaciones(CancellationToken ct)
    {
        var result = await _mastersService.GetEstacionesAsync(ct);
        return Ok(ApiResponse<List<EstacionDto>>.Ok(result, HttpContext.GetTraceId()));
    }

    /// <summary>Listado de vehículos activos.</summary>
    [HttpGet("vehiculos")]
    [ProducesResponseType(typeof(ApiResponse<List<VehiculoDto>>), 200)]
    public async Task<IActionResult> GetVehiculos(CancellationToken ct)
    {
        var result = await _mastersService.GetVehiculosAsync(ct);
        return Ok(ApiResponse<List<VehiculoDto>>.Ok(result, HttpContext.GetTraceId()));
    }

    /// <summary>Listado de tanques activos con filtros opcionales por estación y tipo de combustible.</summary>
    [HttpGet("tanques")]
    [ProducesResponseType(typeof(ApiResponse<List<TanqueDto>>), 200)]
    public async Task<IActionResult> GetTanques(
        [FromQuery] Guid? estacionId,
        [FromQuery] short? tipoCombustibleId,
        CancellationToken ct)
    {
        var result = await _mastersService.GetTanquesAsync(estacionId, tipoCombustibleId, ct);
        return Ok(ApiResponse<List<TanqueDto>>.Ok(result, HttpContext.GetTraceId()));
    }

    /// <summary>Tanques de una estación específica, con filtro opcional por tipo de combustible.</summary>
    [HttpGet("estaciones/{stationId}/tanques")]
    [ProducesResponseType(typeof(ApiResponse<List<TanqueDto>>), 200)]
    public async Task<IActionResult> GetTanquesPorEstacion(
        [FromRoute] Guid stationId,
        [FromQuery] short? tipoCombustibleId,
        CancellationToken ct)
    {
        var result = await _mastersService.GetTanquesAsync(stationId, tipoCombustibleId, ct);
        return Ok(ApiResponse<List<TanqueDto>>.Ok(result, HttpContext.GetTraceId()));
    }

    [HttpGet("fuel-types")]
    public async Task<IActionResult> GetFuelTypes(
    CancellationToken ct)
    {
        var data = await _admin.GetTiposCombustibleAsync(ct);

        return Ok(new
        {
            data,
            meta = new
            {
                traceId = HttpContext.TraceIdentifier
            }
        });
    }
}
