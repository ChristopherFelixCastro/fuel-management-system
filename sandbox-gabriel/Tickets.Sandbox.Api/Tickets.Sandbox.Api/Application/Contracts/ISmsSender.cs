namespace Tickets.Sandbox.Api.Application.Contracts;

public interface ISmsSender
{
    Task SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}
