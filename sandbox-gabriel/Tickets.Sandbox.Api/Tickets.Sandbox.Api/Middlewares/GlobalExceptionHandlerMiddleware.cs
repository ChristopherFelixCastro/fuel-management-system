using System.Net;
using System.Text.Json;
using FluentValidation;
using Tickets.Sandbox.Api.Application.Common;
using Tickets.Sandbox.Api.Domain.Exceptions;

namespace Tickets.Sandbox.Api.Middlewares;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;
        var response = context.Response;
        response.ContentType = "application/json";

        int statusCode;
        ApiResponse errorResponse;

        switch (exception)
        {
            case ValidationException valEx:
                statusCode = (int)HttpStatusCode.BadRequest; // 400
                var validationErrors = valEx.Errors.Select(e => new ApiError(
                    ErrorCodes.ValidationError,
                    e.ErrorMessage,
                    e.PropertyName
                )).ToList();
                errorResponse = ApiResponse.Fail("Error de validación en los datos enviados.", validationErrors);
                break;

            case DomainException domainEx:
                statusCode = MapDomainCodeToHttpStatus(domainEx.Code);
                errorResponse = ApiResponse.Fail(
                    domainEx.Message,
                    domainEx.Code,
                    domainEx.Message,
                    domainEx.Field
                );
                break;

            case UnauthorizedAccessException:
                statusCode = (int)HttpStatusCode.Unauthorized; // 401
                errorResponse = ApiResponse.Fail("No autenticado.", ErrorCodes.Unauthenticated, "Acceso no autorizado.");
                break;

            default:
                _logger.LogError(exception, "Error no controlado. CorrelationId: {CorrelationId}", correlationId);
                statusCode = (int)HttpStatusCode.InternalServerError; // 500
                errorResponse = ApiResponse.Fail(
                    "Ocurrió un error interno en el servidor.",
                    ErrorCodes.InternalError,
                    $"Error interno del servidor. CorrelationId: {correlationId}"
                );
                break;
        }

        response.StatusCode = statusCode;
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await response.WriteAsync(JsonSerializer.Serialize(errorResponse, jsonOptions));
    }

    private static int MapDomainCodeToHttpStatus(string code) => code switch
    {
        ErrorCodes.ValidationError => 400,
        ErrorCodes.Unauthenticated => 401,
        ErrorCodes.Forbidden => 403,
        ErrorCodes.TicketAccessDenied => 403,
        ErrorCodes.ResourceNotFound => 404,
        ErrorCodes.TicketNotFound => 404,
        ErrorCodes.Conflict => 409,
        ErrorCodes.ConcurrencyConflict => 409,
        ErrorCodes.TicketAlreadyExists => 409,
        ErrorCodes.IdempotencyKeyReused => 409,
        ErrorCodes.BusinessRuleViolation => 422,
        ErrorCodes.VehicleFuelMismatch => 422,
        ErrorCodes.QuantityExceedsTankCapacity => 422,
        ErrorCodes.DepartmentIncoherent => 422,
        ErrorCodes.RequestNotEditable => 422,
        ErrorCodes.RequestNotPending => 422,
        ErrorCodes.InsufficientAvailableInventory => 422,
        ErrorCodes.ExpirationInvalid => 422,
        ErrorCodes.TicketExpired => 422,
        ErrorCodes.TicketConsumed => 422,
        ErrorCodes.TicketCancelled => 422,
        ErrorCodes.QrSignatureInvalid => 422,
        ErrorCodes.PdfGenerationFailed => 422,
        _ => 422
    };
}
