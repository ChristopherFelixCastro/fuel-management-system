using System.ComponentModel.DataAnnotations;

namespace CombustibleAPI.Application.Dtos.Closures;

public class CreateDailyClosureRequestDto
{
    [Required]
    public Guid TanqueId { get; set; }

    [Required]
    public DateOnly Fecha { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "El stock físico no puede ser negativo.")]
    public decimal StockFisicoFinal { get; set; }

    public string? MotivoDiferencia { get; set; }
    public string? Observaciones { get; set; }
}

public class RejectClosureRequestDto
{
    [Required(ErrorMessage = "El motivo de rechazo es obligatorio.")]
    public string Motivo { get; set; } = default!;
}

public class ClosureResponseDto
{
    public Guid Id { get; set; }
    public Guid TanqueId { get; set; }
    public string? TanqueCodigo { get; set; }
    public string? TanqueNombre { get; set; }
    public Guid? EstacionId { get; set; }
    public string? EstacionNombre { get; set; }
    public DateOnly FechaCierre { get; set; }
    public decimal StockInicial { get; set; }
    public decimal TotalRecepciones { get; set; }
    public decimal TotalTransferenciasEntrada { get; set; }
    public decimal TotalTransferenciasSalida { get; set; }
    public decimal TotalDespachos { get; set; }
    public decimal TotalAjustesPositivos { get; set; }
    public decimal TotalAjustesNegativos { get; set; }
    public decimal StockTeoricoFinal { get; set; }
    public decimal StockFisicoFinal { get; set; }
    public decimal Diferencia { get; set; }
    public string Estado { get; set; } = default!;
    public Guid CreadoPorUsuarioId { get; set; }
    public string? CreadoPor { get; set; }
    public Guid? RevisadoPorUsuarioId { get; set; }
    public string? RevisadoPor { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaRevision { get; set; }
    public string? MotivoDiferencia { get; set; }
    public string? MotivoRechazo { get; set; }
    public string? Observaciones { get; set; }
}
public class ClosurePreviewDto
{
    public Guid TanqueId { get; set; }
    public DateOnly Fecha { get; set; }

    public decimal StockInicial { get; set; }
    public decimal TotalRecepciones { get; set; }
    public decimal TotalTransferenciasEntrada { get; set; }
    public decimal TotalTransferenciasSalida { get; set; }
    public decimal TotalDespachos { get; set; }
    public decimal TotalAjustesPositivos { get; set; }
    public decimal TotalAjustesNegativos { get; set; }
    public decimal StockTeoricoFinal { get; set; }
}

public class ClosureFilterDto
{
    public DateOnly? FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public Guid? TanqueId { get; set; }
    public string? Estado { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public class ClosurePagedResponseDto
{
    public List<ClosureResponseDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages =>
        PageSize <= 0
            ? 0
            : (int)Math.Ceiling(TotalCount / (double)PageSize);
}