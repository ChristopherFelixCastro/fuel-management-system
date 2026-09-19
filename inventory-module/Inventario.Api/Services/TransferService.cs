using Inventario.Api.Data;
using Inventario.Api.Dtos;
using Inventario.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api.Services
{
    public class TransferService : ITransferService
    {
        private readonly InventoryMockContext _db;

        public TransferService(InventoryMockContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<TransferResponse>> CreateAsync(CreateTransferRequest request, Guid usuarioId)
        {
            if (request.SourceTankId == request.DestinationTankId)
                return ApiResponse<TransferResponse>.Fail("No es posible transferir",
                    new() { new ApiError { Code = "VALIDATION_ERROR", Detail = "El tanque origen y destino no pueden ser el mismo." } });

            var origen = await _db.Tanques.FindAsync(request.SourceTankId);
            var destino = await _db.Tanques.FindAsync(request.DestinationTankId);

            if (origen == null || destino == null)
                return ApiResponse<TransferResponse>.Fail("No es posible transferir",
                    new() { new ApiError { Code = "RESOURCE_NOT_FOUND", Detail = "Alguno de los tanques no existe." } });

            if (origen.TipoCombustibleId != destino.TipoCombustibleId)
                return ApiResponse<TransferResponse>.Fail("No es posible transferir",
                    new() { new ApiError { Code = "TRANSFER_FUEL_MISMATCH", Detail = "Los tanques deben tener el mismo tipo de combustible." } });

            if (request.Quantity <= 0 || request.Quantity > origen.StockActual)
                return ApiResponse<TransferResponse>.Fail("No es posible transferir",
                    new() { new ApiError { Code = "INSUFFICIENT_PHYSICAL_STOCK", Detail = "El tanque origen no tiene existencia suficiente." } });

            if (destino.StockActual + request.Quantity > destino.CapacidadMaxima)
                return ApiResponse<TransferResponse>.Fail("No es posible transferir",
                    new() { new ApiError { Code = "DESTINATION_OVER_CAPACITY", Detail = "El tanque destino no tiene capacidad suficiente." } });

            var ahora = DateTime.UtcNow;
            var transferencia = new TransferenciaInventario
            {
                Id = Guid.NewGuid(),
                TanqueOrigenId = origen.Id,
                TanqueDestinoId = destino.Id,
                RegistradaPorUsuarioId = usuarioId,
                Cantidad = request.Quantity,
                FechaTransferencia = request.TransferredAt,
                Observaciones = request.Notes,
                FechaCreacion = ahora
            };

            var saldoAnteriorOrigen = origen.StockActual;
            var saldoAnteriorDestino = destino.StockActual;

            origen.StockActual -= request.Quantity;
            origen.FechaActualizacion = ahora;

            destino.StockActual += request.Quantity;
            destino.FechaActualizacion = ahora;

            var movimientoSalida = new MovimientoInventario
            {
                Id = Guid.NewGuid(),
                TanqueId = origen.Id,
                RegistradoPorUsuarioId = usuarioId,
                TipoMovimiento = "TRANSFERENCIA_SALIDA",
                Cantidad = request.Quantity,
                SaldoAnterior = saldoAnteriorOrigen,
                SaldoPosterior = origen.StockActual,
                TransferenciaId = transferencia.Id,
                FechaMovimiento = ahora,
                Observaciones = request.Notes
            };

            var movimientoEntrada = new MovimientoInventario
            {
                Id = Guid.NewGuid(),
                TanqueId = destino.Id,
                RegistradoPorUsuarioId = usuarioId,
                TipoMovimiento = "TRANSFERENCIA_ENTRADA",
                Cantidad = request.Quantity,
                SaldoAnterior = saldoAnteriorDestino,
                SaldoPosterior = destino.StockActual,
                TransferenciaId = transferencia.Id,
                FechaMovimiento = ahora,
                Observaciones = request.Notes
            };

            _db.Transferencias.Add(transferencia);
            _db.Movimientos.Add(movimientoSalida);
            _db.Movimientos.Add(movimientoEntrada);
            await _db.SaveChangesAsync();

            return ApiResponse<TransferResponse>.Ok(new TransferResponse
            {
                Id = transferencia.Id,
                SourceTankId = origen.Id,
                DestinationTankId = destino.Id,
                Quantity = request.Quantity,
                TransferredAt = transferencia.FechaTransferencia
            }, "Transferencia registrada");
        }

        public async Task<ApiResponse<List<TransferResponse>>> GetAllAsync()
        {
            var lista = await _db.Transferencias.ToListAsync();
            var respuesta = lista.Select(t => new TransferResponse
            {
                Id = t.Id,
                SourceTankId = t.TanqueOrigenId,
                DestinationTankId = t.TanqueDestinoId,
                Quantity = t.Cantidad,
                TransferredAt = t.FechaTransferencia
            }).ToList();

            return ApiResponse<List<TransferResponse>>.Ok(respuesta);
        }
    }
}