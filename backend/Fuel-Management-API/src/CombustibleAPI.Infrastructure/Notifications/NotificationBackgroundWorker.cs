using System.Data;
using System.Text.RegularExpressions;
using CombustibleAPI.Application.Common;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Domain.Entities;
using CombustibleAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CombustibleAPI.Infrastructure.Notifications;

public class NotificationBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<NotificationOptions> _notificationOptions;
    private readonly IOptionsMonitor<PublicTicketOptions> _publicTicketOptions;
    private readonly ILogger<NotificationBackgroundWorker> _logger;
    private readonly string _workerId;

    public NotificationBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<NotificationOptions> notificationOptions,
        IOptionsMonitor<PublicTicketOptions> publicTicketOptions,
        ILogger<NotificationBackgroundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _notificationOptions = notificationOptions;
        _publicTicketOptions = publicTicketOptions;
        _logger = logger;
        _workerId = $"{Environment.MachineName}_{Environment.ProcessId}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Iniciando NotificationBackgroundWorker [{WorkerId}].", _workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _notificationOptions.CurrentValue;

            if (!options.Enabled)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(5, options.PollingIntervalSeconds)), stoppingToken);
                continue;
            }

            try
            {
                await ProcesarLotePendientesAsync(options, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no controlado en el ciclo de NotificationBackgroundWorker.");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(2, options.PollingIntervalSeconds)), stoppingToken);
        }

        _logger.LogInformation("Finalizando NotificationBackgroundWorker [{WorkerId}].", _workerId);
    }

    public async Task<int> ProcesarLotePendientesAsync(NotificationOptions options, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var smsSender = scope.ServiceProvider.GetRequiredService<ISmsSender>();
        var publicTicketService = scope.ServiceProvider.GetRequiredService<IPublicTicketService>();
        var qrCodeService = scope.ServiceProvider.GetRequiredService<IQrCodeService>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        // 1. Adquisición atómica del lote con lease
        var idsReclamados = await AdquirirLoteLeaseAsync(context, options, ct);
        if (idsReclamados.Count == 0) return 0;

        _logger.LogInformation("Reclamadas {Cantidad} notificaciones para procesamiento en lote.", idsReclamados.Count);

        // 2. Procesar cada notificación reclamada
        foreach (var id in idsReclamados)
        {
            if (ct.IsCancellationRequested) break;

            var entrega = await context.NotificacionesEntrega
                .Include(n => n.Ticket)
                    .ThenInclude(t => t.Solicitud)
                        .ThenInclude(s => s.Empleado)
                .Include(n => n.Ticket)
                    .ThenInclude(t => t.Solicitud)
                        .ThenInclude(s => s.Vehiculo)
                .Include(n => n.Ticket)
                    .ThenInclude(t => t.Estacion)
                .Include(n => n.Ticket)
                    .ThenInclude(t => t.TipoCombustible)
                .FirstOrDefaultAsync(n => n.Id == id, ct);

            if (entrega == null) continue;

            var ticket = entrega.Ticket;

            // Regla 3: Verificar si el ticket ya expiró o fue anulado antes de enviar la notificación
            if (ticket == null || string.Equals(ticket.Estado, "ANULADO", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Omitiendo notificación {Id} porque el ticket está ANULADO.", entrega.Id);
                entrega.Estado = "OMITIDA";
                entrega.UltimoError = "TICKET_ANULADO_ANTES_DEL_ENVIO";
                entrega.ProcesandoDesde = null;
                await context.SaveChangesAsync(ct);
                continue;
            }

            if (ticket.FechaExpiracion <= DateTime.UtcNow)
            {
                _logger.LogInformation("Omitiendo notificación {Id} porque el ticket está EXPIRADO.", entrega.Id);
                entrega.Estado = "OMITIDA";
                entrega.UltimoError = "TICKET_EXPIRADO_ANTES_DEL_ENVIO";
                entrega.ProcesandoDesde = null;
                await context.SaveChangesAsync(ct);
                continue;
            }

            // Cargar acceso público activo
            var acceso = await context.TicketsAccesoPublico
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.TicketId == ticket.Id && a.FechaRevocacion == null && a.FechaExpiracion > DateTime.UtcNow, ct);

            if (acceso == null)
            {
                _logger.LogWarning("Omitiendo notificación {Id} porque no se encontró acceso público activo para el ticket {TicketId}.", entrega.Id, ticket.Id);
                entrega.Estado = "OMITIDA";
                entrega.UltimoError = "ACCESO_PUBLICO_NO_DISPONIBLE";
                entrega.ProcesandoDesde = null;
                await context.SaveChangesAsync(ct);
                continue;
            }

            // Reconstruir token real mediante HMAC de forma determinística
            var tokenReal = publicTicketService.ReconstruirToken(ticket.Id, acceso.Nonce);
            var secureUrl = $"{_publicTicketOptions.CurrentValue.BaseUrl.TrimEnd('/')}/ticket/{tokenReal}";

            var solicitud = ticket.Solicitud;
            var empleado = solicitud?.Empleado;
            var vehiculo = solicitud?.Vehiculo;
            var nombreEmpleado = empleado is not null ? $"{empleado.Nombre} {empleado.Apellido}".Trim() : "Empleado";
            var descripcionVehiculo = vehiculo is not null ? $"{vehiculo.Marca} {vehiculo.Modelo}".Trim() : "Vehículo asignado";

            bool exito;
            string? messageId;
            string? error;

            if (string.Equals(entrega.Canal, "EMAIL", StringComparison.OrdinalIgnoreCase))
            {
                byte[] qrPngBytes;
                try
                {
                    var qrSec = qrCodeService.RebuildQrSecurityData(ticket.Id);
                    qrPngBytes = qrCodeService.GenerateQrImagePng(qrSec.QrPayload);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al generar imagen PNG del QR para el email del ticket {TicketId}", ticket.Id);
                    qrPngBytes = Array.Empty<byte>();
                }

                (exito, messageId, error) = await emailSender.EnviarTicketAprobadoEmailAsync(
                    entrega.Destinatario ?? string.Empty,
                    nombreEmpleado,
                    ticket.NumeroTicket,
                    descripcionVehiculo,
                    vehiculo?.Placa ?? "N/A",
                    vehiculo?.Ficha ?? "N/A",
                    ticket.TipoCombustible?.Nombre ?? "Combustible",
                    ticket.CantidadAutorizada,
                    ticket.Estacion?.Nombre ?? "Estación",
                    ticket.FechaExpiracion,
                    secureUrl,
                    qrPngBytes,
                    ct);
            }
            else if (string.Equals(entrega.Canal, "SMS", StringComparison.OrdinalIgnoreCase))
            {
                (exito, messageId, error) = await smsSender.EnviarTicketAprobadoSmsAsync(
                    entrega.Destinatario ?? string.Empty,
                    ticket.NumeroTicket,
                    ticket.CantidadAutorizada,
                    ticket.TipoCombustible?.Nombre ?? "Combustible",
                    ticket.FechaExpiracion,
                    secureUrl,
                    ct);
            }
            else
            {
                exito = false;
                messageId = null;
                error = $"CANAL_DESCONOCIDO: {entrega.Canal}";
            }

            if (exito)
            {
                entrega.Estado = "ENVIADA";
                entrega.FechaEnvio = DateTime.UtcNow;
                entrega.ProviderMessageId = messageId;
                entrega.UltimoError = null;
                entrega.ProcesandoDesde = null;

                await auditService.RegistrarAsync(
                    null,
                    entrega.Canal == "EMAIL" ? "NOTIFICACION_EMAIL_ENVIADA" : "NOTIFICACION_SMS_ENVIADA",
                    "NotificacionEntrega",
                    entrega.Id.ToString(),
                    null,
                    null,
                    new { ticket.NumeroTicket, entrega.Destinatario, messageId },
                    ct);
            }
            else
            {
                if (entrega.Intentos >= entrega.MaxIntentos)
                {
                    entrega.Estado = "FALLIDA";
                    entrega.UltimoError = $"MAX_INTENTOS_AGOTADOS: {error}";
                    entrega.FechaProximoIntento = null;
                    entrega.ProcesandoDesde = null;

                    await auditService.RegistrarAsync(
                        null,
                        "NOTIFICACION_FALLIDA",
                        "NotificacionEntrega",
                        entrega.Id.ToString(),
                        null,
                        null,
                        new { ticket.NumeroTicket, entrega.Destinatario, error, entrega.Intentos },
                        ct);
                }
                else
                {
                    entrega.Estado = "FALLIDA";
                    var minutosBackoff = Math.Pow(2, entrega.Intentos);
                    entrega.FechaProximoIntento = DateTime.UtcNow.AddMinutes(minutosBackoff);
                    entrega.UltimoError = error;
                    entrega.ProcesandoDesde = null;
                }
            }

            await context.SaveChangesAsync(ct);
        }

        return idsReclamados.Count;
    }

    private async Task<List<Guid>> AdquirirLoteLeaseAsync(
        AppDbContext context,
        NotificationOptions options,
        CancellationToken ct)
    {
        var batchSize = Math.Max(1, options.BatchSize);
        var leaseMinutes = Math.Max(1, options.ProcessingLeaseMinutes);

        if (context.Database.IsNpgsql())
        {
            var sql = @"
WITH lote_candidatos AS (
    SELECT id
    FROM notificacion_entrega
    WHERE (
        ((estado = 'PENDIENTE' OR estado = 'FALLIDA')
         AND intentos < max_intentos
         AND (fecha_proximo_intento IS NULL OR fecha_proximo_intento <= NOW()))
        OR
        (estado = 'EN_PROCESO'
         AND procesando_desde <= NOW() - (@leaseMinutes * INTERVAL '1 minute')
         AND intentos < max_intentos)
    )
    ORDER BY fecha_creacion ASC
    FOR UPDATE SKIP LOCKED
    LIMIT @batchSize
)
UPDATE notificacion_entrega ne
SET estado = 'EN_PROCESO',
    procesando_desde = NOW(),
    worker_id = @workerId,
    intentos = ne.intentos + 1
FROM lote_candidatos lc
WHERE ne.id = lc.id
RETURNING ne.id;";

            var connection = (NpgsqlConnection)context.Database.GetDbConnection();
            var openedHere = connection.State != ConnectionState.Open;
            if (openedHere) await connection.OpenAsync(ct);

            try
            {
                await using var tx = await connection.BeginTransactionAsync(ct);
                try
                {
                    await using var cmd = new NpgsqlCommand(sql, connection, tx);
                    cmd.Parameters.AddWithValue("leaseMinutes", leaseMinutes);
                    cmd.Parameters.AddWithValue("batchSize", batchSize);
                    cmd.Parameters.AddWithValue("workerId", _workerId);

                    var ids = new List<Guid>();
                    await using (var reader = await cmd.ExecuteReaderAsync(ct))
                    {
                        while (await reader.ReadAsync(ct))
                        {
                            ids.Add(reader.GetGuid(0));
                        }
                    }

                    await tx.CommitAsync(ct);
                    return ids;
                }
                catch
                {
                    await tx.RollbackAsync(ct);
                    throw;
                }
            }
            finally
            {
                if (openedHere) await connection.CloseAsync();
            }
        }
        else
        {
            // Fallback en memoria / proveedores no-Npgsql para tests unitarios
            var ahora = DateTime.UtcNow;
            var leaseLimite = ahora.AddMinutes(-leaseMinutes);

            var candidatos = await context.NotificacionesEntrega
                .Where(x =>
                    ((x.Estado == "PENDIENTE" || x.Estado == "FALLIDA")
                     && x.Intentos < x.MaxIntentos
                     && (x.FechaProximoIntento == null || x.FechaProximoIntento <= ahora))
                    ||
                    (x.Estado == "EN_PROCESO"
                     && x.ProcesandoDesde <= leaseLimite
                     && x.Intentos < x.MaxIntentos)
                )
                .OrderBy(x => x.FechaCreacion)
                .Take(batchSize)
                .ToListAsync(ct);

            var ids = new List<Guid>();
            foreach (var item in candidatos)
            {
                item.Estado = "EN_PROCESO";
                item.ProcesandoDesde = ahora;
                item.WorkerId = _workerId;
                item.Intentos += 1;
                ids.Add(item.Id);
            }

            if (ids.Count > 0)
            {
                await context.SaveChangesAsync(ct);
            }

            return ids;
        }
    }
}
