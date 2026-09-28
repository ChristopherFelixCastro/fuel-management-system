using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CombustibleAPI.Application.Common;
using CombustibleAPI.Application.Dtos.Common;
using CombustibleAPI.Application.Dtos.PublicTickets;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Domain.Entities;
using CombustibleAPI.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CombustibleAPI.Tests.Integration;

public class PublicTicketsEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private const string TestSecret = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    public PublicTicketsEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "unit-test-secret-key-at-least-32-characters-long",
                    ["Jwt:Issuer"] = "CombustibleAPI.Tests",
                    ["Jwt:Audience"] = "CombustibleAPI.Tests.Clients",
                    ["PublicTicket:Secret"] = TestSecret,
                    ["PublicTicket:BaseUrl"] = "https://la-bomba-admin.pages.dev",
                    ["PublicTicket:ExpirationDays"] = "7",
                    ["QrSecurity:SecretKey"] = "unit-test-qr-secret-key-at-least-32-chars-long",
                    ["Notifications:Enabled"] = "false"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(AppDbContext)).ToList();

                foreach (var d in descriptors)
                {
                    services.Remove(d);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("IntegrationTest_PublicTickets_Shared");
                });
            });
        });
    }

    private async Task<(string tokenReal, Ticket ticket)> SembrarTicketAsync(bool vencido = false)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publicTicketService = scope.ServiceProvider.GetRequiredService<IPublicTicketService>();
        var qrCodeService = scope.ServiceProvider.GetRequiredService<IQrCodeService>();

        var depId = Guid.NewGuid();
        var dep = new Departamento { Id = depId, Codigo = $"IT-{Guid.NewGuid():N}"[..10], Nombre = "Tecnología" };
        var emp = new Empleado
        {
            Id = Guid.NewGuid(),
            DepartamentoId = depId,
            CodigoEmpleado = $"EMP-{Guid.NewGuid():N}"[..8],
            Nombre = "Carlos",
            Apellido = "Sánchez",
            Cedula = "402-0000000-1",
            Email = "carlos@labomba.com",
            Telefono = "8095559999"
        };

        if (!await db.TiposCombustible.AnyAsync(x => x.Id == 1))
        {
            db.TiposCombustible.Add(new TipoCombustible { Id = 1, Codigo = "DIESEL", Nombre = "Diésel Óptimo" });
        }

        var veh = new Vehiculo
        {
            Id = Guid.NewGuid(),
            DepartamentoId = depId,
            TipoCombustibleId = 1,
            Placa = $"L{Random.Shared.Next(100000, 999999)}",
            Ficha = "F-99",
            CapacidadTanque = 50m
        };
        var est = new Estacion { Id = Guid.NewGuid(), Codigo = $"E-{Guid.NewGuid():N}"[..8], Nombre = "Estación Norte" };
        var usr = new Usuario
        {
            Id = Guid.NewGuid(),
            NombreUsuario = $"usr_{Guid.NewGuid():N}"[..12],
            Email = "carlos@labomba.com",
            PasswordHash = "hash"
        };

        var sol = new Solicitud
        {
            Id = Guid.NewGuid(),
            EmpleadoId = emp.Id,
            VehiculoId = veh.Id,
            DepartamentoId = depId,
            CreadaPorUsuarioId = usr.Id,
            CantidadSolicitada = 30m,
            Estado = "APROBADA",
            FechaSolicitud = DateTime.UtcNow
        };

        var ticketId = Guid.NewGuid();
        var qrSecurity = qrCodeService.GenerateQrSecurityData(ticketId);

        var ticket = new Ticket
        {
            Id = ticketId,
            SolicitudId = sol.Id,
            EstacionId = est.Id,
            TipoCombustibleId = 1,
            NumeroTicket = $"COM-2026-{Random.Shared.Next(100000, 999999)}",
            CantidadAutorizada = 30m,
            Estado = "CREADO",
            TokenQrHash = qrSecurity.TokenHash,
            FirmaQr = qrSecurity.Signature,
            FechaEmision = DateTime.UtcNow,
            FechaExpiracion = vencido ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddDays(3)
        };

        db.Departamentos.Add(dep);
        db.Empleados.Add(emp);
        db.Vehiculos.Add(veh);
        db.Estaciones.Add(est);
        db.Usuarios.Add(usr);
        db.Solicitudes.Add(sol);
        db.Tickets.Add(ticket);

        await db.SaveChangesAsync();

        var (tokenReal, _, _) = await publicTicketService.GenerarTokenAccesoAsync(ticketId, ticket.FechaExpiracion, CancellationToken.None);

        return (tokenReal, ticket);
    }

    [Fact]
    public async Task GetPublicTicket_ConTokenValido_Retorna200_YSinCamposSensibles()
    {
        var client = _factory.CreateClient();
        var (tokenReal, ticket) = await SembrarTicketAsync();

        var response = await client.GetAsync($"/public/tickets/{tokenReal}");
        var json = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: json);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("data", out var data).Should().BeTrue();
        data.GetProperty("numeroTicket").GetString().Should().Be(ticket.NumeroTicket);
        data.GetProperty("empleado").GetString().Should().Be("Carlos Sánchez");
        data.GetProperty("placa").GetString().Should().Be(ticket.Solicitud.Vehiculo.Placa);
        data.GetProperty("tipoCombustible").GetString().Should().Be("Diésel Óptimo");
        data.GetProperty("cantidadAutorizada").GetDecimal().Should().Be(30m);
        data.GetProperty("estacion").GetString().Should().Be("Estación Norte");
        data.GetProperty("estado").GetString().Should().Be("ACTIVO");
        data.GetProperty("permiteDespacho").GetBoolean().Should().BeTrue();

        // Verificar que NO expone UUIDs de BD ni campos internos
        data.TryGetProperty("id", out _).Should().BeFalse();
        data.TryGetProperty("ticketId", out _).Should().BeFalse();
        data.TryGetProperty("solicitudId", out _).Should().BeFalse();
        data.TryGetProperty("tokenHash", out _).Should().BeFalse();
        data.TryGetProperty("nonce", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GetPublicTicketQr_ConTokenValido_RetornaImagePng()
    {
        var client = _factory.CreateClient();
        var (tokenReal, _) = await SembrarTicketAsync();

        var response = await client.GetAsync($"/public/tickets/{tokenReal}/qr");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("image/png");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty();
        // Encabezado mágico de PNG (0x89, 'P', 'N', 'G')
        bytes[0].Should().Be(0x89);
        bytes[1].Should().Be(0x50);
        bytes[2].Should().Be(0x4E);
        bytes[3].Should().Be(0x47);
    }

    [Fact]
    public async Task GetPublicTicket_ConTokenInvalido_Retorna404()
    {
        var client = _factory.CreateClient();
        var invalidToken = new string('a', 64);

        var response = await client.GetAsync($"/public/tickets/{invalidToken}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPublicTicket_ConTokenExpirado_Retorna422()
    {
        var client = _factory.CreateClient();
        var (tokenReal, _) = await SembrarTicketAsync(vencido: true);

        var response = await client.GetAsync($"/public/tickets/{tokenReal}");
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error.Should().NotBeNull();
        error!.Error.Code.Should().Be("ENLACE_EXPIRADO");
    }

    [Fact]
    public async Task SwaggerJson_ContieneRutasPublicas()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");

        paths.TryGetProperty("/public/tickets/{token}", out _).Should().BeTrue();
        paths.TryGetProperty("/public/tickets/{token}/qr", out _).Should().BeTrue();
    }
}
