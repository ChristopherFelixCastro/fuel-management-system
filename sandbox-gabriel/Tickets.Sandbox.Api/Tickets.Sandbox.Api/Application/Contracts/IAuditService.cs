namespace Tickets.Sandbox.Api.Application.Contracts;

/// <summary>
/// Contrato con el servicio de Auditoría transversal (mantenido por Iván).
/// TODO-INTEGRACIÓN: Conectar con el servicio de auditoría real cuando esté integrado.
/// </summary>
public interface IAuditService
{
    Task LogEventAsync(string eventType, string entityName, string entityId, string details, string? userId = null, CancellationToken cancellationToken = default);
}
