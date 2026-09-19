using Inventario.Api.Dtos;

namespace Inventario.Api.Services
{
    public interface IInventoryService
    {
        Task<ApiResponse<List<InventoryItemResponse>>> GetInventoryAsync();
        Task<ApiResponse<List<InventoryMovementResponse>>> GetMovementsAsync(Guid? tankId, string? type);
    }
}