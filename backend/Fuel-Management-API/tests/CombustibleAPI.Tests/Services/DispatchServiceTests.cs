using CombustibleAPI.Application.Dtos.Dispatches;
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

public class DispatchServiceTests
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

    private static (AppDbContext context, Ticket ticket, Tanque tanque, Vehiculo vehiculo, Estacion estacion, Usuario despachador) SetupData(AppDbContext context)
    {
        var estacion = new Estacion { Id = Guid.NewGuid(), Codigo = "EST-01", Nombre = "Estación Central" };
        var combustible = new TipoCombustible { Id = 1, Codigo = "DIESEL", Nombre = "Diésel" };
        var tanque = new Tanque
        {
            Id = Guid.NewGuid(),
            EstacionId = estacion.Id,
            Estacion = estacion,
            TipoCombustibleId = combustible.Id,
            TipoCombustible = combustible,
            Codigo = "TNQ-01",
            CapacidadMaxima = 1000,
            StockActual = 500,
            Activo = true
        };
        var dep = new Departamento { Id = Guid.NewGuid(), Codigo = "DEP-1", Nombre = "Logística" };
        var vehiculo = new Vehiculo
        {
            Id = Guid.NewGuid(),
            DepartamentoId = dep.Id,
            Departamento = dep,
            TipoCombustibleId = combustible.Id,
            TipoCombustible = combustible,
            Placa = "TEST01",
            Ficha = "FICHA-01",
            OdometroActual = 5000,
            CapacidadTanque = 100,
            Activo = true
        };
        var emp = new Empleado
        {
            Id = Guid.NewGuid(),
            DepartamentoId = dep.Id,
            Departamento = dep,
            CodigoEmpleado = "E-01",
            Nombre = "Pedro",
            Apellido = "Gómez",
            Cedula = "001"
        };
        var despachador = new Usuario
        {
            Id = Guid.NewGuid(),
            NombreUsuario = "desp1",
            Email = "d@t.com",
            PasswordHash = "h",
            EstacionId = estacion.Id,
            Estacion = estacion,
            Activo = true
        };
        var sol = new Solicitud
        {
            Id = Guid.NewGuid(),
            EmpleadoId = emp.Id,
            Empleado = emp,
            VehiculoId = vehiculo.Id,
            Vehiculo = vehiculo,
            DepartamentoId = dep.Id,
            Departamento = dep,
            CreadaPorUsuarioId = despachador.Id,
            CreadaPorUsuario = despachador,
            TipoSolicitud = "MANUAL",
            CantidadSolicitada = 40,
            CantidadAutorizada = 40,
            Estado = "APROBADA"
        };
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            SolicitudId = sol.Id,
            Solicitud = sol,
            EstacionId = estacion.Id,
            Estacion = estacion,
            TipoCombustibleId = combustible.Id,
            TipoCombustible = combustible,
            NumeroTicket = "COM-2026-111111",
            CantidadAutorizada = 40,
            Estado = "CREADO",
            TokenQrHash = "TK_HASH_1",
            FirmaQr = "SIG",
            FechaEmision = DateTime.UtcNow,
            FechaExpiracion = DateTime.UtcNow.AddDays(2)
        };

        context.Estaciones.Add(estacion);
        context.TiposCombustible.Add(combustible);
        context.Tanques.Add(tanque);
        context.Departamentos.Add(dep);
        context.Vehiculos.Add(vehiculo);
        context.Empleados.Add(emp);
        context.Usuarios.Add(despachador);
        context.Solicitudes.Add(sol);
        context.Tickets.Add(ticket);
        context.SaveChanges();

        return (context, ticket, tanque, vehiculo, estacion, despachador);
    }

    [Fact]
    public async Task RegistrarDespachoAsync_DatosValidos_DespachaActualizaStockYTicket()
    {
        using var context = CreateInMemoryContext();
        var (_, ticket, tanque, vehiculo, estacion, despachador) = SetupData(context);
        var sut = new DispatchService(context, new SqlFunctionsRepository(), new FakeAuditService());

        var request = new DispatchRequestDto
        {
            TicketId = ticket.Id,
            TanqueId = tanque.Id,
            Galones = 40,
            Odometro = 5150,
            Observacion = "Tanque lleno"
        };

        var result = await sut.RegistrarDespachoAsync(request, despachador.Id, estacion.Id, "127.0.0.1", CancellationToken.None);

        result.Should().NotBeNull();
        result.GalonesDespachados.Should().Be(40);
        result.NumeroTicket.Should().Be("COM-2026-111111");
        result.SaldoResultanteTanque.Should().Be(460); // 500 - 40

        var ticketActualizado = await context.Tickets.FindAsync(ticket.Id);
        ticketActualizado!.Estado.Should().Be("CONSUMIDO");

        var tanqueActualizado = await context.Tanques.FindAsync(tanque.Id);
        tanqueActualizado!.StockActual.Should().Be(460);

        var vehiculoActualizado = await context.Vehiculos.FindAsync(vehiculo.Id);
        vehiculoActualizado!.OdometroActual.Should().Be(5150);

        var despachos = await context.Despachos.ToListAsync();
        despachos.Should().HaveCount(1);
        despachos[0].CantidadDespachada.Should().Be(40);

        var movimientos = await context.MovimientosInventario.ToListAsync();
        movimientos.Should().HaveCount(1);
        movimientos[0].TipoMovimiento.Should().Be("DESPACHO");
        movimientos[0].SaldoAnterior.Should().Be(500);
        movimientos[0].SaldoPosterior.Should().Be(460);
    }

    [Fact]
    public async Task RegistrarDespachoAsync_CantidadNoCoincideConAutorizada_Rechaza()
    {
        using var context = CreateInMemoryContext();
        var (_, ticket, tanque, _, estacion, despachador) = SetupData(context);
        var sut = new DispatchService(context, new SqlFunctionsRepository(), new FakeAuditService());

        var request = new DispatchRequestDto
        {
            TicketId = ticket.Id,
            TanqueId = tanque.Id,
            Galones = 25, // Ticket tiene 40 autorizados
            Odometro = 5150
        };

        var act = () => sut.RegistrarDespachoAsync(request, despachador.Id, estacion.Id, "127.0.0.1", CancellationToken.None);
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Code.Should().Be("CANTIDAD_DEBE_SER_IGUAL_A_AUTORIZADA");
    }

    [Fact]
    public async Task RegistrarDespachoAsync_EstacionInvalida_Rechaza()
    {
        using var context = CreateInMemoryContext();
        var (_, ticket, tanque, _, _, despachador) = SetupData(context);
        var sut = new DispatchService(context, new SqlFunctionsRepository(), new FakeAuditService());

        var otraEstacionId = Guid.NewGuid();
        var request = new DispatchRequestDto
        {
            TicketId = ticket.Id,
            TanqueId = tanque.Id,
            Galones = 40,
            Odometro = 5150
        };

        var act = () => sut.RegistrarDespachoAsync(request, despachador.Id, otraEstacionId, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<ApiException>();
    }

    [Fact]
    public async Task GetPaginatedAsync_FiltraPorEstacionYFecha()
    {
        using var context = CreateInMemoryContext();
        var (_, ticket, tanque, _, estacion, despachador) = SetupData(context);
        var sut = new DispatchService(context, new SqlFunctionsRepository(), new FakeAuditService());

        var d1 = new Despacho
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            Ticket = ticket,
            TanqueId = tanque.Id,
            Tanque = tanque,
            DespachadorUsuarioId = despachador.Id,
            Despachador = despachador,
            CantidadDespachada = 40,
            OdometroRegistrado = 5200,
            FechaDespacho = DateTime.UtcNow.AddHours(-2)
        };
        context.Despachos.Add(d1);
        await context.SaveChangesAsync();

        var filter = new DispatchesFilterDto
        {
            EstacionId = estacion.Id,
            Page = 1,
            PageSize = 10
        };

        var list = await sut.GetPaginatedAsync(filter, CancellationToken.None);
        list.TotalCount.Should().Be(1);
        list.Items.Should().HaveCount(1);
        list.Items[0].NumeroTicket.Should().Be(ticket.NumeroTicket);
    }
}
