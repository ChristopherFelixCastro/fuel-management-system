using CombustibleAPI.Application.Common;
using CombustibleAPI.Application.Dtos.Auth;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Domain.Entities;
using CombustibleAPI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CombustibleAPI.Infrastructure.Services;

/// <summary>
/// Implementa /auth/login, /auth/refresh, /auth/logout y /auth/me (SDP Iván sección 4 "Identidad y sesión").
/// Reglas clave:
///  - Contraseñas con ASP.NET Core Identity PasswordHasher (hash + salt), nunca texto plano.
///  - Access token JWT de vida corta.
///  - Refresh token: el valor real se entrega UNA vez; solo se persiste TokenHash + metadatos.
///  - Cada refresh ROTA e invalida el token anterior (encadenado vía ReemplazadoPorId).
///  - Reutilizar un refresh token ya revocado/rotado se trata como posible robo de sesión:
///    se revoca toda la cadena de tokens activos del usuario.
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;
    private readonly PasswordHasher<Usuario> _passwordHasher = new();
    private readonly JwtOptions _jwtOptions;

    public AuthService(AppDbContext context, ITokenService tokenService, IAuditService auditService, IOptions<JwtOptions> jwtOptions)
    {
        _context = context;
        _tokenService = tokenService;
        _auditService = auditService;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress, CancellationToken ct)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.NombreUsuario == request.Username && u.Activo, ct);

        // Mensaje genérico e idéntico para usuario inexistente y password incorrecta:
        // evita enumeración de usuarios válidos (SDP Iván sec. 4, protección de credenciales).
        if (usuario is null)
        {
            await _auditService.RegistrarAsync(null, "LOGIN_FALLIDO", "Usuario", request.Username, ipAddress, null,
                new { motivo = "usuario_inexistente" }, ct);
            throw ApiException.Unauthorized("Usuario o contraseña inválidos.");
        }

        var verificacion = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password);
        if (verificacion == PasswordVerificationResult.Failed)
        {
            await _auditService.RegistrarAsync(usuario.Id, "LOGIN_FALLIDO", "Usuario", usuario.Id.ToString(), ipAddress, null,
                new { motivo = "password_invalida" }, ct);
            throw ApiException.Unauthorized("Usuario o contraseña inválidos.");
        }

        // Todo DESPACHADOR requiere estación asignada (regla transversal del SDP General sec. 3).
        if (string.Equals(usuario.Rol.Nombre, "DESPACHADOR", StringComparison.OrdinalIgnoreCase) && usuario.EstacionId is null)
            throw ApiException.BusinessRule("DESPACHADOR_SIN_ESTACION", "El usuario DESPACHADOR no tiene estación asignada.");

        var (accessToken, accessExp) = _tokenService.GenerarAccessToken(usuario);
        var (refreshValue, _, refreshExp) = await EmitirRefreshTokenEntityAsync(usuario.Id, ipAddress, ct);

        usuario.UltimoAcceso = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        await _auditService.RegistrarAsync(usuario.Id, "LOGIN_EXITOSO", "Usuario", usuario.Id.ToString(), ipAddress, null, null, ct);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            AccessTokenExpiraEn = accessExp,
            RefreshToken = refreshValue,
            RefreshTokenExpiraEn = refreshExp,
            Usuario = usuario.NombreUsuario,
            Rol = usuario.Rol.Nombre,
            UsuarioId = usuario.Id,
            EstacionId = usuario.EstacionId
        };
    }

    public async Task<LoginResponseDto> RefreshAsync(string refreshTokenValue, string? ipAddress, CancellationToken ct)
    {
        var hash = _tokenService.HashRefreshToken(refreshTokenValue);

        var tokenActual = await _context.RefreshTokens
            .Include(rt => rt.Usuario).ThenInclude(u => u.Rol)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash, ct);

        if (tokenActual is null)
            throw ApiException.Unauthorized("Refresh token inválido.");

        if (tokenActual.FechaRevocacion is not null)
        {
            // Reutilización de un token ya rotado/revocado: posible robo de sesión.
            // Se revoca toda la cadena activa del usuario como medida de contención.
            await RevocarCadenaCompletaAsync(tokenActual.UsuarioId, ipAddress, ct);
            await _auditService.RegistrarAsync(tokenActual.UsuarioId, "REFRESH_TOKEN_REUTILIZADO", "RefreshToken",
                tokenActual.Id.ToString(), ipAddress, null, null, ct);
            throw ApiException.Unauthorized("Sesión inválida: se detectó reutilización de refresh token.");
        }

        if (tokenActual.EstaExpirado)
            throw ApiException.Unauthorized("Refresh token expirado.");

        var usuario = tokenActual.Usuario;
        if (!usuario.Activo)
            throw ApiException.Unauthorized("Usuario inactivo.");

        // Rotación: se revoca el token actual y se emite uno nuevo, encadenado.
        var (accessToken, accessExp) = _tokenService.GenerarAccessToken(usuario);
        var (nuevoValue, nuevoTokenEntity, nuevoExp) = await EmitirRefreshTokenEntityAsync(usuario.Id, ipAddress, ct);

        tokenActual.FechaRevocacion = DateTime.UtcNow;
        tokenActual.IpRevocacion = ipAddress;
        tokenActual.ReemplazadoPorId = nuevoTokenEntity.Id;

        await _context.SaveChangesAsync(ct);
        await _auditService.RegistrarAsync(usuario.Id, "REFRESH_TOKEN_ROTADO", "RefreshToken", tokenActual.Id.ToString(), ipAddress, null, null, ct);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            AccessTokenExpiraEn = accessExp,
            RefreshToken = nuevoValue,
            RefreshTokenExpiraEn = nuevoExp,
            Usuario = usuario.NombreUsuario,
            Rol = usuario.Rol.Nombre,
            UsuarioId = usuario.Id,
            EstacionId = usuario.EstacionId
        };
    }

    public async Task LogoutAsync(string refreshTokenValue, string? ipAddress, CancellationToken ct)
    {
        var hash = _tokenService.HashRefreshToken(refreshTokenValue);
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash, ct);

        if (token is null || token.FechaRevocacion is not null)
            return; // Logout es idempotente: no revela si el token existía.

        token.FechaRevocacion = DateTime.UtcNow;
        token.IpRevocacion = ipAddress;
        await _context.SaveChangesAsync(ct);
        await _auditService.RegistrarAsync(token.UsuarioId, "LOGOUT", "RefreshToken", token.Id.ToString(), ipAddress, null, null, ct);
    }

    public async Task<UserProfileDto> GetCurrentUserProfileAsync(Guid usuarioId, CancellationToken ct)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .Include(u => u.Estacion)
            .Include(u => u.Empleado)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == usuarioId, ct);

        if (usuario is null)
            throw ApiException.NotFound("Usuario");

        return new UserProfileDto
        {
            Id = usuario.Id,
            Usuario = usuario.NombreUsuario,
            Email = usuario.Email,
            Rol = usuario.Rol.Nombre,
            StationId = usuario.EstacionId,
            EstacionNombre = usuario.Estacion?.Nombre,
            EmpleadoId = usuario.EmpleadoId,
            EmpleadoNombre = usuario.Empleado?.NombreCompleto
        };
    }

    private Task<(string value, RefreshToken entity, DateTime expiraEn)> EmitirRefreshTokenEntityAsync(Guid usuarioId, string? ipAddress, CancellationToken ct)
    {
        var value = _tokenService.GenerarRefreshTokenValue();
        var hash = _tokenService.HashRefreshToken(value);
        var expiraEn = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);

        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            TokenHash = hash,
            FechaCreacion = DateTime.UtcNow,
            FechaExpiracion = expiraEn,
            IpCreacion = ipAddress
        };

        _context.RefreshTokens.Add(entity);
        return Task.FromResult((value, entity, expiraEn));
    }

    private async Task RevocarCadenaCompletaAsync(Guid usuarioId, string? ipAddress, CancellationToken ct)
    {
        var activos = await _context.RefreshTokens
            .Where(rt => rt.UsuarioId == usuarioId && rt.FechaRevocacion == null)
            .ToListAsync(ct);

        foreach (var rt in activos)
        {
            rt.FechaRevocacion = DateTime.UtcNow;
            rt.IpRevocacion = ipAddress;
        }

        await _context.SaveChangesAsync(ct);
    }
}
