using FuelManagement.Alerts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FuelManagement.Alerts.Jobs;

public class AlertScannerJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AlertScannerJob> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public AlertScannerJob(IServiceProvider serviceProvider, ILogger<AlertScannerJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AlertScannerJob iniciado. Frecuencia de escaneo: {Interval} min", _checkInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();
                await alertService.ScanAndCreateAlertsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la ejecución periódica del scanner de alertas");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}
