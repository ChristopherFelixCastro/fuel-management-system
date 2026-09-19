using CombustibleAPI.Application.Dtos.Common;
using CombustibleAPI.Application.Dtos.Inventory;
using CombustibleAPI.Application.Dtos.Dispatches;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CombustibleAPI.Api.Controllers;

[ApiController, Authorize, Produces("application/json")]
public sealed class InventoryOperationsController(IInventoryOperationsService operations, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("movements"), Authorize(Roles = nameof(RolUsuario.SUPERVISOR) + "," + nameof(RolUsuario.ADMINISTRADOR) + "," + nameof(RolUsuario.AUDITOR))]
    public async Task<IActionResult> Movements([FromQuery] InventoryQueryDto filter, CancellationToken ct) => Ok(ApiResponse<PaginatedList<InventoryMovementDto>>.Ok(await operations.GetMovementsAsync(filter, null, ct), HttpContext.TraceIdentifier));

    [HttpGet("adjustments"), Authorize(Roles = nameof(RolUsuario.SUPERVISOR) + "," + nameof(RolUsuario.ADMINISTRADOR) + "," + nameof(RolUsuario.AUDITOR))]
    public async Task<IActionResult> Adjustments([FromQuery] InventoryQueryDto filter, CancellationToken ct) => Ok(ApiResponse<PaginatedList<AdjustmentDto>>.Ok(await operations.GetAdjustmentsAsync(filter, null, null, ct), HttpContext.TraceIdentifier));

    [HttpGet("adjustments/mine"), Authorize(Roles = nameof(RolUsuario.DESPACHADOR))]
    public async Task<IActionResult> MyAdjustments([FromQuery] InventoryQueryDto filter, CancellationToken ct) { var user=currentUser.UsuarioId ?? throw ApiException.Unauthorized(); var station=currentUser.EstacionId ?? throw ApiException.BusinessRule("DESPACHADOR_SIN_ESTACION","El usuario no tiene estación asignada."); return Ok(ApiResponse<PaginatedList<AdjustmentDto>>.Ok(await operations.GetAdjustmentsAsync(filter,user,station,ct),HttpContext.TraceIdentifier)); }
    [HttpPost("receptions"), Authorize(Roles = nameof(RolUsuario.SUPERVISOR) + "," + nameof(RolUsuario.ADMINISTRADOR))]
    public async Task<IActionResult> Reception(CreateReceptionRequestDto request, CancellationToken ct) => Ok(ApiResponse<InventoryOperationResultDto>.Ok(await operations.CreateReceptionAsync(request, currentUser.UsuarioId ?? throw ApiException.Unauthorized(), ct), HttpContext.TraceIdentifier));

    [HttpPost("transfers"), Authorize(Roles = nameof(RolUsuario.SUPERVISOR) + "," + nameof(RolUsuario.ADMINISTRADOR))]
    public async Task<IActionResult> Transfer(CreateTransferRequestDto request, CancellationToken ct) => Ok(ApiResponse<InventoryOperationResultDto>.Ok(await operations.CreateTransferAsync(request, currentUser.UsuarioId ?? throw ApiException.Unauthorized(), ct), HttpContext.TraceIdentifier));

    [HttpPost("adjustments"), Authorize(Roles = nameof(RolUsuario.DESPACHADOR))]
    public async Task<IActionResult> ReportAdjustment(ReportAdjustmentRequestDto request, CancellationToken ct)
    {
        var user = currentUser.UsuarioId ?? throw ApiException.Unauthorized(); var station = currentUser.EstacionId ?? throw ApiException.BusinessRule("DESPACHADOR_SIN_ESTACION", "El usuario autenticado no tiene estación asignada.");
        return Ok(ApiResponse<InventoryOperationResultDto>.Ok(await operations.ReportAdjustmentAsync(request, user, station, ct), HttpContext.TraceIdentifier));
    }

    [HttpPut("adjustments/{id:guid}/approve"), Authorize(Roles = nameof(RolUsuario.SUPERVISOR) + "," + nameof(RolUsuario.ADMINISTRADOR))]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct) { await operations.ApproveAdjustmentAsync(id, currentUser.UsuarioId ?? throw ApiException.Unauthorized(), ct); return Ok(ApiResponse<object>.Ok(new { message = "Ajuste aprobado." }, HttpContext.TraceIdentifier)); }

    [HttpPut("adjustments/{id:guid}/reject"), Authorize(Roles = nameof(RolUsuario.SUPERVISOR) + "," + nameof(RolUsuario.ADMINISTRADOR))]
    public async Task<IActionResult> Reject(Guid id, RejectAdjustmentRequestDto request, CancellationToken ct) { await operations.RejectAdjustmentAsync(id, currentUser.UsuarioId ?? throw ApiException.Unauthorized(), request.Motivo, ct); return Ok(ApiResponse<object>.Ok(new { message = "Ajuste rechazado." }, HttpContext.TraceIdentifier)); }
}
