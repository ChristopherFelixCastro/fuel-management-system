using CombustibleAPI.Application.Dtos.Auth;
using CombustibleAPI.Application.Dtos.Closures;
using CombustibleAPI.Application.Dtos.Dispatches;
using CombustibleAPI.Application.Dtos.Inventory;
using CombustibleAPI.Application.Dtos.Masters;
using CombustibleAPI.Application.Dtos.Tickets;
using CombustibleAPI.Domain.Entities;

namespace CombustibleAPI.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress, CancellationToken ct);
    Task<LoginResponseDto> RefreshAsync(string refreshTokenValue, string? ipAddress, CancellationToken ct);
    Task LogoutAsync(string refreshTokenValue, string? ipAddress, CancellationToken ct);
    Task<UserProfileDto> GetCurrentUserProfileAsync(Guid usuarioId, CancellationToken ct);
}

/// <summary>
/// Emisión/validación de JWT de acceso y generación de refresh tokens (valor real + hash).
/// </summary>
public interface ITokenService
{
    (string token, DateTime expiraEn) GenerarAccessToken(Usuario usuario);

    /// <summary>Genera un valor aleatorio criptográficamente seguro para el refresh token.</summary>
    string GenerarRefreshTokenValue();

    string HashRefreshToken(string refreshTokenValue);
}

/// <summary>
/// Orquesta el despacho transaccional: valida ticket, ejecuta despacho atómico
/// en PostgreSQL, actualiza saldo/ledger y escribe auditoría como unidad atómica.
/// </summary>
public interface IDispatchService
{
    Task<DispatchResultDto> RegistrarDespachoAsync(DispatchRequestDto request, Guid despachadorId, Guid estacionDespachadorId, string? ipAddress, CancellationToken ct);
    Task<DispatchDetailDto> GetByIdAsync(Guid id, CancellationToken ct);
    Task<PaginatedList<DispatchDetailDto>> GetPaginatedAsync(DispatchesFilterDto filter, CancellationToken ct);
}

/// <summary>
/// Valida un ticket QR contra el estado oficial en BD.
/// </summary>
public interface ITicketService
{
    Task<TicketOficialDto> ValidarAsync(string qrPayload, CancellationToken ct);
}

/// <summary>
/// Catálogos maestros (roles, estaciones, vehículos, tanques).
/// </summary>
public interface IMastersService
{
    Task<List<RolDto>> GetRolesAsync(CancellationToken ct);
    Task<List<EstacionDto>> GetEstacionesAsync(CancellationToken ct);
    Task<List<VehiculoDto>> GetVehiculosAsync(CancellationToken ct);
    Task<List<TanqueDto>> GetTanquesAsync(Guid? estacionId, short? tipoCombustibleId, CancellationToken ct);
}

/// <summary>
/// Disponibilidad de inventario y tanques compatibles.
/// </summary>
public interface IInventoryService
{
    Task<AvailabilityResponseDto> GetAvailabilityAsync(Guid estacionId, short tipoCombustibleId, CancellationToken ct);
}

/// <summary>
/// Cierres diarios de tanques.
/// </summary>
public interface IClosureService
{
    Task<ClosureResponseDto> CreateDailyClosureAsync(CreateDailyClosureRequestDto request, Guid usuarioId, CancellationToken ct);
    Task ApproveClosureAsync(Guid closureId, Guid supervisorId, CancellationToken ct);
    Task RejectClosureAsync(Guid closureId, Guid supervisorId, string motivo, CancellationToken ct);
}

/// <summary>
/// Registra eventos en audit_log (append-only) encadenando SHA-256(evento canónico + hash anterior).
/// </summary>
public interface IAuditService
{
    Task RegistrarAsync(Guid? usuarioId, string accion, string entidad, string? entidadId,
        string? ipAddress, object? datosAnteriores, object? datosNuevos, CancellationToken ct);
}

public interface ICurrentUserService
{
    Guid? UsuarioId { get; }
    string? Rol { get; }
    Guid? EstacionId { get; }
    string? IpAddress { get; }
}
