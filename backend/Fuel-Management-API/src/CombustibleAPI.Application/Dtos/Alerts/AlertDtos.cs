using System.ComponentModel.DataAnnotations;

namespace CombustibleAPI.Application.Dtos.Alerts;

public class LowInventoryAlertFilterDto
{
    public Guid? EstacionId { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public class LowInventoryAlertDto
{
    public string Tipo { get; set; } = "LOW_INVENTORY";
    public string Severidad { get; set; } = "CRITICAL";

    public Guid TanqueId { get; set; }
    public string TanqueCodigo { get; set; } = default!;
    public string? TanqueNombre { get; set; }

    public Guid EstacionId { get; set; }
    public string Estacion { get; set; } = default!;

    public short TipoCombustibleId { get; set; }
    public string Combustible { get; set; } = default!;

    public decimal StockActualGalones { get; set; }
    public decimal NivelCriticoGalones { get; set; }
    public decimal DeficitGalones { get; set; }

    public string Mensaje { get; set; } = default!;
}

public class LowInventoryAlertPagedResponseDto
{
    public List<LowInventoryAlertDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }

    public int TotalPages =>
        PageSize <= 0
            ? 0
            : (int)Math.Ceiling(TotalCount / (double)PageSize);
}