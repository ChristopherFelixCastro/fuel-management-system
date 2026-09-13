using Tickets.Sandbox.Api.Domain.Enums;

namespace Tickets.Sandbox.Api.Application.DTOs.Requests;

public class RequestResponseDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? EmployeeNumber { get; set; }
    public Guid VehicleId { get; set; }
    public string? VehiclePlate { get; set; }
    public Guid DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid FuelTypeId { get; set; }
    public string? FuelTypeName { get; set; }
    public decimal RequestedQuantity { get; set; }
    public DateTime? RequestedFor { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AuthorApprovedBy { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? TicketId { get; set; }
    public string? TicketNumber { get; set; }
}

public class RequestFilterDto
{
    public RequestStatus? Status { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? DepartmentId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
