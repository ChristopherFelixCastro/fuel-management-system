using Tickets.Sandbox.Api.Application.DTOs.Requests;

namespace Tickets.Sandbox.Api.Application.Interfaces;

public interface IRequestService
{
    Task<RequestResponseDto> CreateRequestAsync(CreateRequestDto dto, string? userId = null, CancellationToken cancellationToken = default);
    Task<(List<RequestResponseDto> Items, int TotalCount)> GetRequestsAsync(RequestFilterDto filter, string? userRole = null, Guid? userEmployeeId = null, CancellationToken cancellationToken = default);
    Task<RequestResponseDto?> GetRequestByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RequestResponseDto> UpdateRequestAsync(Guid id, UpdateRequestDto dto, string? userId = null, string? userRole = null, CancellationToken cancellationToken = default);
    Task<RequestResponseDto> ApproveRequestAsync(Guid id, ApproveRequestDto dto, string? supervisorId = null, CancellationToken cancellationToken = default);
    Task<RequestResponseDto> RejectRequestAsync(Guid id, RejectRequestDto dto, string? supervisorId = null, CancellationToken cancellationToken = default);
    Task<RequestResponseDto> CancelRequestAsync(Guid id, CancelRequestDto? dto = null, string? userId = null, string? userRole = null, CancellationToken cancellationToken = default);
}
