using Inventario.Api.Dtos;
using Inventario.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Api.Controllers
{
    [ApiController]
    [Route("api/v1/adjustments")]
    public class AdjustmentsController : ControllerBase
    {
        private readonly IAdjustmentService _service;

        public AdjustmentsController(IAdjustmentService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Report([FromBody] ReportAdjustmentRequest request)
        {
            if (!MockRoleHelper.HasRole(Request, "DESPACHADOR"))
                return StatusCode(403, ApiResponse<object>.Fail("Sin permiso",
                    new() { new ApiError { Code = "FORBIDDEN", Detail = "Solo DESPACHADOR puede reportar ajustes." } }));

            var usuarioIdSimulado = Guid.NewGuid();
            var result = await _service.ReportAsync(request, usuarioIdSimulado);
            if (!result.Success) return BadRequest(result);
            return StatusCode(201, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(Guid id)
        {
            if (!MockRoleHelper.HasRole(Request, "SUPERVISOR", "ADMINISTRADOR"))
                return StatusCode(403, ApiResponse<object>.Fail("Sin permiso",
                    new() { new ApiError { Code = "FORBIDDEN", Detail = "Solo SUPERVISOR/ADMINISTRADOR puede aprobar ajustes." } }));

            var usuarioIdSimulado = Guid.NewGuid();
            var result = await _service.ApproveAsync(id, usuarioIdSimulado);
            if (!result.Success) return UnprocessableEntity(result);
            return Ok(result);
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectAdjustmentRequest request)
        {
            if (!MockRoleHelper.HasRole(Request, "SUPERVISOR", "ADMINISTRADOR"))
                return StatusCode(403, ApiResponse<object>.Fail("Sin permiso",
                    new() { new ApiError { Code = "FORBIDDEN", Detail = "Solo SUPERVISOR/ADMINISTRADOR puede rechazar ajustes." } }));

            var usuarioIdSimulado = Guid.NewGuid();
            var result = await _service.RejectAsync(id, request, usuarioIdSimulado);
            if (!result.Success) return UnprocessableEntity(result);
            return Ok(result);
        }
    }
}