using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tickets.Sandbox.Api.Application.Common;
using Tickets.Sandbox.Api.Application.Contracts;
using Tickets.Sandbox.Api.Domain.Entities;
using Tickets.Sandbox.Api.Domain.Enums;
using Tickets.Sandbox.Api.Infrastructure.ExternalServices.Stubs;
using Tickets.Sandbox.Api.Infrastructure.Services;
using Xunit;

namespace Tickets.Sandbox.Tests.Application;

public class NotificationTests
{
    private readonly MasterDataStubService _masterData;
    private readonly Mock<IEmailSender> _emailMock;
    private readonly Mock<ISmsSender> _smsMock;
    private readonly Mock<IAuditService> _auditMock;

    public NotificationTests()
    {
        _masterData = new MasterDataStubService();
        _emailMock = new Mock<IEmailSender>();
        _smsMock = new Mock<ISmsSender>();
        _auditMock = new Mock<IAuditService>();
    }

    [Fact]
    public async Task NotifyTicketIssued_WhenChannelsSucceed_ShouldSendEmailAndSms()
    {
        var service = new NotificationService(
            _emailMock.Object,
            _smsMock.Object,
            _masterData,
            _auditMock.Object,
            NullLogger<NotificationService>.Instance);

        var request = new Request
        {
            Id = Guid.NewGuid(),
            EmployeeId = MasterDataStubService.DefaultEmployeeId
        };

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Number = "COM-2026-000001",
            AuthorizedQuantity = 50.0m,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            Status = TicketStatus.ACTIVO
        };

        await service.NotifyTicketIssuedAsync(ticket, request, new byte[] { 1, 2, 3 });

        _emailMock.Verify(e => e.SendEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _smsMock.Verify(s => s.SendSmsAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyTicketIssued_WhenEmailFails_ShouldLogIntegrationFailureAndNotThrow()
    {
        _emailMock.Setup(e => e.SendEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP Timeout"));

        var service = new NotificationService(
            _emailMock.Object,
            _smsMock.Object,
            _masterData,
            _auditMock.Object,
            NullLogger<NotificationService>.Instance);

        var request = new Request
        {
            Id = Guid.NewGuid(),
            EmployeeId = MasterDataStubService.DefaultEmployeeId
        };

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Number = "COM-2026-000001",
            AuthorizedQuantity = 50.0m,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        // No debe lanzar excepción
        await service.NotifyTicketIssuedAsync(ticket, request, new byte[] { 1, 2, 3 });

        // Debe registrar auditoría con código INTEGRATION_FAILURE
        _auditMock.Verify(a => a.LogEventAsync(
            ErrorCodes.IntegrationFailure,
            nameof(Ticket),
            ticket.Id.ToString(),
            It.Is<string>(d => d.Contains("Fallo de canal")),
            "SYSTEM",
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
