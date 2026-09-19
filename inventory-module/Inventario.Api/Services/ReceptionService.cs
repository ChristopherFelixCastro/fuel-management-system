using Inventario.Api.Data;
using Inventario.Api.Dtos;
using Inventario.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api.Services
{
    public class ReceptionService : IReceptionService
    {
        private readonly InventoryMockContext _db;

        public ReceptionService(InventoryMockContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<ReceptionResponse>> CreateAsync(CreateReceptionRequest request, Guid usuarioId)
        {
            var tanque = await _db.Tanques.FirstOrDefaultAsync(t => t.Id == request.TankId && t.EstacionId == request.StationId);
            if (tanque == null)
                return ApiResponse<ReceptionResponse>.Fail("No es posible registrar la recepción",
                    new() { new ApiError { Code = "INVALID_RECEPTION_TANK", Field = "tankId", Detail = "El tanque no pertenece a la estación indicada." } });

            if (!tanque.Activo)
                return ApiResponse<ReceptionResponse>.Fail("No es posible registrar la recepción",
                    new() { new ApiError { Code = "INVALID_RECEPTION_TANK", Field = "tankId", Detail = "El tanque está inactivo." } });

            if (request.Volume <= 0)
                return ApiResponse<ReceptionResponse>.Fail("No es posible registrar la recepción",
                    new() { new ApiError { Code = "VALIDATION_ERROR", Field = "volume", Detail = "El volumen debe ser mayor a cero." } });

            if (tanque.StockActual + request.Volume > tanque.CapacidadMaxima)
                return ApiResponse<ReceptionResponse>.Fail("No es posible registrar la recepción",
                    new() { new ApiError { Code = "TANK_OVER_CAPACITY", Field = "volume", Detail = "El volumen recibido excede la capacidad del tanque." } });

            var facturaDuplicada = await _db.Recepciones.AnyAsync(r => r.ProveedorId == request.SupplierId && r.NumeroFactura == request.InvoiceNumber);
            if (facturaDuplicada)
                return ApiResponse<ReceptionResponse>.Fail("No es posible registrar la recepción",
                    new() { new ApiError { Code = "INVOICE_ALREADY_REGISTERED", Field = "invoiceNumber", Detail = "Ya existe una recepción con esta factura para este proveedor." } });

            var saldoAnterior = tanque.StockActual;
            var ahora = DateTime.UtcNow;

            var recepcion = new RecepcionCombustible
            {
                Id = Guid.NewGuid(),
                ProveedorId = request.SupplierId,
                TanqueId = request.TankId,
                RegistradaPorUsuarioId = usuarioId,
                NumeroFactura = request.InvoiceNumber,
                CantidadRecibida = request.Volume,
                FechaRecepcion = request.ReceivedAt,
                Observaciones = request.Notes,
                FechaCreacion = ahora
            };

            tanque.StockActual += request.Volume;
            tanque.FechaActualizacion = ahora;

            var movimiento = new MovimientoInventario
            {
                Id = Guid.NewGuid(),
                TanqueId = tanque.Id,
                RegistradoPorUsuarioId = usuarioId,
                TipoMovimiento = "RECEPCION",
                Cantidad = request.Volume,
                SaldoAnterior = saldoAnterior,
                SaldoPosterior = tanque.StockActual,
                RecepcionId = recepcion.Id,
                FechaMovimiento = ahora,
                Observaciones = request.Notes
            };

            _db.Recepciones.Add(recepcion);
            _db.Movimientos.Add(movimiento);
            await _db.SaveChangesAsync();

            return ApiResponse<ReceptionResponse>.Ok(new ReceptionResponse
            {
                Id = recepcion.Id,
                SupplierId = recepcion.ProveedorId,
                TankId = recepcion.TanqueId,
                InvoiceNumber = recepcion.NumeroFactura,
                Volume = recepcion.CantidadRecibida,
                ReceivedAt = recepcion.FechaRecepcion,
                TankQuantityAfter = tanque.StockActual
            }, "Recepción registrada");
        }

        public async Task<ApiResponse<List<ReceptionResponse>>> GetAllAsync()
        {
            var lista = await _db.Recepciones.ToListAsync();
            var respuesta = lista.Select(r => new ReceptionResponse
            {
                Id = r.Id,
                SupplierId = r.ProveedorId,
                TankId = r.TanqueId,
                InvoiceNumber = r.NumeroFactura,
                Volume = r.CantidadRecibida,
                ReceivedAt = r.FechaRecepcion,
                TankQuantityAfter = 0
            }).ToList();

            return ApiResponse<List<ReceptionResponse>>.Ok(respuesta);
        }
    }
}