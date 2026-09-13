using Microsoft.Extensions.Logging;
using Tickets.Sandbox.Api.Application.Contracts;

namespace Tickets.Sandbox.Api.Infrastructure.Services;

public class SimulatedSmsSender : ISmsSender
{
    private readonly ILogger<SimulatedSmsSender> _logger;

    public SimulatedSmsSender(ILogger<SimulatedSmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[SMS SIMULADO] Enviando a {PhoneNumber}: \"{Message}\"", phoneNumber, message);
        return Task.CompletedTask;
    }
}
