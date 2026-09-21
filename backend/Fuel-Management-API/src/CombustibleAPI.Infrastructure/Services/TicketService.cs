using CombustibleAPI.Application.Dtos.Tickets;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CombustibleAPI.Infrastructure.Services;

/// <summary>
/// Valida un ticket exclusivamente a partir de un payload QR firmado.
///
/// El QR funciona únicamente como identificador y autenticador.
/// Los datos operativos y el estado oficial siempre se consultan
/// desde la base de datos.
/// </summary>
public class TicketService : ITicketService
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IQrCodeService _qrCodeService;
    private readonly ICurrentUserService _currentUser;

    public TicketService(
        AppDbContext context,
        IAuditService auditService,
        IQrCodeService qrCodeService,
        ICurrentUserService currentUser)
    {
        _context = context;
        _auditService = auditService;
        _qrCodeService = qrCodeService;
        _currentUser = currentUser;
    }

    public async Task<TicketOficialDto> ValidarAsync(
        string qrPayload,
        CancellationToken ct)
    {
        // 1. El endpoint acepta exclusivamente un payload QR.
        if (string.IsNullOrWhiteSpace(qrPayload))
        {
            throw ApiException.ValidationError(
                "El payload del QR no puede estar vacío.");
        }

        // 2. Extraer ticketId, token y firma del QR.
        var parsed = _qrCodeService.ParsePayload(qrPayload);

        if (parsed is null)
        {
            await RegistrarFalloAsync(
                null,
                "firma_invalida",
                ct);

            throw ApiException.FirmaInvalida();
        }

        var (ticketId, rawToken, signature) = parsed.Value;

        // 3. Buscar inicialmente solo el Ticket.
        //
        // No cargamos todavía Solicitud, Empleado, Vehículo,
        // Departamento, Estación ni Combustible.
        //
        // Primero se debe demostrar que el QR y el ticket
        // son válidos.
        var ticket = await _context.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Id == ticketId,
                ct);

        if (ticket is null)
        {
            await RegistrarFalloAsync(
                ticketId,
                "inexistente",
                ct);

            throw ApiException.TicketInexistente();
        }

        // 4. Validar criptográficamente la firma HMAC.
        if (!_qrCodeService.VerifySignature(
                ticketId,
                rawToken,
                signature))
        {
            await RegistrarFalloAsync(
                ticket.Id,
                "firma_invalida",
                ct);

            throw ApiException.FirmaInvalida();
        }

        // 5. Validar que el token del QR corresponda
        // al hash almacenado al emitir el ticket.
        if (!_qrCodeService.VerifyTokenHash(
                rawToken,
                ticket.TokenQrHash))
        {
            await RegistrarFalloAsync(
                ticket.Id,
                "token_invalido",
                ct);

            throw ApiException.FirmaInvalida();
        }

        // 6. La firma recibida debe coincidir también
        // con FirmaQr almacenada en la base de datos.
        if (!FirmasCoinciden(
                signature,
                ticket.FirmaQr))
        {
            await RegistrarFalloAsync(
                ticket.Id,
                "firma_no_corresponde_al_ticket",
                ct);

            throw ApiException.FirmaInvalida();
        }

        // 7. Si quien valida es un DESPACHADOR,
        // solamente puede operar tickets de su estación.
        if (string.Equals(
                _currentUser.Rol,
                "DESPACHADOR",
                StringComparison.OrdinalIgnoreCase))
        {
            if (!_currentUser.EstacionId.HasValue ||
                _currentUser.EstacionId.Value != ticket.EstacionId)
            {
                await RegistrarFalloAsync(
                    ticket.Id,
                    "estacion_no_autorizada",
                    ct);

                throw ApiException.NoAutorizadoParaEstacion();
            }
        }

        var ahora = DateTime.UtcNow;

        var estadoAlmacenado =
            ticket.Estado?.ToUpperInvariant() ??
            string.Empty;

        // 8. Validar estado operativo.
        if (estadoAlmacenado == "ANULADO")
        {
            await RegistrarFalloAsync(
                ticket.Id,
                "anulado",
                ct);

            throw ApiException.TicketAnulado();
        }

        if (estadoAlmacenado == "CONSUMIDO")
        {
            await RegistrarFalloAsync(
                ticket.Id,
                "consumido",
                ct);

            throw ApiException.TicketConsumido();
        }

        // Solo CREADO y ENVIADO permiten continuar
        // hacia el despacho.
        if (estadoAlmacenado != "CREADO" &&
            estadoAlmacenado != "ENVIADO")
        {
            await RegistrarFalloAsync(
                ticket.Id,
                "estado_no_valido",
                ct);

            throw ApiException.BusinessRule(
                "TICKET_ESTADO_INVALIDO",
                "El ticket no se encuentra en un estado válido para despacho.");
        }

        // 9. Validar vencimiento.
        if (ticket.FechaExpiracion <= ahora)
        {
            await RegistrarFalloAsync(
                ticket.Id,
                "vencido",
                ct);

            throw ApiException.TicketVencido();
        }

        // 10. El QR ya superó:
        //
        // - existencia
        // - firma HMAC
        // - hash del token
        // - firma persistida
        // - estación
        // - estado
        // - vencimiento
        //
        // Ahora sí cargamos el detalle completo requerido
        // por la PWA.
        var ticketCompleto = await _context.Tickets
            .Include(t => t.Solicitud)
                .ThenInclude(s => s.Empleado)
            .Include(t => t.Solicitud)
                .ThenInclude(s => s.Vehiculo)
            .Include(t => t.Solicitud)
                .ThenInclude(s => s.Departamento)
            .Include(t => t.Estacion)
            .Include(t => t.TipoCombustible)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Id == ticket.Id,
                ct);

        if (ticketCompleto is null)
        {
            await RegistrarFalloAsync(
                ticket.Id,
                "inexistente",
                ct);

            throw ApiException.TicketInexistente();
        }

        ticket = ticketCompleto;

        var estadoEfectivo =
            ticket.FechaExpiracion <= ahora.AddDays(2)
                ? "PROXIMO_A_VENCER"
                : estadoAlmacenado;

        var solicitud = ticket.Solicitud;
        var empleado = solicitud?.Empleado;
        var vehiculo = solicitud?.Vehiculo;
        var departamento = solicitud?.Departamento;

        // 11. Registrar validación exitosa.
        await _auditService.RegistrarAsync(
            _currentUser.UsuarioId,
            "VALIDACION_TICKET_EXITOSA",
            "Ticket",
            ticket.Id.ToString(),
            _currentUser.IpAddress,
            null,
            new
            {
                ticket.NumeroTicket,
                estadoEfectivo,
                ticket.EstacionId
            },
            ct);

        // 12. La información que verá la PWA procede
        // exclusivamente de la base de datos.
        return new TicketOficialDto
        {
            TicketId = ticket.Id,
            NumeroTicket = ticket.NumeroTicket,

            EmpleadoId =
                empleado?.Id ?? Guid.Empty,

            Empleado =
                empleado is not null
                    ? $"{empleado.Nombre} {empleado.Apellido}".Trim()
                    : string.Empty,

            CodigoEmpleado =
                empleado?.CodigoEmpleado,

            VehiculoId =
                vehiculo?.Id ?? Guid.Empty,

            Vehiculo =
                vehiculo is not null
                    ? $"{vehiculo.Marca} {vehiculo.Modelo}".Trim()
                    : string.Empty,

            Placa =
                vehiculo?.Placa ?? string.Empty,

            Ficha =
                vehiculo?.Ficha ?? string.Empty,

            TipoCombustibleId =
                ticket.TipoCombustibleId,

            TipoCombustible =
                ticket.TipoCombustible?.Nombre ??
                string.Empty,

            CombustibleCodigo =
                ticket.TipoCombustible?.Codigo,

            CantidadAutorizada =
                ticket.CantidadAutorizada,

            CantidadDisponible =
                ticket.CantidadAutorizada,

            FechaExpiracion =
                ticket.FechaExpiracion,

            Estado =
                estadoAlmacenado,

            EstadoEfectivo =
                estadoEfectivo,

            EstacionId =
                ticket.EstacionId,

            EstacionNombre =
                ticket.Estacion?.Nombre,

            DepartamentoId =
                departamento?.Id ?? Guid.Empty,

            DepartamentoNombre =
                departamento?.Nombre
        };
    }

    private async Task RegistrarFalloAsync(
        Guid? ticketId,
        string motivo,
        CancellationToken ct)
    {
        await _auditService.RegistrarAsync(
            _currentUser.UsuarioId,
            "VALIDACION_TICKET_RECHAZADA",
            "Ticket",
            ticketId?.ToString(),
            _currentUser.IpAddress,
            null,
            new
            {
                motivo
            },
            ct);
    }

    public async Task<byte[]> ObtenerQrPngAsync(
    Guid ticketId,
    CancellationToken ct)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Solicitud)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Id == ticketId,
                ct);

        if (ticket is null)
        {
            throw ApiException.TicketInexistente();
        }

        if (string.Equals(_currentUser.Rol, "SOLICITANTE", StringComparison.OrdinalIgnoreCase))
        {
            var usuarioId = _currentUser.UsuarioId;
            var esPropietario = ticket.Solicitud != null && (ticket.Solicitud.CreadaPorUsuarioId == usuarioId);

            if (!esPropietario && usuarioId.HasValue && ticket.Solicitud != null)
            {
                var empleadoIdUsuario = await _context.Usuarios
                    .Where(u => u.Id == usuarioId.Value)
                    .Select(u => u.EmpleadoId)
                    .FirstOrDefaultAsync(ct);

                if (empleadoIdUsuario.HasValue && ticket.Solicitud.EmpleadoId == empleadoIdUsuario.Value)
                {
                    esPropietario = true;
                }
            }

            if (!esPropietario)
            {
                throw ApiException.Forbidden(
                    "Un solicitante solo puede consultar el QR de sus propios tickets.");
            }
        }

        // Solamente los tickets emitidos y todavía utilizables
        // deben exponer su QR.
        var estado = ticket.Estado?.ToUpperInvariant()
            ?? string.Empty;

        if (estado == "ANULADO")
        {
            throw ApiException.TicketAnulado();
        }

        if (estado == "CONSUMIDO")
        {
            throw ApiException.TicketConsumido();
        }

        if (estado != "CREADO" &&
            estado != "ENVIADO")
        {
            throw ApiException.BusinessRule(
                "TICKET_ESTADO_INVALIDO",
                "El ticket no se encuentra en un estado válido para generar su QR.");
        }

        if (ticket.FechaExpiracion <= DateTime.UtcNow)
        {
            throw ApiException.TicketVencido();
        }

        // El token se reconstruye de manera determinística
        // utilizando ticketId + secreto del servidor.
        var qrSecurity =
            _qrCodeService.RebuildQrSecurityData(
                ticket.Id);

        // Antes de entregar el QR comprobamos que el token
        // reconstruido corresponde al hash persistido.
        if (!_qrCodeService.VerifyTokenHash(
                qrSecurity.RawToken,
                ticket.TokenQrHash))
        {
            throw ApiException.FirmaInvalida();
        }

        // La firma reconstruida debe ser exactamente la misma
        // que fue persistida al emitir el ticket.
        if (!FirmasCoinciden(
                qrSecurity.Signature,
                ticket.FirmaQr))
        {
            throw ApiException.FirmaInvalida();
        }

        var png =
            _qrCodeService.GenerateQrImagePng(
                qrSecurity.QrPayload);

        await _auditService.RegistrarAsync(
            _currentUser.UsuarioId,
            "QR_TICKET_GENERADO",
            "Ticket",
            ticket.Id.ToString(),
            _currentUser.IpAddress,
            null,
            new
            {
                ticket.NumeroTicket
            },
            ct);

        return png;
    }

    private static bool FirmasCoinciden(
        string receivedSignature,
        string storedSignature)
    {
        if (string.IsNullOrWhiteSpace(receivedSignature) ||
            string.IsNullOrWhiteSpace(storedSignature))
        {
            return false;
        }

        try
        {
            var receivedBytes =
                Convert.FromHexString(
                    receivedSignature);

            var storedBytes =
                Convert.FromHexString(
                    storedSignature);

            return receivedBytes.Length ==
                   storedBytes.Length &&
                   System.Security.Cryptography
                       .CryptographicOperations
                       .FixedTimeEquals(
                           receivedBytes,
                           storedBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}