using Tickets.Sandbox.Api.Domain.Common;
using Tickets.Sandbox.Api.Domain.Enums;

namespace Tickets.Sandbox.Api.Domain.Entities;

public class Request : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid FuelTypeId { get; set; }
    public decimal RequestedQuantity { get; set; }
    public DateTime? RequestedFor { get; set; }
    public RequestSource Source { get; set; } = RequestSource.MANUAL;
    public string? Notes { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.PENDIENTE;
    public string? CreatedBy { get; set; }
    public string? AuthorApprovedBy { get; set; }
    public string? RejectionReason { get; set; }
    
    // Timestamps de transición
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    // Relación 1 a 1 con Ticket
    public Ticket? Ticket { get; set; }
}
