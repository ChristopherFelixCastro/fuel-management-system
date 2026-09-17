using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using CombustibleAPI.Application.Common;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Domain.Entities;

namespace CombustibleAPI.Application.Services;

public class TokenService : ITokenService
{
    private readonly JwtOptions _options;

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public (string token, DateTime expiraEn) GenerarAccessToken(Usuario usuario)
    {
        var expiraEn = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, usuario.Username),
            // Claim de rol único: RBAC centralizado (SDP General sec. 3 y 9).
            new(ClaimTypes.Role, usuario.Rol.Nombre.ToString())
        };

        if (usuario.EstacionId is not null)
        {
            // Claim mínimo necesario: permite que la política de autorización exija
            // coincidencia de estación para operaciones de DESPACHADOR (dispatches).
            claims.Add(new Claim("estacion_id", usuario.EstacionId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiraEn,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEn);
    }

    public string GenerarRefreshTokenValue()
    {
        // 256 bits de entropía, codificados en Base64Url para que viaje seguro en JSON/headers.
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncoder.Encode(bytes);
    }

    public string HashRefreshToken(string refreshTokenValue)
    {
        // El valor real nunca se persiste; solo su hash (SDP Iván sec. 4).
        var bytes = Encoding.UTF8.GetBytes(refreshTokenValue);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
