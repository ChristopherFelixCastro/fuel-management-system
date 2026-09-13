using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tickets.Sandbox.Api.Infrastructure.Authentication;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "DevelopmentAuth";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Extraer rol del header "X-User-Role" si se provee, o rol SUPERVISOR / ADMINISTRADOR por defecto
        var role = Context.Request.Headers["X-User-Role"].FirstOrDefault() ?? "ADMINISTRADOR";
        var userId = Context.Request.Headers["X-User-Id"].FirstOrDefault() ?? "DEV_USER";

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, userId),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role),
            new Claim("role", role),
            new Claim(ClaimTypes.Role, "SOLICITANTE"),
            new Claim(ClaimTypes.Role, "SUPERVISOR"),
            new Claim(ClaimTypes.Role, "DESPACHADOR"),
            new Claim(ClaimTypes.Role, "AUDITOR"),
            new Claim(ClaimTypes.Role, "ADMINISTRADOR"),
            new Claim(ClaimTypes.Role, "SISTEMA_DESPACHO")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
