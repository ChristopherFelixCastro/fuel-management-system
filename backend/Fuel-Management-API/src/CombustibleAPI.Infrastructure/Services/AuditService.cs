using System.Text.Json;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Domain.Entities;
using CombustibleAPI.Infrastructure.Persistence;
using CombustibleAPI.Infrastructure.Persistence.DbFunctions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CombustibleAPI.Infrastructure.Services;

/// <summary>
/// Registra auditoría append-only: encadena SHA-256 usando fn_registrar_auditoria en PostgreSQL
/// o persiste la entidad en DbContext como respaldo.
/// </summary>
public class AuditService : IAuditService
{
    private readonly AppDbContext _context;
    private readonly SqlFunctionsRepository _sqlFunctions;

    public AuditService(AppDbContext context, SqlFunctionsRepository sqlFunctions)
    {
        _context = context;
        _sqlFunctions = sqlFunctions;
    }

    public async Task RegistrarAsync(Guid? usuarioId, string accion, string entidad, string? entidadId,
        string? ipAddress, object? datosAnteriores, object? datosNuevos, CancellationToken ct)
    {
        var anterioresJson = datosAnteriores is null ? null : JsonSerializer.Serialize(datosAnteriores);
        var nuevosJson = datosNuevos is null ? null : JsonSerializer.Serialize(datosNuevos);

        Guid.TryParse(entidadId, out var parsedEntidadId);

        if (_context.Database.IsNpgsql())
        {
            var connection = (NpgsqlConnection)_context.Database.GetDbConnection();
            var abrioConexion = connection.State != System.Data.ConnectionState.Open;
            if (abrioConexion) await connection.OpenAsync(ct);

            await using var transaction = await connection.BeginTransactionAsync(ct);
            try
            {
                await _sqlFunctions.RegistrarAuditoriaAsync(
                    connection, transaction,
                    usuarioId, accion, entidad, parsedEntidadId == Guid.Empty ? null : parsedEntidadId,
                    anterioresJson, nuevosJson, ipAddress, null, ct);

                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
            }
            finally
            {
                if (abrioConexion) await connection.CloseAsync();
            }
        }
        else
        {
            // Entorno en memoria / pruebas
            var audit = new AuditLog
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                Accion = accion,
                Entidad = entidad,
                EntidadId = parsedEntidadId == Guid.Empty ? null : parsedEntidadId,
                DatosAnteriores = anterioresJson,
                DatosNuevos = nuevosJson,
                DireccionIp = ipAddress,
                FechaHora = DateTime.UtcNow,
                HashAnterior = "INITIAL",
                HashActual = Guid.NewGuid().ToString("N")
            };
            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync(ct);
        }
    }
}
