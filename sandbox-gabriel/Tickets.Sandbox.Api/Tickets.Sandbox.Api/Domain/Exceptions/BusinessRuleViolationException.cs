namespace Tickets.Sandbox.Api.Domain.Exceptions;

public class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string code, string message, string? field = null) 
        : base(code, message, field)
    {
    }
}
