namespace CombustibleAPI.Application.Interfaces;

public interface IEmailSender
{
    Task<(bool Success, string? MessageId, string? Error)> EnviarTicketAprobadoEmailAsync(
        string destinatario,
        string nombreEmpleado,
        string numeroTicket,
        string vehiculo,
        string placa,
        string ficha,
        string combustible,
        decimal cantidad,
        string estacion,
        DateTime fechaExpiracion,
        string secureUrl,
        byte[] qrPngBytes,
        CancellationToken ct);
}
