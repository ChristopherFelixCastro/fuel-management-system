using CombustibleAPI.Domain.Enums;

namespace CombustibleAPI.Domain.Entities;

/// <summary>
/// Tabla "solicitud".
/// </summary>
public class Solicitud
{
    public Guid Id { get; set; }
    public Guid EmpleadoId { get; set; }
    public Empleado Empleado { get; set; } = default!;

    public Guid VehiculoId { get; set; }
    public Vehiculo Vehiculo { get; set; } = default!;

    public Guid DepartamentoId { get; set; }
    public Departamento Departamento { get; set; } = default!;

    public Guid CreadaPorUsuarioId { get; set; }
    public Usuario CreadaPorUsuario { get; set; } = default!;

    public Guid? RevisadaPorUsuarioId { get; set; }
    public Usuario? RevisadaPorUsuario { get; set; }

    public string TipoSolicitud { get; set; } = "MANUAL"; // MANUAL | AUTOMATICA | RECURRENTE
    public decimal CantidadSolicitada { get; set; }
    public decimal? CantidadAutorizada { get; set; }
    public string Estado { get; set; } = "PENDIENTE"; // PENDIENTE | APROBADA | RECHAZADA | CANCELADA
    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
    public DateTime? FechaExpiracion { get; set; }
    public DateTime? FechaRevision { get; set; }
    public string? MotivoRechazo { get; set; }
    public string? MotivoCancelacion { get; set; }
    public string? Observaciones { get; set; }

    // Compatibilidad
    public DateTime CreadoEn { get => FechaSolicitud; set => FechaSolicitud = value; }
}

/// <summary>
/// Tabla "ticket". Numeración COM-{AÑO}-{SECUENCIA_6} (RN-03).
/// </summary>
public class Ticket
{
    public Guid Id { get; set; }
    public Guid SolicitudId { get; set; }
    public Solicitud Solicitud { get; set; } = default!;

    public Guid EstacionId { get; set; }
    public Estacion Estacion { get; set; } = default!;

    public short TipoCombustibleId { get; set; }
    public TipoCombustible TipoCombustible { get; set; } = default!;

    public string NumeroTicket { get; set; } = default!; // COM-2026-000001
    public decimal CantidadAutorizada { get; set; }
    public string Estado { get; set; } = "CREADO"; // CREADO | ENVIADO | CONSUMIDO | ANULADO
    public string TokenQrHash { get; set; } = default!;
    public string FirmaQr { get; set; } = default!;
    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;
    public DateTime FechaExpiracion { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public Guid? AnuladoPorUsuarioId { get; set; }
    public Usuario? AnuladoPorUsuario { get; set; }
    public string? MotivoAnulacion { get; set; }

    // Compatibilidad con código previo
    public string Numero { get => NumeroTicket; set => NumeroTicket = value; }
    public decimal CantidadReservada { get => CantidadAutorizada; set => CantidadAutorizada = value; }
    public DateTime EmitidoEn { get => FechaEmision; set => FechaEmision = value; }
    public DateTime VenceEn { get => FechaExpiracion; set => FechaExpiracion = value; }
    public string TokenHash { get => TokenQrHash; set => TokenQrHash = value; }
    public string FirmaHash { get => FirmaQr; set => FirmaQr = value; }
}

/// <summary>
/// Tabla "despacho". Un solo despacho por ticket (constraint UNIQUE en ticket_id a nivel BD).
/// </summary>
public class Despacho
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = default!;

    public Guid DespachadorUsuarioId { get; set; }
    public Usuario Despachador { get; set; } = default!;

    public Guid TanqueId { get; set; }
    public Tanque Tanque { get; set; } = default!;

    public decimal CantidadDespachada { get; set; }
    public decimal OdometroRegistrado { get; set; }
    public DateTime FechaDespacho { get; set; } = DateTime.UtcNow;
    public string? Observaciones { get; set; }
    public string? DireccionIp { get; set; }

    // Compatibilidad con código previo
    public decimal Galones { get => CantidadDespachada; set => CantidadDespachada = value; }
    public decimal Odometro { get => OdometroRegistrado; set => OdometroRegistrado = value; }
    public DateTime FechaHora { get => FechaDespacho; set => FechaDespacho = value; }
    public string? Observacion { get => Observaciones; set => Observaciones = value; }
    public Guid DespachadorId { get => DespachadorUsuarioId; set => DespachadorUsuarioId = value; }
}

/// <summary>
/// Tabla "movimiento_inventario". Ledger inmutable (RN-09).
/// </summary>
public class MovimientoInventario
{
    public Guid Id { get; set; }
    public Guid TanqueId { get; set; }
    public Tanque Tanque { get; set; } = default!;

    public Guid RegistradoPorUsuarioId { get; set; }
    public Usuario RegistradoPorUsuario { get; set; } = default!;

    public string TipoMovimiento { get; set; } = default!; // RECEPCION, DESPACHO, TRANSFERENCIA_ENTRADA, TRANSFERENCIA_SALIDA, MERMA, AJUSTE_POSITIVO, AJUSTE_NEGATIVO
    public decimal Cantidad { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal SaldoPosterior { get; set; }
    public Guid? RecepcionId { get; set; }
    public Guid? DespachoId { get; set; }
    public Guid? TransferenciaId { get; set; }
    public Guid? AjusteId { get; set; }
    public DateTime FechaMovimiento { get; set; } = DateTime.UtcNow;
    public string? Observaciones { get; set; }

    // Compatibilidad
    public DateTime FechaHora { get => FechaMovimiento; set => FechaMovimiento = value; }
    public string? Observacion { get => Observaciones; set => Observaciones = value; }
}

/// <summary>
/// Tabla "cierre_diario".
/// </summary>
public class CierreDiario
{
    public Guid Id { get; set; }
    public Guid TanqueId { get; set; }
    public Tanque Tanque { get; set; } = default!;

    public Guid CreadoPorUsuarioId { get; set; }
    public Usuario CreadoPorUsuario { get; set; } = default!;

    public Guid? RevisadoPorUsuarioId { get; set; }
    public Usuario? RevisadoPorUsuario { get; set; }

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
    public string Estado { get; set; } = "PENDIENTE_APROBACION"; // PENDIENTE_APROBACION | APROBADO | RECHAZADO
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaRevision { get; set; }
    public string? MotivoDiferencia { get; set; }
    public string? MotivoRechazo { get; set; }
    public string? Observaciones { get; set; }

    // Compatibilidad
    public DateOnly Fecha { get => FechaCierre; set => FechaCierre = value; }
    public decimal InventarioTeorico { get => StockTeoricoFinal; set => StockTeoricoFinal = value; }
    public decimal InventarioFisico { get => StockFisicoFinal; set => StockFisicoFinal = value; }
    public Guid DespachadorId { get => CreadoPorUsuarioId; set => CreadoPorUsuarioId = value; }
    public Guid? AprobadorId { get => RevisadoPorUsuarioId; set => RevisadoPorUsuarioId = value; }
    public DateTime CreadoEn { get => FechaCreacion; set => FechaCreacion = value; }
}
