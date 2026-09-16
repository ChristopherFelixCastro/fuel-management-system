using System.Net;

namespace CombustibleAPI.Application.Exceptions;

/// <summary>
/// Excepción de negocio controlada. El código (p. ej. TICKET_EXPIRED, INSUFFICIENT_STOCK)
/// y el status HTTP los define quien lanza la excepción; el middleware global solo la traduce
/// al formato estándar { "error": {...}, "traceId": "..." } definido en el SDP General sec. 8.
/// </summary>
public class ApiException : Exception
{
    public string Code { get; }
    public HttpStatusCode StatusCode { get; }
    public IEnumerable<string> Details { get; }

    public ApiException(string code, string message, HttpStatusCode statusCode, IEnumerable<string>? details = null)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
        Details = details ?? Enumerable.Empty<string>();
    }

    // ---- Fábricas para los errores más comunes del flujo crítico ----

    public static ApiException ValidationError(string message, IEnumerable<string>? details = null) =>
        new("VALIDATION_ERROR", message, HttpStatusCode.BadRequest, details);

    public static ApiException Unauthorized(string message = "Credenciales inválidas o sesión no presente.") =>
        new("UNAUTHORIZED", message, HttpStatusCode.Unauthorized);

    public static ApiException Forbidden(string message = "No tiene permiso para esta operación.") =>
        new("FORBIDDEN", message, HttpStatusCode.Forbidden);

    public static ApiException NotFound(string entity) =>
        new("NOT_FOUND", $"{entity} no encontrado.", HttpStatusCode.NotFound);

    public static ApiException Conflict(string code, string message) =>
        new(code, message, HttpStatusCode.Conflict);

    public static ApiException Conflict(string message) =>
        new("CONFLICT", message, HttpStatusCode.Conflict);

    public static ApiException BusinessRule(string code, string message) =>
        new(code, message, HttpStatusCode.UnprocessableEntity);

    // ---- Errores específicos del flujo de ticket/despacho (RN-06/RN-07/RN-08) ----

    public static ApiException TicketInexistente() =>
        BusinessRule("TICKET_INEXISTENTE", "El ticket indicado no existe.");

    public static ApiException FirmaInvalida() =>
        BusinessRule("FIRMA_INVALIDA", "La firma del ticket/QR no es válida.");

    public static ApiException TicketVencido() =>
        BusinessRule("TICKET_EXPIRED", "El ticket ha vencido.");

    public static ApiException TicketConsumido() =>
        BusinessRule("TICKET_CONSUMED", "El ticket ya fue consumido.");

    public static ApiException TicketAnulado() =>
        BusinessRule("TICKET_ANULADO", "El ticket fue anulado.");

    public static ApiException NoAutorizadoParaEstacion() =>
        Forbidden("El despachador no está autorizado para esta estación/tanque.");

    public static ApiException InventarioInsuficiente() =>
        BusinessRule("INSUFFICIENT_STOCK", "No hay disponibilidad suficiente en el tanque.");

    public static ApiException OdometroInvalido() =>
        ValidationError("El odómetro no puede ser menor al último registrado para el vehículo.");

    public static ApiException DespachoDuplicado() =>
        Conflict("DISPATCH_ALREADY_DONE", "Este ticket ya fue despachado.");
}
