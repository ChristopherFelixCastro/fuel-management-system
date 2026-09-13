using FuelManagement.Reports.Dtos;
using FuelManagement.Shared.Contracts;
using FuelManagement.Shared.Data;
using FuelManagement.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace FuelManagement.Reports.Services;

public class ReportService : IReportService
{
    private readonly FuelDbContext _db;

    public ReportService(FuelDbContext db)
    {
        _db = db;
    }

    // ----------------------------------------------------------------
    // Consumo Diario — se construye sobre vw_movimientos_tanque
    // filtrando los movimientos tipo DESPACHO y agrupando por día.
    // ----------------------------------------------------------------
    public async Task<PagedResult<VwConsumoDiario>> GetConsumptionReportAsync(
        ConsumptionReportFilter filter,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = BuildConsumptionQuery(filter);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return PagedResult<VwConsumoDiario>.Create(items, page, pageSize, total);
    }

    public async Task<List<VwConsumoDiario>> GetConsumptionDataAsync(
        ConsumptionReportFilter filter,
        CancellationToken ct = default)
    {
        return await BuildConsumptionQuery(filter).ToListAsync(ct);
    }

    private IQueryable<VwConsumoDiario> BuildConsumptionQuery(ConsumptionReportFilter filter)
    {
        IQueryable<VwMovimientosTanque> baseQuery = _db.VwMovimientosTanque.AsNoTracking();

        if (filter.FechaDesde.HasValue)
            baseQuery = baseQuery.Where(x => x.FechaMovimiento >= StartOfDayUtc(filter.FechaDesde.Value));

        if (filter.FechaHasta.HasValue)
            baseQuery = baseQuery.Where(x => x.FechaMovimiento <= EndOfDayUtc(filter.FechaHasta.Value));

        if (filter.TanqueId.HasValue)
            baseQuery = baseQuery.Where(x => x.TanqueId == filter.TanqueId.Value);

        return baseQuery
            .Where(x => x.TipoMovimiento == "DESPACHO")
            .GroupBy(x => new { Fecha = x.FechaMovimiento.Date, x.TanqueId, x.TanqueNombre, x.CombustibleNombre })
            .Select(g => new VwConsumoDiario
            {
                Fecha = g.Key.Fecha,
                TanqueId = g.Key.TanqueId,
                TanqueNombre = g.Key.TanqueNombre,
                CombustibleTipo = g.Key.CombustibleNombre,
                TotalDespachadoLitros = g.Sum(x => x.Cantidad),
                CantidadDespachos = g.Count()
            })
            .OrderByDescending(x => x.Fecha);
    }

    // ----------------------------------------------------------------
    // Resumen Inventario — no existe una vista con capacidad máxima,
    // % de ocupación y nivel crítico, por lo que se consulta la tabla
    // tanque con JOIN a estacion/tipo_combustible.
    // ----------------------------------------------------------------
    public async Task<PagedResult<VwTanqueResumen>> GetInventoryReportAsync(
        InventoryReportFilter filter,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = BuildInventoryQuery(filter);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return PagedResult<VwTanqueResumen>.Create(items, page, pageSize, total);
    }

    public async Task<List<VwTanqueResumen>> GetInventoryDataAsync(
        InventoryReportFilter filter,
        CancellationToken ct = default)
    {
        return await BuildInventoryQuery(filter).ToListAsync(ct);
    }

    private IQueryable<VwTanqueResumen> BuildInventoryQuery(InventoryReportFilter filter)
    {
        IQueryable<Tanque> baseQuery = _db.Tanques.AsNoTracking().Where(t => t.Activo);

        if (filter.TanqueId.HasValue)
            baseQuery = baseQuery.Where(x => x.Id == filter.TanqueId.Value);

        if (!string.IsNullOrWhiteSpace(filter.CombustibleTipo))
            baseQuery = baseQuery.Where(x => x.TipoCombustible!.Nombre == filter.CombustibleTipo);

        return baseQuery
            .Select(x => new VwTanqueResumen
            {
                Id = x.Id,
                TanqueNombre = x.Nombre ?? x.Codigo,
                Codigo = x.Codigo,
                CombustibleTipo = x.TipoCombustible!.Nombre,
                CapacidadTotal = x.CapacidadMaxima,
                StockActual = x.StockActual,
                PorcentajeOcupacion = x.CapacidadMaxima > 0 ? x.StockActual * 100m / x.CapacidadMaxima : 0m,
                EstadoStock = x.StockActual <= x.NivelCritico
                    ? "CRITICO"
                    : x.StockActual >= x.CapacidadMaxima * 0.90m ? "ALTO" : "NORMAL"
            })
            .OrderBy(x => x.TanqueNombre);
    }

    // ----------------------------------------------------------------
    // Kardex Trazabilidad — usa vw_movimientos_tanque.
    // ----------------------------------------------------------------
    public async Task<PagedResult<VwMovimientosTanque>> GetTraceabilityReportAsync(
        TraceabilityReportFilter filter,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = BuildTraceabilityQuery(filter);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return PagedResult<VwMovimientosTanque>.Create(items, page, pageSize, total);
    }

    public async Task<List<VwMovimientosTanque>> GetTraceabilityDataAsync(
        TraceabilityReportFilter filter,
        CancellationToken ct = default)
    {
        return await BuildTraceabilityQuery(filter).ToListAsync(ct);
    }

    private IQueryable<VwMovimientosTanque> BuildTraceabilityQuery(TraceabilityReportFilter filter)
    {
        IQueryable<VwMovimientosTanque> query = _db.VwMovimientosTanque.AsNoTracking();

        if (filter.FechaDesde.HasValue)
            query = query.Where(x => x.FechaMovimiento >= StartOfDayUtc(filter.FechaDesde.Value));

        if (filter.FechaHasta.HasValue)
            query = query.Where(x => x.FechaMovimiento <= EndOfDayUtc(filter.FechaHasta.Value));

        if (filter.TanqueId.HasValue)
            query = query.Where(x => x.TanqueId == filter.TanqueId.Value);

        if (!string.IsNullOrWhiteSpace(filter.TipoMovimiento))
            query = query.Where(x => x.TipoMovimiento == filter.TipoMovimiento.ToUpper());

        return query.OrderByDescending(x => x.FechaMovimiento);
    }

    private static DateTimeOffset StartOfDayUtc(DateOnly date) =>
        new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    private static DateTimeOffset EndOfDayUtc(DateOnly date) =>
        new(date.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
}