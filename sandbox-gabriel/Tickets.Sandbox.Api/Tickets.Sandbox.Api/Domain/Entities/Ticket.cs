using Tickets.Sandbox.Api.Domain.Common;
using Tickets.Sandbox.Api.Domain.Enums;

namespace Tickets.Sandbox.Api.Domain.Entities;

public class Ticket : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public Guid RequestId { get; set; }
    public Request Request { get; set; } = null!;
    
    // Almacenamiento seguro: NUNCA se persiste el token en texto plano, solo su hash SHA-256
    public string QrTokenHash { get; set; } = string.Empty;
    
    // Firma criptográfica HMAC-SHA256
    public string Signature { get; set; } = string.Empty;
    
    public decimal AuthorizedQuantity { get; set; }
    public DateTime ExpiresAt { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.ACTIVO;
    
    // Transición y auditoría
    public DateTime? ConsumedAt { get; set; }
    public string? ConsumedByStationId { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public string? CancelledBy { get; set; }
}
