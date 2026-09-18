using CombustibleAPI.Application.Dtos.Admin;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CombustibleAPI.Api.Controllers;

[ApiController]
[Route("users")]
[Authorize(Roles = "ADMINISTRADOR")]
public class UsersController : ControllerBase
{
    private readonly IAdminService _admin;
    private readonly ICurrentUserService _currentUser;

    public UsersController(
        IAdminService admin,
        ICurrentUserService currentUser)
    {
        _admin = admin;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool incluirInactivos = false,
        CancellationToken ct = default)
    {
        var data = await _admin.GetUsuariosAsync(incluirInactivos, ct);
        return Ok(ApiResponse(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken ct)
    {
        var data = await _admin.GetUsuarioAsync(id, ct);
        return Ok(ApiResponse(data));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUsuarioDto request,
        CancellationToken ct)
    {
        var data = await _admin.CreateUsuarioAsync(
            request, GetUsuarioId(), ct);

        return Created($"/users/{data.Id}", ApiResponse(data));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateUsuarioDto request,
        CancellationToken ct)
    {
        var data = await _admin.UpdateUsuarioAsync(
            id, request, GetUsuarioId(), ct);

        return Ok(ApiResponse(data));
    }

    [HttpPatch("{id:guid}/password")]
    public async Task<IActionResult> ChangePassword(
        Guid id,
        [FromBody] ChangeUsuarioPasswordDto request,
        CancellationToken ct)
    {
        await _admin.ChangeUsuarioPasswordAsync(
            id, request, GetUsuarioId(), ct);

        return NoContent();
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(
        Guid id,
        CancellationToken ct)
    {
        await _admin.DeactivateUsuarioAsync(
            id, GetUsuarioId(), ct);

        return NoContent();
    }

    private object ApiResponse(object data) => new
    {
        data,
        meta = new { traceId = HttpContext.TraceIdentifier }
    };
    private Guid GetUsuarioId() =>
        _currentUser.UsuarioId
        ?? throw ApiException.Unauthorized();
}

