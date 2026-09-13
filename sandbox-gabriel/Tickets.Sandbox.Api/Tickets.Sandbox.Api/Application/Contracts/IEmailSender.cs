namespace Tickets.Sandbox.Api.Application.Contracts;

public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string htmlBody, byte[]? attachment = null, string? attachmentName = null, CancellationToken cancellationToken = default);
}
