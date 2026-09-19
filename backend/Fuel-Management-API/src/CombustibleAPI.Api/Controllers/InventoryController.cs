using CombustibleAPI.Api.Middlewares;
using CombustibleAPI.Application.Dtos.Common;
using CombustibleAPI.Application.Dtos.Inventory;
using CombustibleAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CombustibleAPI.Api.Controllers;

/// <summary>
/// Consulta de inventario y disponibilidad.
/// </summary>
[ApiController]
[Route("inventory")]
[Authorize]
[Produces("application/json")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ICurrentUserService _currentUser;

    public InventoryController(IInventoryService inventoryService, ICurrentUserService currentUser)
    {
        _inventoryService = inventoryService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Consulta el stock físico, reservado, disponible y tanques compatibles para una estación y tipo de combustible.
    /// </summary>
    [HttpGet("availability/{estacionId:guid}/{tipoCombustibleId:int}")]
    [ProducesResponseType(typeof(ApiResponse<AvailabilityResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetAvailability(
        [FromRoute] Guid estacionId,
        [FromRoute] short tipoCombustibleId,
        CancellationToken ct)
    {
        if (string.Equals(_currentUser.Rol, "DESPACHADOR", StringComparison.OrdinalIgnoreCase) &&
            _currentUser.EstacionId != estacionId)
            return Forbid();
        var result = await _inventoryService.GetAvailabilityAsync(estacionId, tipoCombustibleId, ct);
        return Ok(ApiResponse<AvailabilityResponseDto>.Ok(result, HttpContext.GetTraceId()));
    }
}
