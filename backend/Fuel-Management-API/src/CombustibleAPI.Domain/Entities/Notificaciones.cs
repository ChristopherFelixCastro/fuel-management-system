namespace CombustibleAPI.Domain.Entities;

/// <summary>
/// Tabla "ticket_acceso_publico".
/// Permite el acceso web seguro a un ticket mediante un token opaco HMAC derivado.
/// El tokenReal no se almacena en BD; solo se persiste el Nonce y el TokenHash (SHA-256).
/// </summary>
public class TicketAccesoPublico
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = default!;

    public string Nonce { get; set; } = default!; // 32 bytes hex
    public string TokenHash { get; set; } = default!; // SHA-256 hex del tokenReal
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaExpiracion { get; set; }
    public DateTime? FechaRevocacion { get; set; }

    public bool EstaActivo => FechaRevocacion is null && DateTime.UtcNow < FechaExpiracion;
}

/// <summary>
/// Tabla "notificacion_entrega".
/// Outbox persistente y transaccional para envíos asíncronos hacia Brevo (Email) e Infobip (SMS).
/// </summary>
public class NotificacionEntrega
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = default!;

    public string TipoEvento { get; set; } = "TICKET_APROBADO";
    public string Canal { get; set; } = default!; // EMAIL | SMS
    public string? Destinatario { get; set; } // Null cuando Estado == OMITIDA
    public string Estado { get; set; } = "PENDIENTE"; // PENDIENTE | EN_PROCESO | ENVIADA | FALLIDA | OMITIDA
    public int Intentos { get; set; } = 0;
    public int MaxIntentos { get; set; } = 3;
    public string? UltimoError { get; set; }
    public string? ProviderMessageId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaProximoIntento { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public DateTime? ProcesandoDesde { get; set; }
    public string? WorkerId { get; set; }
}
