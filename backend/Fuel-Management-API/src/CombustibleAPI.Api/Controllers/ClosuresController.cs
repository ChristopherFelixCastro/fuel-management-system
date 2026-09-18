using CombustibleAPI.Api.Middlewares;
using CombustibleAPI.Application.Dtos.Closures;
using CombustibleAPI.Application.Dtos.Common;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CombustibleAPI.Api.Controllers;

/// <summary>
/// Gestión de cierres diarios de combustible por tanque.
/// </summary>
[ApiController]
[Route("closures")]
[Authorize]
[Produces("application/json")]
public class ClosuresController : ControllerBase
{
    private readonly IClosureService _closureService;
    private readonly ICurrentUserService _currentUser;

    public ClosuresController(
        IClosureService closureService,
        ICurrentUserService currentUser)
    {
        _closureService = closureService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Obtiene el cálculo preliminar de un cierre diario para un tanque.
    /// </summary>
    [HttpGet("daily/preview")]
    [Authorize(Roles =
        nameof(RolUsuario.DESPACHADOR) + "," +
        nameof(RolUsuario.SUPERVISOR) + "," +
        nameof(RolUsuario.ADMINISTRADOR))]
    [ProducesResponseType(typeof(ApiResponse<ClosurePreviewDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> Preview(
        [FromQuery] Guid tanqueId,
        [FromQuery] DateOnly fecha,
        CancellationToken ct)
    {
        var result = await _closureService.GetPreviewAsync(
            tanqueId,
            fecha,
            ct);

        return Ok(
            ApiResponse<ClosurePreviewDto>.Ok(
                result,
                HttpContext.GetTraceId()));
    }

    /// <summary>
    /// Obtiene los cierres diarios con filtros y paginación.
    /// </summary>
    [HttpGet]
    [Authorize(Roles =
        nameof(RolUsuario.DESPACHADOR) + "," +
        nameof(RolUsuario.SUPERVISOR) + "," +
        nameof(RolUsuario.ADMINISTRADOR))]
    [ProducesResponseType(typeof(ApiResponse<ClosurePagedResponseDto>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] ClosureFilterDto filter,
        CancellationToken ct)
    {
        var result = await _closureService.GetClosuresAsync(
            filter,
            ct);

        return Ok(
            ApiResponse<ClosurePagedResponseDto>.Ok(
                result,
                HttpContext.GetTraceId()));
    }

    /// <summary>
    /// Obtiene el detalle de un cierre diario.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles =
        nameof(RolUsuario.DESPACHADOR) + "," +
        nameof(RolUsuario.SUPERVISOR) + "," +
        nameof(RolUsuario.ADMINISTRADOR))]
    [ProducesResponseType(typeof(ApiResponse<ClosureResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var result = await _closureService.GetClosureByIdAsync(
            id,
            ct);

        return Ok(
            ApiResponse<ClosureResponseDto>.Ok(
                result,
                HttpContext.GetTraceId()));
    }

    /// <summary>
    /// Registra un cierre diario para un tanque con estado PENDIENTE_APROBACION.
    /// </summary>
    [HttpPost("daily")]
    [Authorize(Roles = nameof(RolUsuario.DESPACHADOR))]
    [ProducesResponseType(typeof(ApiResponse<ClosureResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 422)]
    public async Task<IActionResult> CreateDaily(
        [FromBody] CreateDailyClosureRequestDto request,
        CancellationToken ct)
    {
        var usuarioId =
            _currentUser.UsuarioId ??
            throw ApiException.Unauthorized();

        var result = await _closureService.CreateDailyClosureAsync(
            request,
            usuarioId,
            ct);

        return Ok(
            ApiResponse<ClosureResponseDto>.Ok(
                result,
                HttpContext.GetTraceId()));
    }

    /// <summary>
    /// Aprueba un cierre diario pendiente.
    /// </summary>
    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles =
        nameof(RolUsuario.SUPERVISOR) + "," +
        nameof(RolUsuario.ADMINISTRADOR))]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    [ProducesResponseType(typeof(ApiErrorResponse), 409)]
    public async Task<IActionResult> Approve(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var supervisorId =
            _currentUser.UsuarioId ??
            throw ApiException.Unauthorized();

        await _closureService.ApproveClosureAsync(
            id,
            supervisorId,
            ct);

        return Ok(
            ApiResponse<object>.Ok(
                new
                {
                    message = "Cierre diario aprobado exitosamente."
                },
                HttpContext.GetTraceId()));
    }

    /// <summary>
    /// Rechaza un cierre diario pendiente con motivo explicativo.
    /// </summary>
    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles =
        nameof(RolUsuario.SUPERVISOR) + "," +
        nameof(RolUsuario.ADMINISTRADOR))]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    [ProducesResponseType(typeof(ApiErrorResponse), 409)]
    public async Task<IActionResult> Reject(
        [FromRoute] Guid id,
        [FromBody] RejectClosureRequestDto request,
        CancellationToken ct)
    {
        var supervisorId =
            _currentUser.UsuarioId ??
            throw ApiException.Unauthorized();

        await _closureService.RejectClosureAsync(
            id,
            supervisorId,
            request.Motivo,
            ct);

        return Ok(
            ApiResponse<object>.Ok(
                new
                {
                    message = "Cierre diario rechazado."
                },
                HttpContext.GetTraceId()));
    }
}