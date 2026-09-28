using CombustibleAPI.Application.Dtos.PublicTickets;

namespace CombustibleAPI.Application.Interfaces;

public interface IPublicTicketService
{
    Task<(string TokenReal, string Nonce, string TokenHash)> GenerarTokenAccesoAsync(
        Guid ticketId,
        DateTime fechaExpiracionTicket,
        CancellationToken ct);

    string ReconstruirToken(Guid ticketId, string nonce);

    Task<PublicTicketDto> ObtenerTicketPublicoAsync(
        string token,
        CancellationToken ct);

    Task<byte[]> ObtenerQrPngPublicoAsync(
        string token,
        CancellationToken ct);
}
