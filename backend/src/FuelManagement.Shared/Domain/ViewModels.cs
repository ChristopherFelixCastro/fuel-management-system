using System.ComponentModel.DataAnnotations.Schema;

namespace FuelManagement.Shared.Domain;

// ============================================================
// View Models - proyecciones de las vistas y funciones PostgreSQL
// ============================================================

public class VwCierresResumen
{
    public Guid CierreId { get; set; }
    public DateOnly FechaCierre { get; set; }
    public string Estado { get; set; } = null!;
    public Guid TanqueId { get; set; }
    public string TanqueCodigo { get; set; } = null!;
    public string? TanqueNombre { get; set; }
    public Guid EstacionId { get; set; }
    public string EstacionCodigo { get; set; } = null!;
    public string EstacionNombre { get; set; } = null!;
    public short TipoCombustibleId { get; set; }
    public string CombustibleCodigo { get; set; } = null!;
    public string CombustibleNombre { get; set; } = null!;
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
    public Guid CreadoPorUsuarioId { get; set; }
    public string CreadoPor { get; set; } = null!;
    public Guid? RevisadoPorUsuarioId { get; set; }
    public string? RevisadoPor { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public DateTimeOffset? FechaRevision { get; set; }
    public string? MotivoDiferencia { get; set; }
    public string? MotivoRechazo { get; set; }
    public string? Observaciones { get; set; }
}

public class VwStockDisponible
{
    public Guid EstacionId { get; set; }
    public string EstacionCodigo { get; set; } = null!;
    public string EstacionNombre { get; set; } = null!;
    public short TipoCombustibleId { get; set; }
    public string CombustibleCodigo { get; set; } = null!;
    public string CombustibleNombre { get; set; } = null!;
    public decimal StockFisico { get; set; }
    public decimal StockReservado { get; set; }
    public decimal StockDisponible { get; set; }
}

public class VwMovimientosTanque
{
    public Guid MovimientoId { get; set; }
    public Guid TanqueId { get; set; }
    public string TanqueCodigo { get; set; } = null!;
    public string? TanqueNombre { get; set; }
    public Guid EstacionId { get; set; }
    public string EstacionCodigo { get; set; } = null!;
    public string EstacionNombre { get; set; } = null!;
    public short TipoCombustibleId { get; set; }
    public string CombustibleCodigo { get; set; } = null!;
    public string CombustibleNombre { get; set; } = null!;
    public string TipoMovimiento { get; set; } = null!;
    public decimal Cantidad { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal SaldoPosterior { get; set; }
    public Guid RegistradoPorUsuarioId { get; set; }
    public string RegistradoPor { get; set; } = null!;
    public Guid? RecepcionId { get; set; }
    public Guid? DespachoId { get; set; }
    public Guid? TransferenciaId { get; set; }
    public Guid? AjusteId { get; set; }
    public DateTimeOffset FechaMovimiento { get; set; }
    public string? Observaciones { get; set; }
}

public class VwTicketsOperativos
{
    public Guid TicketId { get; set; }
    public string NumeroTicket { get; set; } = null!;
    public Guid SolicitudId { get; set; }
    public Guid EmpleadoId { get; set; }
    public string CodigoEmpleado { get; set; } = null!;
    public string EmpleadoNombre { get; set; } = null!;
    public string EmpleadoApellido { get; set; } = null!;
    public Guid VehiculoId { get; set; }
    public string Placa { get; set; } = null!;
    public Guid EstacionId { get; set; }
    public string EstacionCodigo { get; set; } = null!;
    public string EstacionNombre { get; set; } = null!;
    public short TipoCombustibleId { get; set; }
    public string CombustibleCodigo { get; set; } = null!;
    public decimal CantidadAutorizada { get; set; }
    public string EstadoAlmacenado { get; set; } = null!;
    public string EstadoEfectivo { get; set; } = null!;
    public DateTimeOffset FechaEmision { get; set; }
    public DateTimeOffset FechaExpiracion { get; set; }
}

public class VwConsumoDiario
{
    public DateTime Fecha { get; set; }
    public Guid TanqueId { get; set; }
    public string? TanqueNombre { get; set; }
    public string? CombustibleTipo { get; set; }
    public decimal? TotalDespachadoLitros { get; set; }
    public int CantidadDespachos { get; set; }
}

public class VwTanqueResumen
{
    public Guid Id { get; set; }
    public string TanqueNombre { get; set; } = null!;
    public string Codigo { get; set; } = null!;
    public string CombustibleTipo { get; set; } = null!;
    public decimal CapacidadTotal { get; set; }
    public decimal StockActual { get; set; }
    public decimal PorcentajeOcupacion { get; set; }
    public string EstadoStock { get; set; } = null!;
}

public class FnCalcCierreRow
{
    [Column("stock_inicial")]
    public decimal StockInicial { get; set; }

    [Column("total_recepciones")]
    public decimal TotalRecepciones { get; set; }

    [Column("total_transferencias_entrada")]
    public decimal TotalTransferenciasEntrada { get; set; }

    [Column("total_transferencias_salida")]
    public decimal TotalTransferenciasSalida { get; set; }

    [Column("total_despachos")]
    public decimal TotalDespachos { get; set; }

    [Column("total_ajustes_positivos")]
    public decimal TotalAjustesPositivos { get; set; }

    [Column("total_ajustes_negativos")]
    public decimal TotalAjustesNegativos { get; set; }

    [Column("stock_teorico_final")]
    public decimal StockTeoricoFinal { get; set; }
}
