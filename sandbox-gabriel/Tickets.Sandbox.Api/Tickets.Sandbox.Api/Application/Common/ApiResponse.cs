namespace Tickets.Sandbox.Api.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<ApiError> Errors { get; set; } = new();

    public static ApiResponse<T> Ok(T data, string message = "Operación realizada con éxito")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Errors = new List<ApiError>()
        };
    }

    public static ApiResponse<T> Fail(string message, List<ApiError>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Errors = errors ?? new List<ApiError>()
        };
    }

    public static ApiResponse<T> Fail(string message, string code, string detail, string? field = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Errors = new List<ApiError> { new ApiError(code, detail, field) }
        };
    }
}

public class ApiResponse : ApiResponse<object?>
{
    public static ApiResponse Ok(string message = "Operación realizada con éxito")
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            Data = null,
            Errors = new List<ApiError>()
        };
    }

    public static new ApiResponse Fail(string message, List<ApiError>? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Data = null,
            Errors = errors ?? new List<ApiError>()
        };
    }

    public static new ApiResponse Fail(string message, string code, string detail, string? field = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Data = null,
            Errors = new List<ApiError> { new ApiError(code, detail, field) }
        };
    }
}
