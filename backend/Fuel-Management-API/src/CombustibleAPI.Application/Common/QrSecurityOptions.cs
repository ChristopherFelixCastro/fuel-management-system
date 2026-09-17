namespace CombustibleAPI.Application.Common;

public class QrSecurityOptions
{
    public const string SectionName = "QrSecurity";

    public string SecretKey { get; set; } = string.Empty;
}