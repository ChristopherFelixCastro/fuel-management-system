namespace CombustibleAPI.Domain.Enums;

/// <summary>
/// Los 5 roles de negocio definitivos del SDP General (sección 3).
/// Un usuario posee exactamente un rol.
/// </summary>
public enum RolUsuario : short
{
    ADMINISTRADOR = 1,
    SUPERVISOR = 2,
    DESPACHADOR = 3,
    SOLICITANTE = 4,
    AUDITOR = 5
}

public enum EstadoSolicitud
{
    PENDIENTE,
    APROBADA,
    RECHAZADA,
    CANCELADA
}

/// <summary>
/// Estados de ticket según el esquema físico de PostgreSQL y la vista de operaciones.
/// </summary>
public enum EstadoTicket
{
    CREADO,
    ENVIADO,
    ACTIVO,
    CONSUMIDO,
    VENCIDO,
    ANULADO
}

/// <summary>
/// Tipos de movimiento de inventario en el ledger (RN-09).
/// </summary>
public enum TipoMovimientoInventario
{
    RECEPCION,
    DESPACHO,
    TRANSFERENCIA_ENTRADA,
    TRANSFERENCIA_SALIDA,
    MERMA,
    AJUSTE_POSITIVO,
    AJUSTE_NEGATIVO
}

public enum EstadoCierre
{
    PENDIENTE_APROBACION,
    APROBADO,
    RECHAZADO
}

/// <summary>
/// Motivos de rechazo de validación de ticket.
/// </summary>
public enum MotivoRechazoTicket
{
    INEXISTENTE,
    FIRMA_INVALIDA,
    VENCIDO,
    CONSUMIDO,
    ANULADO,
    NO_AUTORIZADO
}
