using Inventario.Api.Dtos;

namespace Inventario.Api.Services
{
    public interface ITankService
    {
        Task<ApiResponse<TankResponse>> CreateAsync(CreateTankRequest request);
        Task<ApiResponse<List<TankResponse>>> GetAllAsync();
        Task<ApiResponse<TankResponse>> GetByIdAsync(Guid id);
        Task<ApiResponse<TankResponse>> UpdateAsync(Guid id, UpdateTankRequest request);
        Task<ApiResponse<TankResponse>> DeactivateAsync(Guid id);
    }
}