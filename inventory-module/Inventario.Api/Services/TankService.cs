using Inventario.Api.Data;
using Inventario.Api.Dtos;
using Inventario.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api.Services
{
    public class TankService : ITankService
    {
        private readonly InventoryMockContext _db;

        public TankService(InventoryMockContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<TankResponse>> CreateAsync(CreateTankRequest request)
        {
            var estacionExiste = await _db.Estaciones.AnyAsync(e => e.Id == request.StationId);
            if (!estacionExiste)
                return ApiResponse<TankResponse>.Fail("No es posible crear el tanque",
                    new() { new ApiError { Code = "RESOURCE_NOT_FOUND", Field = "stationId", Detail = "La estación no existe." } });

            var codigoDuplicado = await _db.Tanques.AnyAsync(t => t.Codigo == request.Code);
            if (codigoDuplicado)
                return ApiResponse<TankResponse>.Fail("No es posible crear el tanque",
                    new() { new ApiError { Code = "DUPLICATE_TANK_CODE", Field = "code", Detail = "El código de tanque ya existe." } });

            if (request.CriticalLevel > request.MaxCapacity || request.CurrentQuantity > request.MaxCapacity || request.CurrentQuantity < 0)
                return ApiResponse<TankResponse>.Fail("No es posible crear el tanque",
                    new() { new ApiError { Code = "TANK_CAPACITY_EXCEEDED", Detail = "La existencia o el nivel crítico exceden la capacidad." } });

            var ahora = DateTime.UtcNow;
            var tanque = new Tanque
            {
                Id = Guid.NewGuid(),
                EstacionId = request.StationId,
                TipoCombustibleId = request.FuelTypeId,
                Codigo = request.Code,
                Nombre = request.Code,
                CapacidadMaxima = request.MaxCapacity,
                StockActual = request.CurrentQuantity,
                NivelCritico = request.CriticalLevel,
                Activo = request.IsActive ?? true,
                FechaCreacion = ahora,
                FechaActualizacion = ahora
            };

            _db.Tanques.Add(tanque);
            await _db.SaveChangesAsync();

            return ApiResponse<TankResponse>.Ok(ToResponse(tanque), "Tanque creado");
        }

        public async Task<ApiResponse<List<TankResponse>>> GetAllAsync()
        {
            var lista = await _db.Tanques.ToListAsync();
            return ApiResponse<List<TankResponse>>.Ok(lista.Select(ToResponse).ToList());
        }

        public async Task<ApiResponse<TankResponse>> GetByIdAsync(Guid id)
        {
            var tanque = await _db.Tanques.FindAsync(id);
            if (tanque == null)
                return ApiResponse<TankResponse>.Fail("Tanque no encontrado",
                    new() { new ApiError { Code = "RESOURCE_NOT_FOUND", Detail = "No existe un tanque con ese id." } });

            return ApiResponse<TankResponse>.Ok(ToResponse(tanque));
        }

        public async Task<ApiResponse<TankResponse>> UpdateAsync(Guid id, UpdateTankRequest request)
        {
            var tanque = await _db.Tanques.FindAsync(id);
            if (tanque == null)
                return ApiResponse<TankResponse>.Fail("Tanque no encontrado",
                    new() { new ApiError { Code = "RESOURCE_NOT_FOUND", Detail = "No existe un tanque con ese id." } });

            if (request.MaxCapacity.HasValue) tanque.CapacidadMaxima = request.MaxCapacity.Value;
            if (request.CriticalLevel.HasValue) tanque.NivelCritico = request.CriticalLevel.Value;
            tanque.FechaActualizacion = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return ApiResponse<TankResponse>.Ok(ToResponse(tanque), "Tanque actualizado");
        }

        public async Task<ApiResponse<TankResponse>> DeactivateAsync(Guid id)
        {
            var tanque = await _db.Tanques.FindAsync(id);
            if (tanque == null)
                return ApiResponse<TankResponse>.Fail("Tanque no encontrado",
                    new() { new ApiError { Code = "RESOURCE_NOT_FOUND", Detail = "No existe un tanque con ese id." } });

            tanque.Activo = false;
            tanque.FechaActualizacion = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return ApiResponse<TankResponse>.Ok(ToResponse(tanque), "Tanque desactivado");
        }

        private static TankResponse ToResponse(Tanque t) => new()
        {
            Id = t.Id,
            StationId = t.EstacionId,
            Code = t.Codigo,
            FuelTypeId = t.TipoCombustibleId,
            MaxCapacity = t.CapacidadMaxima,
            CurrentQuantity = t.StockActual,
            CriticalLevel = t.NivelCritico,
            IsActive = t.Activo
        };
    }
}