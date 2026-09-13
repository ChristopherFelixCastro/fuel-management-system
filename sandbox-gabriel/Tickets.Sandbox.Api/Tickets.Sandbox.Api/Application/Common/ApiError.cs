namespace Tickets.Sandbox.Api.Application.Common;

public class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string? Field { get; set; }
    public string Detail { get; set; } = string.Empty;

    public ApiError() { }

    public ApiError(string code, string detail, string? field = null)
    {
        Code = code;
        Detail = detail;
        Field = field;
    }
}
