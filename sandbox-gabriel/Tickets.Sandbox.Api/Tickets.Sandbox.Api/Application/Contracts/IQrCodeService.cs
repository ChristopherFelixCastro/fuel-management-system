namespace Tickets.Sandbox.Api.Application.Contracts;

public record QrSecurityResult(string RawToken, string TokenHash, string Signature, string QrPayload);

public interface IQrCodeService
{
    /// <summary>
    /// Genera el token opaco, su hash para persistencia, la firma HMAC-SHA256 y el payload compacto para el QR.
    /// </summary>
    QrSecurityResult GenerateQrSecurityData(Guid ticketId);

    /// <summary>
    /// Valida si la firma HMAC-SHA256 coincide con el secreto configurado.
    /// </summary>
    bool VerifySignature(Guid ticketId, string rawToken, string signature);

    /// <summary>
    /// Calcula el hash SHA-256 de un token en texto plano.
    /// </summary>
    string HashToken(string rawToken);

    /// <summary>
    /// Genera la imagen del código QR en formato PNG (bytes).
    /// </summary>
    byte[] GenerateQrImagePng(string qrPayload);

    /// <summary>
    /// Parsea el payload compacto del QR (ticketId, token, signature).
    /// </summary>
    (Guid ticketId, string rawToken, string signature)? ParsePayload(string qrPayload);
}
