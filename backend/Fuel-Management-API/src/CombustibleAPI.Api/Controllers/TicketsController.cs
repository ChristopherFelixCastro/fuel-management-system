using CombustibleAPI.Api.Middlewares;
using CombustibleAPI.Application.Dtos.Common;
using CombustibleAPI.Application.Dtos.Tickets;
using CombustibleAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CombustibleAPI.Api.Controllers;

/// <summary>
/// Emisión visual y validación de tickets QR.
/// </summary>
[ApiController]
[Route("tickets")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketsController(
        ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    /// <summary>
    /// Obtiene la imagen PNG del QR correspondiente
    /// a un ticket emitido.
    /// </summary>
    [HttpGet("{ticketId:guid}/qr")]
    [Authorize(
        Roles = "SOLICITANTE,SUPERVISOR,ADMINISTRADOR")]
    [Produces("image/png")]
    [ProducesResponseType(200)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        401)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        403)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        404)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        422)]
    public async Task<IActionResult> GetQr(
        Guid ticketId,
        CancellationToken ct)
    {
        var png =
            await _ticketService.ObtenerQrPngAsync(
                ticketId,
                ct);

        return File(
            png,
            "image/png",
            $"ticket-{ticketId}.png");
    }

    /// <summary>
    /// Valida un QR escaneado contra el estado
    /// oficial del ticket en la base de datos.
    ///
    /// Devuelve los datos oficiales que utilizará
    /// la PWA antes de confirmar el despacho.
    /// </summary>
    [HttpPost("validate")]
    [Authorize(Roles = "DESPACHADOR")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(ApiResponse<TicketOficialDto>),
        200)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        400)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        401)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        403)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        404)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        422)]
    public async Task<IActionResult> Validate(
        [FromBody] ValidateTicketRequestDto request,
        CancellationToken ct)
    {
        var result =
            await _ticketService.ValidarAsync(
                request.QrPayload,
                ct);

        return Ok(
            ApiResponse<TicketOficialDto>.Ok(
                result,
                HttpContext.GetTraceId()));
    }
}