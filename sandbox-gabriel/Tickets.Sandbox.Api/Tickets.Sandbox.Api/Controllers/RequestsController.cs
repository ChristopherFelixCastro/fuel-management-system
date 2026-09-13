using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickets.Sandbox.Api.Application.Common;
using Tickets.Sandbox.Api.Application.DTOs.Requests;
using Tickets.Sandbox.Api.Application.Interfaces;

namespace Tickets.Sandbox.Api.Controllers;

[ApiController]
[Route("api/v1/requests")]
[Produces("application/json")]
public class RequestsController : ControllerBase
{
    private readonly IRequestService _requestService;

    public RequestsController(IRequestService requestService)
    {
        _requestService = requestService;
    }

    /// <summary>
    /// Crea una nueva solicitud de combustible.
    /// Roles permitidos: SOLICITANTE, SUPERVISOR, ADMINISTRADOR.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SOLICITANTE,SUPERVISOR,ADMINISTRADOR")]
    [ProducesResponseType(typeof(ApiResponse<RequestResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateRequestDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = User.Identity?.Name ?? "SOLICITANTE";
        var result = await _requestService.CreateRequestAsync(dto, currentUserId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<RequestResponseDto>.Ok(result, "Solicitud creada exitosamente."));
    }

    /// <summary>
    /// Lista las solicitudes de combustible con filtros opcionales.
    /// Roles permitidos: SOLICITANTE (propias), SUPERVISOR, AUDITOR, ADMINISTRADOR.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SOLICITANTE,SUPERVISOR,AUDITOR,ADMINISTRADOR")]
    [ProducesResponseType(typeof(ApiResponse<List<RequestResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] RequestFilterDto filter, CancellationToken cancellationToken)
    {
        var role = User.FindFirst("role")?.Value;
        var (items, totalCount) = await _requestService.GetRequestsAsync(filter, role, null, cancellationToken);
        
        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        return Ok(ApiResponse<List<RequestResponseDto>>.Ok(items, "Solicitudes obtenidas exitosamente."));
    }

    /// <summary>
    /// Obtiene una solicitud de combustible por su ID.
    /// Roles permitidos: SOLICITANTE (dueño), SUPERVISOR, AUDITOR, ADMINISTRADOR.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "SOLICITANTE,SUPERVISOR,AUDITOR,ADMINISTRADOR")]
    [ProducesResponseType(typeof(ApiResponse<RequestResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _requestService.GetRequestByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFound(ApiResponse.Fail("Solicitud no encontrada.", ErrorCodes.ResourceNotFound, "No se encontró la solicitud solicitada.", nameof(id)));
        }

        return Ok(ApiResponse<RequestResponseDto>.Ok(result));
    }

    /// <summary>
    /// Actualiza parcialmente una solicitud (solo permitida en BORRADOR o PENDIENTE).
    /// Roles permitidos: SOLICITANTE (autor), SUPERVISOR, ADMINISTRADOR.
    /// </summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "SOLICITANTE,SUPERVISOR,ADMINISTRADOR")]
    [ProducesResponseType(typeof(ApiResponse<RequestResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRequestDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = User.Identity?.Name ?? "SOLICITANTE";
        var role = User.FindFirst("role")?.Value;
        var result = await _requestService.UpdateRequestAsync(id, dto, currentUserId, role, cancellationToken);
        return Ok(ApiResponse<RequestResponseDto>.Ok(result, "Solicitud actualizada exitosamente."));
    }

    /// <summary>
    /// Aprueba una solicitud de combustible y emite un Ticket digital (Rol: SUPERVISOR, ADMINISTRADOR).
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "SUPERVISOR,ADMINISTRADOR")]
    [ProducesResponseType(typeof(ApiResponse<RequestResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveRequestDto dto, CancellationToken cancellationToken)
    {
        var supervisorId = User.Identity?.Name ?? "SUPERVISOR";
        var result = await _requestService.ApproveRequestAsync(id, dto, supervisorId, cancellationToken);
        return Ok(ApiResponse<RequestResponseDto>.Ok(result, "Solicitud aprobada y ticket emitido exitosamente."));
    }

    /// <summary>
    /// Rechaza una solicitud de combustible (Rol: SUPERVISOR, ADMINISTRADOR).
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "SUPERVISOR,ADMINISTRADOR")]
    [ProducesResponseType(typeof(ApiResponse<RequestResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectRequestDto dto, CancellationToken cancellationToken)
    {
        var supervisorId = User.Identity?.Name ?? "SUPERVISOR";
        var result = await _requestService.RejectRequestAsync(id, dto, supervisorId, cancellationToken);
        return Ok(ApiResponse<RequestResponseDto>.Ok(result, "Solicitud rechazada exitosamente."));
    }

    /// <summary>
    /// Cancela una solicitud de combustible (solo antes de emitir ticket).
    /// Roles permitidos: SOLICITANTE (dueño), SUPERVISOR, ADMINISTRADOR.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "SOLICITANTE,SUPERVISOR,ADMINISTRADOR")]
    [ProducesResponseType(typeof(ApiResponse<RequestResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelRequestDto? dto, CancellationToken cancellationToken)
    {
        var currentUserId = User.Identity?.Name ?? "SOLICITANTE";
        var role = User.FindFirst("role")?.Value;
        var result = await _requestService.CancelRequestAsync(id, dto, currentUserId, role, cancellationToken);
        return Ok(ApiResponse<RequestResponseDto>.Ok(result, "Solicitud cancelada exitosamente."));
    }
}
