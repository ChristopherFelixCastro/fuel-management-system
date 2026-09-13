using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickets.Sandbox.Api.Application.Common;
using Tickets.Sandbox.Api.Application.DTOs.Tickets;
using Tickets.Sandbox.Api.Application.Interfaces;

namespace Tickets.Sandbox.Api.Controllers;

[ApiController]
[Route("api/v1/internal/tickets")]
[Produces("application/json")]
public class InternalTicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public InternalTicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    /// <summary>
    /// Endpoint interno para confirmar el consumo de un ticket dentro de la transacción atómica de despacho.
    /// Invocado exclusivamente por el módulo de Despacho (mantenido por Christopher / José Enrique).
    /// Restricción: Rol de servicio SISTEMA_DESPACHO o ADMINISTRADOR.
    /// </summary>
    [HttpPost("{id:guid}/consume")]
    [Authorize(Roles = "SISTEMA_DESPACHO,ADMINISTRADOR")]
    [ProducesResponseType(typeof(ApiResponse<TicketResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Consume(Guid id, [FromBody] ConsumeTicketRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _ticketService.ConsumeTicketAsync(id, dto, cancellationToken);
        return Ok(ApiResponse<TicketResponseDto>.Ok(result, "Ticket consumido exitosamente."));
    }
}
