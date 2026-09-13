using System.ComponentModel.DataAnnotations;
using Tickets.Sandbox.Api.Domain.Enums;

namespace Tickets.Sandbox.Api.Application.DTOs.Tickets;

public record TicketEmployeeDto(string Name, string EmployeeNumber);
public record TicketVehicleDto(Guid? Id, string Plate);
public record TicketFuelTypeDto(Guid? Id, string Name);

public class TicketResponseDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public Guid RequestId { get; set; }
    public TicketEmployeeDto Employee { get; set; } = null!;
    public TicketVehicleDto Vehicle { get; set; } = null!;
    public string FuelType { get; set; } = string.Empty;
    public decimal AuthorizedQuantity { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? QrToken { get; set; } // Identificador de presentación seguro o payload formateado, nunca el secreto de firma
    public DateTime CreatedAt { get; set; }
}

public class TicketFilterDto
{
    public string? Number { get; set; }
    public TicketStatus? Status { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? VehicleId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ValidateTicketRequestDto
{
    [Required]
    public string QrPayload { get; set; } = string.Empty;

    [Required]
    public string StationId { get; set; } = string.Empty;
}

public class ValidateTicketResponseDto
{
    public Guid TicketId { get; set; }
    public string Number { get; set; } = string.Empty;
    public TicketEmployeeDto Employee { get; set; } = null!;
    public TicketVehicleDto Vehicle { get; set; } = null!;
    public string FuelType { get; set; } = string.Empty;
    public decimal AuthorizedQuantity { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool CanDispatch { get; set; }
    public Guid ValidationId { get; set; } = Guid.NewGuid();
}

public class CancelTicketRequestDto
{
    [Required]
    [MinLength(3, ErrorMessage = "El motivo de anulación es obligatorio")]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class ConsumeTicketRequestDto
{
    [Required]
    public string StationId { get; set; } = string.Empty;

    public string? DispatcherId { get; set; }
}
