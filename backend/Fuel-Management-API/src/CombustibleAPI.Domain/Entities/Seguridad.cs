using CombustibleAPI.Domain.Enums;

namespace CombustibleAPI.Domain.Entities;

/// <summary>
/// Tabla "rol". Catálogo fijo de los 5 roles del sistema (SDP General, sec. 3).
/// </summary>
public class Rol
{
    public short Id { get; set; }
    public string Nombre { get; set; } = default!; // ADMINISTRADOR, SUPERVISOR, DESPACHADOR, SOLICITANTE, AUDITOR
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}

/// <summary>
/// Tabla "usuario". Empleado y Usuario son entidades distintas (SDP General, sec. 3):
/// puede existir un empleado sin cuenta. Todo DESPACHADOR requiere EstacionId.
/// </summary>
public class Usuario
{
    public Guid Id { get; set; }
    public short RolId { get; set; }
    public Rol Rol { get; set; } = default!;

    public Guid? EmpleadoId { get; set; }
    public Empleado? Empleado { get; set; }

    /// <summary>Obligatorio si Rol == DESPACHADOR (regla RN de negocio, ver SDP General sec. 4).</summary>
    public Guid? EstacionId { get; set; }
    public Estacion? Estacion { get; set; }

    public string NombreUsuario { get; set; } = default!;
    public string Email { get; set; } = default!;

    /// <summary>Alias de compatibilidad para código existente</summary>
    public string Username
    {
        get => NombreUsuario;
        set => NombreUsuario = value;
    }

    /// <summary>Hash producido por ASP.NET Core Identity PasswordHasher&lt;Usuario&gt;. Nunca texto plano.</summary>
    public string PasswordHash { get; set; } = default!;

    public bool Activo { get; set; } = true;
    public bool Bloqueado { get; set; } = false;
    public short IntentosFallidos { get; set; } = 0;
    public DateTime? UltimoAcceso { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }

    public DateTime CreadoEn
    {
        get => FechaCreacion;
        set => FechaCreacion = value;
    }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

/// <summary>
/// Tabla "refresh_token". Solo se persiste el TokenHash, nunca el valor real
/// (SDP Iván, sección 4 "Identidad y sesión"). Cada refresh rota e invalida al anterior;
/// la reutilización de un token ya rotado implica revocar toda la cadena de sesión.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = default!;

    public string TokenHash { get; set; } = default!;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaExpiracion { get; set; }
    public DateTime? FechaRevocacion { get; set; }
    public Guid? ReemplazadoPorId { get; set; }
    public string? IpCreacion { get; set; }
    public string? IpRevocacion { get; set; }

    // Compatibilidad con código previo
    public DateTime CreadoEn { get => FechaCreacion; set => FechaCreacion = value; }
    public DateTime ExpiraEn { get => FechaExpiracion; set => FechaExpiracion = value; }
    public DateTime? RevocadoEn { get => FechaRevocacion; set => FechaRevocacion = value; }

    public bool EstaActivo => FechaRevocacion is null && DateTime.UtcNow < FechaExpiracion;
    public bool EstaExpirado => DateTime.UtcNow >= FechaExpiracion;
}

/// <summary>
/// Tabla "audit_log". Append-only con hash encadenado SHA-256 (SDP Iván sec. 4 / SDP General sec. 9).
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? UsuarioId { get; set; }
    public string Accion { get; set; } = default!;
    public string Entidad { get; set; } = default!;
    public Guid? EntidadId { get; set; }
    public string? DatosAnteriores { get; set; } // JSONB serializado
    public string? DatosNuevos { get; set; }      // JSONB serializado
    public string? DireccionIp { get; set; }
    public string? UserAgent { get; set; }
    public DateTime FechaHora { get; set; } = DateTime.UtcNow;
    public string? HashAnterior { get; set; }
    public string HashActual { get; set; } = default!;
}
