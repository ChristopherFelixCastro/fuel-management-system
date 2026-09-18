using System.ComponentModel.DataAnnotations;

namespace CombustibleAPI.Application.Dtos.Reports;

public class ReportPaginationDto
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 50;
}

public class ConsumptionReportFilterDto : ReportPaginationDto
{
    public DateOnly? FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public Guid? TanqueId { get; set; }
}

public class InventoryReportFilterDto : ReportPaginationDto
{
    public Guid? TanqueId { get; set; }
    public short? TipoCombustibleId { get; set; }
    public Guid? EstacionId { get; set; }
}

public class TraceabilityReportFilterDto : ReportPaginationDto
{
    public DateOnly? FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public Guid? TanqueId { get; set; }
    public string? TipoMovimiento { get; set; }
}

public class ConsumptionReportItemDto
{
    public DateOnly Fecha { get; set; }
    public Guid TanqueId { get; set; }
    public string TanqueCodigo { get; set; } = default!;
    public string? TanqueNombre { get; set; }
    public short TipoCombustibleId { get; set; }
    public string Combustible { get; set; } = default!;
    public decimal TotalDespachadoGalones { get; set; }
    public int CantidadDespachos { get; set; }
}

public class InventoryReportItemDto
{
    public Guid TanqueId { get; set; }
    public string TanqueCodigo { get; set; } = default!;
    public string? TanqueNombre { get; set; }

    public Guid EstacionId { get; set; }
    public string Estacion { get; set; } = default!;

    public short TipoCombustibleId { get; set; }
    public string Combustible { get; set; } = default!;

    public decimal CapacidadMaximaGalones { get; set; }
    public decimal StockActualGalones { get; set; }
    public decimal NivelCriticoGalones { get; set; }
    public decimal PorcentajeOcupacion { get; set; }
    public string EstadoStock { get; set; } = default!;
}

public class TraceabilityReportItemDto
{
    public Guid MovimientoId { get; set; }

    public Guid TanqueId { get; set; }
    public string TanqueCodigo { get; set; } = default!;
    public string? TanqueNombre { get; set; }

    public Guid EstacionId { get; set; }
    public string Estacion { get; set; } = default!;

    public short TipoCombustibleId { get; set; }
    public string Combustible { get; set; } = default!;

    public string TipoMovimiento { get; set; } = default!;
    public decimal CantidadGalones { get; set; }
    public decimal SaldoAnteriorGalones { get; set; }
    public decimal SaldoPosteriorGalones { get; set; }

    public Guid RegistradoPorUsuarioId { get; set; }
    public string RegistradoPor { get; set; } = default!;

    public Guid? RecepcionId { get; set; }
    public Guid? DespachoId { get; set; }
    public Guid? TransferenciaId { get; set; }
    public Guid? AjusteId { get; set; }

    public DateTime FechaMovimiento { get; set; }
    public string? Observaciones { get; set; }
}

public class ReportPagedResponseDto<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }

    public int TotalPages =>
        PageSize <= 0
            ? 0
            : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class ReportExportResultDto
{
    public byte[] Content { get; set; } = [];
    public string ContentType { get; set; } = default!;
    public string FileName { get; set; } = default!;
}