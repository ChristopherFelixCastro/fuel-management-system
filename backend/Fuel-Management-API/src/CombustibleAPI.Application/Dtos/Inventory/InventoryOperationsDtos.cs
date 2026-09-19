using System.ComponentModel.DataAnnotations;

namespace CombustibleAPI.Application.Dtos.Inventory;

public sealed class CreateReceptionRequestDto
{
    [Required] public Guid ProveedorId { get; set; }
    [Required] public Guid TanqueId { get; set; }
    [Required, MaxLength(50)] public string NumeroFactura { get; set; } = default!;
    [Range(typeof(decimal), "0.01", "999999")] public decimal Cantidad { get; set; }
    public DateTime FechaRecepcion { get; set; } = DateTime.UtcNow;
    [MaxLength(500)] public string? Observaciones { get; set; }
}

public sealed class CreateTransferRequestDto
{
    [Required] public Guid TanqueOrigenId { get; set; }
    [Required] public Guid TanqueDestinoId { get; set; }
    [Range(typeof(decimal), "0.01", "999999")] public decimal Cantidad { get; set; }
    [MaxLength(500)] public string? Observaciones { get; set; }
}

public sealed class ReportAdjustmentRequestDto
{
    [Required] public Guid TanqueId { get; set; }
    [Range(typeof(decimal), "0", "999999")] public decimal ConteoFisico { get; set; }
    [Required, MaxLength(300)] public string Motivo { get; set; } = default!;
    [MaxLength(500)] public string? Observaciones { get; set; }
}

public sealed class RejectAdjustmentRequestDto { [Required, MaxLength(300)] public string Motivo { get; set; } = default!; }
public sealed class InventoryOperationResultDto { public Guid Id { get; set; } public string Estado { get; set; } = default!; }

public sealed class InventoryQueryDto { public Guid? EstacionId { get; set; } public Guid? TanqueId { get; set; } public string? Tipo { get; set; } public string? Estado { get; set; } public DateTime? Desde { get; set; } public DateTime? Hasta { get; set; } public int Page { get; set; } = 1; public int PageSize { get; set; } = 20; }
public sealed class InventoryMovementDto { public Guid Id { get; set; } public Guid TanqueId { get; set; } public string TanqueCodigo { get; set; } = default!; public Guid EstacionId { get; set; } public string Tipo { get; set; } = default!; public decimal Cantidad { get; set; } public decimal SaldoAnterior { get; set; } public decimal SaldoPosterior { get; set; } public DateTime Fecha { get; set; } public string? Observaciones { get; set; } }
public sealed class AdjustmentDto { public Guid Id { get; set; } public Guid TanqueId { get; set; } public string TanqueCodigo { get; set; } = default!; public Guid EstacionId { get; set; } public Guid ReportadoPorUsuarioId { get; set; } public decimal ConteoFisico { get; set; } public string Tipo { get; set; } = default!; public decimal Cantidad { get; set; } public string Motivo { get; set; } = default!; public string Estado { get; set; } = default!; public DateTime FechaReporte { get; set; } public DateTime? FechaRevision { get; set; } public string? MotivoRechazo { get; set; } public string? Observaciones { get; set; } }
