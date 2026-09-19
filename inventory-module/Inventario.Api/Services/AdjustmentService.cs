using Inventario.Api.Data;
using Inventario.Api.Dtos;
using Inventario.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api.Services
{
    public class AdjustmentService : IAdjustmentService
    {
        private readonly InventoryMockContext _db;

        public AdjustmentService(InventoryMockContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<AdjustmentResponse>> ReportAsync(ReportAdjustmentRequest request, Guid usuarioId)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return ApiResponse<AdjustmentResponse>.Fail("No es posible reportar el ajuste",
                    new() { new ApiError { Code = "VALIDATION_ERROR", Field = "reason", Detail = "El motivo es obligatorio." } });

            if (request.PhysicalQuantity < 0)
                return ApiResponse<AdjustmentResponse>.Fail("No es posible reportar el ajuste",
                    new() { new ApiError { Code = "PHYSICAL_QUANTITY_INVALID", Field = "physicalQuantity", Detail = "La cantidad física no puede ser negativa." } });

            var ajuste = new AjusteInventario
            {
                Id = Guid.NewGuid(),
                TanqueId = request.TankId,
                ReportadoPorUsuarioId = usuarioId,
                TipoAjuste = "PENDIENTE_DE_DECISION",
                Cantidad = request.PhysicalQuantity,
                Motivo = request.Reason,
                Estado = "PENDIENTE",
                FechaReporte = DateTime.UtcNow
            };

            _db.Ajustes.Add(ajuste);
            await _db.SaveChangesAsync();

            return ApiResponse<AdjustmentResponse>.Ok(ToResponse(ajuste), "Ajuste reportado");
        }

        public async Task<ApiResponse<List<AdjustmentResponse>>> GetAllAsync()
        {
            var lista = await _db.Ajustes.ToListAsync();
            return ApiResponse<List<AdjustmentResponse>>.Ok(lista.Select(ToResponse).ToList());
        }

        public async Task<ApiResponse<AdjustmentResponse>> ApproveAsync(Guid id, Guid usuarioId)
        {
            var ajuste = await _db.Ajustes.FindAsync(id);
            if (ajuste == null)
                return ApiResponse<AdjustmentResponse>.Fail("Ajuste no encontrado",
                    new() { new ApiError { Code = "RESOURCE_NOT_FOUND", Detail = "No existe un ajuste con ese id." } });

            if (ajuste.Estado != "PENDIENTE")
                return ApiResponse<AdjustmentResponse>.Fail("No es posible aprobar",
                    new() { new ApiError { Code = "ADJUSTMENT_NOT_PENDING", Detail = "El ajuste ya fue decidido." } });

            var tanque = await _db.Tanques.FindAsync(ajuste.TanqueId);
            if (tanque == null)
                return ApiResponse<AdjustmentResponse>.Fail("No es posible aprobar",
                    new() { new ApiError { Code = "RESOURCE_NOT_FOUND", Detail = "El tanque del ajuste no existe." } });

            var saldoAnterior = tanque.StockActual;
            var diferencia = ajuste.Cantidad - tanque.StockActual;
            var tipoMovimiento = diferencia >= 0 ? "AJUSTE_POSITIVO" : "AJUSTE_NEGATIVO";

            var ahora = DateTime.UtcNow;
            tanque.StockActual = ajuste.Cantidad;
            tanque.FechaActualizacion = ahora;

            ajuste.Estado = "APROBADO";
            ajuste.TipoAjuste = tipoMovimiento;
            ajuste.RevisadoPorUsuarioId = usuarioId;
            ajuste.FechaRevision = ahora;

            var movimiento = new MovimientoInventario
            {
                Id = Guid.NewGuid(),
                TanqueId = tanque.Id,
                RegistradoPorUsuarioId = usuarioId,
                TipoMovimiento = tipoMovimiento,
                Cantidad = Math.Abs(diferencia),
                SaldoAnterior = saldoAnterior,
                SaldoPosterior = tanque.StockActual,
                AjusteId = ajuste.Id,
                FechaMovimiento = ahora,
                Observaciones = ajuste.Motivo
            };

            _db.Movimientos.Add(movimiento);
            await _db.SaveChangesAsync();

            return ApiResponse<AdjustmentResponse>.Ok(ToResponse(ajuste), "Ajuste aprobado");
        }

        public async Task<ApiResponse<AdjustmentResponse>> RejectAsync(Guid id, RejectAdjustmentRequest request, Guid usuarioId)
        {
            if (string.IsNullOrWhiteSpace(request.RejectionReason))
                return ApiResponse<AdjustmentResponse>.Fail("No es posible rechazar",
                    new() { new ApiError { Code = "ADJUSTMENT_REJECTION_REASON_REQUIRED", Field = "rejectionReason", Detail = "El motivo de rechazo es obligatorio." } });

            var ajuste = await _db.Ajustes.FindAsync(id);
            if (ajuste == null)
                return ApiResponse<AdjustmentResponse>.Fail("Ajuste no encontrado",
                    new() { new ApiError { Code = "RESOURCE_NOT_FOUND", Detail = "No existe un ajuste con ese id." } });

            if (ajuste.Estado != "PENDIENTE")
                return ApiResponse<AdjustmentResponse>.Fail("No es posible rechazar",
                    new() { new ApiError { Code = "ADJUSTMENT_NOT_PENDING", Detail = "El ajuste ya fue decidido." } });

            ajuste.Estado = "RECHAZADO";
            ajuste.MotivoRechazo = request.RejectionReason;
            ajuste.RevisadoPorUsuarioId = usuarioId;
            ajuste.FechaRevision = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return ApiResponse<AdjustmentResponse>.Ok(ToResponse(ajuste), "Ajuste rechazado");
        }

        private static AdjustmentResponse ToResponse(AjusteInventario a) => new()
        {
            Id = a.Id,
            TankId = a.TanqueId,
            PhysicalQuantity = a.Cantidad,
            Status = a.Estado,
            Reason = a.Motivo
        };
    }
}