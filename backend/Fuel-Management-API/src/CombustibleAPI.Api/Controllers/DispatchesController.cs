using CombustibleAPI.Api.Middlewares;
using CombustibleAPI.Application.Dtos.Common;
using CombustibleAPI.Application.Dtos.Dispatches;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CombustibleAPI.Api.Controllers;

/// <summary>
/// Endpoint transaccional de despacho de combustible y consulta de historial.
/// </summary>
[ApiController]
[Route("dispatches")]
[Authorize]
[Produces("application/json")]
public class DispatchesController : ControllerBase
{
    private readonly IDispatchService _dispatchService;
    private readonly ICurrentUserService _currentUser;

    public DispatchesController(IDispatchService dispatchService, ICurrentUserService currentUser)
    {
        _dispatchService = dispatchService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Registra un despacho de combustible de forma atómica: valida ticket activo, cantidad exacta,
    /// estación, tanque, combustible compatible, inventario físico suficiente, odómetro y doble consumo.
    /// Solo el rol DESPACHADOR puede despachar sobre su propia estación asignada.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = nameof(RolUsuario.DESPACHADOR))]
    [ProducesResponseType(typeof(ApiResponse<DispatchResultDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(ApiErrorResponse), 409)]
    [ProducesResponseType(typeof(ApiErrorResponse), 422)]
    public async Task<IActionResult> Registrar([FromBody] DispatchRequestDto request, CancellationToken ct)
    {
        var despachadorId = _currentUser.UsuarioId ?? throw ApiException.Unauthorized();
        var estacionId = _currentUser.EstacionId ?? throw ApiException.BusinessRule(
            "DESPACHADOR_SIN_ESTACION", "El usuario autenticado no tiene estación asignada.");

        var result = await _dispatchService.RegistrarDespachoAsync(
            request, despachadorId, estacionId, _currentUser.IpAddress, ct);

        return Ok(ApiResponse<DispatchResultDto>.Ok(result, HttpContext.GetTraceId()));
    }

    /// <summary>Obtiene el detalle de un despacho por su identificador.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DispatchDetailDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _dispatchService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<DispatchDetailDto>.Ok(result, HttpContext.GetTraceId()));
    }

    /// <summary>
    /// Listado paginado y filtrable de despachos por fecha, estación, ticket o vehículo
    /// (para supervisor, administrador y auditor).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<DispatchDetailDto>>), 200)]
    public async Task<IActionResult> GetList([FromQuery] DispatchesFilterDto filter, CancellationToken ct)
    {
        var result = await _dispatchService.GetPaginatedAsync(filter, ct);
        return Ok(ApiResponse<PaginatedList<DispatchDetailDto>>.Ok(result, HttpContext.GetTraceId()));
    }
}
