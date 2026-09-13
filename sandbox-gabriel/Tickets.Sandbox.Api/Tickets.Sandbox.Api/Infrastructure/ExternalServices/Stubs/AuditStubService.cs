using Microsoft.Extensions.Logging;
using Tickets.Sandbox.Api.Application.Contracts;

namespace Tickets.Sandbox.Api.Infrastructure.ExternalServices.Stubs;

/// <summary>
/// TODO-INTEGRACIÓN: Stub temporal que simula el módulo de Auditoría transversal (mantenido por Iván).
/// </summary>
public class AuditStubService : IAuditService
{
    private readonly ILogger<AuditStubService> _logger;

    public AuditStubService(ILogger<AuditStubService> logger)
    {
        _logger = logger;
    }

    public Task LogEventAsync(string eventType, string entityName, string entityId, string details, string? userId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[AUDIT] Event: {EventType} | Entity: {EntityName} ({EntityId}) | User: {UserId} | Details: {Details}",
            eventType, entityName, entityId, userId ?? "SYSTEM", details);
        return Task.CompletedTask;
    }
}
