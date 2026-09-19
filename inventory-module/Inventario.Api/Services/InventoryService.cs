using Inventario.Api.Data;
using Inventario.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly InventoryMockContext _db;

        public InventoryService(InventoryMockContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<List<InventoryItemResponse>>> GetInventoryAsync()
        {
            var tanques = await _db.Tanques.ToListAsync();
            var reservadoSimulado = 0m; // placeholder hasta integrar con el módulo de tickets de Gabriel

            var respuesta = tanques.Select(t => new InventoryItemResponse
            {
                TankId = t.Id,
                StationId = t.EstacionId,
                PhysicalQuantity = t.StockActual,
                ReservedQuantity = reservadoSimulado,
                AvailableQuantity = t.StockActual - reservadoSimulado,
                CriticalLevel = t.NivelCritico,
                LastUpdatedAt = t.FechaActualizacion
            }).ToList();

            return ApiResponse<List<InventoryItemResponse>>.Ok(respuesta);
        }

        public async Task<ApiResponse<List<InventoryMovementResponse>>> GetMovementsAsync(Guid? tankId, string? type)
        {
            var query = _db.Movimientos.AsQueryable();

            if (tankId.HasValue)
                query = query.Where(m => m.TanqueId == tankId.Value);

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(m => m.TipoMovimiento == type);

            var lista = await query.OrderByDescending(m => m.FechaMovimiento).ToListAsync();

            var respuesta = lista.Select(m => new InventoryMovementResponse
            {
                Id = m.Id,
                TankId = m.TanqueId,
                Type = m.TipoMovimiento,
                Quantity = m.Cantidad,
                PreviousBalance = m.SaldoAnterior,
                NewBalance = m.SaldoPosterior,
                OccurredAt = m.FechaMovimiento,
                Notes = m.Observaciones
            }).ToList();

            return ApiResponse<List<InventoryMovementResponse>>.Ok(respuesta);
        }
    }
}