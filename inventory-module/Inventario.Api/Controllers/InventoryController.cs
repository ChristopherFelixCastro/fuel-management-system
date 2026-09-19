using Inventario.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Api.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _service;

        public InventoryController(IInventoryService service)
        {
            _service = service;
        }

        [HttpGet("inventory")]
        public async Task<IActionResult> GetInventory() => Ok(await _service.GetInventoryAsync());

        [HttpGet("inventory-movements")]
        public async Task<IActionResult> GetMovements([FromQuery] Guid? tankId, [FromQuery] string? type)
            => Ok(await _service.GetMovementsAsync(tankId, type));
    }
}