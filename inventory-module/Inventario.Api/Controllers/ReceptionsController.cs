using Inventario.Api.Dtos;
using Inventario.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Api.Controllers
{
    [ApiController]
    [Route("api/v1/receptions")]
    public class ReceptionsController : ControllerBase
    {
        private readonly IReceptionService _service;

        public ReceptionsController(IReceptionService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReceptionRequest request)
        {
            if (!MockRoleHelper.HasRole(Request, "SUPERVISOR"))
                return StatusCode(403, ApiResponse<object>.Fail("Sin permiso",
                    new() { new ApiError { Code = "FORBIDDEN", Detail = "Solo SUPERVISOR puede registrar recepciones." } }));

            var usuarioIdSimulado = Guid.NewGuid(); // hasta que exista auth real, se simula el usuario actual
            var result = await _service.CreateAsync(request, usuarioIdSimulado);
            if (!result.Success) return UnprocessableEntity(result);
            return StatusCode(201, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());
    }
}