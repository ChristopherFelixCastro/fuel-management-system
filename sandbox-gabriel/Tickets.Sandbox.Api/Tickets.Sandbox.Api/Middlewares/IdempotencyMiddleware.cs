using System.Net;
using System.Text;
using System.Text.Json;
using Tickets.Sandbox.Api.Application.Common;
using Tickets.Sandbox.Api.Application.Contracts;

namespace Tickets.Sandbox.Api.Middlewares;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IIdempotencyService _idempotencyService;

    public IdempotencyMiddleware(RequestDelegate next, IIdempotencyService idempotencyService)
    {
        _next = next;
        _idempotencyService = idempotencyService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var rawKey) || string.IsNullOrWhiteSpace(rawKey))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = rawKey.ToString();

        // Permitir múltiples lecturas del cuerpo de la petición
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var requestBody = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        var requestHash = _idempotencyService.ComputePayloadHash(context.Request.Method, context.Request.Path, requestBody);

        if (_idempotencyService.TryGet(idempotencyKey, out var cached) && cached != null)
        {
            if (cached.RequestHash != requestHash)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Conflict; // 409
                context.Response.ContentType = "application/json";

                var errorResponse = ApiResponse.Fail(
                    "La llave de idempotencia fue reutilizada con una carga o parámetros distintos.",
                    ErrorCodes.IdempotencyKeyReused,
                    $"La clave de idempotencia '{idempotencyKey}' ya fue procesada con un contenido diferente.",
                    "Idempotency-Key"
                );

                var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                await context.Response.WriteAsync(json);
                return;
            }

            // Misma clave y misma carga: devolver respuesta cacheada sin re-ejecutar efectos
            context.Response.StatusCode = cached.StatusCode;
            context.Response.ContentType = cached.ContentType;
            await context.Response.WriteAsync(cached.Body);
            return;
        }

        // Interceptar respuesta para cachear
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);

            responseBodyStream.Position = 0;
            var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
            responseBodyStream.Position = 0;

            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                _idempotencyService.Save(
                    idempotencyKey,
                    requestHash,
                    context.Response.StatusCode,
                    context.Response.ContentType ?? "application/json",
                    responseBody);
            }

            await responseBodyStream.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
}
