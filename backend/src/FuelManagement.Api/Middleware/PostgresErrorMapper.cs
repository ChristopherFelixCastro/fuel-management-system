using Npgsql;

namespace FuelManagement.Api.Middleware;

/// <summary>
/// Mapea excepciones de PostgreSQL (RAISE EXCEPTION) a codigos HTTP semanticos.
/// Los mensajes de error de las funciones PL/pgSQL se mapean a 400/403/404/409.
/// </summary>
public static class PostgresErrorMapper
{
    private static readonly Dictionary<string, (int Status, string Message)> _pgMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["STOCK_FISICO_FINAL_INVALIDO"]         = (400, "El stock fisico final debe ser mayor o igual a cero."),
            ["FECHA_CIERRE_FUTURA_NO_PERMITIDA"]    = (400, "No se puede registrar un cierre con fecha futura."),
            ["MOTIVO_DIFERENCIA_REQUERIDO"]         = (422, "Se requiere motivo de diferencia cuando el stock fisico difiere del teorico."),
            ["MOTIVO_RECHAZO_REQUERIDO"]            = (422, "Se requiere motivo de rechazo."),
            ["USUARIO_INEXISTENTE"]                 = (404, "Usuario no encontrado."),
            ["USUARIO_INACTIVO"]                    = (403, "El usuario esta inactivo."),
            ["USUARIO_NO_ES_DESPACHADOR"]           = (403, "Solo los despachadores pueden crear cierres diarios."),
            ["SUPERVISOR_INEXISTENTE"]              = (404, "Supervisor no encontrado."),
            ["SUPERVISOR_INACTIVO"]                 = (403, "El supervisor esta inactivo."),
            ["USUARIO_NO_ES_SUPERVISOR"]            = (403, "Solo los supervisores pueden aprobar o rechazar cierres."),
            ["TANQUE_INEXISTENTE_O_INACTIVO"]       = (404, "Tanque no encontrado o inactivo."),
            ["DESPACHADOR_ESTACION_INVALIDA"]       = (403, "El despachador no pertenece a la estacion del tanque."),
            ["CIERRE_INEXISTENTE"]                  = (404, "Cierre diario no encontrado."),
            ["CIERRE_YA_REVISADO"]                  = (409, "El cierre ya fue aprobado o rechazado."),
        };

    public static (int StatusCode, string Message) Map(Exception ex)
    {
        if (ex is PostgresException pgEx)
        {
            if (_pgMap.TryGetValue(pgEx.MessageText, out var mapped))
                return mapped;

            return (400, $"Error de base de datos: {pgEx.MessageText}");
        }

        if (ex is KeyNotFoundException)
            return (404, ex.Message);

        if (ex is UnauthorizedAccessException)
            return (403, ex.Message);

        if (ex is ArgumentException or InvalidOperationException)
            return (400, ex.Message);

        return (500, "Error interno del servidor. Por favor intente de nuevo.");
    }
}
