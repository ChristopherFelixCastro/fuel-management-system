using CombustibleAPI.Application.Dtos.Alerts;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CombustibleAPI.Infrastructure.Services;

public class AlertService : IAlertService
{
    private readonly AppDbContext _db;

    public AlertService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<LowInventoryAlertPagedResponseDto> GetLowInventoryAsync(
        LowInventoryAlertFilterDto filter,
        CancellationToken ct)
    {
        var query = _db.Tanques
            .AsNoTracking()
            .Where(t =>
                t.Activo &&
                t.StockActual <= t.NivelCritico);

        if (filter.EstacionId.HasValue)
        {
            query = query.Where(
                t => t.EstacionId == filter.EstacionId.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(t => t.StockActual)
            .ThenBy(t => t.Codigo)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => new LowInventoryAlertDto
            {
                TanqueId = t.Id,
                TanqueCodigo = t.Codigo,
                TanqueNombre = t.Nombre,

                EstacionId = t.EstacionId,
                Estacion = t.Estacion.Nombre,

                TipoCombustibleId = t.TipoCombustibleId,
                Combustible = t.TipoCombustible.Nombre,

                StockActualGalones = t.StockActual,
                NivelCriticoGalones = t.NivelCritico,

                DeficitGalones =
                    t.NivelCritico > t.StockActual
                        ? t.NivelCritico - t.StockActual
                        : 0,

                Mensaje =
                    "El tanque " + t.Codigo +
                    " se encuentra en nivel crítico de combustible."
            })
            .ToListAsync(ct);

        return new LowInventoryAlertPagedResponseDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }
}