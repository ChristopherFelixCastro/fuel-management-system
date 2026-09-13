namespace Tickets.Sandbox.Api.Domain.Exceptions;

public class DomainException : Exception
{
    public string Code { get; }
    public string? Field { get; }

    public DomainException(string code, string message, string? field = null) : base(message)
    {
        Code = code;
        Field = field;
    }
}
