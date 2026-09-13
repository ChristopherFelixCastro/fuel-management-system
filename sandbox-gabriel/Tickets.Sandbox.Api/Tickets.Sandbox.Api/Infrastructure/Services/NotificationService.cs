using Microsoft.Extensions.Logging;
using Tickets.Sandbox.Api.Application.Common;
using Tickets.Sandbox.Api.Application.Contracts;
using Tickets.Sandbox.Api.Domain.Entities;

namespace Tickets.Sandbox.Api.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    private readonly IMasterDataService _masterDataService;
    private readonly IAuditService _auditService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailSender emailSender,
        ISmsSender smsSender,
        IMasterDataService masterDataService,
        IAuditService auditService,
        ILogger<NotificationService> logger)
    {
        _emailSender = emailSender;
        _smsSender = smsSender;
        _masterDataService = masterDataService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task NotifyTicketIssuedAsync(Ticket ticket, Request request, byte[]? pdfBytes = null, CancellationToken cancellationToken = default)
    {
        var employee = await _masterDataService.GetEmployeeByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null)
        {
            _logger.LogWarning("No se pudo obtener información del empleado {EmployeeId} para notificar la emisión del ticket {TicketNumber}",
                request.EmployeeId, ticket.Number);
            return;
        }

        // 1. Notificación por Correo Electrónico
        if (!string.IsNullOrWhiteSpace(employee.Email))
        {
            try
            {
                var subject = $"Nuevo Ticket de Combustible Emitido: {ticket.Number}";
                var body = $@"
                    <h2>¡Tu ticket de combustible ha sido emitido!</h2>
                    <p>Hola <strong>{employee.FullName}</strong>,</p>
                    <p>Se ha aprobado tu solicitud y se ha generado el siguiente ticket:</p>
                    <ul>
                        <li><strong>Número de Ticket:</strong> {ticket.Number}</li>
                        <li><strong>Cantidad Autorizada:</strong> {ticket.AuthorizedQuantity:N2} galones</li>
                        <li><strong>Fecha de Vencimiento:</strong> {ticket.ExpiresAt:dd/MM/yyyy HH:mm}</li>
                    </ul>
                    <p>Adjunto encontrarás tu ticket en PDF con el código QR para presentar en la estación de combustible.</p>
                ";

                await _emailSender.SendEmailAsync(
                    employee.Email,
                    subject,
                    body,
                    pdfBytes,
                    $"Ticket_{ticket.Number}.pdf",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallo al enviar notificación por correo para el ticket {TicketNumber}. Código: {Code}",
                    ticket.Number, ErrorCodes.IntegrationFailure);

                await _auditService.LogEventAsync(
                    ErrorCodes.IntegrationFailure,
                    nameof(Ticket),
                    ticket.Id.ToString(),
                    $"Fallo de canal de correo electrónico: {ex.Message}",
                    "SYSTEM",
                    cancellationToken);
            }
        }

        // 2. Notificación por SMS
        if (!string.IsNullOrWhiteSpace(employee.PhoneNumber))
        {
            try
            {
                var smsText = $"FMS: Ticket {ticket.Number} emitido por {ticket.AuthorizedQuantity:N2} galones. Vence: {ticket.ExpiresAt:dd/MM HH:mm}.";
                await _smsSender.SendSmsAsync(employee.PhoneNumber, smsText, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallo al enviar notificación SMS para el ticket {TicketNumber}. Código: {Code}",
                    ticket.Number, ErrorCodes.IntegrationFailure);

                await _auditService.LogEventAsync(
                    ErrorCodes.IntegrationFailure,
                    nameof(Ticket),
                    ticket.Id.ToString(),
                    $"Fallo de canal SMS: {ex.Message}",
                    "SYSTEM",
                    cancellationToken);
            }
        }
    }
}
