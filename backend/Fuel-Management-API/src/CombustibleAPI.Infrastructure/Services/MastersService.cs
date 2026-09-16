using CombustibleAPI.Application.Dtos.Masters;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CombustibleAPI.Infrastructure.Services;

public class MastersService : IMastersService
{
    private readonly AppDbContext _context;

    public MastersService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<RolDto>> GetRolesAsync(CancellationToken ct)
    {
        return await _context.Roles
            .AsNoTracking()
            .Where(r => r.Activo)
            .OrderBy(r => r.Id)
            .Select(r => new RolDto
            {
                Id = r.Id,
                Nombre = r.Nombre,
                Descripcion = r.Descripcion,
                Activo = r.Activo
            })
            .ToListAsync(ct);
    }

    public async Task<List<EstacionDto>> GetEstacionesAsync(CancellationToken ct)
    {
        return await _context.Estaciones
            .AsNoTracking()
            .Where(e => e.Activo)
            .OrderBy(e => e.Nombre)
            .Select(e => new EstacionDto
            {
                Id = e.Id,
                Codigo = e.Codigo,
                Nombre = e.Nombre,
                Ubicacion = e.Ubicacion,
                Descripcion = e.Descripcion,
                Activo = e.Activo
            })
            .ToListAsync(ct);
    }

    public async Task<List<VehiculoDto>> GetVehiculosAsync(CancellationToken ct)
    {
        return await _context.Vehiculos
            .Include(v => v.TipoCombustible)
            .Include(v => v.Departamento)
            .AsNoTracking()
            .Where(v => v.Activo)
            .OrderBy(v => v.Placa)
            .Select(v => new VehiculoDto
            {
                Id = v.Id,
                Placa = v.Placa,
                Ficha = v.Ficha,
                Marca = v.Marca,
                Modelo = v.Modelo,
                Anio = v.Anio,
                TipoVehiculo = v.TipoVehiculo,
                CapacidadTanque = v.CapacidadTanque,
                OdometroActual = v.OdometroActual,
                TipoCombustibleId = v.TipoCombustibleId,
                CombustibleNombre = v.TipoCombustible.Nombre,
                DepartamentoId = v.DepartamentoId,
                DepartamentoNombre = v.Departamento.Nombre,
                Activo = v.Activo
            })
            .ToListAsync(ct);
    }

    public async Task<List<TanqueDto>> GetTanquesAsync(Guid? estacionId, short? tipoCombustibleId, CancellationToken ct)
    {
        var query = _context.Tanques
            .Include(t => t.Estacion)
            .Include(t => t.TipoCombustible)
            .AsNoTracking()
            .Where(t => t.Activo);

        if (estacionId.HasValue && estacionId.Value != Guid.Empty)
        {
            query = query.Where(t => t.EstacionId == estacionId.Value);
        }

        if (tipoCombustibleId.HasValue && tipoCombustibleId.Value > 0)
        {
            query = query.Where(t => t.TipoCombustibleId == tipoCombustibleId.Value);
        }

        return await query
            .OrderBy(t => t.Codigo)
            .Select(t => new TanqueDto
            {
                Id = t.Id,
                Codigo = t.Codigo,
                Nombre = t.Nombre,
                EstacionId = t.EstacionId,
                EstacionNombre = t.Estacion.Nombre,
                TipoCombustibleId = t.TipoCombustibleId,
                CombustibleNombre = t.TipoCombustible.Nombre,
                CapacidadMaxima = t.CapacidadMaxima,
                StockActual = t.StockActual,
                NivelCritico = t.NivelCritico,
                Activo = t.Activo
            })
            .ToListAsync(ct);
    }
}
