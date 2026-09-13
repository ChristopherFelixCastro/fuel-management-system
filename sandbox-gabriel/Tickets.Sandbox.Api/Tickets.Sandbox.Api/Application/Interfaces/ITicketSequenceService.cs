namespace Tickets.Sandbox.Api.Application.Interfaces;

public interface ITicketSequenceService
{
    /// <summary>
    /// Genera de forma atómica y sin colisión bajo concurrencia el número de ticket "COM-{AÑO}-{6_DIGITOS}".
    /// </summary>
    Task<string> NextTicketNumberAsync(int year, CancellationToken cancellationToken = default);
}
