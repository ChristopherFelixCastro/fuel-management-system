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

    public ClosuresController(IClosureService closureService, ICurrentUserService currentUser)
    {
        _closureService = closureService;
        _currentUser = currentUser;
    }

    /// <summary>Registra un cierre diario para un tanque con estado PENDIENTE_APROBACION.</summary>
    [HttpPost("daily")]
    [ProducesResponseType(typeof(ApiResponse<ClosureResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 422)]
    public async Task<IActionResult> CreateDaily([FromBody] CreateDailyClosureRequestDto request, CancellationToken ct)
    {
        var usuarioId = _currentUser.UsuarioId ?? throw ApiException.Unauthorized();
        var result = await _closureService.CreateDailyClosureAsync(request, usuarioId, ct);
        return Ok(ApiResponse<ClosureResponseDto>.Ok(result, HttpContext.GetTraceId()));
    }

    /// <summary>Aprueba un cierre diario pendiente (Supervisor/Administrador).</summary>
    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = nameof(RolUsuario.SUPERVISOR) + "," + nameof(RolUsuario.ADMINISTRADOR))]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    [ProducesResponseType(typeof(ApiErrorResponse), 409)]
    public async Task<IActionResult> Approve([FromRoute] Guid id, CancellationToken ct)
    {
        var supervisorId = _currentUser.UsuarioId ?? throw ApiException.Unauthorized();
        await _closureService.ApproveClosureAsync(id, supervisorId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Cierre diario aprobado exitosamente." }, HttpContext.GetTraceId()));
    }

    /// <summary>Rechaza un cierre diario pendiente con motivo explicativo (Supervisor/Administrador).</summary>
    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = nameof(RolUsuario.SUPERVISOR) + "," + nameof(RolUsuario.ADMINISTRADOR))]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    [ProducesResponseType(typeof(ApiErrorResponse), 409)]
    public async Task<IActionResult> Reject([FromRoute] Guid id, [FromBody] RejectClosureRequestDto request, CancellationToken ct)
    {
        var supervisorId = _currentUser.UsuarioId ?? throw ApiException.Unauthorized();
        await _closureService.RejectClosureAsync(id, supervisorId, request.Motivo, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Cierre diario rechazado." }, HttpContext.GetTraceId()));
    }
}
