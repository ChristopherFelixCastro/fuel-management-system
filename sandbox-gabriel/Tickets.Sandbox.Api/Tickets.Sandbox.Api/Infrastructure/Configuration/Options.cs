namespace Tickets.Sandbox.Api.Infrastructure.Configuration;

public class QrSecurityOptions
{
    public const string SectionName = "QrSecurity";

    /// <summary>
    /// Secreto criptográfico para firma HMAC-SHA256. 
    /// En producción se inyecta por variable de entorno; en dev por user-secrets / appsettings.
    /// </summary>
    public string SecretKey { get; set; } = "DefaultDevSecretKey_ChangeInProduction_FMS2026";
}

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public bool UseSsl { get; set; } = false;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string FromEmail { get; set; } = "tickets@combustible.local";
    public string FromName { get; set; } = "Sistema de Gestión de Combustible";
}
