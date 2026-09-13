using FuelManagement.Closures.Dtos;
using FuelManagement.Closures.Services;
using FuelManagement.Shared.Contracts;
using FuelManagement.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace FuelManagement.Api.Controllers;

/// <summary>
/// Cierres diarios — calculo, creacion, aprobacion, rechazo y PDF.
/// Toda la logica de negocio y funciones PostgreSQL son delegadas a IClosureService.
/// </summary>
[ApiController]
[Route("api/v1/closures")]
[Produces("application/json")]
public class ClosuresController : ControllerBase
{
    private readonly IClosureService _closureService;
    private readonly ICurrentUserService _currentUserService;

    public ClosuresController(IClosureService closureService, ICurrentUserService currentUserService)
    {
        _closureService = closureService;
        _currentUserService = currentUserService;
    }

    // --- GET /api/v1/closures -------------------------------------------
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ClosureDto>>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateOnly? fechaDesde,
        [FromQuery] DateOnly? fechaHasta,
        [FromQuery] Guid? tanqueId,
        [FromQuery] string? estado,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _closureService.GetAllAsync(fechaDesde, fechaHasta, tanqueId, estado, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<ClosureDto>>.Ok(result));
    }

    // --- GET /api/v1/closures/{id} --------------------------------------
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ClosureDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _closureService.GetByIdAsync(id, ct);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail($"No se encontró el cierre con ID {id}"));

        return Ok(ApiResponse<ClosureDto>.Ok(result));
    }

    // --- GET /api/v1/closures/preview ----------------------------------
    [HttpGet("preview")]
    [ProducesResponseType(typeof(ApiResponse<ClosurePreviewDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Preview(
        [FromQuery] Guid tanqueId,
        [FromQuery] DateOnly fecha,
        CancellationToken ct = default)
    {
        var result = await _closureService.GetPreviewAsync(tanqueId, fecha, ct);
        return Ok(ApiResponse<ClosurePreviewDto>.Ok(result));
    }

    // --- POST /api/v1/closures -----------------------------------------
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateClosureRequest request, CancellationToken ct = default)
    {
        var newId = await _closureService.CreateAsync(request, _currentUserService.UserId, ct);
        return StatusCode(201, ApiResponse<Guid>.Ok(newId, "Cierre creado exitosamente"));
    }

    // --- POST /api/v1/closures/{id}/approve ----------------------------
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct = default)
    {
        await _closureService.ApproveAsync(id, _currentUserService.UserId, ct);
        return Ok(ApiResponse<string>.Ok("Aprobado", "Cierre aprobado exitosamente"));
    }

    // --- POST /api/v1/closures/{id}/reject -----------------------------
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectClosureRequest request, CancellationToken ct = default)
    {
        await _closureService.RejectAsync(id, _currentUserService.UserId, request.MotivoRechazo, ct);
        return Ok(ApiResponse<string>.Ok("Rechazado", "Cierre rechazado exitosamente"));
    }

    // --- GET /api/v1/closures/{id}/pdf ---------------------------------
    [HttpGet("{id:guid}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), 200, "application/pdf")]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPdf(Guid id, CancellationToken ct = default)
    {
        var pdfBytes = await _closureService.GeneratePdfAsync(id, ct);
        return File(pdfBytes, "application/pdf", $"cierre-{id}.pdf");
    }
}
