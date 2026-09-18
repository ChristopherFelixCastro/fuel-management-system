using CombustibleAPI.Api.Middlewares;
using CombustibleAPI.Application.Dtos.Common;
using CombustibleAPI.Application.Dtos.Dashboard;
using CombustibleAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CombustibleAPI.Api.Controllers;

[ApiController]
[Route("dashboard")]
[Authorize(Roles = "ADMINISTRADOR,SUPERVISOR,AUDITOR")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<DashboardSummaryDto>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] DashboardFilterDto filter,
        CancellationToken ct)
    {
        var result = await _dashboardService
            .GetSummaryAsync(filter, ct);

        return Ok(
            ApiResponse<DashboardSummaryDto>.Ok(
                result,
                HttpContext.GetTraceId()));
    }
}