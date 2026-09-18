using CombustibleAPI.Application.Dtos.Reports;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CombustibleAPI.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _context;

    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ReportPagedResponseDto<ConsumptionReportItemDto>> GetConsumptionAsync(
        ConsumptionReportFilterDto filter,
        CancellationToken ct)
    {
        var query = _context.MovimientosInventario
            .AsNoTracking()
            .Where(m => m.TipoMovimiento == "DESPACHO");

        if (filter.FechaDesde.HasValue)
        {
            var desde = ToUtcStart(filter.FechaDesde.Value);
            query = query.Where(m => m.FechaMovimiento >= desde);
        }

        if (filter.FechaHasta.HasValue)
        {
            var hasta = ToUtcStart(filter.FechaHasta.Value.AddDays(1));
            query = query.Where(m => m.FechaMovimiento < hasta);
        }

        if (filter.TanqueId.HasValue)
            query = query.Where(m => m.TanqueId == filter.TanqueId.Value);

        var groupedQuery = query
            .GroupBy(m => new
            {
                Fecha = m.FechaMovimiento.Date,
                m.TanqueId,
                TanqueCodigo = m.Tanque.Codigo,
                TanqueNombre = m.Tanque.Nombre,
                m.Tanque.TipoCombustibleId,
                Combustible = m.Tanque.TipoCombustible.Nombre
            })
            .Select(g => new
            {
                g.Key.Fecha,
                g.Key.TanqueId,
                g.Key.TanqueCodigo,
                g.Key.TanqueNombre,
                g.Key.TipoCombustibleId,
                g.Key.Combustible,
                TotalDespachado = g.Sum(x => x.Cantidad),
                CantidadDespachos = g.Count()
            });

        var total = await groupedQuery.CountAsync(ct);

        var rawItems = await groupedQuery
            .OrderByDescending(x => x.Fecha)
            .ThenBy(x => x.TanqueCodigo)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        var items = rawItems.Select(x => new ConsumptionReportItemDto
        {
            Fecha = DateOnly.FromDateTime(x.Fecha),
            TanqueId = x.TanqueId,
            TanqueCodigo = x.TanqueCodigo,
            TanqueNombre = x.TanqueNombre,
            TipoCombustibleId = x.TipoCombustibleId,
            Combustible = x.Combustible,
            TotalDespachadoGalones = x.TotalDespachado,
            CantidadDespachos = x.CantidadDespachos
        }).ToList();

        return new ReportPagedResponseDto<ConsumptionReportItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<ReportPagedResponseDto<InventoryReportItemDto>> GetInventoryAsync(
        InventoryReportFilterDto filter,
        CancellationToken ct)
    {
        var query = _context.Tanques
            .AsNoTracking()
            .Where(t => t.Activo);

        if (filter.TanqueId.HasValue)
            query = query.Where(t => t.Id == filter.TanqueId.Value);

        if (filter.TipoCombustibleId.HasValue)
            query = query.Where(t => t.TipoCombustibleId == filter.TipoCombustibleId.Value);

        if (filter.EstacionId.HasValue)
            query = query.Where(t => t.EstacionId == filter.EstacionId.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(t => t.Codigo)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => new InventoryReportItemDto
            {
                TanqueId = t.Id,
                TanqueCodigo = t.Codigo,
                TanqueNombre = t.Nombre,

                EstacionId = t.EstacionId,
                Estacion = t.Estacion.Nombre,

                TipoCombustibleId = t.TipoCombustibleId,
                Combustible = t.TipoCombustible.Nombre,

                CapacidadMaximaGalones = t.CapacidadMaxima,
                StockActualGalones = t.StockActual,
                NivelCriticoGalones = t.NivelCritico,

                PorcentajeOcupacion = t.CapacidadMaxima > 0
                    ? t.StockActual * 100m / t.CapacidadMaxima
                    : 0,

                EstadoStock = t.StockActual <= t.NivelCritico
                    ? "CRITICO"
                    : t.StockActual >= t.CapacidadMaxima * 0.90m
                        ? "ALTO"
                        : "NORMAL"
            })
            .ToListAsync(ct);

        return new ReportPagedResponseDto<InventoryReportItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<ReportPagedResponseDto<TraceabilityReportItemDto>> GetTraceabilityAsync(
        TraceabilityReportFilterDto filter,
        CancellationToken ct)
    {
        var query = _context.MovimientosInventario
            .AsNoTracking()
            .AsQueryable();

        if (filter.FechaDesde.HasValue)
        {
            var desde = ToUtcStart(filter.FechaDesde.Value);
            query = query.Where(m => m.FechaMovimiento >= desde);
        }

        if (filter.FechaHasta.HasValue)
        {
            var hasta = ToUtcStart(filter.FechaHasta.Value.AddDays(1));
            query = query.Where(m => m.FechaMovimiento < hasta);
        }

        if (filter.TanqueId.HasValue)
            query = query.Where(m => m.TanqueId == filter.TanqueId.Value);

        if (!string.IsNullOrWhiteSpace(filter.TipoMovimiento))
        {
            var tipo = filter.TipoMovimiento.Trim().ToUpperInvariant();
            query = query.Where(m => m.TipoMovimiento == tipo);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(m => m.FechaMovimiento)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(m => new TraceabilityReportItemDto
            {
                MovimientoId = m.Id,

                TanqueId = m.TanqueId,
                TanqueCodigo = m.Tanque.Codigo,
                TanqueNombre = m.Tanque.Nombre,

                EstacionId = m.Tanque.EstacionId,
                Estacion = m.Tanque.Estacion.Nombre,

                TipoCombustibleId = m.Tanque.TipoCombustibleId,
                Combustible = m.Tanque.TipoCombustible.Nombre,

                TipoMovimiento = m.TipoMovimiento,
                CantidadGalones = m.Cantidad,
                SaldoAnteriorGalones = m.SaldoAnterior,
                SaldoPosteriorGalones = m.SaldoPosterior,

                RegistradoPorUsuarioId = m.RegistradoPorUsuarioId,
                RegistradoPor = m.RegistradoPorUsuario.NombreUsuario,

                RecepcionId = m.RecepcionId,
                DespachoId = m.DespachoId,
                TransferenciaId = m.TransferenciaId,
                AjusteId = m.AjusteId,

                FechaMovimiento = m.FechaMovimiento,
                Observaciones = m.Observaciones
            })
            .ToListAsync(ct);

        return new ReportPagedResponseDto<TraceabilityReportItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    private static DateTime ToUtcStart(DateOnly fecha)
    {
        return DateTime.SpecifyKind(
            fecha.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);
    }
}