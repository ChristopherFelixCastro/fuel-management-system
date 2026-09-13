using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickets.Sandbox.Api.Application.Common;
using Tickets.Sandbox.Api.Application.DTOs.Tickets;
using Tickets.Sandbox.Api.Application.Interfaces;

namespace Tickets.Sandbox.Api.Controllers;

[ApiController]
[Route("api/v1/tickets")]
[Produces("application/json")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    /// <summary>
    /// Lista los tickets con filtros opcionales (number, status, employeeId, vehicleId, from, to).
    /// Roles permitidos: SOLICITANTE (propios), SUPERVISOR, DESPACHADOR, AUDITOR, ADMINISTRADOR.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SOLICITANTE,SUPERVISOR,DESPACHADOR,AUDITOR,ADMINISTRADOR")]
    [ProducesResponseType(typeof(ApiResponse<List<TicketResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] TicketFilterDto filter, CancellationToken cancellationToken)
    {
        var role = User.FindFirst("role")?.Value;
        var (items, totalCount) = await _ticketService.GetTicketsAsync(filter, role, null, cancellationToken);
        
        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        return Ok(ApiResponse<List<TicketResponseDto>>.Ok(items, "Tickets obtenidos exitosamente."));
    }

    /// <summary>
    /// Obtiene el detalle de un ticket por su ID.
    /// Roles permitidos: SOLICITANTE (dueño), SUPERVISOR, DESPACHADOR, AUDITOR, ADMINISTRADOR.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "SOLICITANTE,SUPERVISOR,DESPACHADOR,AUDITOR,ADMINISTRADOR")]
    [ProducesResponseType(typeof(ApiResponse<TicketResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var role = User.FindFirst("role")?.Value;
        var result = await _ticketService.GetTicketByIdAsync(id, role, null, cancellationToken);
        return Ok(ApiResponse<TicketResponseDto>.Ok(result));
    }

    /// <summary>
    /// Descarga el documento oficial en formato PDF del ticket con código QR.
    /// Roles permitidos: SOLICITANTE (dueño), SUPERVISOR, DESPACHADOR, AUDITOR, ADMINISTRADOR.
    /// </summary>
    [HttpGet("{id:guid}/pdf")]
    [Authorize(Roles = "SOLICITANTE,SUPERVISOR,DESPACHADOR,AUDITOR,ADMINISTRADOR")]
    [Produces("application/pdf", "application/json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetPdf(Guid id, CancellationToken cancellationToken)
    {
        var role = User.FindFirst("role")?.Value;
        var pdfBytes = await _ticketService.GetTicketPdfBytesAsync(id, role, null, cancellationToken);
        return File(pdfBytes, "application/pdf", $"Ticket_{id}.pdf");
    }

    /// <summary>
    /// Valida el código QR escaneado en estación de combustible (Rol: DESPACHADOR, ADMINISTRADOR).
    /// No consume el ticket, solo valida y retorna confirmación visual antes del despacho.
    /// </summary>
    [HttpPost("validate")]
    [Authorize(Roles = "DESPACHADOR,ADMINISTRADOR")]
    [ProducesResponseType(typeof(ApiResponse<ValidateTicketResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Validate([FromBody] ValidateTicketRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _ticketService.ValidateTicketQrAsync(dto, cancellationToken);
        return Ok(ApiResponse<ValidateTicketResponseDto>.Ok(result, "Ticket validado y autorizado para despacho."));
    }

    /// <summary>
    /// Anula un ticket activo y libera la reserva de inventario (Rol: SUPERVISOR, ADMINISTRADOR).
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "SUPERVISOR,ADMINISTRADOR")]
    [ProducesResponseType(typeof(ApiResponse<TicketResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelTicketRequestDto dto, CancellationToken cancellationToken)
    {
        var supervisorId = User.Identity?.Name ?? "SUPERVISOR";
        var result = await _ticketService.CancelTicketAsync(id, dto, supervisorId, cancellationToken);
        return Ok(ApiResponse<TicketResponseDto>.Ok(result, "Ticket anulado exitosamente."));
    }
}
