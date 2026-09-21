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
    private const string RawToken = "test-raw-token";
    private const string TokenHash = "test-token-hash";
    private const string Signature =
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    private static AppDbContext CreateInMemoryContext()
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(
                    databaseName: Guid.NewGuid().ToString())
                .Options;

        return new AppDbContext(options);
    }

    private class FakeAuditService : IAuditService
    {
        public Task RegistrarAsync(
            Guid? usuarioId,
            string accion,
            string entidad,
            string? entidadId,
            string? ipAddress,
            object? datosAnteriores,
            object? datosNuevos,
            CancellationToken ct)
            => Task.CompletedTask;
    }

    private class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? UsuarioId { get; set; } = Guid.NewGuid();

        public string? Rol { get; set; } = "DESPACHADOR";

        public Guid? EstacionId { get; set; }

        public string? IpAddress { get; set; } = "127.0.0.1";
    }

    private class FakeQrCodeService : IQrCodeService
    {
        public QrSecurityResult GenerateQrSecurityData(
            Guid ticketId)
        {
            var payload = BuildPayload(ticketId);

            return new QrSecurityResult(
                RawToken,
                TokenHash,
                Signature,
                payload);
        }

        public QrSecurityResult RebuildQrSecurityData(
            Guid ticketId)
        {
            return GenerateQrSecurityData(ticketId);
        }

        public bool VerifySignature(
            Guid ticketId,
            string rawToken,
            string signature)
        {
            return rawToken == RawToken &&
                   signature == Signature;
        }

        public bool VerifyTokenHash(
            string rawToken,
            string storedHash)
        {
            return rawToken == RawToken &&
                   storedHash == TokenHash;
        }

        public string HashToken(string rawToken)
        {
            return TokenHash;
        }

        public byte[] GenerateQrImagePng(
            string qrPayload)
        {
            return [0x89, 0x50, 0x4E, 0x47];
        }

        public (
            Guid TicketId,
            string RawToken,
            string Signature)?
            ParsePayload(string qrPayload)
        {
            try
            {
                using var document =
                    System.Text.Json.JsonDocument.Parse(
                        qrPayload);

                var root = document.RootElement;

                if (!root.TryGetProperty(
                        "ticketId",
                        out var idElement) ||
                    !root.TryGetProperty(
                        "token",
                        out var tokenElement) ||
                    !root.TryGetProperty(
                        "sig",
                        out var signatureElement))
                {
                    return null;
                }

                if (!Guid.TryParse(
                        idElement.GetString(),
                        out var ticketId))
                {
                    return null;
                }

                return (
                    ticketId,
                    tokenElement.GetString() ?? "",
                    signatureElement.GetString() ?? "");
            }
            catch
            {
                return null;
            }
        }

        public static string BuildPayload(
            Guid ticketId,
            string token = RawToken,
            string signature = Signature)
        {
            return System.Text.Json.JsonSerializer.Serialize(
                new
                {
                    ticketId,
                    token,
                    sig = signature
                });
        }
    }

    private static TicketService CreateService(
        AppDbContext context,
        Guid? estacionId,
        string rol = "DESPACHADOR",
        Guid? usuarioId = null)
    {
        return new TicketService(
            context,
            new FakeAuditService(),
            new FakeQrCodeService(),
            new FakeCurrentUserService
            {
                UsuarioId = usuarioId ?? Guid.NewGuid(),
                Rol = rol,
                EstacionId = estacionId
            });
    }

    [Fact]
    public async Task ValidarAsync_TicketValido_RetornaDatosCompletosRequeridos()
    {
        using var context =
            CreateInMemoryContext();

        var dep = new Departamento
        {
            Id = Guid.NewGuid(),
            Codigo = "DEP-01",
            Nombre = "Transporte"
        };

        var emp = new Empleado
        {
            Id = Guid.NewGuid(),
            DepartamentoId = dep.Id,
            Departamento = dep,
            CodigoEmpleado = "EMP-100",
            Nombre = "Juan",
            Apellido = "Pérez",
            Cedula = "402-0000000-1"
        };

        var comb = new TipoCombustible
        {
            Id = 1,
            Codigo = "DIESEL",
            Nombre = "Diésel"
        };

        var veh = new Vehiculo
        {
            Id = Guid.NewGuid(),
            DepartamentoId = dep.Id,
            Departamento = dep,
            TipoCombustibleId = comb.Id,
            TipoCombustible = comb,
            Placa = "L123456",
            Ficha = "F-01",
            Marca = "Toyota",
            Modelo = "Hilux",
            OdometroActual = 10000,
            CapacidadTanque = 80
        };

        var est = new Estacion
        {
            Id = Guid.NewGuid(),
            Codigo = "EST-01",
            Nombre = "Estación Norte"
        };

        var usr = new Usuario
        {
            Id = Guid.NewGuid(),
            NombreUsuario = "creator",
            Email = "c@t.com",
            PasswordHash = "h"
        };

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
            TokenQrHash = TokenHash,
            FirmaQr = Signature,
            FechaEmision = DateTime.UtcNow,
            FechaExpiracion =
                DateTime.UtcNow.AddDays(1)
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

        var sut =
            CreateService(context, est.Id);

        var qrPayload =
            FakeQrCodeService.BuildPayload(ticket.Id);

        var dto = await sut.ValidarAsync(
            qrPayload,
            CancellationToken.None);

        dto.Should().NotBeNull();
        dto.TicketId.Should().Be(ticket.Id);
        dto.NumeroTicket.Should()
            .Be("COM-2026-000001");
        dto.Empleado.Should().Be("Juan Pérez");
        dto.CodigoEmpleado.Should()
            .Be("EMP-100");
        dto.Vehiculo.Should()
            .Be("Toyota Hilux");
        dto.Placa.Should().Be("L123456");
        dto.Ficha.Should().Be("F-01");
        dto.TipoCombustible.Should()
            .Be("Diésel");
        dto.CantidadAutorizada.Should()
            .Be(50);
        dto.Vencimiento.Should()
            .Be(ticket.FechaExpiracion);
        dto.Estado.Should().Be("CREADO");
        dto.EstadoEfectivo.Should()
            .Be("PROXIMO_A_VENCER");
    }

    [Fact]
    public async Task ValidarAsync_TicketConsumido_LanzaExcepcion()
    {
        using var context =
            CreateInMemoryContext();

        var estacionId = Guid.NewGuid();

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            EstacionId = estacionId,
            NumeroTicket = "COM-2026-999999",
            Estado = "CONSUMIDO",
            TokenQrHash = TokenHash,
            FirmaQr = Signature,
            CantidadAutorizada = 20,
            FechaExpiracion =
                DateTime.UtcNow.AddDays(1)
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var sut =
            CreateService(context, estacionId);

        var qrPayload =
            FakeQrCodeService.BuildPayload(ticket.Id);

        var act = () =>
            sut.ValidarAsync(
                qrPayload,
                CancellationToken.None);

        var exception = await act.Should()
            .ThrowAsync<ApiException>();

        exception.Which.Code.Should()
            .Be("TICKET_CONSUMED");
    }

    [Fact]
    public async Task ValidarAsync_TicketVencido_LanzaExcepcion()
    {
        using var context =
            CreateInMemoryContext();

        var estacionId = Guid.NewGuid();

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            EstacionId = estacionId,
            NumeroTicket = "COM-2026-888888",
            Estado = "CREADO",
            TokenQrHash = TokenHash,
            FirmaQr = Signature,
            CantidadAutorizada = 20,
            FechaExpiracion =
                DateTime.UtcNow.AddDays(-1)
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var sut =
            CreateService(context, estacionId);

        var qrPayload =
            FakeQrCodeService.BuildPayload(ticket.Id);

        var act = () =>
            sut.ValidarAsync(
                qrPayload,
                CancellationToken.None);

        var exception = await act.Should()
            .ThrowAsync<ApiException>();

        exception.Which.Code.Should()
            .Be("TICKET_EXPIRED");
    }

    [Fact]
    public async Task ValidarAsync_QrManipulado_LanzaFirmaInvalida()
    {
        using var context =
            CreateInMemoryContext();

        var estacionId = Guid.NewGuid();

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            EstacionId = estacionId,
            NumeroTicket = "COM-2026-000010",
            Estado = "CREADO",
            TokenQrHash = TokenHash,
            FirmaQr = Signature,
            CantidadAutorizada = 20,
            FechaExpiracion =
                DateTime.UtcNow.AddDays(1)
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var sut =
            CreateService(context, estacionId);

        var qrManipulado =
            FakeQrCodeService.BuildPayload(
                ticket.Id,
                token: "token-manipulado",
                signature: Signature);

        var act = () =>
            sut.ValidarAsync(
                qrManipulado,
                CancellationToken.None);

        var exception = await act.Should()
            .ThrowAsync<ApiException>();

        exception.Which.Code.Should()
            .Be("FIRMA_INVALIDA");
    }

    [Fact]
    public async Task ValidarAsync_DespachadorOtraEstacion_LanzaForbidden()
    {
        using var context =
            CreateInMemoryContext();

        var estacionTicket = Guid.NewGuid();
        var estacionDespachador = Guid.NewGuid();

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            EstacionId = estacionTicket,
            NumeroTicket = "COM-2026-000011",
            Estado = "CREADO",
            TokenQrHash = TokenHash,
            FirmaQr = Signature,
            CantidadAutorizada = 20,
            FechaExpiracion =
                DateTime.UtcNow.AddDays(1)
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var sut =
            CreateService(
                context,
                estacionDespachador);

        var qrPayload =
            FakeQrCodeService.BuildPayload(ticket.Id);

        var act = () =>
            sut.ValidarAsync(
                qrPayload,
                CancellationToken.None);

        var exception = await act.Should()
            .ThrowAsync<ApiException>();

        exception.Which.Code.Should()
            .Be("FORBIDDEN");
    }

    [Fact]
    public async Task ObtenerQrPngAsync_SolicitantePropietario_RetornaPng()
    {
        using var context = CreateInMemoryContext();
        var usuarioId = Guid.NewGuid();

        var solicitud = new Solicitud
        {
            Id = Guid.NewGuid(),
            CreadaPorUsuarioId = usuarioId,
            Estado = "APROBADA",
            CantidadSolicitada = 10
        };
        context.Solicitudes.Add(solicitud);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            SolicitudId = solicitud.Id,
            Solicitud = solicitud,
            NumeroTicket = "COM-2026-100001",
            Estado = "CREADO",
            TokenQrHash = TokenHash,
            FirmaQr = Signature,
            CantidadAutorizada = 10,
            FechaExpiracion = DateTime.UtcNow.AddDays(5)
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var sut = CreateService(context, null, rol: "SOLICITANTE", usuarioId: usuarioId);

        var result = await sut.ObtenerQrPngAsync(ticket.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ObtenerQrPngAsync_SolicitanteNoPropietario_LanzaForbidden()
    {
        using var context = CreateInMemoryContext();
        var duenioId = Guid.NewGuid();
        var otroUsuarioId = Guid.NewGuid();

        var solicitud = new Solicitud
        {
            Id = Guid.NewGuid(),
            CreadaPorUsuarioId = duenioId,
            Estado = "APROBADA",
            CantidadSolicitada = 10
        };
        context.Solicitudes.Add(solicitud);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            SolicitudId = solicitud.Id,
            Solicitud = solicitud,
            NumeroTicket = "COM-2026-100002",
            Estado = "CREADO",
            TokenQrHash = TokenHash,
            FirmaQr = Signature,
            CantidadAutorizada = 10,
            FechaExpiracion = DateTime.UtcNow.AddDays(5)
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var sut = CreateService(context, null, rol: "SOLICITANTE", usuarioId: otroUsuarioId);

        var act = () => sut.ObtenerQrPngAsync(ticket.Id, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ApiException>();
        exception.Which.Code.Should().Be("FORBIDDEN");
    }

    [Fact]
    public async Task ObtenerQrPngAsync_Supervisor_RetornaPng()
    {
        using var context = CreateInMemoryContext();
        var duenioId = Guid.NewGuid();

        var solicitud = new Solicitud
        {
            Id = Guid.NewGuid(),
            CreadaPorUsuarioId = duenioId,
            Estado = "APROBADA",
            CantidadSolicitada = 10
        };
        context.Solicitudes.Add(solicitud);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            SolicitudId = solicitud.Id,
            Solicitud = solicitud,
            NumeroTicket = "COM-2026-100003",
            Estado = "CREADO",
            TokenQrHash = TokenHash,
            FirmaQr = Signature,
            CantidadAutorizada = 10,
            FechaExpiracion = DateTime.UtcNow.AddDays(5)
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var sut = CreateService(context, null, rol: "SUPERVISOR", usuarioId: Guid.NewGuid());

        var result = await sut.ObtenerQrPngAsync(ticket.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }
}