using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CombustibleAPI.Application.Common;
using CombustibleAPI.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CombustibleAPI.Infrastructure.Notifications;

public class BrevoEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly EmailOptions _options;
    private readonly ILogger<BrevoEmailSender> _logger;

    public BrevoEmailSender(
        HttpClient httpClient,
        IOptions<EmailOptions> options,
        ILogger<BrevoEmailSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string? MessageId, string? Error)> EnviarTicketAprobadoEmailAsync(
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
        CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Envío de email omitido porque Email:Enabled es falso. Destinatario: {Destinatario}", destinatario);
            return (true, "EMAIL_DISABLED_MOCK", null);
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogError("Email:Enabled es true pero Email:ApiKey no está configurado.");
            return (false, null, "EMAIL_API_KEY_FALTANTE");
        }

        try
        {
            var htmlContent = GenerarPlantillaHtml(
                nombreEmpleado,
                numeroTicket,
                vehiculo,
                placa,
                ficha,
                combustible,
                cantidad,
                estacion,
                fechaExpiracion,
                secureUrl);

            var qrBase64 = Convert.ToBase64String(qrPngBytes);
            var attachmentName = $"LaBomba-{numeroTicket}-QR.png";

            var payload = new
            {
                sender = new
                {
                    name = string.IsNullOrWhiteSpace(_options.FromName) ? "La Bomba" : _options.FromName,
                    email = _options.FromAddress
                },
                to = new[]
                {
                    new { email = destinatario, name = nombreEmpleado }
                },
                subject = "La Bomba | Ticket de combustible aprobado",
                htmlContent,
                attachment = new[]
                {
                    new
                    {
                        name = attachmentName,
                        content = qrBase64
                    }
                }
            };

            var url = $"{_options.ApiBaseUrl.TrimEnd('/')}/v3/smtp/email";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("api-key", _options.ApiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Brevo respondió con error HTTP {StatusCode}: {Response}", (int)response.StatusCode, responseBody);
                return (false, null, $"BREVO_HTTP_{(int)response.StatusCode}: {responseBody}");
            }

            string? messageId = null;
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("messageId", out var idProp))
                {
                    messageId = idProp.GetString();
                }
            }
            catch
            {
                // Ignorar error al parsear messageId
            }

            _logger.LogInformation("Email enviado exitosamente a {Destinatario}. Ticket: {NumeroTicket}. MessageId: {MessageId}",
                destinatario, numeroTicket, messageId);

            return (true, messageId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al enviar email vía Brevo a {Destinatario}. Ticket: {NumeroTicket}", destinatario, numeroTicket);
            return (false, null, ex.Message);
        }
    }

    private static string GenerarPlantillaHtml(
        string nombreEmpleado,
        string numeroTicket,
        string vehiculo,
        string placa,
        string ficha,
        string combustible,
        decimal cantidad,
        string estacion,
        DateTime fechaExpiracion,
        string secureUrl)
    {
        var fechaStr = fechaExpiracion.ToString("dd/MM/yyyy hh:mm tt") + " UTC";

        return $@"
<!DOCTYPE html>
<html lang=""es"">
<head>
  <meta charset=""utf-8"">
  <title>Ticket de Combustible Aprobado</title>
</head>
<body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 24px; color: #1e293b;"">
  <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"">
    <tr>
      <td align=""center"">
        <table width=""600"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""background-color: #ffffff; border-radius: 12px; overflow: hidden; border: 1px solid #e2e8f0; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);"">
          <!-- Header -->
          <tr>
            <td style=""background-color: #062d4f; padding: 28px 32px; text-align: left;"">
              <span style=""font-size: 22px; font-weight: 800; color: #ffffff; letter-spacing: -0.5px;"">LA BOMBA</span>
              <span style=""display: block; font-size: 13px; color: #38bdf8; margin-top: 4px; font-weight: 500;"">Suministro y Control de Combustible</span>
            </td>
          </tr>
          <!-- Body -->
          <tr>
            <td style=""padding: 32px;"">
              <h2 style=""font-size: 20px; font-weight: 700; color: #0f172a; margin-top: 0; margin-bottom: 8px;"">¡Hola, {nombreEmpleado}!</h2>
              <p style=""font-size: 15px; color: #475569; line-height: 1.5; margin-top: 0; margin-bottom: 24px;"">
                Tu solicitud de combustible ha sido <strong>aprobada</strong> exitosamente. Ya puedes presentar tu ticket oficial para validación y despacho.
              </p>

              <!-- Ticket Info Box -->
              <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""background-color: #f1f5f9; border-radius: 8px; margin-bottom: 24px; padding: 16px 20px;"">
                <tr>
                  <td style=""padding: 6px 0; font-size: 13px; color: #64748b; font-weight: 600;"">Número de Ticket:</td>
                  <td style=""padding: 6px 0; font-size: 15px; color: #0f172a; font-weight: 700; text-align: right;"">{numeroTicket}</td>
                </tr>
                <tr>
                  <td style=""padding: 6px 0; font-size: 13px; color: #64748b; font-weight: 600;"">Vehículo:</td>
                  <td style=""padding: 6px 0; font-size: 14px; color: #1e293b; text-align: right;"">{vehiculo}</td>
                </tr>
                <tr>
                  <td style=""padding: 6px 0; font-size: 13px; color: #64748b; font-weight: 600;"">Placa / Ficha:</td>
                  <td style=""padding: 6px 0; font-size: 14px; color: #1e293b; text-align: right;"">{placa} (Ficha: {ficha})</td>
                </tr>
                <tr>
                  <td style=""padding: 6px 0; font-size: 13px; color: #64748b; font-weight: 600;"">Combustible:</td>
                  <td style=""padding: 6px 0; font-size: 14px; color: #087e8b; font-weight: 700; text-align: right;"">{combustible}</td>
                </tr>
                <tr>
                  <td style=""padding: 6px 0; font-size: 13px; color: #64748b; font-weight: 600;"">Cantidad Autorizada:</td>
                  <td style=""padding: 6px 0; font-size: 16px; color: #0f172a; font-weight: 800; text-align: right;"">{cantidad:N2} gal</td>
                </tr>
                <tr>
                  <td style=""padding: 6px 0; font-size: 13px; color: #64748b; font-weight: 600;"">Estación:</td>
                  <td style=""padding: 6px 0; font-size: 14px; color: #1e293b; text-align: right;"">{estacion}</td>
                </tr>
                <tr>
                  <td style=""padding: 6px 0; font-size: 13px; color: #64748b; font-weight: 600;"">Válido hasta:</td>
                  <td style=""padding: 6px 0; font-size: 14px; color: #b91c1c; font-weight: 600; text-align: right;"">{fechaStr}</td>
                </tr>
              </table>

              <p style=""font-size: 14px; color: #334155; line-height: 1.6; margin-bottom: 24px;"">
                Presenta este código QR al despachador de la estación para validar el suministro. Hemos adjuntado el código QR oficial en formato PNG a este correo. También puedes visualizarlo y descargarlo directamente desde tu navegador en el siguiente enlace:
              </p>

              <!-- CTA Button -->
              <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""margin-bottom: 28px;"">
                <tr>
                  <td align=""center"">
                    <a href=""{secureUrl}"" target=""_blank"" style=""display: inline-block; background-color: #087e8b; color: #ffffff; text-decoration: none; padding: 14px 32px; border-radius: 8px; font-size: 15px; font-weight: 700; letter-spacing: 0.3px;"">
                      VER TICKET Y QR
                    </a>
                  </td>
                </tr>
              </table>

              <p style=""font-size: 12px; color: #94a3b8; line-height: 1.4; text-align: center; margin: 0;"">
                Si no puedes pulsar el botón, copia y pega este enlace en tu navegador:<br>
                <a href=""{secureUrl}"" style=""color: #087e8b; word-break: break-all;"">{secureUrl}</a>
              </p>
            </td>
          </tr>
          <!-- Footer -->
          <tr>
            <td style=""background-color: #f8fafc; padding: 20px 32px; border-top: 1px solid #e2e8f0; text-align: center;"">
              <p style=""font-size: 12px; color: #64748b; margin: 0;"">
                Este es un mensaje automático del sistema de gestión de combustible La Bomba.
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }
}
