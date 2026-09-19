namespace Inventario.Api.Dtos
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<ApiError> Errors { get; set; } = new();

        public static ApiResponse<T> Ok(T data, string message = "")
            => new() { Success = true, Message = message, Data = data, Errors = new() };

        public static ApiResponse<T> Fail(string message, List<ApiError> errors)
            => new() { Success = false, Message = message, Data = default, Errors = errors };
    }

    public class ApiError
    {
        public string Code { get; set; } = string.Empty;
        public string? Field { get; set; }
        public string Detail { get; set; } = string.Empty;
    }
}