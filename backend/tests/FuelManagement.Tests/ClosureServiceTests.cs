using FluentAssertions;
using FuelManagement.Closures.Dtos;
using FuelManagement.Closures.Services;
using FuelManagement.Shared.Data;
using FuelManagement.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FuelManagement.Tests;

public class ClosureServiceTests
{
    private readonly DbContextOptions<FuelDbContext> _dbOptions;

    public ClosureServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<FuelDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsClosure_WhenExists()
    {
        // Arrange
        using var db = new FuelDbContext(_dbOptions);
        var closureId = Guid.NewGuid();
        var tankId = Guid.NewGuid();
        var stationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.VwCierresResumen.Add(new VwCierresResumen
        {
            CierreId = closureId,
            TanqueId = tankId,
            TanqueCodigo = "TK-01",
            TanqueNombre = "Tanque 1",
            EstacionId = stationId,
            EstacionCodigo = "EST-01",
            EstacionNombre = "Estacion Central",
            CombustibleCodigo = "DSL",
            CombustibleNombre = "Diesel",
            FechaCierre = new DateOnly(2026, 9, 12),
            StockInicial = 5000,
            StockFisicoFinal = 4800,
            StockTeoricoFinal = 4800,
            Diferencia = 0,
            Estado = "APROBADO",
            CreadoPor = "test.user",
            FechaCreacion = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ClosureService(db);

        // Act
        var result = await service.GetByIdAsync(closureId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(closureId);
        result.Estado.Should().Be("APROBADO");
    }

    [Fact]
    public async Task GeneratePdfAsync_ReturnsPdfBytes_WhenClosureExists()
    {
        // Arrange
        using var db = new FuelDbContext(_dbOptions);
        var closureId = Guid.NewGuid();

        db.VwCierresResumen.Add(new VwCierresResumen
        {
            CierreId = closureId,
            TanqueId = Guid.NewGuid(),
            TanqueCodigo = "TK-01",
            TanqueNombre = "Tanque 1",
            EstacionId = Guid.NewGuid(),
            EstacionCodigo = "EST-01",
            EstacionNombre = "Estacion Central",
            CombustibleCodigo = "DSL",
            CombustibleNombre = "Diesel",
            FechaCierre = new DateOnly(2026, 9, 12),
            StockInicial = 1000,
            StockFisicoFinal = 950,
            StockTeoricoFinal = 950,
            Diferencia = 0,
            Estado = "APROBADO",
            CreadoPor = "test.user",
            FechaCreacion = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ClosureService(db);

        // Act
        var pdfBytes = await service.GeneratePdfAsync(closureId);

        // Assert
        pdfBytes.Should().NotBeNullOrEmpty();
        pdfBytes.Length.Should().BeGreaterThan(100);
    }
}
