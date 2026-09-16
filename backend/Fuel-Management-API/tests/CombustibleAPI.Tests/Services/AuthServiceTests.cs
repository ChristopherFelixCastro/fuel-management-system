using CombustibleAPI.Application.Common;
using CombustibleAPI.Application.Dtos.Auth;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Application.Services;
using CombustibleAPI.Domain.Entities;
using CombustibleAPI.Domain.Enums;
using CombustibleAPI.Infrastructure.Persistence;
using CombustibleAPI.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace CombustibleAPI.Tests.Services;

public class AuthServiceTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static ITokenService CreateTokenService() => new TokenService(
        Options.Create(new JwtOptions
        {
            Issuer = "CombustibleAPI.Tests",
            Audience = "CombustibleAPI.Tests.Clients",
            Key = "unit-test-secret-key-at-least-32-characters-long",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        }));

    private class FakeAuditService : IAuditService
    {
        public Task RegistrarAsync(Guid? usuarioId, string accion, string entidad, string? entidadId,
            string? ipAddress, object? datosAnteriores, object? datosNuevos, CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public async Task LoginAsync_CredencialesValidas_RetornaTokensUsuarioRolYEstacion()
    {
        using var context = CreateInMemoryContext();
        var tokenService = CreateTokenService();
        var auditService = new FakeAuditService();
        var jwtOptions = Options.Create(new JwtOptions { RefreshTokenDays = 7 });

        var hasher = new PasswordHasher<Usuario>();
        var estacionId = Guid.NewGuid();
        var rol = new Rol { Id = (short)RolUsuario.DESPACHADOR, Nombre = "DESPACHADOR" };
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            NombreUsuario = "despachador1",
            Email = "despachador1@test.com",
            RolId = rol.Id,
            Rol = rol,
            EstacionId = estacionId,
            Activo = true
        };
        usuario.PasswordHash = hasher.HashPassword(usuario, "Pass123*");
        context.Roles.Add(rol);
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();

        var sut = new AuthService(context, tokenService, auditService, jwtOptions);

        var result = await sut.LoginAsync(new LoginRequestDto
        {
            Username = "despachador1",
            Password = "Pass123*"
        }, "127.0.0.1", CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.Usuario.Should().Be("despachador1");
        result.Rol.Should().Be("DESPACHADOR");
        result.EstacionId.Should().Be(estacionId);
        result.Expiracion.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task GetCurrentUserProfileAsync_UsuarioExistente_RetornaPerfilCompleto()
    {
        using var context = CreateInMemoryContext();
        var tokenService = CreateTokenService();
        var auditService = new FakeAuditService();
        var jwtOptions = Options.Create(new JwtOptions { RefreshTokenDays = 7 });

        var estacion = new Estacion { Id = Guid.NewGuid(), Codigo = "EST-01", Nombre = "Estación Central" };
        var rol = new Rol { Id = (short)RolUsuario.DESPACHADOR, Nombre = "DESPACHADOR" };
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            NombreUsuario = "despachador.pwa",
            Email = "despachador@pwa.local",
            RolId = rol.Id,
            Rol = rol,
            EstacionId = estacion.Id,
            Estacion = estacion,
            Activo = true,
            PasswordHash = "hash"
        };
        context.Estaciones.Add(estacion);
        context.Roles.Add(rol);
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();

        var sut = new AuthService(context, tokenService, auditService, jwtOptions);

        var profile = await sut.GetCurrentUserProfileAsync(usuario.Id, CancellationToken.None);

        profile.Should().NotBeNull();
        profile.Usuario.Should().Be("despachador.pwa");
        profile.Rol.Should().Be("DESPACHADOR");
        profile.StationId.Should().Be(estacion.Id);
        profile.EstacionNombre.Should().Be("Estación Central");
    }

    [Fact]
    public async Task RefreshAsync_RotaTokenYNoPermiteReutilizacion()
    {
        using var context = CreateInMemoryContext();
        var tokenService = CreateTokenService();
        var auditService = new FakeAuditService();
        var jwtOptions = Options.Create(new JwtOptions { RefreshTokenDays = 7 });

        var rol = new Rol { Id = (short)RolUsuario.SUPERVISOR, Nombre = "SUPERVISOR" };
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            NombreUsuario = "sup1",
            Email = "sup1@test.com",
            RolId = rol.Id,
            Rol = rol,
            Activo = true,
            PasswordHash = "hash"
        };
        context.Roles.Add(rol);
        context.Usuarios.Add(usuario);

        var rawRefresh = tokenService.GenerarRefreshTokenValue();
        var refreshHash = tokenService.HashRefreshToken(rawRefresh);
        var tokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            TokenHash = refreshHash,
            FechaCreacion = DateTime.UtcNow,
            FechaExpiracion = DateTime.UtcNow.AddDays(7)
        };
        context.RefreshTokens.Add(tokenEntity);
        await context.SaveChangesAsync();

        var sut = new AuthService(context, tokenService, auditService, jwtOptions);

        // Primer refresh: exitoso
        var refreshResult = await sut.RefreshAsync(rawRefresh, "127.0.0.1", CancellationToken.None);
        refreshResult.Should().NotBeNull();
        refreshResult.RefreshToken.Should().NotBe(rawRefresh);

        // Segundo intento con el token viejo ya rotado: debe fallar por reutilización
        var act = () => sut.RefreshAsync(rawRefresh, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<ApiException>();
    }
}
