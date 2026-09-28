namespace CombustibleAPI.Application.Common;

public class NotificationOptions
{
    public const string SectionName = "Notifications";

    public bool Enabled { get; set; } = true;
    public int PollingIntervalSeconds { get; set; } = 10;
    public int BatchSize { get; set; } = 10;
    public int MaxAttempts { get; set; } = 3;
    public int ProcessingLeaseMinutes { get; set; } = 5;
}

public class PublicTicketOptions
{
    public const string SectionName = "PublicTicket";

    public string BaseUrl { get; set; } = "https://la-bomba-admin.pages.dev";
    public string Secret { get; set; } = string.Empty; // Mínimo 32 bytes criptográficos
    public int ExpirationDays { get; set; } = 7;
}

public class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; } = false;
    public string Provider { get; set; } = "Brevo";
    public string ApiBaseUrl { get; set; } = "https://api.brevo.com";
    public string ApiKey { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "La Bomba";
}

public class SmsOptions
{
    public const string SectionName = "Sms";

    public bool Enabled { get; set; } = false;
    public string Provider { get; set; } = "Infobip";
    public string BaseUrl { get; set; } = "https://eelvdn.api.infobip.com";
    public string ApiKey { get; set; } = string.Empty;
    public string Sender { get; set; } = "447491163443";
}
