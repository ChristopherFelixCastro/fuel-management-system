using System.ComponentModel.DataAnnotations;

namespace Tickets.Sandbox.Api.Application.DTOs.Requests;

public class UpdateRequestDto
{
    public decimal? RequestedQuantity { get; set; }
    public DateTime? RequestedFor { get; set; }
    public string? Notes { get; set; }
    public Guid? FuelTypeId { get; set; }
}

public class ApproveRequestDto
{
    [Required]
    [Range(0.01, 10000.00)]
    public decimal AuthorizedQuantity { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class RejectRequestDto
{
    [Required]
    [MinLength(3, ErrorMessage = "El motivo de rechazo es obligatorio")]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class CancelRequestDto
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}
