using FuelManagement.Shared.Domain;

namespace FuelManagement.Alerts.Dtos;

public sealed class AlertDto
{
    public Guid Id { get; init; }
    public string Tipo { get; init; } = string.Empty;
    public string Severidad { get; init; } = string.Empty;
    public string Modulo { get; init; } = string.Empty;
    public string Mensaje { get; init; } = string.Empty;
    public Guid? EntidadId { get; init; }
    public string Estado { get; init; } = string.Empty;
    public DateTimeOffset FechaGeneracion { get; init; }
    public DateTimeOffset? FechaReconocimiento { get; init; }
    public string? ReconocidoPor { get; init; }

    public static AlertDto FromEntity(AlertaOperativa entity) => new()
    {
        Id = entity.Id,
        Tipo = entity.Tipo,
        Severidad = entity.Severidad,
        Modulo = entity.EntidadOrigen ?? "SISTEMA",
        Mensaje = entity.Mensaje,
        EntidadId = entity.EntidadId,
        Estado = entity.Estado,
        FechaGeneracion = entity.FechaCreacion,
        FechaReconocimiento = entity.FechaResolucion,
        ReconocidoPor = entity.ResueltaPor?.NombreUsuario
    };
}

public sealed class AcknowledgeAlertRequest
{
    public string? Nota { get; init; }
}
