using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Tickets.Sandbox.Api.Application.Contracts;
using Tickets.Sandbox.Api.Infrastructure.Configuration;

namespace Tickets.Sandbox.Api.Infrastructure.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, byte[]? attachment = null, string? attachmentName = null, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = htmlBody
        };

        if (attachment is { Length: > 0 } && !string.IsNullOrWhiteSpace(attachmentName))
        {
            builder.Attachments.Add(attachmentName, attachment, ContentType.Parse("application/pdf"));
        }

        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_options.Host, _options.Port, _options.UseSsl, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.UserName) && !string.IsNullOrWhiteSpace(_options.Password))
            {
                await client.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Correo enviado exitosamente a {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error enviando correo a {To} vía SMTP ({Host}:{Port})", to, _options.Host, _options.Port);
            throw;
        }
    }
}
