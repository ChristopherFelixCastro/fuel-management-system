using CombustibleAPI.Application.Dtos.Dispatches;
using CombustibleAPI.Application.Dtos.Requests;

namespace CombustibleAPI.Application.Interfaces;

public interface IRequestService
{
    Task<RequestResponseDto> CrearAsync(CreateRequestDto request, Guid usuarioId, CancellationToken ct);
    Task<PaginatedList<RequestResponseDto>> ListarAsync(RequestFilterDto filter, Guid usuarioId, string rol, CancellationToken ct);
    Task<RequestResponseDto> ObtenerAsync(Guid id, Guid usuarioId, string rol, CancellationToken ct);
    Task<RequestResponseDto> ActualizarAsync(Guid id, UpdateRequestDto request, Guid usuarioId, string rol, CancellationToken ct);
    Task<RequestResponseDto> AprobarAsync(Guid id, ApproveRequestDto request, Guid revisorId, CancellationToken ct);
    Task<RequestResponseDto> RechazarAsync(Guid id, RejectRequestDto request, Guid revisorId, CancellationToken ct);
    Task<RequestResponseDto> CancelarAsync(Guid id, CancelRequestDto? request, Guid usuarioId, string rol, CancellationToken ct);
}
