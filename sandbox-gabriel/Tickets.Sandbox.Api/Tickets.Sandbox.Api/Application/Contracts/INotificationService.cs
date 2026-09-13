using Tickets.Sandbox.Api.Domain.Entities;

namespace Tickets.Sandbox.Api.Application.Contracts;

public interface INotificationService
{
    Task NotifyTicketIssuedAsync(Ticket ticket, Request request, byte[]? pdfBytes = null, CancellationToken cancellationToken = default);
}
