namespace CombustibleAPI.Application.Dtos.Common;

/// <summary>
/// Formato de respuesta exitosa acordado en el SDP General (sección 8):
/// { "data": {...}, "meta": { "traceId": "..." } }
/// </summary>
public class ApiResponse<T>
{
    public T? Data { get; set; }
    public ApiMeta Meta { get; set; } = new();

    public static ApiResponse<T> Ok(T data, string traceId) => new()
    {
        Data = data,
        Meta = new ApiMeta { TraceId = traceId }
    };
}

public class ApiMeta
{
    public string TraceId { get; set; } = default!;
}

/// <summary>
/// Formato de error estándar acordado en el SDP General (sección 8):
/// { "error": { "code": "...", "message": "...", "details": [] }, "traceId": "..." }
/// </summary>
public class ApiErrorResponse
{
    public ApiErrorBody Error { get; set; } = default!;
    public string TraceId { get; set; } = default!;

    public static ApiErrorResponse From(string code, string message, string traceId, IEnumerable<string>? details = null) => new()
    {
        TraceId = traceId,
        Error = new ApiErrorBody
        {
            Code = code,
            Message = message,
            Details = details?.ToList() ?? new List<string>()
        }
    };
}

public class ApiErrorBody
{
    public string Code { get; set; } = default!;
    public string Message { get; set; } = default!;
    public List<string> Details { get; set; } = new();
}
