using CombustibleAPI.Application.Dtos.Requests;
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

public class RequestServiceTests
{
    private sealed class Audit : IAuditService
    {
        public Task RegistrarAsync(Guid? usuarioId, string accion, string entidad, string? entidadId, string? ipAddress, object? datosAnteriores, object? datosNuevos, CancellationToken ct) => Task.CompletedTask;
    }

    private static AppDbContext Context() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<(AppDbContext context, Usuario usuario, Empleado empleado, Vehiculo vehiculo, Estacion estacion)> SembrarAsync()
    {
        var context = Context();
        var dep = new Departamento { Id = Guid.NewGuid(), Codigo = "TR", Nombre = "Transporte" };
        var combustible = new TipoCombustible { Id = 1, Codigo = "DIESEL", Nombre = "Diésel" };
        var usuario = new Usuario { Id = Guid.NewGuid(), NombreUsuario = "solicitante", Email = "s@x.test", PasswordHash = "hash" };
        var empleado = new Empleado { Id = Guid.NewGuid(), DepartamentoId = dep.Id, CodigoEmpleado = "E1", Nombre = "Ana", Apellido = "Lora", Cedula = "001" };
        var vehiculo = new Vehiculo { Id = Guid.NewGuid(), DepartamentoId = dep.Id, TipoCombustibleId = combustible.Id, Placa = "A123", Ficha = "F1", CapacidadTanque = 80 };
        var estacion = new Estacion { Id = Guid.NewGuid(), Codigo = "E1", Nombre = "Norte" };
        context.AddRange(dep, combustible, usuario, empleado, vehiculo, estacion, new Tanque { Id = Guid.NewGuid(), EstacionId = estacion.Id, TipoCombustibleId = combustible.Id, Codigo = "T1", CapacidadMaxima = 500, StockActual = 100 });
        await context.SaveChangesAsync();
        return (context, usuario, empleado, vehiculo, estacion);
    }

    [Fact]
    public async Task CrearAsync_DerivaCombustibleDelVehiculo_Y_NoExigeStock()
    {
        var (context, usuario, empleado, vehiculo, _) = await SembrarAsync();
        (await context.Tanques.SingleAsync()).StockActual = 0m;
        await context.SaveChangesAsync();
        var service = new RequestService(context, new SqlFunctionsRepository(), new Audit());
        var result = await service.CrearAsync(new CreateRequestDto { EmpleadoId = empleado.Id, VehiculoId = vehiculo.Id, DepartamentoId = empleado.DepartamentoId, CantidadSolicitada = 30 }, usuario.Id, default);
        result.Estado.Should().Be("PENDIENTE");
        result.TipoCombustibleId.Should().Be(vehiculo.TipoCombustibleId);
    }

    [Fact]
    public async Task AprobarAsync_CreaTicketCreado_SinReducirStockFisico()
    {
        var (context, usuario, empleado, vehiculo, estacion) = await SembrarAsync();
        var service = new RequestService(context, new SqlFunctionsRepository(), new Audit());
        var solicitud = await service.CrearAsync(new CreateRequestDto { EmpleadoId = empleado.Id, VehiculoId = vehiculo.Id, DepartamentoId = empleado.DepartamentoId, CantidadSolicitada = 30 }, usuario.Id, default);
        var result = await service.AprobarAsync(solicitud.Id, new ApproveRequestDto { EstacionId = estacion.Id, CantidadAutorizada = 25, FechaExpiracion = DateTime.UtcNow.AddHours(2) }, Guid.NewGuid(), default);
        var ticket = await context.Tickets.SingleAsync();
        result.Estado.Should().Be("APROBADA");
        ticket.Estado.Should().Be("CREADO");
        ticket.TipoCombustibleId.Should().Be(vehiculo.TipoCombustibleId);
        (await context.Tanques.SingleAsync()).StockActual.Should().Be(100);
    }

    [Fact]
    public async Task SolicitudDeOtroSolicitante_NoPuedeLeerse()
    {
        var (context, usuario, empleado, vehiculo, _) = await SembrarAsync();
        var service = new RequestService(context, new SqlFunctionsRepository(), new Audit());
        var solicitud = await service.CrearAsync(new CreateRequestDto { EmpleadoId = empleado.Id, VehiculoId = vehiculo.Id, DepartamentoId = empleado.DepartamentoId, CantidadSolicitada = 20 }, usuario.Id, default);
        var action = () => service.ObtenerAsync(solicitud.Id, Guid.NewGuid(), "SOLICITANTE", default);
        await action.Should().ThrowAsync<ApiException>().Where(x => x.Code == "FORBIDDEN");
    }
}
