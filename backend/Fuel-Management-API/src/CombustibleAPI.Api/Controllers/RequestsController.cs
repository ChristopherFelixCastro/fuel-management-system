using CombustibleAPI.Api.Middlewares;
using CombustibleAPI.Application.Dtos.Common;
using CombustibleAPI.Application.Dtos.Dispatches;
using CombustibleAPI.Application.Dtos.Requests;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CombustibleAPI.Api.Controllers;

[ApiController]
[Route("requests")]
[Authorize]
[Produces("application/json")]
public class RequestsController : ControllerBase
{
    private readonly IRequestService _service;
    private readonly ICurrentUserService _currentUser;
    public RequestsController(IRequestService service, ICurrentUserService currentUser) => (_service, _currentUser) = (service, currentUser);
    private Guid UsuarioId => _currentUser.UsuarioId ?? throw ApiException.Unauthorized();
    private string Rol => _currentUser.Rol ?? throw ApiException.Unauthorized();

    [HttpPost]
    [Authorize(Roles = "SOLICITANTE,SUPERVISOR,ADMINISTRADOR")]
    [ProducesResponseType(typeof(ApiResponse<RequestResponseDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(CreateRequestDto request, CancellationToken ct)
    {
        var result = await _service.CrearAsync(request, UsuarioId, ct);
        return CreatedAtAction(nameof(Obtener), new { id = result.Id }, ApiResponse<RequestResponseDto>.Ok(result, HttpContext.GetTraceId()));
    }

    [HttpGet]
    [Authorize(Roles = "SOLICITANTE,SUPERVISOR,ADMINISTRADOR,AUDITOR")]
    public async Task<IActionResult> Listar([FromQuery] RequestFilterDto filter, CancellationToken ct) => Ok(ApiResponse<PaginatedList<RequestResponseDto>>.Ok(await _service.ListarAsync(filter, UsuarioId, Rol, ct), HttpContext.GetTraceId()));

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "SOLICITANTE,SUPERVISOR,ADMINISTRADOR,AUDITOR")]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct) => Ok(ApiResponse<RequestResponseDto>.Ok(await _service.ObtenerAsync(id, UsuarioId, Rol, ct), HttpContext.GetTraceId()));

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "SOLICITANTE,SUPERVISOR,ADMINISTRADOR")]
    public async Task<IActionResult> Actualizar(Guid id, UpdateRequestDto request, CancellationToken ct) => Ok(ApiResponse<RequestResponseDto>.Ok(await _service.ActualizarAsync(id, request, UsuarioId, Rol, ct), HttpContext.GetTraceId()));

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "SUPERVISOR,ADMINISTRADOR")]
    public async Task<IActionResult> Aprobar(Guid id, ApproveRequestDto request, CancellationToken ct) => Ok(ApiResponse<RequestResponseDto>.Ok(await _service.AprobarAsync(id, request, UsuarioId, ct), HttpContext.GetTraceId()));

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "SUPERVISOR,ADMINISTRADOR")]
    public async Task<IActionResult> Rechazar(Guid id, RejectRequestDto request, CancellationToken ct) => Ok(ApiResponse<RequestResponseDto>.Ok(await _service.RechazarAsync(id, request, UsuarioId, ct), HttpContext.GetTraceId()));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "SOLICITANTE,SUPERVISOR,ADMINISTRADOR")]
    public async Task<IActionResult> Cancelar(Guid id, CancelRequestDto? request, CancellationToken ct) => Ok(ApiResponse<RequestResponseDto>.Ok(await _service.CancelarAsync(id, request, UsuarioId, Rol, ct), HttpContext.GetTraceId()));
}
