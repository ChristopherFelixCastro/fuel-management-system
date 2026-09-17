using CombustibleAPI.Application.Dtos.Inventory;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Infrastructure.Persistence;
using CombustibleAPI.Infrastructure.Persistence.DbFunctions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CombustibleAPI.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _context;
    private readonly SqlFunctionsRepository _sqlFunctions;

    public InventoryService(AppDbContext context, SqlFunctionsRepository sqlFunctions)
    {
        _context = context;
        _sqlFunctions = sqlFunctions;
    }

    public async Task<AvailabilityResponseDto> GetAvailabilityAsync(Guid estacionId, short tipoCombustibleId, CancellationToken ct)
    {
        var estacion = await _context.Estaciones
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == estacionId && e.Activo, ct);

        if (estacion is null)
            throw ApiException.NotFound("Estación");

        var combustible = await _context.TiposCombustible
            .AsNoTracking()
            .FirstOrDefaultAsync(tc => tc.Id == tipoCombustibleId && tc.Activo, ct);

        if (combustible is null)
            throw ApiException.NotFound("Tipo de combustible");

        // Tanques compatibles de la estación
        var tanques = await _context.Tanques
            .AsNoTracking()
            .Where(t => t.EstacionId == estacionId && t.TipoCombustibleId == tipoCombustibleId && t.Activo)
            .OrderBy(t => t.Codigo)
            .Select(t => new TanqueCompatibleDto
            {
                Id = t.Id,
                Codigo = t.Codigo,
                Nombre = t.Nombre,
                CapacidadMaxima = t.CapacidadMaxima,
                StockActual = t.StockActual,
                NivelCritico = t.NivelCritico,
                Activo = t.Activo
            })
            .ToListAsync(ct);

        decimal stockFisico = 0;
        decimal stockReservado = 0;
        decimal stockDisponible = 0;

        // Intentar obtener stock a través de la función de PostgreSQL si estamos en conexión Npgsql real
        if (_context.Database.IsNpgsql())
        {
            try
            {
                var connection = (NpgsqlConnection)_context.Database.GetDbConnection();
                var abrioConexion = connection.State != System.Data.ConnectionState.Open;
                if (abrioConexion) await connection.OpenAsync(ct);

                try
                {
                    var (f, r, d) = await _sqlFunctions.ObtenerStockDisponibleAsync(connection, null, estacionId, tipoCombustibleId, ct);
                    stockFisico = f;
                    stockReservado = r;
                    stockDisponible = d;
                }
                finally
                {
                    if (abrioConexion) await connection.CloseAsync();
                }
            }
            catch
            {
                // Fallback a cálculo EF Core si la función SQL no está creada en el entorno
                CalcularStockViaEf(tanques, out stockFisico, out stockReservado, out stockDisponible, estacionId, tipoCombustibleId);
            }
        }
        else
        {
            CalcularStockViaEf(tanques, out stockFisico, out stockReservado, out stockDisponible, estacionId, tipoCombustibleId);
        }

        return new AvailabilityResponseDto
        {
            EstacionId = estacion.Id,
            EstacionNombre = estacion.Nombre,
            TipoCombustibleId = combustible.Id,
            CombustibleNombre = combustible.Nombre,
            StockFisico = stockFisico,
            StockReservado = stockReservado,
            StockDisponible = stockDisponible,
            TanquesCompatibles = tanques
        };
    }

    private void CalcularStockViaEf(
        List<TanqueCompatibleDto> tanques,
        out decimal stockFisico,
        out decimal stockReservado,
        out decimal stockDisponible,
        Guid estacionId,
        short tipoCombustibleId)
    {
        stockFisico = tanques.Sum(t => t.StockActual);

        var ahora = DateTime.UtcNow;
        stockReservado = _context.Tickets
            .AsNoTracking()
            .Where(tk => tk.EstacionId == estacionId &&
                         tk.TipoCombustibleId == tipoCombustibleId &&
                         (tk.Estado == "CREADO" || tk.Estado == "ENVIADO" || tk.Estado == "ACTIVO") &&
                         tk.FechaExpiracion > ahora)
            .Sum(tk => tk.CantidadAutorizada);

        stockDisponible = stockFisico - stockReservado;
    }
}
