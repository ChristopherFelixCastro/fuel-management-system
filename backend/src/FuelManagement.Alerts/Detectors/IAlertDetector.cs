using FuelManagement.Shared.Data;
using FuelManagement.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace FuelManagement.Alerts.Detectors;

public interface IAlertDetector
{
    Task<List<AlertaOperativa>> DetectAlertsAsync(FuelDbContext dbContext, CancellationToken ct = default);
}

public class LowInventoryDetector : IAlertDetector
{
    public async Task<List<AlertaOperativa>> DetectAlertsAsync(FuelDbContext dbContext, CancellationToken ct = default)
    {
        var alerts = new List<AlertaOperativa>();

        var lowTanks = await dbContext.Tanques
            .AsNoTracking()
            .Where(t => t.Activo && t.StockActual < t.NivelCritico)
            .ToListAsync(ct);

        foreach (var tank in lowTanks)
        {
            alerts.Add(new AlertaOperativa
            {
                Tipo = "STOCK_BAJO",
                Severidad = tank.StockActual <= (tank.NivelCritico * 0.5m) ? "CRITICA" : "ADVERTENCIA",
                EntidadOrigen = "INVENTARIO",
                Mensaje = $"El tanque '{tank.Nombre}' ({tank.Codigo}) tiene stock crítico: {tank.StockActual:N2} L (Mínimo: {tank.NivelCritico:N2} L)",
                EntidadId = tank.Id,
                Estado = "ACTIVA",
                FechaCreacion = DateTimeOffset.UtcNow
            });
        }

        return alerts;
    }
}

public class AdjustmentPendingDetector : IAlertDetector
{
    public async Task<List<AlertaOperativa>> DetectAlertsAsync(FuelDbContext dbContext, CancellationToken ct = default)
    {
        var alerts = new List<AlertaOperativa>();

        var thresholdDate = DateTimeOffset.UtcNow.AddHours(-24);
        var pendingAdjustments = await dbContext.AjustesInventario
            .AsNoTracking()
            .Where(a => a.Estado == "PENDIENTE" && a.FechaReporte <= thresholdDate)
            .ToListAsync(ct);

        foreach (var adj in pendingAdjustments)
        {
            alerts.Add(new AlertaOperativa
            {
                Tipo = "AJUSTE_PENDIENTE",
                Severidad = "ADVERTENCIA",
                EntidadOrigen = "AJUSTES",
                Mensaje = $"El ajuste de inventario (ID: {adj.Id}) lleva más de 24h pendiente de aprobación",
                EntidadId = adj.Id,
                Estado = "ACTIVA",
                FechaCreacion = DateTimeOffset.UtcNow
            });
        }

        return alerts;
    }
}
