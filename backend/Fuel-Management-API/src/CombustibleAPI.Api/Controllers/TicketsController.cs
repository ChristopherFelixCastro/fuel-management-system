using CombustibleAPI.Api.Middlewares;
using CombustibleAPI.Application.Dtos.Common;
using CombustibleAPI.Application.Dtos.Tickets;
using CombustibleAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CombustibleAPI.Api.Controllers;

/// <summary>
/// Validación de tickets QR (RN-06/RN-07).
/// </summary>
[ApiController]
[Route("tickets")]
[Authorize]
[Produces("application/json")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    /// <summary>
    /// Valida el payload de un ticket o código QR contra el estado oficial de la BD y
    /// devuelve ticket, empleado, vehículo, placa, ficha, combustible, cantidad autorizada, vencimiento y estado.
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ApiResponse<TicketOficialDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    [ProducesResponseType(typeof(ApiErrorResponse), 422)]
    public async Task<IActionResult> Validate([FromBody] ValidateTicketRequestDto request, CancellationToken ct)
    {
        var result = await _ticketService.ValidarAsync(request.QrPayload, ct);
        return Ok(ApiResponse<TicketOficialDto>.Ok(result, HttpContext.GetTraceId()));
    }
}
