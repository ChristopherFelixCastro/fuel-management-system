namespace CombustibleAPI.Application.Common;

/// <summary>
/// Se enlaza a la sección "Jwt" de appsettings/ variables de entorno / secret manager.
/// El valor de "Key" NUNCA debe estar en el repositorio: en desarrollo usar
/// `dotnet user-secrets`, en producción variables de entorno o el secret manager
/// del proveedor Free Tier elegido (SDP General sección 7).
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public string Key { get; set; } = default!;

    /// <summary>Vida corta del access token, en minutos (recomendado 10-15).</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>Vida del refresh token, en días.</summary>
    public int RefreshTokenDays { get; set; } = 7;
}
