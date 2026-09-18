using CombustibleAPI.Application.Dtos.Dashboard;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CombustibleAPI.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(
        DashboardFilterDto filter,
        CancellationToken ct)
    {
        if (filter.From.HasValue &&
            filter.To.HasValue &&
            filter.From.Value > filter.To.Value)
        {
            throw ApiException.ValidationError(
                "La fecha desde no puede ser posterior a la fecha hasta.");
        }

        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var tomorrowStart = todayStart.AddDays(1);

        DateTime? from = filter.From.HasValue
            ? ToUtcStart(filter.From.Value)
            : null;

        DateTime? toExclusive = filter.To.HasValue
            ? ToUtcStart(filter.To.Value.AddDays(1))
            : null;

        // -----------------------------
        // Solicitudes pendientes
        // -----------------------------
        var pendingRequestsQuery = _db.Solicitudes
            .AsNoTracking()
            .Where(s => s.Estado == "PENDIENTE");

        // Una solicitud pendiente todavía no tiene necesariamente estación,
        // porque la estación se asigna durante la aprobación/ticket.
        var pendingRequests = await pendingRequestsQuery.CountAsync(ct);

        // -----------------------------
        // Tickets activos
        // -----------------------------
        var activeTicketsQuery = _db.Tickets
            .AsNoTracking()
            .Where(t =>
                (t.Estado == "CREADO" || t.Estado == "ENVIADO") &&
                t.FechaExpiracion > now);

        if (filter.StationId.HasValue)
        {
            activeTicketsQuery = activeTicketsQuery
                .Where(t => t.EstacionId == filter.StationId.Value);
        }

        var activeTickets = await activeTicketsQuery.CountAsync(ct);

        // -----------------------------
        // Tanques / inventario
        // -----------------------------
        var tanksQuery = _db.Tanques
            .AsNoTracking()
            .Where(t => t.Activo);

        if (filter.StationId.HasValue)
        {
            tanksQuery = tanksQuery
                .Where(t => t.EstacionId == filter.StationId.Value);
        }

        var lowInventoryTanks = await tanksQuery
            .CountAsync(t => t.StockActual <= t.NivelCritico, ct);

        var inventoryByFuel = await tanksQuery
            .GroupBy(t => new
            {
                t.TipoCombustibleId,
                Nombre = t.TipoCombustible.Nombre
            })
            .Select(g => new DashboardInventoryByFuelDto
            {
                FuelTypeId = g.Key.TipoCombustibleId,
                FuelTypeName = g.Key.Nombre,
                CurrentStock = g.Sum(t => t.StockActual),
                TotalCapacity = g.Sum(t => t.CapacidadMaxima),
                Percentage = g.Sum(t => t.CapacidadMaxima) > 0
                    ? g.Sum(t => t.StockActual) * 100m /
                      g.Sum(t => t.CapacidadMaxima)
                    : 0
            })
            .OrderBy(x => x.FuelTypeName)
            .ToListAsync(ct);

        var recentAlerts = await tanksQuery
            .Where(t => t.StockActual <= t.NivelCritico)
            .OrderBy(t => t.StockActual)
            .ThenBy(t => t.Codigo)
            .Take(10)
            .Select(t => new DashboardAlertDto
            {
                TankId = t.Id,
                TankCode = t.Codigo,
                TankName = t.Nombre,
                StationName = t.Estacion.Nombre,
                FuelTypeName = t.TipoCombustible.Nombre,
                CurrentStock = t.StockActual,
                CriticalLevel = t.NivelCritico,
                Message =
                    "El tanque " + t.Codigo +
                    " se encuentra en nivel crítico de combustible."
            })
            .ToListAsync(ct);

        // -----------------------------
        // Despachos de hoy
        // -----------------------------
        var todayDispatchesQuery = _db.Despachos
            .AsNoTracking()
            .Where(d =>
                d.FechaDespacho >= todayStart &&
                d.FechaDespacho < tomorrowStart);

        if (filter.StationId.HasValue)
        {
            todayDispatchesQuery = todayDispatchesQuery
                .Where(d =>
                    d.Ticket.EstacionId == filter.StationId.Value);
        }

        var dispatchesToday = await todayDispatchesQuery.CountAsync(ct);

        var gallonsDispatchedToday = await todayDispatchesQuery
            .Select(d => (decimal?)d.CantidadDespachada)
            .SumAsync(ct) ?? 0m;

        // -----------------------------
        // Consumo según filtros
        // -----------------------------
        var consumptionQuery = _db.Despachos
            .AsNoTracking()
            .AsQueryable();

        if (from.HasValue)
        {
            consumptionQuery = consumptionQuery
                .Where(d => d.FechaDespacho >= from.Value);
        }

        if (toExclusive.HasValue)
        {
            consumptionQuery = consumptionQuery
                .Where(d => d.FechaDespacho < toExclusive.Value);
        }

        if (filter.StationId.HasValue)
        {
            consumptionQuery = consumptionQuery
                .Where(d =>
                    d.Ticket.EstacionId == filter.StationId.Value);
        }

        var consumptionByDepartment = await consumptionQuery
            .GroupBy(d => new
            {
                d.Ticket.Solicitud.DepartamentoId,
                d.Ticket.Solicitud.Departamento.Nombre
            })
            .Select(g => new DashboardConsumptionDto
            {
                Name = g.Key.Nombre,
                Gallons = g.Sum(d => d.CantidadDespachada)
            })
            .OrderByDescending(x => x.Gallons)
            .Take(10)
            .ToListAsync(ct);

        var consumptionByVehicle = await consumptionQuery
            .GroupBy(d => new
            {
                d.Ticket.Solicitud.VehiculoId,
                d.Ticket.Solicitud.Vehiculo.Placa,
                d.Ticket.Solicitud.Vehiculo.Ficha
            })
            .Select(g => new DashboardConsumptionDto
            {
                Name = g.Key.Placa + " / " + g.Key.Ficha,
                Gallons = g.Sum(d => d.CantidadDespachada)
            })
            .OrderByDescending(x => x.Gallons)
            .Take(10)
            .ToListAsync(ct);

        return new DashboardSummaryDto
        {
            PendingRequests = pendingRequests,
            ActiveTickets = activeTickets,
            LowInventoryTanks = lowInventoryTanks,
            DispatchesToday = dispatchesToday,
            GallonsDispatchedToday = gallonsDispatchedToday,
            InventoryByFuel = inventoryByFuel,
            RecentAlerts = recentAlerts,
            ConsumptionByDepartment = consumptionByDepartment,
            ConsumptionByVehicle = consumptionByVehicle
        };
    }

    private static DateTime ToUtcStart(DateOnly date)
    {
        return DateTime.SpecifyKind(
            date.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);
    }
}