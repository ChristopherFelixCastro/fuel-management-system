using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CombustibleAPI.Api.Controllers;

/// <summary>
/// Health check del API para monitoreo y verificación de disponibilidad.
/// </summary>
[ApiController]
[Route("health")]
[AllowAnonymous]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            service = "FuelManagement.Api"
        });
    }
}
