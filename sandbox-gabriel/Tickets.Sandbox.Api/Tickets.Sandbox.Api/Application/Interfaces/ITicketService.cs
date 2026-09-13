using Tickets.Sandbox.Api.Application.DTOs.Tickets;

namespace Tickets.Sandbox.Api.Application.Interfaces;

public interface ITicketService
{
    Task<(List<TicketResponseDto> Items, int TotalCount)> GetTicketsAsync(TicketFilterDto filter, string? userRole = null, Guid? userEmployeeId = null, CancellationToken cancellationToken = default);
    Task<TicketResponseDto> GetTicketByIdAsync(Guid id, string? userRole = null, Guid? userEmployeeId = null, CancellationToken cancellationToken = default);
    Task<byte[]> GetTicketPdfBytesAsync(Guid id, string? userRole = null, Guid? userEmployeeId = null, CancellationToken cancellationToken = default);
    Task<ValidateTicketResponseDto> ValidateTicketQrAsync(ValidateTicketRequestDto dto, CancellationToken cancellationToken = default);
    Task<TicketResponseDto> CancelTicketAsync(Guid id, CancelTicketRequestDto dto, string? supervisorId = null, CancellationToken cancellationToken = default);
    Task<TicketResponseDto> ConsumeTicketAsync(Guid id, ConsumeTicketRequestDto dto, CancellationToken cancellationToken = default);
}
