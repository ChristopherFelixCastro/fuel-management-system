using Inventario.Api.Dtos;
using Inventario.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Api.Controllers
{
    [ApiController]
    [Route("api/v1/transfers")]
    public class TransfersController : ControllerBase
    {
        private readonly ITransferService _service;

        public TransfersController(ITransferService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTransferRequest request)
        {
            if (!MockRoleHelper.HasRole(Request, "SUPERVISOR"))
                return StatusCode(403, ApiResponse<object>.Fail("Sin permiso",
                    new() { new ApiError { Code = "FORBIDDEN", Detail = "Solo SUPERVISOR puede registrar transferencias." } }));

            var usuarioIdSimulado = Guid.NewGuid();
            var result = await _service.CreateAsync(request, usuarioIdSimulado);
            if (!result.Success) return UnprocessableEntity(result);
            return StatusCode(201, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());
    }
}