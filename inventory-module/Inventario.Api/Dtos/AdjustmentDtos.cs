namespace Inventario.Api.Dtos
{
    public class ReportAdjustmentRequest
    {
        public Guid TankId { get; set; }
        public decimal PhysicalQuantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? EvidenceUrl { get; set; }
    }

    public class RejectAdjustmentRequest
    {
        public string RejectionReason { get; set; } = string.Empty;
    }

    public class AdjustmentResponse
    {
        public Guid Id { get; set; }
        public Guid TankId { get; set; }
        public decimal PhysicalQuantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}