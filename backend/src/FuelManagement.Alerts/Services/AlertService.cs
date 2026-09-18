using FuelManagement.Alerts.Detectors;
using FuelManagement.Alerts.Dtos;
using FuelManagement.Shared.Contracts;
using FuelManagement.Shared.Data;
using FuelManagement.Shared.Domain;
using FuelManagement.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FuelManagement.Alerts.Services;

public class AlertService : IAlertService
{
    private readonly FuelDbContext _db;
    private readonly IEnumerable<IAlertDetector> _detectors;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        FuelDbContext db,
        IEnumerable<IAlertDetector> detectors,
        ICurrentUserService currentUserService,
        ILogger<AlertService> logger)
    {
        _db = db;
        _detectors = detectors;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<PagedResult<AlertDto>> GetPaginatedAsync(
        string? estado,
        string? severidad,
        string? modulo,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = _db.AlertasOperativas
            .Include(a => a.ResueltaPor)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(a => a.Estado == estado.ToUpper());

        if (!string.IsNullOrWhiteSpace(severidad))
            query = query.Where(a => a.Severidad == severidad.ToUpper());

        if (!string.IsNullOrWhiteSpace(modulo))
            query = query.Where(a => a.EntidadOrigen == modulo.ToUpper());

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.FechaCreacion)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => AlertDto.FromEntity(a))
            .ToListAsync(ct);

        return PagedResult<AlertDto>.Create(items, page, pageSize, totalCount);
    }

    public async Task<AlertDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var alert = await _db.AlertasOperativas
            .Include(a => a.ResueltaPor)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        return alert is null ? null : AlertDto.FromEntity(alert);
    }

    public async Task<AlertDto> AcknowledgeAsync(Guid id, AcknowledgeAlertRequest? request, CancellationToken ct = default)
    {
        var alert = await _db.AlertasOperativas
            .Include(a => a.ResueltaPor)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (alert is null)
            throw new KeyNotFoundException($"No se encontró la alerta con ID {id}");

        if (alert.Estado == "RECONOCIDA" || alert.Estado == "RESUELTA")
            return AlertDto.FromEntity(alert);

        alert.Estado = "RECONOCIDA";
        alert.FechaResolucion = DateTimeOffset.UtcNow;
        alert.ResueltaPorUsuarioId = _currentUserService.UserId;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Alerta {AlertId} reconocida por usuario {User}", id, _currentUserService.UserName);

        return AlertDto.FromEntity(alert);
    }

    public async Task ScanAndCreateAlertsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Iniciando escaneo de alertas operativas...");

        foreach (var detector in _detectors)
        {
            try
            {
                var detectedAlerts = await detector.DetectAlertsAsync(_db, ct);
                foreach (var newAlert in detectedAlerts)
                {
                    var exists = await _db.AlertasOperativas.AnyAsync(a =>
                        a.Tipo == newAlert.Tipo &&
                        a.EntidadId == newAlert.EntidadId &&
                        a.Estado == "ACTIVA", ct);

                    if (!exists)
                    {
                        _db.AlertasOperativas.Add(newAlert);
                        _logger.LogWarning("Nueva alerta generada: [{Severidad}] {Tipo} - {Mensaje}",
                            newAlert.Severidad, newAlert.Tipo, newAlert.Mensaje);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar el detector {DetectorType}", detector.GetType().Name);
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
