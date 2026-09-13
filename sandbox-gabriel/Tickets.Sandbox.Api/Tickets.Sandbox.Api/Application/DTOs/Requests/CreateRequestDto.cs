using System.ComponentModel.DataAnnotations;
using Tickets.Sandbox.Api.Domain.Enums;

namespace Tickets.Sandbox.Api.Application.DTOs.Requests;

public class CreateRequestDto
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    public Guid VehicleId { get; set; }

    [Required]
    public Guid DepartmentId { get; set; }

    [Required]
    public Guid FuelTypeId { get; set; }

    [Range(0.01, 10000.00)]
    public decimal RequestedQuantity { get; set; }

    public DateTime? RequestedFor { get; set; }

    public RequestSource Source { get; set; } = RequestSource.MANUAL;

    [MaxLength(500)]
    public string? Notes { get; set; }
}
