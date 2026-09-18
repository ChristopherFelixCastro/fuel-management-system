using Microsoft.AspNetCore.Http;

namespace FuelManagement.Shared.Services;

/// <summary>
/// Implementacion de desarrollo: lee userId/userName desde headers HTTP
/// para facilitar pruebas sin autenticacion real.
/// Solo se registra en el entorno Development.
/// </summary>
public sealed class DevCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DevCurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var raw = _httpContextAccessor.HttpContext?.Request.Headers["X-Dev-UserId"].FirstOrDefault();
            return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
        }
    }

    public string UserName =>
        _httpContextAccessor.HttpContext?.Request.Headers["X-Dev-UserName"].FirstOrDefault()
        ?? "dev-user";

    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent =>
        _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.FirstOrDefault();
}
