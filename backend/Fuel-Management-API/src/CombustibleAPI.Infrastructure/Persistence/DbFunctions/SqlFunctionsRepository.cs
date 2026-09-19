using Npgsql;
using NpgsqlTypes;
using CombustibleAPI.Application.Exceptions;

namespace CombustibleAPI.Infrastructure.Persistence.DbFunctions;

/// <summary>
/// Conector hacia las funciones y procedimientos almacenados nativos de PostgreSQL
/// definidos por Christopher en los scripts SQL (01_inventory_functions.sql,
/// 04_dispatch_functions.sql, 07_closure_operations.sql, 08_audit_functions.sql, etc.).
/// </summary>
public class SqlFunctionsRepository
{
    public async Task<string> GenerarNumeroTicketAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand("SELECT fn_generar_numero_ticket()", connection, transaction);
        var result = await cmd.ExecuteScalarAsync(ct);
        if (result is not string numero || string.IsNullOrWhiteSpace(numero))
            throw ApiException.BusinessRule("TICKET_NUMBER_FAILED", "No se pudo generar el número consecutivo del ticket.");
        return numero;
    }
    /// <summary>
    /// Invoca fn_registrar_despacho(p_ticket_id, p_despachador_usuario_id, p_tanque_id,
    /// p_cantidad_despachada, p_odometro_registrado, p_observaciones, p_direccion_ip) -> UUID
    /// </summary>
    public async Task<Guid> RegistrarDespachoAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction,
        Guid ticketId, Guid despachadorUsuarioId, Guid tanqueId,
        decimal cantidadDespachada, decimal odometroRegistrado,
        string? observaciones, string? direccionIp, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT fn_registrar_despacho(@p_ticket_id, @p_despachador_usuario_id, @p_tanque_id, @p_cantidad_despachada, @p_odometro_registrado, @p_observaciones, @p_direccion_ip)",
            connection, transaction);

        cmd.Parameters.Add(new NpgsqlParameter("p_ticket_id", NpgsqlDbType.Uuid) { Value = ticketId });
        cmd.Parameters.Add(new NpgsqlParameter("p_despachador_usuario_id", NpgsqlDbType.Uuid) { Value = despachadorUsuarioId });
        cmd.Parameters.Add(new NpgsqlParameter("p_tanque_id", NpgsqlDbType.Uuid) { Value = tanqueId });
        cmd.Parameters.Add(new NpgsqlParameter("p_cantidad_despachada", NpgsqlDbType.Numeric) { Value = cantidadDespachada });
        cmd.Parameters.Add(new NpgsqlParameter("p_odometro_registrado", NpgsqlDbType.Numeric) { Value = odometroRegistrado });
        cmd.Parameters.Add(new NpgsqlParameter("p_observaciones", NpgsqlDbType.Varchar) { Value = (object?)observaciones ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_direccion_ip", NpgsqlDbType.Varchar) { Value = (object?)direccionIp ?? DBNull.Value });

        try
        {
            var result = await cmd.ExecuteScalarAsync(ct);
            if (result is null || result is DBNull || !Guid.TryParse(result.ToString(), out var despachoId))
            {
                throw ApiException.BusinessRule("DISPATCH_FAILED", "La función de despacho no devolvió un identificador válido.");
            }
            return despachoId;
        }
        catch (PostgresException pgEx)
        {
            throw TraducirPostgresException(pgEx);
        }
    }

    /// <summary>
    /// Invoca fn_obtener_stock_disponible(p_estacion_id, p_tipo_combustible_id)
    /// Retorna (stock_fisico, stock_reservado, stock_disponible).
    /// </summary>
    public async Task<(decimal stockFisico, decimal stockReservado, decimal stockDisponible)> ObtenerStockDisponibleAsync(
        NpgsqlConnection connection, NpgsqlTransaction? transaction,
        Guid estacionId, short tipoCombustibleId, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT stock_fisico, stock_reservado, stock_disponible FROM fn_obtener_stock_disponible(@p_estacion_id, @p_tipo_combustible_id)",
            connection, transaction);

        cmd.Parameters.Add(new NpgsqlParameter("p_estacion_id", NpgsqlDbType.Uuid) { Value = estacionId });
        cmd.Parameters.Add(new NpgsqlParameter("p_tipo_combustible_id", NpgsqlDbType.Smallint) { Value = tipoCombustibleId });

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            var fisico = reader.IsDBNull(0) ? 0m : reader.GetDecimal(0);
            var reservado = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1);
            var disponible = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2);
            return (fisico, reservado, disponible);
        }

        return (0m, 0m, 0m);
    }

    /// <summary>
    /// Invoca fn_registrar_auditoria(p_usuario_id, p_accion, p_entidad, p_entidad_id, p_datos_anteriores, p_datos_nuevos, p_direccion_ip, p_user_agent) -> UUID
    /// </summary>
    public async Task<Guid?> RegistrarAuditoriaAsync(
        NpgsqlConnection connection, NpgsqlTransaction? transaction,
        Guid? usuarioId, string accion, string entidad, Guid? entidadId,
        string? datosAnteriores, string? datosNuevos, string? direccionIp, string? userAgent,
        CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT fn_registrar_auditoria(@p_usuario_id, @p_accion, @p_entidad, @p_entidad_id, @p_datos_anteriores::jsonb, @p_datos_nuevos::jsonb, @p_direccion_ip, @p_user_agent)",
            connection, transaction);

        cmd.Parameters.Add(new NpgsqlParameter("p_usuario_id", NpgsqlDbType.Uuid) { Value = (object?)usuarioId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_accion", NpgsqlDbType.Varchar) { Value = accion });
        cmd.Parameters.Add(new NpgsqlParameter("p_entidad", NpgsqlDbType.Varchar) { Value = entidad });
        cmd.Parameters.Add(new NpgsqlParameter("p_entidad_id", NpgsqlDbType.Uuid) { Value = (object?)entidadId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_datos_anteriores", NpgsqlDbType.Text) { Value = (object?)datosAnteriores ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_datos_nuevos", NpgsqlDbType.Text) { Value = (object?)datosNuevos ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_direccion_ip", NpgsqlDbType.Varchar) { Value = (object?)direccionIp ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_user_agent", NpgsqlDbType.Varchar) { Value = (object?)userAgent ?? DBNull.Value });

        try
        {
            var res = await cmd.ExecuteScalarAsync(ct);
            if (res != null && Guid.TryParse(res.ToString(), out var auditId))
                return auditId;
            return null;
        }
        catch
        {
            // La auditoría no debe quebrar transacciones si la función de hashing está en modo de prueba/desarrollo
            return null;
        }
    }

    /// <summary>
    /// Invoca fn_crear_cierre_diario(p_tanque_id, p_usuario_id, p_fecha, p_stock_fisico_final, p_motivo_diferencia, p_observaciones) -> UUID
    /// </summary>
    public async Task<Guid> CrearCierreDiarioAsync(
        NpgsqlConnection connection, NpgsqlTransaction? transaction,
        Guid tanqueId, Guid usuarioId, DateOnly fecha, decimal stockFisicoFinal,
        string? motivoDiferencia, string? observaciones, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT fn_crear_cierre_diario(@p_tanque_id, @p_usuario_id, @p_fecha, @p_stock_fisico_final, @p_motivo_diferencia, @p_observaciones)",
            connection, transaction);

        cmd.Parameters.Add(new NpgsqlParameter("p_tanque_id", NpgsqlDbType.Uuid) { Value = tanqueId });
        cmd.Parameters.Add(new NpgsqlParameter("p_usuario_id", NpgsqlDbType.Uuid) { Value = usuarioId });
        cmd.Parameters.Add(new NpgsqlParameter("p_fecha", NpgsqlDbType.Date) { Value = fecha });
        cmd.Parameters.Add(new NpgsqlParameter("p_stock_fisico_final", NpgsqlDbType.Numeric) { Value = stockFisicoFinal });
        cmd.Parameters.Add(new NpgsqlParameter("p_motivo_diferencia", NpgsqlDbType.Varchar) { Value = (object?)motivoDiferencia ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_observaciones", NpgsqlDbType.Varchar) { Value = (object?)observaciones ?? DBNull.Value });

        try
        {
            var result = await cmd.ExecuteScalarAsync(ct);
            if (result is null || !Guid.TryParse(result.ToString(), out var cierreId))
                throw ApiException.BusinessRule("CLOSURE_FAILED", "No se pudo registrar el cierre diario.");
            return cierreId;
        }
        catch (PostgresException pgEx)
        {
            throw TraducirPostgresException(pgEx);
        }
    }

    /// <summary>
    /// Invoca fn_aprobar_cierre(p_cierre_id, p_supervisor_id)
    /// </summary>
    public async Task AprobarCierreAsync(
        NpgsqlConnection connection, NpgsqlTransaction? transaction,
        Guid cierreId, Guid supervisorId, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT fn_aprobar_cierre(@p_cierre_id, @p_supervisor_id)", connection, transaction);

        cmd.Parameters.Add(new NpgsqlParameter("p_cierre_id", NpgsqlDbType.Uuid) { Value = cierreId });
        cmd.Parameters.Add(new NpgsqlParameter("p_supervisor_id", NpgsqlDbType.Uuid) { Value = supervisorId });

        try
        {
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (PostgresException pgEx)
        {
            throw TraducirPostgresException(pgEx);
        }
    }

    /// <summary>
    /// Invoca fn_rechazar_cierre(p_cierre_id, p_supervisor_id, p_motivo)
    /// </summary>
    public async Task RechazarCierreAsync(
        NpgsqlConnection connection, NpgsqlTransaction? transaction,
        Guid cierreId, Guid supervisorId, string motivo, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT fn_rechazar_cierre(@p_cierre_id, @p_supervisor_id, @p_motivo)", connection, transaction);

        cmd.Parameters.Add(new NpgsqlParameter("p_cierre_id", NpgsqlDbType.Uuid) { Value = cierreId });
        cmd.Parameters.Add(new NpgsqlParameter("p_supervisor_id", NpgsqlDbType.Uuid) { Value = supervisorId });
        cmd.Parameters.Add(new NpgsqlParameter("p_motivo", NpgsqlDbType.Varchar) { Value = motivo });

        try
        {
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (PostgresException pgEx)
        {
            throw TraducirPostgresException(pgEx);
        }
    }

    private static Exception TraducirPostgresException(PostgresException pgEx)
    {
        var msg = pgEx.MessageText;

        if (msg.Contains("TICKET_CONSUMIDO", StringComparison.OrdinalIgnoreCase))
            return ApiException.TicketConsumido();
        if (msg.Contains("TICKET_VENCIDO", StringComparison.OrdinalIgnoreCase))
            return ApiException.TicketVencido();
        if (msg.Contains("TICKET_ANULADO", StringComparison.OrdinalIgnoreCase))
            return ApiException.TicketAnulado();
        if (msg.Contains("TICKET_INEXISTENTE", StringComparison.OrdinalIgnoreCase))
            return ApiException.TicketInexistente();
        if (msg.Contains("STOCK_FISICO_INSUFICIENTE", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("STOCK", StringComparison.OrdinalIgnoreCase))
            return ApiException.InventarioInsuficiente();
        if (msg.Contains("ODOMETRO_MENOR_AL_REGISTRADO", StringComparison.OrdinalIgnoreCase))
            return ApiException.BusinessRule("ODOMETRO_INVALIDO", "El odómetro registrado no puede ser menor al actual.");
        if (msg.Contains("DESPACHADOR_ESTACION_INVALIDA", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("TANQUE_ESTACION_INVALIDA", StringComparison.OrdinalIgnoreCase))
            return ApiException.NoAutorizadoParaEstacion();
        if (msg.Contains("COMBUSTIBLE_TANQUE_INVALIDO", StringComparison.OrdinalIgnoreCase))
            return ApiException.BusinessRule("COMBUSTIBLE_INCOMPATIBLE", "El combustible del tanque no coincide con el ticket.");
        if (msg.Contains("MOTIVO_DIFERENCIA_REQUERIDO", StringComparison.OrdinalIgnoreCase))
            return ApiException.ValidationError("Se requiere motivo para la diferencia en el cierre.");
        if (msg.Contains("MOTIVO_RECHAZO_REQUERIDO", StringComparison.OrdinalIgnoreCase))
            return ApiException.ValidationError("Se requiere un motivo para rechazar el cierre.");
        if (msg.Contains("CIERRE_YA_REVISADO", StringComparison.OrdinalIgnoreCase))
            return ApiException.Conflict("El cierre ya fue revisado.");

        if (pgEx.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            if (string.Equals(
                pgEx.ConstraintName,
                "uq_cierre_tanque_fecha_activo",
                StringComparison.OrdinalIgnoreCase))
            {
                return ApiException.Conflict(
                    "CLOSURE_ALREADY_EXISTS",
                    "Ya existe un cierre activo para este tanque y fecha.");
            }

            if (
                pgEx.ConstraintName?.Contains(
                    "despacho",
                    StringComparison.OrdinalIgnoreCase) == true ||
                pgEx.ConstraintName?.Contains(
                    "ticket",
                    StringComparison.OrdinalIgnoreCase) == true)
            {
                return ApiException.DespachoDuplicado();
            }

            return ApiException.Conflict(
                "DUPLICATE_RESOURCE",
                "Ya existe un registro con los mismos datos únicos.");
        }

        return ApiException.BusinessRule("OPERACION_FALLIDA", msg);
    }
}
