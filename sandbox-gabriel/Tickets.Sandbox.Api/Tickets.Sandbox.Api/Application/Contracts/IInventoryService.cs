namespace Tickets.Sandbox.Api.Application.Contracts;

/// <summary>
/// Contrato con el módulo de Inventario (mantenido por José Enrique).
/// TODO-INTEGRACIÓN: Conectar con el servicio real de Inventario cuando esté integrado.
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Consulta disponibilidad real: existencia física - reservas activas.
    /// </summary>
    Task<decimal> GetAvailableInventoryAsync(Guid fuelTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea una reserva atómica de inventario asociada al ticket.
    /// </summary>
    Task<bool> ReserveInventoryAsync(Guid fuelTypeId, decimal quantity, Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Libera la reserva activa al cancelar o vencer un ticket.
    /// </summary>
    Task<bool> ReleaseReservationAsync(Guid ticketId, CancellationToken cancellationToken = default);
}
