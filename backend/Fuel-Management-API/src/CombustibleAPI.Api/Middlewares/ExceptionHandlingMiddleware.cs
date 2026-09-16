using System.Net;
using System.Text.Json;
using CombustibleAPI.Application.Dtos.Common;
using CombustibleAPI.Application.Exceptions;

namespace CombustibleAPI.Api.Middlewares;

/// <summary>
/// Middleware Global de Excepciones (SDP Iván sec. 3): traduce cualquier excepción a
/// { "error": { "code", "message", "details" }, "traceId" }. Nunca deja escapar stack
/// traces, secretos ni detalles internos de PostgreSQL hacia el cliente (SDP General sec. 8:
/// "Nunca se retornan secretos, hashes ni detalles internos").
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var traceId = context.GetTraceId();

        var (status, body) = ex switch
        {
            ApiException apiEx => ((int)apiEx.StatusCode, ApiErrorResponse.From(apiEx.Code, apiEx.Message, traceId, apiEx.Details)),

            UnauthorizedAccessException => ((int)HttpStatusCode.Forbidden,
                ApiErrorResponse.From("FORBIDDEN", "No tiene permiso para esta operación.", traceId)),

            OperationCanceledException => (499, ApiErrorResponse.From("REQUEST_CANCELLED", "La solicitud fue cancelada.", traceId)),

            _ => ((int)HttpStatusCode.InternalServerError,
                ApiErrorResponse.From("INTERNAL_ERROR", "Ocurrió un error inesperado. Contacte al administrador.", traceId))
        };

        if (status == (int)HttpStatusCode.InternalServerError)
        {
            // El detalle completo solo va a logs del servidor, nunca a la respuesta HTTP.
            _logger.LogError(ex, "Error no controlado. TraceId={TraceId}", traceId);
        }
        else
        {
            _logger.LogWarning("Error de negocio {Code}: {Message}. TraceId={TraceId}",
                (ex as ApiException)?.Code ?? "N/A", ex.Message, traceId);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = status;
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}
