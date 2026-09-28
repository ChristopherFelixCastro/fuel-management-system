using System.Security.Cryptography;
using System.Text;
using CombustibleAPI.Application.Common;
using CombustibleAPI.Application.Dtos.PublicTickets;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Domain.Entities;
using CombustibleAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CombustibleAPI.Infrastructure.Services;

public class PublicTicketService : IPublicTicketService
{
    private const string Purpose = "LA_BOMBA_PUBLIC_TICKET_V1";
    private readonly AppDbContext _context;
    private readonly IQrCodeService _qrCodeService;
    private readonly PublicTicketOptions _options;
    private readonly byte[] _secretKey;

    public PublicTicketService(
        AppDbContext context,
        IQrCodeService qrCodeService,
        IOptions<PublicTicketOptions> options)
    {
        _context = context;
        _qrCodeService = qrCodeService;
        _options = options.Value;

        var secret = _options.Secret;
        if (string.IsNullOrWhiteSpace(secret))
        {
            // Default temporal no versionado para entorno de pruebas/desarrollo local
            secret = "DefaultInsecurePublicTicketSecretKeyForLocalTestingMin32Bytes";
        }

        _secretKey = Encoding.UTF8.GetBytes(secret);
    }

    public async Task<(string TokenReal, string Nonce, string TokenHash)> GenerarTokenAccesoAsync(
        Guid ticketId,
        DateTime fechaExpiracionTicket,
        CancellationToken ct)
    {
        // 1. Generar Nonce criptográfico aleatorio (32 bytes = 256 bits)
        var nonceBytes = RandomNumberGenerator.GetBytes(32);
        var nonce = Convert.ToHexString(nonceBytes).ToLowerInvariant();

        // 2. Derivar TokenReal mediante HMAC-SHA256
        var tokenReal = ReconstruirToken(ticketId, nonce);

        // 3. Calcular TokenHash unidireccional (SHA-256)
        var tokenHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(tokenReal));
        var tokenHash = Convert.ToHexString(tokenHashBytes).ToLowerInvariant();

        // 4. Expiración efectiva: min(ticket.FechaExpiracion, DateTime.UtcNow + ExpirationDays)
        var maxPermitido = DateTime.UtcNow.AddDays(Math.Max(1, _options.ExpirationDays));
        var fechaExpiracionEfectiva = fechaExpiracionTicket < maxPermitido
            ? fechaExpiracionTicket
            : maxPermitido;

        var acceso = new TicketAccesoPublico
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Nonce = nonce,
            TokenHash = tokenHash,
            FechaCreacion = DateTime.UtcNow,
            FechaExpiracion = fechaExpiracionEfectiva
        };

        _context.TicketsAccesoPublico.Add(acceso);
        await _context.SaveChangesAsync(ct);

        return (tokenReal, nonce, tokenHash);
    }

    public string ReconstruirToken(Guid ticketId, string nonce)
    {
        var messageBytes = Encoding.UTF8.GetBytes($"{ticketId}:{nonce}:{Purpose}");
        var tokenRealBytes = HMACSHA256.HashData(_secretKey, messageBytes);
        return Convert.ToHexString(tokenRealBytes).ToLowerInvariant();
    }

    public async Task<PublicTicketDto> ObtenerTicketPublicoAsync(
        string token,
        CancellationToken ct)
    {
        var (ticket, acceso) = await ValidarYResolverTicketAsync(token, ct);

        var estadoAlmacenado = ticket.Estado?.ToUpperInvariant() ?? string.Empty;
        var ahora = DateTime.UtcNow;

        string estadoPublico;
        string mensajeEstado;
        bool permiteDespacho;

        if (estadoAlmacenado == "ANULADO")
        {
            estadoPublico = "ANULADO";
            mensajeEstado = "Este ticket ha sido anulado y no es válido para despacho.";
            permiteDespacho = false;
        }
        else if (estadoAlmacenado == "CONSUMIDO")
        {
            estadoPublico = "CONSUMIDO";
            mensajeEstado = "Este ticket ya fue utilizado.";
            permiteDespacho = false;
        }
        else if (ticket.FechaExpiracion <= ahora)
        {
            estadoPublico = "VENCIDO";
            mensajeEstado = "Este ticket ha expirado.";
            permiteDespacho = false;
        }
        else
        {
            estadoPublico = "ACTIVO";
            mensajeEstado = "Ticket autorizado y listo para despacho.";
            permiteDespacho = true;
        }

        var solicitud = ticket.Solicitud;
        var empleado = solicitud?.Empleado;
        var vehiculo = solicitud?.Vehiculo;

        return new PublicTicketDto
        {
            NumeroTicket = ticket.NumeroTicket,
            Empleado = empleado is not null
                ? $"{empleado.Nombre} {empleado.Apellido}".Trim()
                : "No especificado",
            CodigoEmpleado = empleado?.CodigoEmpleado,
            Vehiculo = vehiculo is not null
                ? $"{vehiculo.Marca} {vehiculo.Modelo}".Trim()
                : "No especificado",
            Placa = vehiculo?.Placa ?? "N/A",
            Ficha = vehiculo?.Ficha ?? "N/A",
            TipoCombustible = ticket.TipoCombustible?.Nombre ?? "N/A",
            CantidadAutorizada = ticket.CantidadAutorizada,
            Estacion = ticket.Estacion?.Nombre ?? "Estación autorizada",
            FechaExpiracion = ticket.FechaExpiracion,
            Estado = estadoPublico,
            MensajeEstado = mensajeEstado,
            PermiteDespacho = permiteDespacho
        };
    }

    public async Task<byte[]> ObtenerQrPngPublicoAsync(
        string token,
        CancellationToken ct)
    {
        var (ticket, _) = await ValidarYResolverTicketAsync(token, ct);

        var estado = ticket.Estado?.ToUpperInvariant() ?? string.Empty;
        if (estado == "ANULADO") throw ApiException.TicketAnulado();
        if (estado == "CONSUMIDO") throw ApiException.TicketConsumido();
        if (ticket.FechaExpiracion <= DateTime.UtcNow) throw ApiException.TicketVencido();

        var qrSecurity = _qrCodeService.RebuildQrSecurityData(ticket.Id);

        if (!_qrCodeService.VerifyTokenHash(qrSecurity.RawToken, ticket.TokenQrHash) ||
            !FirmasCoinciden(qrSecurity.Signature, ticket.FirmaQr))
        {
            throw ApiException.FirmaInvalida();
        }

        return _qrCodeService.GenerateQrImagePng(qrSecurity.QrPayload);
    }

    private async Task<(Ticket Ticket, TicketAccesoPublico Acceso)> ValidarYResolverTicketAsync(
        string? token,
        CancellationToken ct)
    {
        // Validación defensiva del formato del token recibido:
        // No produce FormatException ni 500, responde NotFound de manera controlada.
        if (string.IsNullOrWhiteSpace(token) ||
            token.Length != 64 ||
            !EsHexValido(token))
        {
            throw ApiException.NotFound("Ticket");
        }

        var tokenLower = token.ToLowerInvariant();
        var tokenHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(tokenLower));
        var tokenHash = Convert.ToHexString(tokenHashBytes).ToLowerInvariant();

        var acceso = await _context.TicketsAccesoPublico
            .Include(a => a.Ticket)
                .ThenInclude(t => t.Solicitud)
                    .ThenInclude(s => s.Empleado)
            .Include(a => a.Ticket)
                .ThenInclude(t => t.Solicitud)
                    .ThenInclude(s => s.Vehiculo)
            .Include(a => a.Ticket)
                .ThenInclude(t => t.Estacion)
            .Include(a => a.Ticket)
                .ThenInclude(t => t.TipoCombustible)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.TokenHash == tokenHash && a.FechaRevocacion == null, ct);

        if (acceso is null || acceso.Ticket is null)
        {
            throw ApiException.NotFound("Ticket");
        }

        if (acceso.FechaExpiracion <= DateTime.UtcNow)
        {
            throw ApiException.BusinessRule("ENLACE_EXPIRADO", "El enlace de acceso público ha expirado.");
        }

        // Validación en tiempo constante contra el HMAC reconstruido
        var expectedToken = ReconstruirToken(acceso.TicketId, acceso.Nonce);
        var expectedBytes = Convert.FromHexString(expectedToken);
        var receivedBytes = Convert.FromHexString(tokenLower);

        if (!CryptographicOperations.FixedTimeEquals(expectedBytes, receivedBytes))
        {
            throw ApiException.NotFound("Ticket");
        }

        return (acceso.Ticket, acceso);
    }

    private static bool EsHexValido(string value)
    {
        foreach (var c in value)
        {
            var isHex = (c >= '0' && c <= '9') ||
                        (c >= 'a' && c <= 'f') ||
                        (c >= 'A' && c <= 'F');
            if (!isHex) return false;
        }
        return true;
    }

    private static bool FirmasCoinciden(string s1, string s2)
    {
        if (string.IsNullOrWhiteSpace(s1) || string.IsNullOrWhiteSpace(s2)) return false;
        try
        {
            var b1 = Convert.FromHexString(s1);
            var b2 = Convert.FromHexString(s2);
            return b1.Length == b2.Length && CryptographicOperations.FixedTimeEquals(b1, b2);
        }
        catch
        {
            return false;
        }
    }
}
