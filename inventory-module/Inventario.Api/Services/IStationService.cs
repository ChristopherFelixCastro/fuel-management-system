using Inventario.Api.Dtos;

namespace Inventario.Api.Services
{
    public interface IStationService
    {
        Task<ApiResponse<StationResponse>> CreateAsync(CreateStationRequest request);
        Task<ApiResponse<List<StationResponse>>> GetAllAsync();
        Task<ApiResponse<StationResponse>> GetByIdAsync(Guid id);
        Task<ApiResponse<StationResponse>> UpdateAsync(Guid id, UpdateStationRequest request);
        Task<ApiResponse<StationResponse>> DeactivateAsync(Guid id);
    }
}