using CombustibleAPI.Application.Dtos.Admin;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CombustibleAPI.Api.Controllers;

[ApiController]
[Route("employees")]
[Authorize(Roles = "ADMINISTRADOR,SUPERVISOR")]
public class EmployeesController : ControllerBase

{
    private readonly IAdminService _admin;
    private readonly ICurrentUserService _currentUser;

    public EmployeesController(
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
        var data = await _admin.GetEmpleadosAsync(incluirInactivos, ct);
        return Ok(ApiResponse(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken ct)
    {
        var data = await _admin.GetEmpleadoAsync(id, ct);
        return Ok(ApiResponse(data));
    }

    [HttpPost]
    [Authorize(Roles = "ADMINISTRADOR")]
    public async Task<IActionResult> Create(
        [FromBody] CreateEmpleadoDto request,
        CancellationToken ct)
    {
        var data = await _admin.CreateEmpleadoAsync(
            request, GetUsuarioId(), ct);

        return Created($"/employees/{data.Id}", ApiResponse(data));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ADMINISTRADOR")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateEmpleadoDto request,
        CancellationToken ct)
    {
        var data = await _admin.UpdateEmpleadoAsync(
            id, request, GetUsuarioId(), ct);

        return Ok(ApiResponse(data));
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = "ADMINISTRADOR")]
    public async Task<IActionResult> Deactivate(
        Guid id,
        CancellationToken ct)
    {
        await _admin.DeactivateEmpleadoAsync(
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

