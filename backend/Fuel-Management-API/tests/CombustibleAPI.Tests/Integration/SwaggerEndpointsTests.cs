using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CombustibleAPI.Tests.Integration;

public class SwaggerEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SwaggerEndpointsTests(WebApplicationFactory<Program> factory)
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
                    ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test"
                });
            });
        });
    }

    [Fact]
    public async Task SwaggerJson_ContieneTodosLosEndpointsRequeridos()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        var jsonString = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonString);
        var root = doc.RootElement;

        root.TryGetProperty("paths", out var paths).Should().BeTrue();

        // 1. Endpoints de la captura de Swagger
        paths.TryGetProperty("/health", out var healthPath).Should().BeTrue();
        healthPath.TryGetProperty("get", out _).Should().BeTrue();

        paths.TryGetProperty("/auth/login", out var loginPath).Should().BeTrue();
        loginPath.TryGetProperty("post", out _).Should().BeTrue();

        paths.TryGetProperty("/auth/refresh", out var refreshPath).Should().BeTrue();
        refreshPath.TryGetProperty("post", out _).Should().BeTrue();

        paths.TryGetProperty("/auth/logout", out var logoutPath).Should().BeTrue();
        logoutPath.TryGetProperty("post", out _).Should().BeTrue();

        paths.TryGetProperty("/masters/roles", out var rolesPath).Should().BeTrue();
        rolesPath.TryGetProperty("get", out _).Should().BeTrue();

        paths.TryGetProperty("/masters/estaciones", out var estacionesPath).Should().BeTrue();
        estacionesPath.TryGetProperty("get", out _).Should().BeTrue();

        paths.TryGetProperty("/masters/vehiculos", out var vehiculosPath).Should().BeTrue();
        vehiculosPath.TryGetProperty("get", out _).Should().BeTrue();

        paths.TryGetProperty("/tickets/validate", out var validatePath).Should().BeTrue();
        validatePath.TryGetProperty("post", out _).Should().BeTrue();

        paths.TryGetProperty("/dispatches", out var dispatchesPath).Should().BeTrue();
        dispatchesPath.TryGetProperty("post", out _).Should().BeTrue();
        dispatchesPath.TryGetProperty("get", out _).Should().BeTrue(); // GET /dispatches paginado

        paths.TryGetProperty("/dispatches/{id}", out var dispatchIdPath).Should().BeTrue();
        dispatchIdPath.TryGetProperty("get", out _).Should().BeTrue();

        paths.TryGetProperty("/inventory/availability/{estacionId}/{tipoCombustibleId}", out var availPath).Should().BeTrue();
        availPath.TryGetProperty("get", out _).Should().BeTrue();

        paths.TryGetProperty("/closures/daily", out var closureDailyPath).Should().BeTrue();
        closureDailyPath.TryGetProperty("post", out _).Should().BeTrue();

        paths.TryGetProperty("/closures/{id}/approve", out var approvePath).Should().BeTrue();
        approvePath.TryGetProperty("put", out _).Should().BeTrue();

        paths.TryGetProperty("/closures/{id}/reject", out var rejectPath).Should().BeTrue();
        rejectPath.TryGetProperty("put", out _).Should().BeTrue();

        // 2. Endpoints adicionales acordados
        paths.TryGetProperty("/auth/me", out var mePath).Should().BeTrue();
        mePath.TryGetProperty("get", out _).Should().BeTrue();

        paths.TryGetProperty("/masters/tanques", out var tanquesPath).Should().BeTrue();
        tanquesPath.TryGetProperty("get", out _).Should().BeTrue();

        paths.TryGetProperty("/masters/estaciones/{stationId}/tanques", out var stationTanquesPath).Should().BeTrue();
        stationTanquesPath.TryGetProperty("get", out _).Should().BeTrue();
    }
}
