using CombustibleAPI.Application.Dtos.Closures;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Domain.Entities;
using CombustibleAPI.Infrastructure.Persistence;
using CombustibleAPI.Infrastructure.Persistence.DbFunctions;
using CombustibleAPI.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CombustibleAPI.Tests.Services;

public class InventoryAndClosureTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private class FakeAuditService : IAuditService
    {
        public Task RegistrarAsync(Guid? usuarioId, string accion, string entidad, string? entidadId,
            string? ipAddress, object? datosAnteriores, object? datosNuevos, CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public async Task GetAvailabilityAsync_CalculaFisicoReservadoDisponibleYTanques()
    {
        using var context = CreateInMemoryContext();
        var est = new Estacion { Id = Guid.NewGuid(), Codigo = "E1", Nombre = "Estación A" };
        var comb = new TipoCombustible { Id = 1, Codigo = "DIESEL", Nombre = "Diésel" };
        var t1 = new Tanque { Id = Guid.NewGuid(), EstacionId = est.Id, TipoCombustibleId = 1, Codigo = "T1", CapacidadMaxima = 1000, StockActual = 400, Activo = true };
        var t2 = new Tanque { Id = Guid.NewGuid(), EstacionId = est.Id, TipoCombustibleId = 1, Codigo = "T2", CapacidadMaxima = 800, StockActual = 300, Activo = true };

        var ticketReserva = new Ticket
        {
            Id = Guid.NewGuid(),
            EstacionId = est.Id,
            TipoCombustibleId = 1,
            NumeroTicket = "COM-2026-000010",
            CantidadAutorizada = 150,
            Estado = "CREADO",
            TokenQrHash = "H1",
            FirmaQr = "S1",
            FechaExpiracion = DateTime.UtcNow.AddDays(1)
        };

        context.Estaciones.Add(est);
        context.TiposCombustible.Add(comb);
        context.Tanques.AddRange(t1, t2);
        context.Tickets.Add(ticketReserva);
        await context.SaveChangesAsync();

        var sut = new InventoryService(context, new SqlFunctionsRepository());

        var res = await sut.GetAvailabilityAsync(est.Id, 1, CancellationToken.None);

        res.Should().NotBeNull();
        res.StockFisico.Should().Be(700); // 400 + 300
        res.StockReservado.Should().Be(150);
        res.StockDisponible.Should().Be(550); // 700 - 150
        res.TanquesCompatibles.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateDailyClosureAsync_CreaCierrePendienteYAprueba()
    {
        using var context = CreateInMemoryContext();
        var est = new Estacion { Id = Guid.NewGuid(), Codigo = "E1", Nombre = "Estación B" };
        var comb = new TipoCombustible { Id = 1, Codigo = "GASOLINA", Nombre = "Gasolina" };
        var t = new Tanque { Id = Guid.NewGuid(), EstacionId = est.Id, Estacion = est, TipoCombustibleId = 1, TipoCombustible = comb, Codigo = "T-GAS", CapacidadMaxima = 1000, StockActual = 600, Activo = true };
        var despachador = new Usuario { Id = Guid.NewGuid(), NombreUsuario = "desp.cierre", Email = "dc@t.com", PasswordHash = "h" };
        var supervisor = new Usuario { Id = Guid.NewGuid(), NombreUsuario = "sup.cierre", Email = "sc@t.com", PasswordHash = "h" };

        context.Estaciones.Add(est);
        context.TiposCombustible.Add(comb);
        context.Tanques.Add(t);
        context.Usuarios.AddRange(despachador, supervisor);
        await context.SaveChangesAsync();

        var sut = new ClosureService(context, new SqlFunctionsRepository(), new FakeAuditService());

        var req = new CreateDailyClosureRequestDto
        {
            TanqueId = t.Id,
            Fecha = DateOnly.FromDateTime(DateTime.UtcNow),
            StockFisicoFinal = 600,
            Observaciones = "Cierre normal sin diferencia"
        };

        var cierre = await sut.CreateDailyClosureAsync(req, despachador.Id, CancellationToken.None);
        cierre.Should().NotBeNull();
        cierre.Estado.Should().Be("PENDIENTE_APROBACION");
        cierre.StockFisicoFinal.Should().Be(600);

        // Aprobar cierre
        await sut.ApproveClosureAsync(cierre.Id, supervisor.Id, CancellationToken.None);

        var guardado = await context.CierresDiarios.FindAsync(cierre.Id);
        guardado!.Estado.Should().Be("APROBADO");
        guardado.RevisadoPorUsuarioId.Should().Be(supervisor.Id);
    }

    [Fact]
    public async Task RejectClosureAsync_RechazaCierreConMotivo()
    {
        using var context = CreateInMemoryContext();
        var est = new Estacion { Id = Guid.NewGuid(), Codigo = "E1", Nombre = "Estación C" };
        var t = new Tanque { Id = Guid.NewGuid(), EstacionId = est.Id, Estacion = est, TipoCombustibleId = 1, Codigo = "T3", CapacidadMaxima = 1000, StockActual = 500, Activo = true };
        var despachador = new Usuario { Id = Guid.NewGuid(), NombreUsuario = "desp2", Email = "d2@t.com", PasswordHash = "h" };
        var supervisor = new Usuario { Id = Guid.NewGuid(), NombreUsuario = "sup2", Email = "s2@t.com", PasswordHash = "h" };

        context.Estaciones.Add(est);
        context.Tanques.Add(t);
        context.Usuarios.AddRange(despachador, supervisor);
        await context.SaveChangesAsync();

        var sut = new ClosureService(context, new SqlFunctionsRepository(), new FakeAuditService());

        var req = new CreateDailyClosureRequestDto
        {
            TanqueId = t.Id,
            Fecha = DateOnly.FromDateTime(DateTime.UtcNow),
            StockFisicoFinal = 480,
            MotivoDiferencia = "Evaporación leve",
            Observaciones = "Diferencia de 20 galones"
        };

        var cierre = await sut.CreateDailyClosureAsync(req, despachador.Id, CancellationToken.None);

        await sut.RejectClosureAsync(cierre.Id, supervisor.Id, "Diferencia excede el límite permitido sin acta", CancellationToken.None);

        var guardado = await context.CierresDiarios.FindAsync(cierre.Id);
        guardado!.Estado.Should().Be("RECHAZADO");
        guardado.MotivoRechazo.Should().Be("Diferencia excede el límite permitido sin acta");
    }
}
