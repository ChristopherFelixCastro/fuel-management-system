using Inventario.Api.Dtos;
using Inventario.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Api.Controllers
{
    [ApiController]
    [Route("api/v1/stations")]
    public class StationsController : ControllerBase
    {
        private readonly IStationService _service;

        public StationsController(IStationService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStationRequest request)
        {
            if (!MockRoleHelper.HasRole(Request, "ADMINISTRADOR"))
                return StatusCode(403, ApiResponse<object>.Fail("Sin permiso",
                    new() { new ApiError { Code = "FORBIDDEN", Detail = "Solo ADMINISTRADOR puede crear estaciones." } }));

            var result = await _service.CreateAsync(request);
            if (!result.Success) return BadRequest(result);
            return StatusCode(201, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStationRequest request)
        {
            if (!MockRoleHelper.HasRole(Request, "ADMINISTRADOR"))
                return StatusCode(403, ApiResponse<object>.Fail("Sin permiso",
                    new() { new ApiError { Code = "FORBIDDEN", Detail = "Solo ADMINISTRADOR puede editar estaciones." } }));

            var result = await _service.UpdateAsync(id, request);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPatch("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            if (!MockRoleHelper.HasRole(Request, "ADMINISTRADOR"))
                return StatusCode(403, ApiResponse<object>.Fail("Sin permiso",
                    new() { new ApiError { Code = "FORBIDDEN", Detail = "Solo ADMINISTRADOR puede desactivar estaciones." } }));

            var result = await _service.DeactivateAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }
    }
}