namespace FuelManagement.Shared.Services;

/// <summary>
/// Abstrae la identidad del usuario actualmente autenticado,
/// permitiendo su uso en servicios y DbContext sin depender de HttpContext directamente.
/// </summary>
public interface ICurrentUserService
{
    Guid UserId { get; }
    string UserName { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}
