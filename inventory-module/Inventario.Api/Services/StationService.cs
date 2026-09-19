using Inventario.Api.Data;
using Inventario.Api.Dtos;
using Inventario.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api.Services
{
    public class StationService : IStationService
    {
        private readonly InventoryMockContext _db;

        public StationService(InventoryMockContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<StationResponse>> CreateAsync(CreateStationRequest request)
        {
            var duplicada = await _db.Estaciones.AnyAsync(e => e.Codigo == request.Code);
            if (duplicada)
            {
                return ApiResponse<StationResponse>.Fail("No es posible crear la estación",
                    new() { new ApiError { Code = "DUPLICATE_TANK_CODE", Field = "code", Detail = "El código de estación ya existe." } });
            }

            var ahora = DateTime.UtcNow;
            var estacion = new Estacion
            {
                Id = Guid.NewGuid(),
                Codigo = request.Code,
                Nombre = request.Name,
                Ubicacion = request.Address,
                Activo = request.IsActive ?? true,
                FechaCreacion = ahora,
                FechaActualizacion = ahora
            };

            _db.Estaciones.Add(estacion);
            await _db.SaveChangesAsync();

            return ApiResponse<StationResponse>.Ok(ToResponse(estacion), "Estación creada");
        }

        public async Task<ApiResponse<List<StationResponse>>> GetAllAsync()
        {
            var lista = await _db.Estaciones.ToListAsync();
            return ApiResponse<List<StationResponse>>.Ok(lista.Select(ToResponse).ToList());
        }

        public async Task<ApiResponse<StationResponse>> GetByIdAsync(Guid id)
        {
            var estacion = await _db.Estaciones.FindAsync(id);
            if (estacion == null)
                return ApiResponse<StationResponse>.Fail("Estación no encontrada",
                    new() { new ApiError { Code = "RESOURCE_NOT_FOUND", Detail = "No existe una estación con ese id." } });

            return ApiResponse<StationResponse>.Ok(ToResponse(estacion));
        }

        public async Task<ApiResponse<StationResponse>> UpdateAsync(Guid id, UpdateStationRequest request)
        {
            var estacion = await _db.Estaciones.FindAsync(id);
            if (estacion == null)
                return ApiResponse<StationResponse>.Fail("Estación no encontrada",
                    new() { new ApiError { Code = "RESOURCE_NOT_FOUND", Detail = "No existe una estación con ese id." } });

            if (!string.IsNullOrWhiteSpace(request.Name)) estacion.Nombre = request.Name;
            if (request.Address != null) estacion.Ubicacion = request.Address;
            estacion.FechaActualizacion = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return ApiResponse<StationResponse>.Ok(ToResponse(estacion), "Estación actualizada");
        }

        public async Task<ApiResponse<StationResponse>> DeactivateAsync(Guid id)
        {
            var estacion = await _db.Estaciones.FindAsync(id);
            if (estacion == null)
                return ApiResponse<StationResponse>.Fail("Estación no encontrada",
                    new() { new ApiError { Code = "RESOURCE_NOT_FOUND", Detail = "No existe una estación con ese id." } });

            estacion.Activo = false;
            estacion.FechaActualizacion = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return ApiResponse<StationResponse>.Ok(ToResponse(estacion), "Estación desactivada");
        }

        private static StationResponse ToResponse(Estacion e) => new()
        {
            Id = e.Id,
            Code = e.Codigo,
            Name = e.Nombre,
            Address = e.Ubicacion,
            IsActive = e.Activo
        };
    }
}