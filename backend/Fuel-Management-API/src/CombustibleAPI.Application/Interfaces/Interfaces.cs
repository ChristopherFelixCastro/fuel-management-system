using CombustibleAPI.Application.Dtos.Auth;
using CombustibleAPI.Application.Dtos.Closures;
using CombustibleAPI.Application.Dtos.Dispatches;
using CombustibleAPI.Application.Dtos.Inventory;
using CombustibleAPI.Application.Dtos.Masters;
using CombustibleAPI.Application.Dtos.Tickets;
using CombustibleAPI.Domain.Entities;
using CombustibleAPI.Application.Dtos.Reports;
using CombustibleAPI.Application.Dtos.Alerts;
namespace CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Application.Dtos.Admin;
using CombustibleAPI.Application.Dtos.Dashboard;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(
        LoginRequestDto request,
        string? ipAddress,
        CancellationToken ct);

    Task<LoginResponseDto> RefreshAsync(
        string refreshTokenValue,
        string? ipAddress,
        CancellationToken ct);

    Task LogoutAsync(
        string refreshTokenValue,
        string? ipAddress,
        CancellationToken ct);

    Task<UserProfileDto> GetCurrentUserProfileAsync(
        Guid usuarioId,
        CancellationToken ct);
}

/// <summary>
/// EmisiÃ³n/validaciÃ³n de JWT de acceso y generaciÃ³n de refresh tokens.
/// </summary>
public interface ITokenService
{
    (string token, DateTime expiraEn)
        GenerarAccessToken(Usuario usuario);

    /// <summary>
    /// Genera un valor aleatorio criptogrÃ¡ficamente seguro
    /// para el refresh token.
    /// </summary>
    string GenerarRefreshTokenValue();

    string HashRefreshToken(
        string refreshTokenValue);
}

/// <summary>
/// Orquesta el despacho transaccional.
/// </summary>
public interface IDispatchService
{
    Task<DispatchResultDto> RegistrarDespachoAsync(
        DispatchRequestDto request,
        Guid despachadorId,
        Guid estacionDespachadorId,
        string? ipAddress,
        CancellationToken ct);

    Task<DispatchDetailDto> GetByIdAsync(
        Guid id,
        CancellationToken ct);

    Task<PaginatedList<DispatchDetailDto>>
        GetPaginatedAsync(
            DispatchesFilterDto filter,
            CancellationToken ct);
}

/// <summary>
/// Operaciones relacionadas con tickets y cÃ³digos QR.
/// </summary>
public interface ITicketService
{
    /// <summary>
    /// Valida un payload QR contra el ticket oficial
    /// almacenado en la base de datos.
    /// </summary>
    Task<TicketOficialDto> ValidarAsync(
        string qrPayload,
        CancellationToken ct);

    /// <summary>
    /// Reconstruye de forma segura el QR de un ticket
    /// previamente emitido y devuelve su imagen PNG.
    /// </summary>
    Task<byte[]> ObtenerQrPngAsync(
        Guid ticketId,
        CancellationToken ct);
}

/// <summary>
/// CatÃ¡logos maestros.
/// </summary>
public interface IMastersService
{
    Task<List<RolDto>> GetRolesAsync(
        CancellationToken ct);

    Task<List<EstacionDto>> GetEstacionesAsync(
        CancellationToken ct);

    Task<List<VehiculoDto>> GetVehiculosAsync(
        CancellationToken ct);

    Task<List<TanqueDto>> GetTanquesAsync(
        Guid? estacionId,
        short? tipoCombustibleId,
        CancellationToken ct);
}

/// <summary>
/// Disponibilidad de inventario y tanques compatibles.
/// </summary>
public interface IInventoryService
{
    Task<AvailabilityResponseDto> GetAvailabilityAsync(
        Guid estacionId,
        short tipoCombustibleId,
        CancellationToken ct);
}

/// <summary>
/// Cierres diarios de tanques.
/// </summary>
public interface IClosureService
{
    Task<ClosurePreviewDto> GetPreviewAsync(
        Guid tanqueId,
        DateOnly fecha,
        CancellationToken ct);

    Task<ClosurePagedResponseDto> GetClosuresAsync(
        ClosureFilterDto filter,
        CancellationToken ct);

    Task<ClosureResponseDto> GetClosureByIdAsync(
        Guid closureId,
        CancellationToken ct);

    Task<ClosureResponseDto> CreateDailyClosureAsync(
        CreateDailyClosureRequestDto request,
        Guid usuarioId,
        CancellationToken ct);

    Task ApproveClosureAsync(
        Guid closureId,
        Guid supervisorId,
        CancellationToken ct);

    Task RejectClosureAsync(
        Guid closureId,
        Guid supervisorId,
        string motivo,
        CancellationToken ct);
}

/// <summary>
/// Registra eventos en audit_log utilizando el mecanismo
/// de auditorÃ­a append-only del sistema.
/// </summary>
public interface IAuditService
{
    Task RegistrarAsync(
        Guid? usuarioId,
        string accion,
        string entidad,
        string? entidadId,
        string? ipAddress,
        object? datosAnteriores,
        object? datosNuevos,
        CancellationToken ct);
}

public interface ICurrentUserService
{
    Guid? UsuarioId { get; }
    string? Rol { get; }
    Guid? EstacionId { get; }
    string? IpAddress { get; }
}

public interface IReportService
{
    Task<ReportPagedResponseDto<ConsumptionReportItemDto>> GetConsumptionAsync(
        ConsumptionReportFilterDto filter,
        CancellationToken ct);

    Task<ReportPagedResponseDto<InventoryReportItemDto>> GetInventoryAsync(
        InventoryReportFilterDto filter,
        CancellationToken ct);

    Task<ReportPagedResponseDto<TraceabilityReportItemDto>> GetTraceabilityAsync(
        TraceabilityReportFilterDto filter,
        CancellationToken ct);
}

public interface IReportExportService
{
    Task<ReportExportResultDto> ExportConsumptionAsync(
        ConsumptionReportFilterDto filter,
        string format,
        CancellationToken ct);

    Task<ReportExportResultDto> ExportInventoryAsync(
        InventoryReportFilterDto filter,
        string format,
        CancellationToken ct);

    Task<ReportExportResultDto> ExportTraceabilityAsync(
        TraceabilityReportFilterDto filter,
        string format,
        CancellationToken ct);
}
public interface IAlertService
{
    Task<LowInventoryAlertPagedResponseDto> GetLowInventoryAsync(
        LowInventoryAlertFilterDto filter,
        CancellationToken ct);
}

public interface IAdminService
{
    // Departamentos
    Task<List<DepartamentoAdminDto>> GetDepartamentosAsync(
        bool incluirInactivos,
        CancellationToken ct);

    Task<DepartamentoAdminDto> GetDepartamentoAsync(
        Guid id,
        CancellationToken ct);

    Task<DepartamentoAdminDto> CreateDepartamentoAsync(
        CreateDepartamentoDto request,
        Guid usuarioId,
        CancellationToken ct);

    Task<DepartamentoAdminDto> UpdateDepartamentoAsync(
        Guid id,
        UpdateDepartamentoDto request,
        Guid usuarioId,
        CancellationToken ct);

    Task DeactivateDepartamentoAsync(
        Guid id,
        Guid usuarioId,
        CancellationToken ct);

    // Empleados
    Task<List<EmpleadoAdminDto>> GetEmpleadosAsync(
        bool incluirInactivos,
        CancellationToken ct);

    Task<EmpleadoAdminDto> GetEmpleadoAsync(
        Guid id,
        CancellationToken ct);

    Task<EmpleadoAdminDto> CreateEmpleadoAsync(
        CreateEmpleadoDto request,
        Guid usuarioId,
        CancellationToken ct);

    Task<EmpleadoAdminDto> UpdateEmpleadoAsync(
        Guid id,
        UpdateEmpleadoDto request,
        Guid usuarioId,
        CancellationToken ct);

    Task DeactivateEmpleadoAsync(
        Guid id,
        Guid usuarioId,
        CancellationToken ct);

    // Vehículos
    Task<List<VehiculoAdminDto>> GetVehiculosAsync(
        bool incluirInactivos,
        CancellationToken ct);

    Task<VehiculoAdminDto> GetVehiculoAsync(
        Guid id,
        CancellationToken ct);

    Task<VehiculoAdminDto> CreateVehiculoAsync(
        CreateVehiculoDto request,
        Guid usuarioId,
        CancellationToken ct);

    Task<VehiculoAdminDto> UpdateVehiculoAsync(
        Guid id,
        UpdateVehiculoDto request,
        Guid usuarioId,
        CancellationToken ct);

    Task DeactivateVehiculoAsync(
        Guid id,
        Guid usuarioId,
        CancellationToken ct);

    // Usuarios
    Task<List<UsuarioAdminDto>> GetUsuariosAsync(
        bool incluirInactivos,
        CancellationToken ct);

    Task<UsuarioAdminDto> GetUsuarioAsync(
        Guid id,
        CancellationToken ct);

    Task<UsuarioAdminDto> CreateUsuarioAsync(
        CreateUsuarioDto request,
        Guid usuarioId,
        CancellationToken ct);

    Task<UsuarioAdminDto> UpdateUsuarioAsync(
        Guid id,
        UpdateUsuarioDto request,
        Guid usuarioId,
        CancellationToken ct);

    Task ChangeUsuarioPasswordAsync(
        Guid id,
        ChangeUsuarioPasswordDto request,
        Guid usuarioId,
        CancellationToken ct);

    Task DeactivateUsuarioAsync(
        Guid id,
        Guid usuarioId,
        CancellationToken ct);

    // Catálogos
    Task<List<TipoCombustibleAdminDto>> GetTiposCombustibleAsync(
        CancellationToken ct);
}

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(
        DashboardFilterDto filter,
        CancellationToken ct);
}