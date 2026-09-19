using Inventario.Api.Dtos;

namespace Inventario.Api.Services
{
    public interface ISupplierService
    {
        Task<ApiResponse<SupplierResponse>> CreateAsync(CreateSupplierRequest request);
        Task<ApiResponse<List<SupplierResponse>>> GetAllAsync();
        Task<ApiResponse<SupplierResponse>> GetByIdAsync(Guid id);
        Task<ApiResponse<SupplierResponse>> UpdateAsync(Guid id, UpdateSupplierRequest request);
        Task<ApiResponse<SupplierResponse>> DeactivateAsync(Guid id);
    }
}