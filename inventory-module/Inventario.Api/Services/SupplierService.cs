using Inventario.Api.Data;
using Inventario.Api.Dtos;
using Inventario.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly InventoryMockContext _db;

        public SupplierService(InventoryMockContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<SupplierResponse>> CreateAsync(CreateSupplierRequest request)
        {
            var duplicado = await _db.Proveedores.AnyAsync(p => p.Rnc == request.Rnc);
            if (duplicado)
                return ApiResponse<SupplierResponse>.Fail("No es posible crear el proveedor",
                    new() { new ApiError { Code = "RNC_ALREADY_EXISTS", Field = "rnc", Detail = "Ya existe un proveedor con ese RNC." } });

            var ahora = DateTime.UtcNow;
            var proveedor = new Proveedor
            {
                Id = Guid.NewGuid(),
                Rnc = request.Rnc,
                Nombre = request.Name,
                Email = request.Email,
                Telefono = request.Phone,
                Activo = request.IsActive ?? true,
                FechaCreacion = ahora,
                FechaActualizacion = ahora
            };

            _db.Proveedores.Add(proveedor);
            await _db.SaveChangesAsync();

            return ApiResponse<SupplierResponse>.Ok(ToResponse(proveedor), "Proveedor creado");
        }

        public async Task<ApiResponse<List<SupplierResponse>>> GetAllAsync()
        {
            var lista = await _db.Proveedores.ToListAsync();
            return ApiResponse<List<SupplierResponse>>.Ok(lista.Select(ToResponse).ToList());
        }

        public async Task<ApiResponse<SupplierResponse>> GetByIdAsync(Guid id)
        {
            var proveedor = await _db.Proveedores.FindAsync(id);
            if (proveedor == null)
                return ApiResponse<SupplierResponse>.Fail("Proveedor no encontrado",
                    new() { new ApiError { Code = "RESOURCE_NOT_FOUND", Detail = "No existe un proveedor con ese id." } });

            return ApiResponse<SupplierResponse>.Ok(ToResponse(proveedor));
        }

        public async Task<ApiResponse<SupplierResponse>> UpdateAsync(Guid id, UpdateSupplierRequest request)
        {
            var proveedor = await _db.Proveedores.FindAsync(id);
            if (proveedor == null)
                return ApiResponse<SupplierResponse>.Fail("Proveedor no encontrado",
                    new() { new ApiError { Code = "RESOURCE_NOT_FOUND", Detail = "No existe un proveedor con ese id." } });

            if (!string.IsNullOrWhiteSpace(request.Name)) proveedor.Nombre = request.Name;
            if (request.Email != null) proveedor.Email = request.Email;
            if (request.Phone != null) proveedor.Telefono = request.Phone;
            proveedor.FechaActualizacion = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return ApiResponse<SupplierResponse>.Ok(ToResponse(proveedor), "Proveedor actualizado");
        }

        public async Task<ApiResponse<SupplierResponse>> DeactivateAsync(Guid id)
        {
            var proveedor = await _db.Proveedores.FindAsync(id);
            if (proveedor == null)
                return ApiResponse<SupplierResponse>.Fail("Proveedor no encontrado",
                    new() { new ApiError { Code = "RESOURCE_NOT_FOUND", Detail = "No existe un proveedor con ese id." } });

            proveedor.Activo = false;
            proveedor.FechaActualizacion = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return ApiResponse<SupplierResponse>.Ok(ToResponse(proveedor), "Proveedor desactivado");
        }

        private static SupplierResponse ToResponse(Proveedor p) => new()
        {
            Id = p.Id,
            Rnc = p.Rnc,
            Name = p.Nombre,
            Email = p.Email,
            Phone = p.Telefono,
            IsActive = p.Activo
        };
    }
}