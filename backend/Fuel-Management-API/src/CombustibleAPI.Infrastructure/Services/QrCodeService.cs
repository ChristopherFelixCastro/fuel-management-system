using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CombustibleAPI.Application.Common;
using CombustibleAPI.Application.Interfaces;
using Microsoft.Extensions.Options;
using QRCoder;

namespace CombustibleAPI.Infrastructure.Services;

public class QrCodeService : IQrCodeService
{
    private readonly byte[] _secretKey;

    public QrCodeService(IOptions<QrSecurityOptions> options)
    {
        var secret = options.Value.SecretKey;

        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
            throw new InvalidOperationException(
                "QrSecurity:SecretKey debe contener al menos 32 caracteres.");

        _secretKey = Encoding.UTF8.GetBytes(secret);
    }

    public QrSecurityResult GenerateQrSecurityData(Guid ticketId)
        => BuildSecurityData(ticketId);

    public QrSecurityResult RebuildQrSecurityData(Guid ticketId)
        => BuildSecurityData(ticketId);

    private QrSecurityResult BuildSecurityData(Guid ticketId)
    {
        var rawToken = ComputeHmacSha256($"token:{ticketId}");

        var tokenHash = HashToken(rawToken);

        var signature =
            ComputeHmacSha256($"{ticketId}:{rawToken}");

        var payload = JsonSerializer.Serialize(new
        {
            ticketId,
            token = rawToken,
            sig = signature
        });

        return new QrSecurityResult(
            rawToken,
            tokenHash,
            signature,
            payload);
    }

    public bool VerifySignature(
        Guid ticketId,
        string rawToken,
        string signature)
    {
        if (string.IsNullOrWhiteSpace(rawToken) ||
            string.IsNullOrWhiteSpace(signature))
            return false;

        var expected =
            ComputeHmacSha256($"{ticketId}:{rawToken}");

        return FixedTimeHexEquals(expected, signature);
    }

    public bool VerifyTokenHash(
        string rawToken,
        string storedHash)
    {
        if (string.IsNullOrWhiteSpace(rawToken) ||
            string.IsNullOrWhiteSpace(storedHash))
            return false;

        var calculated = HashToken(rawToken);

        return FixedTimeHexEquals(calculated, storedHash);
    }

    public string HashToken(string rawToken)
    {
        var bytes =
            SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));

        return Convert
            .ToHexString(bytes)
            .ToLowerInvariant();
    }

    public byte[] GenerateQrImagePng(string qrPayload)
    {
        using var generator = new QRCodeGenerator();

        using var data = generator.CreateQrCode(
            qrPayload,
            QRCodeGenerator.ECCLevel.Q);

        using var qr = new PngByteQRCode(data);

        return qr.GetGraphic(20);
    }

    public (Guid TicketId, string RawToken, string Signature)?
        ParsePayload(string qrPayload)
    {
        if (string.IsNullOrWhiteSpace(qrPayload))
            return null;

        try
        {
            using var document =
                JsonDocument.Parse(qrPayload);

            var root = document.RootElement;

            if (!root.TryGetProperty("ticketId", out var idElement) ||
                !root.TryGetProperty("token", out var tokenElement) ||
                !root.TryGetProperty("sig", out var signatureElement))
                return null;

            if (!Guid.TryParse(
                    idElement.GetString(),
                    out var ticketId))
                return null;

            var token = tokenElement.GetString();
            var signature = signatureElement.GetString();

            if (string.IsNullOrWhiteSpace(token) ||
                string.IsNullOrWhiteSpace(signature))
                return null;

            return (
                ticketId,
                token,
                signature);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private string ComputeHmacSha256(string value)
    {
        using var hmac =
            new HMACSHA256(_secretKey);

        var hash = hmac.ComputeHash(
            Encoding.UTF8.GetBytes(value));

        return Convert
            .ToHexString(hash)
            .ToLowerInvariant();
    }

    private static bool FixedTimeHexEquals(
        string expected,
        string supplied)
    {
        try
        {
            var expectedBytes =
                Convert.FromHexString(expected);

            var suppliedBytes =
                Convert.FromHexString(supplied);

            return expectedBytes.Length == suppliedBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(
                       expectedBytes,
                       suppliedBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}