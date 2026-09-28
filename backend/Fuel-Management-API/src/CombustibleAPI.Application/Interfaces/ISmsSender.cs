namespace CombustibleAPI.Application.Interfaces;

public interface ISmsSender
{
    Task<(bool Success, string? MessageId, string? Error)> EnviarTicketAprobadoSmsAsync(
        string destinatario,
        string numeroTicket,
        decimal cantidad,
        string combustible,
        DateTime fechaExpiracion,
        string secureUrl,
        CancellationToken ct);
}
