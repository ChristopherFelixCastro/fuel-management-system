using Inventario.Api.Dtos;

namespace Inventario.Api.Services
{
    public interface ITransferService
    {
        Task<ApiResponse<TransferResponse>> CreateAsync(CreateTransferRequest request, Guid usuarioId);
        Task<ApiResponse<List<TransferResponse>>> GetAllAsync();
    }
}