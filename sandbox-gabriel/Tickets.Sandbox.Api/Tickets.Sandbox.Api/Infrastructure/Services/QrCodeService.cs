using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using QRCoder;
using Tickets.Sandbox.Api.Application.Contracts;
using Tickets.Sandbox.Api.Infrastructure.Configuration;

namespace Tickets.Sandbox.Api.Infrastructure.Services;

public class QrCodeService : IQrCodeService
{
    private readonly QrSecurityOptions _options;

    public QrCodeService(IOptions<QrSecurityOptions> options)
    {
        _options = options.Value;
    }

    public QrSecurityResult GenerateQrSecurityData(Guid ticketId)
    {
        // 1. Generar token opaco criptográficamente seguro
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Convert.ToHexString(tokenBytes).ToLowerInvariant();

        // 2. Calcular el Hash SHA-256 para persistencia en BD (NUNCA el token en claro)
        var tokenHash = HashToken(rawToken);

        // 3. Firmar el payload (ticketId + rawToken) usando HMAC-SHA256 con la clave secreta
        var signature = ComputeHmacSha256($"{ticketId}:{rawToken}", _options.SecretKey);

        // 4. Construir payload compacto para el QR (ticketId + token opaco + firma)
        // NUNCA incluye cantidad, estado, ni datos operativos
        var payloadObj = new
        {
            ticketId = ticketId.ToString(),
            token = rawToken,
            sig = signature
        };

        var qrPayload = JsonSerializer.Serialize(payloadObj);

        return new QrSecurityResult(rawToken, tokenHash, signature, qrPayload);
    }

    public bool VerifySignature(Guid ticketId, string rawToken, string signature)
    {
        if (string.IsNullOrWhiteSpace(rawToken) || string.IsNullOrWhiteSpace(signature))
            return false;

        var expectedSignature = ComputeHmacSha256($"{ticketId}:{rawToken}", _options.SecretKey);
        
        // Comparación en tiempo constante para mitigar ataques de temporización
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(signature));
    }

    public string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public byte[] GenerateQrImagePng(string qrPayload)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(qrPayload, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(20);
    }

    public (Guid ticketId, string rawToken, string signature)? ParsePayload(string qrPayload)
    {
        if (string.IsNullOrWhiteSpace(qrPayload))
            return null;

        try
        {
            // Intentar deserializar formato JSON
            using var doc = JsonDocument.Parse(qrPayload);
            var root = doc.RootElement;

            if (root.TryGetProperty("ticketId", out var tidProp) &&
                root.TryGetProperty("token", out var tokProp) &&
                root.TryGetProperty("sig", out var sigProp))
            {
                if (Guid.TryParse(tidProp.GetString(), out var tid))
                {
                    return (tid, tokProp.GetString() ?? "", sigProp.GetString() ?? "");
                }
            }
        }
        catch
        {
            // Intentar formato delimitado: ticketId:rawToken:signature
            var parts = qrPayload.Split(':');
            if (parts.Length == 3 && Guid.TryParse(parts[0], out var tid))
            {
                return (tid, parts[1], parts[2]);
            }
        }

        return null;
    }

    private static string ComputeHmacSha256(string data, string secretKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var dataBytes = Encoding.UTF8.GetBytes(data);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
