namespace FuelManagement.Shared.Domain;

// ============================================================
// Entidades de dominio mapeadas al esquema PostgreSQL existente.
// NO se usan EF Migrations — el DDL ya esta definido en /database.
// ============================================================

public class Rol
{
    public short Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}

public class Departamento
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public DateTimeOffset? FechaActualizacion { get; set; }
}

public class TipoCombustible
{
    public short Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}

public class Estacion
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Ubicacion { get; set; }
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public DateTimeOffset? FechaActualizacion { get; set; }
}

public class Tanque
{
    public Guid Id { get; set; }
    public Guid EstacionId { get; set; }
    public short TipoCombustibleId { get; set; }
    public string Codigo { get; set; } = null!;
    public string? Nombre { get; set; }
    public decimal CapacidadMaxima { get; set; }
    public decimal StockActual { get; set; }
    public decimal NivelCritico { get; set; }
    public bool Activo { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public DateTimeOffset? FechaActualizacion { get; set; }

    // Nav
    public Estacion? Estacion { get; set; }
    public TipoCombustible? TipoCombustible { get; set; }
}

public class Usuario
{
    public Guid Id { get; set; }
    public short RolId { get; set; }
    public Guid? EmpleadoId { get; set; }
    public Guid? EstacionId { get; set; }
    public string NombreUsuario { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public bool Activo { get; set; }
    public bool Bloqueado { get; set; }
    public short IntentosFallidos { get; set; }
    public DateTimeOffset? UltimoAcceso { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public DateTimeOffset? FechaActualizacion { get; set; }
}

public class CierreDiario
{
    public Guid Id { get; set; }
    public Guid TanqueId { get; set; }
    public Guid CreadoPorUsuarioId { get; set; }
    public Guid? RevisadoPorUsuarioId { get; set; }
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

    public string Estado { get; set; } = "PENDIENTE_APROBACION";
    public DateTimeOffset FechaCreacion { get; set; }
    public DateTimeOffset? FechaRevision { get; set; }
    public string? MotivoDiferencia { get; set; }
    public string? MotivoRechazo { get; set; }
    public string? Observaciones { get; set; }

    // Nav
    public Tanque? Tanque { get; set; }
    public Usuario? CreadoPor { get; set; }
    public Usuario? RevisadoPor { get; set; }
}

public class AlertaOperativa
{
    public Guid Id { get; set; }
    public string Tipo { get; set; } = null!;
    public string Severidad { get; set; } = "WARNING";
    public string? EntidadOrigen { get; set; }
    public Guid? EntidadId { get; set; }
    public string Mensaje { get; set; } = null!;
    public string Estado { get; set; } = "PENDIENTE";
    public DateTimeOffset FechaCreacion { get; set; }
    public DateTimeOffset? FechaResolucion { get; set; }
    public Guid? ResueltaPorUsuarioId { get; set; }

    // Nav
    public Usuario? ResueltaPor { get; set; }
}

public class AjusteInventario
{
    public Guid Id { get; set; }
    public Guid TanqueId { get; set; }
    public Guid ReportadoPorUsuarioId { get; set; }
    public Guid? RevisadoPorUsuarioId { get; set; }
    public string TipoAjuste { get; set; } = null!;
    public decimal Cantidad { get; set; }
    public string Motivo { get; set; } = null!;
    public string Estado { get; set; } = "PENDIENTE";
    public DateTimeOffset FechaReporte { get; set; }
    public DateTimeOffset? FechaRevision { get; set; }
    public string? MotivoRechazo { get; set; }
    public string? Observaciones { get; set; }

    // Nav
    public Tanque? Tanque { get; set; }
}

public class Ticket
{
    public Guid Id { get; set; }
    public Guid SolicitudId { get; set; }
    public Guid EstacionId { get; set; }
    public short TipoCombustibleId { get; set; }
    public string NumeroTicket { get; set; } = null!;
    public decimal CantidadAutorizada { get; set; }
    public string Estado { get; set; } = "CREADO";
    public DateTimeOffset FechaEmision { get; set; }
    public DateTimeOffset FechaExpiracion { get; set; }
    public DateTimeOffset? FechaEnvio { get; set; }
    public DateTimeOffset? FechaAnulacion { get; set; }
}

public class MovimientoInventario
{
    public Guid Id { get; set; }
    public Guid TanqueId { get; set; }
    public Guid RegistradoPorUsuarioId { get; set; }
    public string TipoMovimiento { get; set; } = null!;
    public decimal Cantidad { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal SaldoPosterior { get; set; }
    public Guid? RecepcionId { get; set; }
    public Guid? DespachoId { get; set; }
    public Guid? TransferenciaId { get; set; }
    public Guid? AjusteId { get; set; }
    public DateTimeOffset FechaMovimiento { get; set; }
    public string? Observaciones { get; set; }

    public Tanque? Tanque { get; set; }
}
