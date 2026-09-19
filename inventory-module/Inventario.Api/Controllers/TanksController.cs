using Inventario.Api.Dtos;
using Inventario.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Api.Controllers
{
    [ApiController]
    [Route("api/v1/tanks")]
    public class TanksController : ControllerBase
    {
        private readonly ITankService _service;

        public TanksController(ITankService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTankRequest request)
        {
            if (!MockRoleHelper.HasRole(Request, "ADMINISTRADOR"))
                return StatusCode(403, ApiResponse<object>.Fail("Sin permiso",
                    new() { new ApiError { Code = "FORBIDDEN", Detail = "Solo ADMINISTRADOR puede crear tanques." } }));

            var result = await _service.CreateAsync(request);
            if (!result.Success) return BadRequest(result);
            return StatusCode(201, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTankRequest request)
        {
            if (!MockRoleHelper.HasRole(Request, "ADMINISTRADOR"))
                return StatusCode(403, ApiResponse<object>.Fail("Sin permiso",
                    new() { new ApiError { Code = "FORBIDDEN", Detail = "Solo ADMINISTRADOR puede editar tanques." } }));

            var result = await _service.UpdateAsync(id, request);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPatch("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            if (!MockRoleHelper.HasRole(Request, "ADMINISTRADOR"))
                return StatusCode(403, ApiResponse<object>.Fail("Sin permiso",
                    new() { new ApiError { Code = "FORBIDDEN", Detail = "Solo ADMINISTRADOR puede desactivar tanques." } }));

            var result = await _service.DeactivateAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }
    }
}