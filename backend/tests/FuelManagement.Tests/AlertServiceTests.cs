using FluentAssertions;
using FuelManagement.Alerts.Detectors;
using FuelManagement.Alerts.Dtos;
using FuelManagement.Alerts.Services;
using FuelManagement.Shared.Data;
using FuelManagement.Shared.Domain;
using FuelManagement.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FuelManagement.Tests;

public class AlertServiceTests
{
    private readonly DbContextOptions<FuelDbContext> _dbOptions;
    private readonly Mock<ICurrentUserService> _mockUserService;
    private readonly Mock<ILogger<AlertService>> _mockLogger;

    public AlertServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<FuelDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _mockUserService = new Mock<ICurrentUserService>();
        _mockUserService.Setup(u => u.UserId).Returns(Guid.NewGuid());
        _mockUserService.Setup(u => u.UserName).Returns("admin.test");

        _mockLogger = new Mock<ILogger<AlertService>>();
    }

    [Fact]
    public async Task LowInventoryDetector_DetectsTankBelowCriticalLevel()
    {
        // Arrange
        using var db = new FuelDbContext(_dbOptions);
        db.Tanques.Add(new Tanque
        {
            Id = Guid.NewGuid(),
            Codigo = "TK-01",
            Nombre = "Tanque Diesel 1",
            CapacidadMaxima = 10000,
            StockActual = 500,
            NivelCritico = 1000,
            Activo = true
        });
        await db.SaveChangesAsync();

        var detector = new LowInventoryDetector();

        // Act
        var alerts = await detector.DetectAlertsAsync(db);

        // Assert
        alerts.Should().HaveCount(1);
        alerts[0].Tipo.Should().Be("STOCK_BAJO");
        alerts[0].Severidad.Should().Be("CRITICA");
    }

    [Fact]
    public async Task AcknowledgeAsync_UpdatesAlertStateToReconocida()
    {
        // Arrange
        using var db = new FuelDbContext(_dbOptions);
        var alertId = Guid.NewGuid();
        db.AlertasOperativas.Add(new AlertaOperativa
        {
            Id = alertId,
            Tipo = "STOCK_BAJO",
            Severidad = "ADVERTENCIA",
            EntidadOrigen = "INVENTARIO",
            Mensaje = "Stock bajo detectado",
            Estado = "ACTIVA",
            FechaCreacion = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new AlertService(db, new List<IAlertDetector>(), _mockUserService.Object, _mockLogger.Object);

        // Act
        var result = await service.AcknowledgeAsync(alertId, new AcknowledgeAlertRequest { Nota = "Revisado" });

        // Assert
        result.Estado.Should().Be("RECONOCIDA");
        result.FechaReconocimiento.Should().NotBeNull();
    }
}
