using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using CombustibleAPI.Application.Common;
using CombustibleAPI.Application.Dtos.Requests;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Domain.Entities;
using CombustibleAPI.Infrastructure.Notifications;
using CombustibleAPI.Infrastructure.Persistence;
using CombustibleAPI.Infrastructure.Persistence.DbFunctions;
using CombustibleAPI.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CombustibleAPI.Tests.Services;

public class NotificationAndPublicTicketTests
{
    private const string TestSecret = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"; // 32 bytes hex = 256 bits

    private static AppDbContext CreateContext()
    {
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    private sealed class TestAudit : IAuditService
    {
        public Task RegistrarAsync(
            Guid? usuarioId,
            string accion,
            string entidad,
            string? entidadId,
            string? ipAddress,
            object? datosAnteriores,
            object? datosNuevos,
            CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class TestQrCode : IQrCodeService
    {
        public QrSecurityResult GenerateQrSecurityData(Guid ticketId)
            => new("token", "hash", "sig", $"{{\"ticketId\":\"{ticketId}\",\"token\":\"token\",\"sig\":\"sig\"}}");

        public QrSecurityResult RebuildQrSecurityData(Guid ticketId)
            => GenerateQrSecurityData(ticketId);

        public bool VerifySignature(Guid ticketId, string rawToken, string signature) => true;
        public bool VerifyTokenHash(string rawToken, string storedHash) => true;
        public string HashToken(string rawToken) => "hash";
        public byte[] GenerateQrImagePng(string qrPayload) => [0x89, 0x50, 0x4E, 0x47];
        public (Guid TicketId, string RawToken, string Signature)? ParsePayload(string qrPayload) => null;
    }

    private static (PublicTicketService service, AppDbContext context) CreatePublicTicketService(AppDbContext? context = null)
    {
        context ??= CreateContext();
        var options = Options.Create(new PublicTicketOptions
        {
            BaseUrl = "https://la-bomba-admin.pages.dev",
            Secret = TestSecret,
            ExpirationDays = 7
        });

        var service = new PublicTicketService(context, new TestQrCode(), options);
        return (service, context);
    }

    private static async Task<(AppDbContext context, Solicitud solicitud, Empleado empleado)> SembrarSolicitudAsync(
        AppDbContext context,
        string? email = "empleado@labomba.com",
        string? telefono = "8095551234")
    {
        var dep = new Departamento { Id = Guid.NewGuid(), Codigo = "DEP1", Nombre = "Operaciones" };
        var emp = new Empleado
        {
            Id = Guid.NewGuid(),
            DepartamentoId = dep.Id,
            CodigoEmpleado = "EMP-001",
            Nombre = "Juan",
            Apellido = "Pérez",
            Cedula = "001-0000000-1",
            Email = email,
            Telefono = telefono
        };
        var tc = new TipoCombustible { Id = 1, Codigo = "GASOLINA", Nombre = "Gasolina Regular" };
        var veh = new Vehiculo
        {
            Id = Guid.NewGuid(),
            DepartamentoId = dep.Id,
            TipoCombustibleId = 1,
            Placa = "A123456",
            Ficha = "F-01",
            CapacidadTanque = 25m
        };
        var est = new Estacion { Id = Guid.NewGuid(), Codigo = "EST-01", Nombre = "Estación Central" };
        var tq = new Tanque
        {
            Id = Guid.NewGuid(),
            EstacionId = est.Id,
            TipoCombustibleId = 1,
            Codigo = "TQ-01",
            StockActual = 5000m,
            CapacidadMaxima = 10000m,
            NivelCritico = 500m
        };
        var usr = new Usuario
        {
            Id = Guid.NewGuid(),
            NombreUsuario = "juanperez",
            Email = "usuario@labomba.com",
            PasswordHash = "hash"
        };

        var sol = new Solicitud
        {
            Id = Guid.NewGuid(),
            EmpleadoId = emp.Id,
            VehiculoId = veh.Id,
            DepartamentoId = dep.Id,
            CreadaPorUsuarioId = usr.Id,
            CantidadSolicitada = 15m,
            Estado = "PENDIENTE",
            FechaSolicitud = DateTime.UtcNow
        };

        context.Departamentos.Add(dep);
        context.Empleados.Add(emp);
        context.TiposCombustible.Add(tc);
        context.Vehiculos.Add(veh);
        context.Estaciones.Add(est);
        context.Tanques.Add(tq);
        context.Usuarios.Add(usr);
        context.Solicitudes.Add(sol);

        await context.SaveChangesAsync();
        return (context, sol, emp);
    }

    [Fact]
    public async Task Aprobacion_GeneraTicket_AccesoPublico_Y_DosNotificacionesOutbox()
    {
        var context = CreateContext();
        var (publicTicketService, _) = CreatePublicTicketService(context);
        var (ctx, sol, emp) = await SembrarSolicitudAsync(context);

        var requestService = new RequestService(
            ctx,
            new SqlFunctionsRepository(),
            new TestAudit(),
            new TestQrCode(),
            publicTicketService);

        var dto = new ApproveRequestDto
        {
            EstacionId = (await ctx.Estaciones.FirstAsync()).Id,
            CantidadAutorizada = 15m,
            FechaExpiracion = DateTime.UtcNow.AddDays(3),
            Observaciones = "Aprobado para ruta"
        };

        var resultado = await requestService.AprobarAsync(sol.Id, dto, Guid.NewGuid(), CancellationToken.None);

        resultado.Should().NotBeNull();
        resultado.Estado.Should().Be("APROBADA");
        resultado.TicketId.Should().NotBeNull();

        // 1. Ticket persistido
        var ticket = await ctx.Tickets.FirstOrDefaultAsync(t => t.SolicitudId == sol.Id);
        ticket.Should().NotBeNull();
        ticket!.Estado.Should().Be("CREADO");

        // 2. Acceso público generado con Nonce y TokenHash (sin token real en texto plano)
        var acceso = await ctx.TicketsAccesoPublico.FirstOrDefaultAsync(a => a.TicketId == ticket.Id);
        acceso.Should().NotBeNull();
        acceso!.Nonce.Should().HaveLength(64);
        acceso.TokenHash.Should().HaveLength(64);
        acceso.FechaExpiracion.Should().BeCloseTo(dto.FechaExpiracion, TimeSpan.FromSeconds(2));

        // 3. Notificaciones Outbox registradas
        var notificaciones = await ctx.NotificacionesEntrega.Where(n => n.TicketId == ticket.Id).ToListAsync();
        notificaciones.Should().HaveCount(2);

        var notifEmail = notificaciones.FirstOrDefault(n => n.Canal == "EMAIL");
        notifEmail.Should().NotBeNull();
        notifEmail!.Estado.Should().Be("PENDIENTE");
        notifEmail.Destinatario.Should().Be(emp.Email);

        var notifSms = notificaciones.FirstOrDefault(n => n.Canal == "SMS");
        notifSms.Should().NotBeNull();
        notifSms!.Estado.Should().Be("PENDIENTE");
        notifSms.Destinatario.Should().Be("+18095551234");
    }

    [Fact]
    public async Task Aprobacion_ConEmailFaltante_CreaNotificacionOmitida_SinRomperAprobacion()
    {
        var context = CreateContext();
        var (publicTicketService, _) = CreatePublicTicketService(context);
        var (ctx, sol, _) = await SembrarSolicitudAsync(context, email: null);

        var requestService = new RequestService(
            ctx,
            new SqlFunctionsRepository(),
            new TestAudit(),
            new TestQrCode(),
            publicTicketService);

        var dto = new ApproveRequestDto
        {
            EstacionId = (await ctx.Estaciones.FirstAsync()).Id,
            CantidadAutorizada = 10m,
            FechaExpiracion = DateTime.UtcNow.AddDays(2)
        };

        var resultado = await requestService.AprobarAsync(sol.Id, dto, Guid.NewGuid(), CancellationToken.None);

        resultado.Estado.Should().Be("APROBADA");

        var ticket = await ctx.Tickets.FirstAsync(t => t.SolicitudId == sol.Id);
        var notifEmail = await ctx.NotificacionesEntrega.FirstOrDefaultAsync(n => n.TicketId == ticket.Id && n.Canal == "EMAIL");

        notifEmail.Should().NotBeNull();
        notifEmail!.Estado.Should().Be("OMITIDA");
        notifEmail.Destinatario.Should().BeNull();
        notifEmail.UltimoError.Should().Be("EMAIL_NO_DISPONIBLE");
    }

    [Fact]
    public async Task Aprobacion_ConTelefonoInvalido_CreaNotificacionOmitida_SinRomperAprobacion()
    {
        var context = CreateContext();
        var (publicTicketService, _) = CreatePublicTicketService(context);
        var (ctx, sol, _) = await SembrarSolicitudAsync(context, telefono: "telefono-invalido");

        var requestService = new RequestService(
            ctx,
            new SqlFunctionsRepository(),
            new TestAudit(),
            new TestQrCode(),
            publicTicketService);

        var dto = new ApproveRequestDto
        {
            EstacionId = (await ctx.Estaciones.FirstAsync()).Id,
            CantidadAutorizada = 10m,
            FechaExpiracion = DateTime.UtcNow.AddDays(2)
        };

        var resultado = await requestService.AprobarAsync(sol.Id, dto, Guid.NewGuid(), CancellationToken.None);

        resultado.Estado.Should().Be("APROBADA");

        var ticket = await ctx.Tickets.FirstAsync(t => t.SolicitudId == sol.Id);
        var notifSms = await ctx.NotificacionesEntrega.FirstOrDefaultAsync(n => n.TicketId == ticket.Id && n.Canal == "SMS");

        notifSms.Should().NotBeNull();
        notifSms!.Estado.Should().Be("OMITIDA");
        notifSms.Destinatario.Should().BeNull();
        notifSms.UltimoError.Should().Be("TELEFONO_INVALIDO");
    }

    [Fact]
    public async Task TokenOpaco_Valido_ResuelveTicketPublico()
    {
        var (service, context) = CreatePublicTicketService();
        var (_, sol, emp) = await SembrarSolicitudAsync(context);

        var ticketId = Guid.NewGuid();
        var ticket = new Ticket
        {
            Id = ticketId,
            SolicitudId = sol.Id,
            EstacionId = (await context.Estaciones.FirstAsync()).Id,
            TipoCombustibleId = 1,
            NumeroTicket = "COM-2026-000042",
            CantidadAutorizada = 15m,
            Estado = "CREADO",
            TokenQrHash = "hash",
            FirmaQr = "sig",
            FechaEmision = DateTime.UtcNow,
            FechaExpiracion = DateTime.UtcNow.AddDays(3)
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var (tokenReal, _, _) = await service.GenerarTokenAccesoAsync(ticketId, ticket.FechaExpiracion, CancellationToken.None);

        var publicDto = await service.ObtenerTicketPublicoAsync(tokenReal, CancellationToken.None);

        publicDto.Should().NotBeNull();
        publicDto.NumeroTicket.Should().Be("COM-2026-000042");
        publicDto.Empleado.Should().Be($"{emp.Nombre} {emp.Apellido}");
        publicDto.Estado.Should().Be("ACTIVO");
        publicDto.PermiteDespacho.Should().BeTrue();
    }

    [Fact]
    public async Task TokenOpaco_Malformed_FallaDefensivamente_ConNotFound()
    {
        var (service, _) = CreatePublicTicketService();

        // 1. Cadena vacía
        Func<Task> actEmpty = async () => await service.ObtenerTicketPublicoAsync("", CancellationToken.None);
        await actEmpty.Should().ThrowAsync<ApiException>().Where(e => e.Code == "NOT_FOUND");

        // 2. Longitud incorrecta
        Func<Task> actWrongLen = async () => await service.ObtenerTicketPublicoAsync("abc123short", CancellationToken.None);
        await actWrongLen.Should().ThrowAsync<ApiException>().Where(e => e.Code == "NOT_FOUND");

        // 3. Caracteres no hexadecimales (evita FormatException)
        var invalidHex = new string('z', 64);
        Func<Task> actNonHex = async () => await service.ObtenerTicketPublicoAsync(invalidHex, CancellationToken.None);
        await actNonHex.Should().ThrowAsync<ApiException>().Where(e => e.Code == "NOT_FOUND");
    }

    [Fact]
    public async Task TokenOpaco_Alterado_NoCoincide()
    {
        var (service, context) = CreatePublicTicketService();
        var (_, sol, _) = await SembrarSolicitudAsync(context);

        var ticketId = Guid.NewGuid();
        var ticket = new Ticket
        {
            Id = ticketId,
            SolicitudId = sol.Id,
            EstacionId = (await context.Estaciones.FirstAsync()).Id,
            TipoCombustibleId = 1,
            NumeroTicket = "COM-2026-000043",
            CantidadAutorizada = 10m,
            Estado = "CREADO",
            TokenQrHash = "hash",
            FirmaQr = "sig",
            FechaEmision = DateTime.UtcNow,
            FechaExpiracion = DateTime.UtcNow.AddDays(3)
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var (tokenReal, _, _) = await service.GenerarTokenAccesoAsync(ticketId, ticket.FechaExpiracion, CancellationToken.None);

        // Alterar el último caracter del token
        var alteredToken = tokenReal[..^1] + (tokenReal[^1] == '0' ? '1' : '0');

        Func<Task> act = async () => await service.ObtenerTicketPublicoAsync(alteredToken, CancellationToken.None);
        await act.Should().ThrowAsync<ApiException>().Where(e => e.Code == "NOT_FOUND");
    }

    [Fact]
    public async Task TokenOpaco_Expirado_ArrojaError()
    {
        var (service, context) = CreatePublicTicketService();
        var (_, sol, _) = await SembrarSolicitudAsync(context);

        var ticketId = Guid.NewGuid();
        var ticket = new Ticket
        {
            Id = ticketId,
            SolicitudId = sol.Id,
            EstacionId = (await context.Estaciones.FirstAsync()).Id,
            TipoCombustibleId = 1,
            NumeroTicket = "COM-2026-000044",
            CantidadAutorizada = 10m,
            Estado = "CREADO",
            TokenQrHash = "hash",
            FirmaQr = "sig",
            FechaEmision = DateTime.UtcNow.AddDays(-10),
            FechaExpiracion = DateTime.UtcNow.AddDays(-1) // Vencido
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var (tokenReal, _, _) = await service.GenerarTokenAccesoAsync(ticketId, ticket.FechaExpiracion, CancellationToken.None);

        Func<Task> act = async () => await service.ObtenerTicketPublicoAsync(tokenReal, CancellationToken.None);
        await act.Should().ThrowAsync<ApiException>().Where(e => e.Code == "ENLACE_EXPIRADO");
    }

    [Fact]
    public async Task TicketConsumido_EnlacePublico_MuestraEstadoConsumido()
    {
        var (service, context) = CreatePublicTicketService();
        var (_, sol, _) = await SembrarSolicitudAsync(context);

        var ticketId = Guid.NewGuid();
        var ticket = new Ticket
        {
            Id = ticketId,
            SolicitudId = sol.Id,
            EstacionId = (await context.Estaciones.FirstAsync()).Id,
            TipoCombustibleId = 1,
            NumeroTicket = "COM-2026-000045",
            CantidadAutorizada = 10m,
            Estado = "CONSUMIDO", // Despachado
            TokenQrHash = "hash",
            FirmaQr = "sig",
            FechaEmision = DateTime.UtcNow.AddHours(-2),
            FechaExpiracion = DateTime.UtcNow.AddDays(3)
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var (tokenReal, _, _) = await service.GenerarTokenAccesoAsync(ticketId, ticket.FechaExpiracion, CancellationToken.None);

        var publicDto = await service.ObtenerTicketPublicoAsync(tokenReal, CancellationToken.None);

        publicDto.Estado.Should().Be("CONSUMIDO");
        publicDto.MensajeEstado.Should().Contain("utilizado");
        publicDto.PermiteDespacho.Should().BeFalse();
    }

    [Fact]
    public void NormalizarTelefonoE164_FormateaCorrectamenteRepublicaDominicana()
    {
        // 10 dígitos locales
        InfobipSmsSender.NormalizarTelefonoE164("8095551234").Should().Be("+18095551234");
        InfobipSmsSender.NormalizarTelefonoE164("829-555-1234").Should().Be("+18295551234");
        InfobipSmsSender.NormalizarTelefonoE164("(849) 555 1234").Should().Be("+18495551234");

        // 11 dígitos con código país
        InfobipSmsSender.NormalizarTelefonoE164("18095551234").Should().Be("+18095551234");
        InfobipSmsSender.NormalizarTelefonoE164("+18095551234").Should().Be("+18095551234");

        // Teléfono inválido o extranjero arbitrario
        InfobipSmsSender.NormalizarTelefonoE164("").Should().BeNull();
        InfobipSmsSender.NormalizarTelefonoE164("123").Should().BeNull();
        InfobipSmsSender.NormalizarTelefonoE164("invalid").Should().BeNull();
    }

    [Fact]
    public async Task Worker_TicketExpiradoAntesDelEnvio_MarcaOmitida_SinLlamarProveedor()
    {
        var context = CreateContext();
        var (solicitudContext, sol, _) = await SembrarSolicitudAsync(context);

        var ticketId = Guid.NewGuid();
        var ticket = new Ticket
        {
            Id = ticketId,
            SolicitudId = sol.Id,
            EstacionId = (await context.Estaciones.FirstAsync()).Id,
            TipoCombustibleId = 1,
            NumeroTicket = "COM-2026-000046",
            CantidadAutorizada = 10m,
            Estado = "CREADO",
            TokenQrHash = "hash",
            FirmaQr = "sig",
            FechaEmision = DateTime.UtcNow.AddDays(-5),
            FechaExpiracion = DateTime.UtcNow.AddMinutes(-10) // Vencido
        };
        context.Tickets.Add(ticket);

        var entrega = new NotificacionEntrega
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Canal = "EMAIL",
            TipoEvento = "TICKET_APROBADO",
            Destinatario = "empleado@labomba.com",
            Estado = "PENDIENTE",
            Intentos = 0
        };
        context.NotificacionesEntrega.Add(entrega);
        await context.SaveChangesAsync();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(context);
        serviceCollection.AddSingleton<IEmailSender, BrevoEmailSender>();
        serviceCollection.AddSingleton<ISmsSender, InfobipSmsSender>();
        serviceCollection.AddSingleton<IQrCodeService, TestQrCode>();
        serviceCollection.AddSingleton<IAuditService, TestAudit>();
        serviceCollection.AddSingleton<IPublicTicketService>(CreatePublicTicketService(context).service);
        serviceCollection.AddHttpClient<BrevoEmailSender>();
        serviceCollection.AddHttpClient<InfobipSmsSender>();

        var sp = serviceCollection.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var notifOptions = Options.Create(new NotificationOptions { Enabled = true, ProcessingLeaseMinutes = 5 });
        var publicOptions = Options.Create(new PublicTicketOptions { BaseUrl = "https://la-bomba-admin.pages.dev", Secret = TestSecret });

        var worker = new NotificationBackgroundWorker(
            scopeFactory,
            new FakeOptionsMonitor<NotificationOptions>(notifOptions.Value),
            new FakeOptionsMonitor<PublicTicketOptions>(publicOptions.Value),
            NullLogger<NotificationBackgroundWorker>.Instance);

        var procesados = await worker.ProcesarLotePendientesAsync(notifOptions.Value, CancellationToken.None);

        procesados.Should().Be(1);

        var entregaActualizada = await context.NotificacionesEntrega.FirstAsync(n => n.Id == entrega.Id);
        entregaActualizada.Estado.Should().Be("OMITIDA");
        entregaActualizada.UltimoError.Should().Be("TICKET_EXPIRADO_ANTES_DEL_ENVIO");
    }

    [Fact]
    public async Task Worker_TicketAnuladoAntesDelEnvio_MarcaOmitida_SinLlamarProveedor()
    {
        var context = CreateContext();
        var (solicitudContext, sol, _) = await SembrarSolicitudAsync(context);

        var ticketId = Guid.NewGuid();
        var ticket = new Ticket
        {
            Id = ticketId,
            SolicitudId = sol.Id,
            EstacionId = (await context.Estaciones.FirstAsync()).Id,
            TipoCombustibleId = 1,
            NumeroTicket = "COM-2026-000047",
            CantidadAutorizada = 10m,
            Estado = "ANULADO",
            TokenQrHash = "hash",
            FirmaQr = "sig",
            FechaEmision = DateTime.UtcNow,
            FechaExpiracion = DateTime.UtcNow.AddDays(3)
        };
        context.Tickets.Add(ticket);

        var entrega = new NotificacionEntrega
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Canal = "SMS",
            TipoEvento = "TICKET_APROBADO",
            Destinatario = "+18095551234",
            Estado = "PENDIENTE",
            Intentos = 0
        };
        context.NotificacionesEntrega.Add(entrega);
        await context.SaveChangesAsync();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(context);
        serviceCollection.AddSingleton<IEmailSender, BrevoEmailSender>();
        serviceCollection.AddSingleton<ISmsSender, InfobipSmsSender>();
        serviceCollection.AddSingleton<IQrCodeService, TestQrCode>();
        serviceCollection.AddSingleton<IAuditService, TestAudit>();
        serviceCollection.AddSingleton<IPublicTicketService>(CreatePublicTicketService(context).service);
        serviceCollection.AddHttpClient<BrevoEmailSender>();
        serviceCollection.AddHttpClient<InfobipSmsSender>();

        var sp = serviceCollection.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var notifOptions = Options.Create(new NotificationOptions { Enabled = true, ProcessingLeaseMinutes = 5 });
        var publicOptions = Options.Create(new PublicTicketOptions { BaseUrl = "https://la-bomba-admin.pages.dev", Secret = TestSecret });

        var worker = new NotificationBackgroundWorker(
            scopeFactory,
            new FakeOptionsMonitor<NotificationOptions>(notifOptions.Value),
            new FakeOptionsMonitor<PublicTicketOptions>(publicOptions.Value),
            NullLogger<NotificationBackgroundWorker>.Instance);

        var procesados = await worker.ProcesarLotePendientesAsync(notifOptions.Value, CancellationToken.None);

        procesados.Should().Be(1);

        var entregaActualizada = await context.NotificacionesEntrega.FirstAsync(n => n.Id == entrega.Id);
        entregaActualizada.Estado.Should().Be("OMITIDA");
        entregaActualizada.UltimoError.Should().Be("TICKET_ANULADO_ANTES_DEL_ENVIO");
    }

    [Fact]
    public async Task Worker_RecuperaLease_CuandoProcesandoDesde_ExcedeTiempoLimite()
    {
        var context = CreateContext();
        var (solicitudContext, sol, _) = await SembrarSolicitudAsync(context);

        var ticketId = Guid.NewGuid();
        var ticket = new Ticket
        {
            Id = ticketId,
            SolicitudId = sol.Id,
            EstacionId = (await context.Estaciones.FirstAsync()).Id,
            TipoCombustibleId = 1,
            NumeroTicket = "COM-2026-000048",
            CantidadAutorizada = 10m,
            Estado = "CREADO",
            TokenQrHash = "hash",
            FirmaQr = "sig",
            FechaEmision = DateTime.UtcNow,
            FechaExpiracion = DateTime.UtcNow.AddDays(3)
        };
        context.Tickets.Add(ticket);

        // Registro abandonado en EN_PROCESO hace 10 minutos (lease = 5)
        var entrega = new NotificacionEntrega
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Canal = "EMAIL",
            TipoEvento = "TICKET_APROBADO",
            Destinatario = "empleado@labomba.com",
            Estado = "EN_PROCESO",
            ProcesandoDesde = DateTime.UtcNow.AddMinutes(-10),
            Intentos = 1,
            MaxIntentos = 3
        };
        context.NotificacionesEntrega.Add(entrega);
        await context.SaveChangesAsync();

        var (pubService, _) = CreatePublicTicketService(context);
        await pubService.GenerarTokenAccesoAsync(ticketId, ticket.FechaExpiracion, CancellationToken.None);

        // Mock EmailSender que retorna éxito
        var mockEmail = new MockEmailSender(true, "msg-recuperado", null);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(context);
        serviceCollection.AddSingleton<IEmailSender>(mockEmail);
        serviceCollection.AddSingleton<ISmsSender, InfobipSmsSender>();
        serviceCollection.AddSingleton<IQrCodeService, TestQrCode>();
        serviceCollection.AddSingleton<IAuditService, TestAudit>();
        serviceCollection.AddSingleton<IPublicTicketService>(pubService);
        serviceCollection.AddHttpClient<InfobipSmsSender>();

        var sp = serviceCollection.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var notifOptions = Options.Create(new NotificationOptions { Enabled = true, ProcessingLeaseMinutes = 5 });
        var publicOptions = Options.Create(new PublicTicketOptions { BaseUrl = "https://la-bomba-admin.pages.dev", Secret = TestSecret });

        var worker = new NotificationBackgroundWorker(
            scopeFactory,
            new FakeOptionsMonitor<NotificationOptions>(notifOptions.Value),
            new FakeOptionsMonitor<PublicTicketOptions>(publicOptions.Value),
            NullLogger<NotificationBackgroundWorker>.Instance);

        var procesados = await worker.ProcesarLotePendientesAsync(notifOptions.Value, CancellationToken.None);

        procesados.Should().Be(1);

        var entregaActualizada = await context.NotificacionesEntrega.FirstAsync(n => n.Id == entrega.Id);
        entregaActualizada.Estado.Should().Be("ENVIADA");
        entregaActualizada.Intentos.Should().Be(2); // Incrementado
        entregaActualizada.ProviderMessageId.Should().Be("msg-recuperado");
        entregaActualizada.ProcesandoDesde.Should().BeNull();
    }

    private sealed class MockEmailSender : IEmailSender
    {
        private readonly bool _success;
        private readonly string? _messageId;
        private readonly string? _error;

        public MockEmailSender(bool success, string? messageId, string? error)
        {
            _success = success;
            _messageId = messageId;
            _error = error;
        }

        public Task<(bool Success, string? MessageId, string? Error)> EnviarTicketAprobadoEmailAsync(
            string destinatario,
            string nombreEmpleado,
            string numeroTicket,
            string vehiculo,
            string placa,
            string ficha,
            string combustible,
            decimal cantidad,
            string estacion,
            DateTime fechaExpiracion,
            string secureUrl,
            byte[] qrPngBytes,
            CancellationToken ct)
        {
            return Task.FromResult((_success, _messageId, _error));
        }
    }

    private sealed class FakeOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public FakeOptionsMonitor(T currentValue) => CurrentValue = currentValue;
        public T CurrentValue { get; }
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
