using CombustibleAPI.Api.Middlewares;
using CombustibleAPI.Application.Dtos.Auth;
using CombustibleAPI.Application.Dtos.Common;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CombustibleAPI.Api.Controllers;

/// <summary>
/// Autenticación y sesión (SDP General sec. 8 / SDP Iván sec. 3-4).
/// </summary>
[ApiController]
[Route("auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;

    public AuthController(IAuthService authService, ICurrentUserService currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }

    /// <summary>Inicia sesión y entrega access token (JWT corto) + refresh token (valor único).</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.LoginAsync(request, ip, ct);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result, HttpContext.GetTraceId()));
    }

    /// <summary>Rota el refresh token: revoca el actual y entrega un nuevo par de tokens.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.RefreshAsync(request.RefreshToken, ip, ct);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result, HttpContext.GetTraceId()));
    }

    /// <summary>Revoca el refresh token indicado (logout). Idempotente.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDto request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _authService.LogoutAsync(request.RefreshToken, ip, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Sesión cerrada." }, HttpContext.GetTraceId()));
    }

    /// <summary>Devuelve el perfil del usuario autenticado, rol y estación asignada (para PWA / portal).</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var usuarioId = _currentUser.UsuarioId ?? throw ApiException.Unauthorized();
        var result = await _authService.GetCurrentUserProfileAsync(usuarioId, ct);
        return Ok(ApiResponse<UserProfileDto>.Ok(result, HttpContext.GetTraceId()));
    }
}
