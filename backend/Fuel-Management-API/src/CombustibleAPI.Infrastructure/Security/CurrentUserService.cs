using System.Security.Claims;
using CombustibleAPI.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CombustibleAPI.Infrastructure.Security;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;

    public Guid? UsuarioId
    {
        get
        {
            var sub = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Rol => User?.FindFirstValue(ClaimTypes.Role);

    public Guid? EstacionId
    {
        get
        {
            var value = User?.FindFirstValue("estacion_id");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
