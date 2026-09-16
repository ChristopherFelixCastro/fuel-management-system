using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Domain.Entities;
using CombustibleAPI.Infrastructure.Persistence;
using CombustibleAPI.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CombustibleAPI.Tests.Services;

public class TicketServiceTests
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
    public async Task ValidarAsync_TicketValido_RetornaDatosCompletosRequeridos()
    {
        using var context = CreateInMemoryContext();
        var audit = new FakeAuditService();

        var dep = new Departamento { Id = Guid.NewGuid(), Codigo = "DEP-01", Nombre = "Transporte" };
        var emp = new Empleado { Id = Guid.NewGuid(), DepartamentoId = dep.Id, Departamento = dep, CodigoEmpleado = "EMP-100", Nombre = "Juan", Apellido = "Pérez", Cedula = "402-0000000-1" };
        var comb = new TipoCombustible { Id = 1, Codigo = "DIESEL", Nombre = "Diésel" };
        var veh = new Vehiculo { Id = Guid.NewGuid(), DepartamentoId = dep.Id, Departamento = dep, TipoCombustibleId = comb.Id, TipoCombustible = comb, Placa = "L123456", Ficha = "F-01", Marca = "Toyota", Modelo = "Hilux", OdometroActual = 10000, CapacidadTanque = 80 };
        var est = new Estacion { Id = Guid.NewGuid(), Codigo = "EST-01", Nombre = "Estación Norte" };
        var usr = new Usuario { Id = Guid.NewGuid(), NombreUsuario = "creator", Email = "c@t.com", PasswordHash = "h" };

        var sol = new Solicitud
        {
            Id = Guid.NewGuid(),
            EmpleadoId = emp.Id,
            Empleado = emp,
            VehiculoId = veh.Id,
            Vehiculo = veh,
            DepartamentoId = dep.Id,
            Departamento = dep,
            CreadaPorUsuarioId = usr.Id,
            CreadaPorUsuario = usr,
            TipoSolicitud = "MANUAL",
            CantidadSolicitada = 50,
            CantidadAutorizada = 50,
            Estado = "APROBADA"
        };

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            SolicitudId = sol.Id,
            Solicitud = sol,
            EstacionId = est.Id,
            Estacion = est,
            TipoCombustibleId = comb.Id,
            TipoCombustible = comb,
            NumeroTicket = "COM-2026-000001",
            CantidadAutorizada = 50,
            Estado = "CREADO",
            TokenQrHash = "QR_HASH_TEST_1",
            FirmaQr = "SIG_1",
            FechaEmision = DateTime.UtcNow,
            FechaExpiracion = DateTime.UtcNow.AddDays(1)
        };

        context.Departamentos.Add(dep);
        context.Empleados.Add(emp);
        context.TiposCombustible.Add(comb);
        context.Vehiculos.Add(veh);
        context.Estaciones.Add(est);
        context.Usuarios.Add(usr);
        context.Solicitudes.Add(sol);
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var sut = new TicketService(context, audit);

        var dto = await sut.ValidarAsync("COM-2026-000001", CancellationToken.None);

        dto.Should().NotBeNull();
        dto.TicketId.Should().Be(ticket.Id);
        dto.NumeroTicket.Should().Be("COM-2026-000001");
        dto.Empleado.Should().Be("Juan Pérez");
        dto.CodigoEmpleado.Should().Be("EMP-100");
        dto.Vehiculo.Should().Be("Toyota Hilux");
        dto.Placa.Should().Be("L123456");
        dto.Ficha.Should().Be("F-01");
        dto.TipoCombustible.Should().Be("Diésel");
        dto.CantidadAutorizada.Should().Be(50);
        dto.Vencimiento.Should().Be(ticket.FechaExpiracion);
        dto.Estado.Should().Be("CREADO");
        dto.EstadoEfectivo.Should().Be("PROXIMO_A_VENCER");
    }

    [Fact]
    public async Task ValidarAsync_TicketConsumido_LanzaExcepcion()
    {
        using var context = CreateInMemoryContext();
        var audit = new FakeAuditService();

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            NumeroTicket = "COM-2026-999999",
            Estado = "CONSUMIDO",
            TokenQrHash = "HASH_C",
            FirmaQr = "SIG_C",
            CantidadAutorizada = 20,
            FechaExpiracion = DateTime.UtcNow.AddDays(1)
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var sut = new TicketService(context, audit);

        var act = () => sut.ValidarAsync("COM-2026-999999", CancellationToken.None);
        await act.Should().ThrowAsync<ApiException>();
    }

    [Fact]
    public async Task ValidarAsync_TicketVencido_LanzaExcepcion()
    {
        using var context = CreateInMemoryContext();
        var audit = new FakeAuditService();

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            NumeroTicket = "COM-2026-888888",
            Estado = "CREADO",
            TokenQrHash = "HASH_V",
            FirmaQr = "SIG_V",
            CantidadAutorizada = 20,
            FechaExpiracion = DateTime.UtcNow.AddDays(-1)
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var sut = new TicketService(context, audit);

        var act = () => sut.ValidarAsync("COM-2026-888888", CancellationToken.None);
        await act.Should().ThrowAsync<ApiException>();
    }
}
