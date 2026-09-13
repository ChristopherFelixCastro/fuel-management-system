namespace FuelManagement.Closures.Dtos;

// ================================================================
// DTOs de entrada (requests)
// ================================================================

public sealed class CreateClosureRequest
{
    public Guid TanqueId { get; init; }
    public DateOnly FechaCierre { get; init; }
    public decimal StockFisicoFinal { get; init; }
    public string? MotivoDiferencia { get; init; }
    public string? Observaciones { get; init; }
}

public sealed class RejectClosureRequest
{
    public string MotivoRechazo { get; init; } = null!;
}

// ================================================================
// DTOs de salida (responses)
// ================================================================

public sealed class ClosurePreviewDto
{
    public Guid TanqueId { get; init; }
    public DateOnly Fecha { get; init; }
    public decimal StockInicial { get; init; }
    public decimal TotalRecepciones { get; init; }
    public decimal TotalTransferenciasEntrada { get; init; }
    public decimal TotalTransferenciasSalida { get; init; }
    public decimal TotalDespachos { get; init; }
    public decimal TotalAjustesPositivos { get; init; }
    public decimal TotalAjustesNegativos { get; init; }
    public decimal StockTeoricoFinal { get; init; }
}

public sealed class ClosureDto
{
    public Guid Id { get; init; }
    public Guid TanqueId { get; init; }
    public string TanqueCodigo { get; init; } = null!;
    public string? TanqueNombre { get; init; }
    public Guid EstacionId { get; init; }
    public string EstacionCodigo { get; init; } = null!;
    public string EstacionNombre { get; init; } = null!;
    public string CombustibleNombre { get; init; } = null!;
    public DateOnly FechaCierre { get; init; }
    public string Estado { get; init; } = null!;
    public decimal StockInicial { get; init; }
    public decimal TotalRecepciones { get; init; }
    public decimal TotalTransferenciasEntrada { get; init; }
    public decimal TotalTransferenciasSalida { get; init; }
    public decimal TotalDespachos { get; init; }
    public decimal TotalAjustesPositivos { get; init; }
    public decimal TotalAjustesNegativos { get; init; }
    public decimal StockTeoricoFinal { get; init; }
    public decimal StockFisicoFinal { get; init; }
    public decimal Diferencia { get; init; }
    public string CreadoPor { get; init; } = null!;
    public string? RevisadoPor { get; init; }
    public DateTimeOffset FechaCreacion { get; init; }
    public DateTimeOffset? FechaRevision { get; init; }
    public string? MotivoDiferencia { get; init; }
    public string? MotivoRechazo { get; init; }
    public string? Observaciones { get; init; }
}
