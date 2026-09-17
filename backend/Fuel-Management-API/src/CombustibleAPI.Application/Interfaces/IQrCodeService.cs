namespace CombustibleAPI.Application.Interfaces;

public record QrSecurityResult(
    string RawToken,
    string TokenHash,
    string Signature,
    string QrPayload);

public interface IQrCodeService
{
    QrSecurityResult GenerateQrSecurityData(Guid ticketId);

    QrSecurityResult RebuildQrSecurityData(Guid ticketId);

    bool VerifySignature(Guid ticketId, string rawToken, string signature);

    bool VerifyTokenHash(string rawToken, string storedHash);

    string HashToken(string rawToken);

    byte[] GenerateQrImagePng(string qrPayload);

    (Guid TicketId, string RawToken, string Signature)? ParsePayload(string qrPayload);
}