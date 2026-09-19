using Inventario.Api.Dtos;

namespace Inventario.Api.Services
{
    public interface IReceptionService
    {
        Task<ApiResponse<ReceptionResponse>> CreateAsync(CreateReceptionRequest request, Guid usuarioId);
        Task<ApiResponse<List<ReceptionResponse>>> GetAllAsync();
    }
}