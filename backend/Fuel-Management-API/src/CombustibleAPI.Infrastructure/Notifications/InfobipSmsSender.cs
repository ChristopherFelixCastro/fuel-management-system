using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CombustibleAPI.Application.Common;
using CombustibleAPI.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CombustibleAPI.Infrastructure.Notifications;

public class InfobipSmsSender : ISmsSender
{
    private readonly HttpClient _httpClient;
    private readonly SmsOptions _options;
    private readonly ILogger<InfobipSmsSender> _logger;

    public InfobipSmsSender(
        HttpClient httpClient,
        IOptions<SmsOptions> options,
        ILogger<InfobipSmsSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public static string? NormalizarTelefonoE164(string? telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono)) return null;

        var limpio = Regex.Replace(telefono.Trim(), @"[^\d+]", "");
        if (string.IsNullOrEmpty(limpio)) return null;

        if (limpio.StartsWith("+"))
        {
            var soloDigitos = limpio[1..];
            if (soloDigitos.Length is >= 10 and <= 15 && Regex.IsMatch(soloDigitos, @"^\d+$"))
            {
                return limpio;
            }
            return null;
        }

        // Si son 10 dígitos y empieza por códigos de área de República Dominicana (809, 829, 849)
        if (limpio.Length == 10 && (limpio.StartsWith("809") || limpio.StartsWith("829") || limpio.StartsWith("849")))
        {
            return $"+1{limpio}";
        }

        // Si son 11 dígitos y empieza por 1809, 1829, 1849
        if (limpio.Length == 11 && limpio.StartsWith("1") &&
            (limpio.StartsWith("1809") || limpio.StartsWith("1829") || limpio.StartsWith("1849")))
        {
            return $"+{limpio}";
        }

        return null;
    }

    public async Task<(bool Success, string? MessageId, string? Error)> EnviarTicketAprobadoSmsAsync(
        string destinatario,
        string numeroTicket,
        decimal cantidad,
        string combustible,
        DateTime fechaExpiracion,
        string secureUrl,
        CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Envío de SMS omitido porque Sms:Enabled es falso. Destinatario: {Destinatario}", destinatario);
            return (true, "SMS_DISABLED_MOCK", null);
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogError("Sms:Enabled es true pero Sms:ApiKey no está configurado.");
            return (false, null, "SMS_API_KEY_FALTANTE");
        }

        var telefonoE164 = NormalizarTelefonoE164(destinatario);
        if (telefonoE164 is null)
        {
            _logger.LogWarning("Número de teléfono no normalizable a E.164: {Destinatario}", destinatario);
            return (false, null, "TELEFONO_NO_NORMALIZABLE_E164");
        }

        try
        {
            // Plantilla corta y concisa para evitar fragmentación GSM-7 / UCS-2
            var fechaStr = fechaExpiracion.ToString("dd/MM");
            var texto = $"La Bomba: Solicitud aprobada. Ticket {numeroTicket}. {cantidad:0.##} gal de {combustible}. Vence {fechaStr}. Ver QR: {secureUrl}";

            var payload = new
            {
                messages = new[]
                {
                    new
                    {
                        sender = string.IsNullOrWhiteSpace(_options.Sender) ? "447491163443" : _options.Sender,
                        destinations = new[]
                        {
                            new { to = telefonoE164 }
                        },
                        content = new
                        {
                            text = texto
                        }
                    }
                }
            };

            var url = $"{_options.BaseUrl.TrimEnd('/')}/sms/3/messages";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"App {_options.ApiKey}");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Infobip respondió con error HTTP {StatusCode}: {Response}", (int)response.StatusCode, responseBody);
                return (false, null, $"INFOBIP_HTTP_{(int)response.StatusCode}: {responseBody}");
            }

            string? messageId = null;
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("messages", out var msgArray) &&
                    msgArray.GetArrayLength() > 0 &&
                    msgArray[0].TryGetProperty("messageId", out var idProp))
                {
                    messageId = idProp.GetString();
                }
            }
            catch
            {
                // Ignorar error al parsear messageId
            }

            _logger.LogInformation("SMS enviado exitosamente a {Destinatario}. Ticket: {NumeroTicket}. MessageId: {MessageId}",
                telefonoE164, numeroTicket, messageId);

            return (true, messageId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al enviar SMS vía Infobip a {Destinatario}. Ticket: {NumeroTicket}", destinatario, numeroTicket);
            return (false, null, ex.Message);
        }
    }
}
