using Inventario.Api.Dtos;

namespace Inventario.Api.Services
{
    public interface IAdjustmentService
    {
        Task<ApiResponse<AdjustmentResponse>> ReportAsync(ReportAdjustmentRequest request, Guid usuarioId);
        Task<ApiResponse<List<AdjustmentResponse>>> GetAllAsync();
        Task<ApiResponse<AdjustmentResponse>> ApproveAsync(Guid id, Guid usuarioId);
        Task<ApiResponse<AdjustmentResponse>> RejectAsync(Guid id, RejectAdjustmentRequest request, Guid usuarioId);
    }
}