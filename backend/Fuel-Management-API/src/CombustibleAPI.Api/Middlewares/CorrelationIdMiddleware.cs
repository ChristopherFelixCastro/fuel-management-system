namespace CombustibleAPI.Api.Middlewares;

/// <summary>
/// Asigna/propaga un traceId por request (header X-Correlation-Id), usado tanto en
/// respuestas exitosas (meta.traceId) como en errores (error.traceId) -- SDP General sec. 8.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "TraceId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = context.Request.Headers.TryGetValue(HeaderName, out var incoming) && !string.IsNullOrWhiteSpace(incoming)
            ? incoming.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[ItemKey] = traceId;
        context.Response.Headers[HeaderName] = traceId;

        await _next(context);
    }
}

public static class HttpContextTraceIdExtensions
{
    public static string GetTraceId(this HttpContext context) =>
        context.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var value) && value is string s
            ? s
            : context.TraceIdentifier;
}
