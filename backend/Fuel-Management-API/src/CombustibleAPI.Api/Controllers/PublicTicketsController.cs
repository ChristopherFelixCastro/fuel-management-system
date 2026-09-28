using CombustibleAPI.Api.Middlewares;
using CombustibleAPI.Application.Dtos.Common;
using CombustibleAPI.Application.Dtos.PublicTickets;
using CombustibleAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CombustibleAPI.Api.Controllers;

[ApiController]
[Route("public/tickets")]
[AllowAnonymous]
public class PublicTicketsController : ControllerBase
{
    private readonly IPublicTicketService _publicTicketService;

    public PublicTicketsController(IPublicTicketService publicTicketService)
    {
        _publicTicketService = publicTicketService;
    }

    /// <summary>
    /// Consulta los datos públicos y estado en tiempo real de un ticket mediante su token opaco firmado.
    /// No expone IDs internos, tokens de autenticación ni datos de auditoría.
    /// </summary>
    [HttpGet("{token}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<PublicTicketDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ObtenerTicketPublico(
        string token,
        CancellationToken ct)
    {
        var result = await _publicTicketService.ObtenerTicketPublicoAsync(token, ct);
        return Ok(ApiResponse<PublicTicketDto>.Ok(result, HttpContext.GetTraceId()));
    }

    /// <summary>
    /// Descarga la imagen oficial en formato PNG del código QR del ticket protegido por su token opaco.
    /// </summary>
    [HttpGet("{token}/qr")]
    [Produces("image/png")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ObtenerQrPngPublico(
        string token,
        CancellationToken ct)
    {
        var png = await _publicTicketService.ObtenerQrPngPublicoAsync(token, ct);
        return File(png, "image/png", "ticket-qr.png");
    }
}
