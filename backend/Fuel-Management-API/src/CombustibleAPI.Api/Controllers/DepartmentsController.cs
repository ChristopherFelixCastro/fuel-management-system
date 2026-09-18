using CombustibleAPI.Application.Dtos.Admin;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CombustibleAPI.Api.Controllers;

[ApiController]
[Route("departments")]
[Authorize(Roles = "ADMINISTRADOR,SUPERVISOR")]
public class DepartmentsController : ControllerBase
{
    private readonly IAdminService _admin;
    private readonly ICurrentUserService _currentUser;

    public DepartmentsController(
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
        var data = await _admin.GetDepartamentosAsync(
            incluirInactivos,
            ct);

        return Ok(new
        {
            data,
            meta = new
            {
                traceId = HttpContext.TraceIdentifier
            }
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken ct)
    {
        var data = await _admin.GetDepartamentoAsync(id, ct);

        return Ok(new
        {
            data,
            meta = new
            {
                traceId = HttpContext.TraceIdentifier
            }
        });
    }

    [HttpPost]
    [Authorize(Roles = "ADMINISTRADOR")]
    public async Task<IActionResult> Create(
        [FromBody] CreateDepartamentoDto request,
        CancellationToken ct)
    {
        var data = await _admin.CreateDepartamentoAsync(
            request,
            GetUsuarioId(),
            ct);

        return Created(
            $"/departments/{data.Id}",
            new
            {
                data,
                meta = new
                {
                    traceId = HttpContext.TraceIdentifier
                }
            });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ADMINISTRADOR")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateDepartamentoDto request,
        CancellationToken ct)
    {
        var data = await _admin.UpdateDepartamentoAsync(
            id,
            request,
            GetUsuarioId(),
            ct);

        return Ok(new
        {
            data,
            meta = new
            {
                traceId = HttpContext.TraceIdentifier
            }
        });
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = "ADMINISTRADOR")]
    public async Task<IActionResult> Deactivate(
        Guid id,
        CancellationToken ct)
    {
        await _admin.DeactivateDepartamentoAsync(
            id,
            GetUsuarioId(),
            ct);

        return NoContent();
    }

    private Guid GetUsuarioId()
    {
        return _currentUser.UsuarioId
            ?? throw ApiException.Unauthorized();
    }
}