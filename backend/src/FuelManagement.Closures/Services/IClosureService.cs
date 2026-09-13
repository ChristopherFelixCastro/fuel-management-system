using FuelManagement.Closures.Dtos;
using FuelManagement.Shared.Contracts;

namespace FuelManagement.Closures.Services;

/// <summary>
/// Contrato del servicio de cierres diarios.
/// Toda operacion de escritura llama a funciones PL/pgSQL via FromSql.
/// </summary>
public interface IClosureService
{
    /// <summary>Calcula el cierre del dia sin persistirlo (fn_calcular_cierre_diario).</summary>
    Task<ClosurePreviewDto> GetPreviewAsync(Guid tanqueId, DateOnly fecha, CancellationToken ct = default);

    /// <summary>Crea un cierre diario (fn_crear_cierre_diario) y registra auditoria.</summary>
    Task<Guid> CreateAsync(CreateClosureRequest request, Guid usuarioId, CancellationToken ct = default);

    /// <summary>Lista cierres con filtros opcionales paginados.</summary>
    Task<PagedResult<ClosureDto>> GetAllAsync(
        DateOnly? fechaDesde,
        DateOnly? fechaHasta,
        Guid? tanqueId,
        string? estado,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Obtiene un cierre por su id.</summary>
    Task<ClosureDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Aprueba un cierre (fn_aprobar_cierre) y registra auditoria.</summary>
    Task ApproveAsync(Guid cierreId, Guid supervisorId, CancellationToken ct = default);

    /// <summary>Rechaza un cierre (fn_rechazar_cierre) y registra auditoria.</summary>
    Task RejectAsync(Guid cierreId, Guid supervisorId, string motivo, CancellationToken ct = default);

    /// <summary>Genera el PDF del cierre usando QuestPDF.</summary>
    Task<byte[]> GeneratePdfAsync(Guid cierreId, CancellationToken ct = default);
}
