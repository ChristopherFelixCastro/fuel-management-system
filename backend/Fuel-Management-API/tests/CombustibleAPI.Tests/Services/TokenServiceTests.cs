using CombustibleAPI.Application.Common;
using CombustibleAPI.Application.Services;
using CombustibleAPI.Domain.Entities;
using CombustibleAPI.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CombustibleAPI.Tests.Services;

public class TokenServiceTests
{
    private static TokenService CreateSut(int accessMinutes = 15) => new(
        Options.Create(new JwtOptions
        {
            Issuer = "CombustibleAPI.Tests",
            Audience = "CombustibleAPI.Tests.Clients",
            Key = "unit-test-secret-key-at-least-32-characters-long",
            AccessTokenMinutes = accessMinutes,
            RefreshTokenDays = 7
        }));

    private static Usuario CrearUsuarioDespachador() => new()
    {
        Id = Guid.NewGuid(),
        Username = "despachador.test",
        Email = "despachador.test@example.com",
        PasswordHash = "irrelevante-para-este-test",
        RolId = (short)RolUsuario.DESPACHADOR,
        Rol = new Rol { Id = (short)RolUsuario.DESPACHADOR, Nombre = "DESPACHADOR" },
        EstacionId = Guid.NewGuid(),
        Activo = true
    };

    [Fact]
    public void GenerarAccessToken_IncluyeClaimDeRolYEstacion()
    {
        var sut = CreateSut();
        var usuario = CrearUsuarioDespachador();

        var (token, expiraEn) = sut.GenerarAccessToken(usuario);

        token.Should().NotBeNullOrWhiteSpace();
        expiraEn.Should().BeAfter(DateTime.UtcNow);

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "DESPACHADOR");
        jwt.Claims.Should().Contain(c => c.Type == "estacion_id" && c.Value == usuario.EstacionId!.Value.ToString());
    }

    [Fact]
    public void GenerarRefreshTokenValue_GeneraValoresUnicosCadaVez()
    {
        var sut = CreateSut();

        var valor1 = sut.GenerarRefreshTokenValue();
        var valor2 = sut.GenerarRefreshTokenValue();

        valor1.Should().NotBe(valor2);
        valor1.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HashRefreshToken_EsDeterministaYNuncaIgualAlValorOriginal()
    {
        var sut = CreateSut();
        var valor = sut.GenerarRefreshTokenValue();

        var hash1 = sut.HashRefreshToken(valor);
        var hash2 = sut.HashRefreshToken(valor);

        hash1.Should().Be(hash2, "el hash debe ser determinista para poder buscar por TokenHash en BD");
        hash1.Should().NotBe(valor, "el valor real del refresh token nunca debe coincidir con lo persistido");
    }
}
