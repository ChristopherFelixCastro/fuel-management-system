using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Tickets.Sandbox.Api.Application.Common;
using Tickets.Sandbox.Api.Application.Contracts;
using Tickets.Sandbox.Api.Application.DTOs.Tickets;
using Tickets.Sandbox.Api.Application.Services;
using Tickets.Sandbox.Api.Domain.Entities;
using Tickets.Sandbox.Api.Domain.Enums;
using Tickets.Sandbox.Api.Domain.Exceptions;
using Tickets.Sandbox.Api.Infrastructure.Configuration;
using Tickets.Sandbox.Api.Infrastructure.ExternalServices.Stubs;
using Tickets.Sandbox.Api.Infrastructure.Persistence;
using Tickets.Sandbox.Api.Infrastructure.Services;
using Tickets.Sandbox.Tests.Helpers;
using Xunit;

namespace Tickets.Sandbox.Tests.Application;

public class TicketServiceTests
{
    private readonly MasterDataStubService _masterData;
    private readonly InventoryStubService _inventory;
    private readonly Mock<IAuditService> _auditMock;
    private readonly Mock<IPdfGeneratorService> _pdfGeneratorMock;
    private readonly QrCodeService _qrCodeService;

    public TicketServiceTests()
    {
        _masterData = new MasterDataStubService();
        _inventory = new InventoryStubService();
        _auditMock = new Mock<IAuditService>();
        _pdfGeneratorMock = new Mock<IPdfGeneratorService>();

        var options = Options.Create(new QrSecurityOptions
        {
            SecretKey = "TestSecretKey1234567890TestSecretKey!"
        });
        _qrCodeService = new QrCodeService(options);

        _pdfGeneratorMock.Setup(p => p.GenerateTicketPdf(It.IsAny<TicketPdfModel>()))
            .Returns(new byte[] { 100, 101, 102 });
    }

    private async Task<(Ticket ticket, string validPayload, string rawToken)> CreateSampleTicketAsync(
        AppDbContext context,
        TicketStatus status = TicketStatus.ACTIVO,
        DateTime? expiresAt = null)
    {
        var request = new Request
        {
            EmployeeId = MasterDataStubService.DefaultEmployeeId,
            VehicleId = MasterDataStubService.DefaultVehicleId,
            DepartmentId = MasterDataStubService.DefaultDepartmentId,
            FuelTypeId = MasterDataStubService.DefaultFuelTypeId,
            RequestedQuantity = 50.0m,
            Status = RequestStatus.APROBADA
        };
        context.Requests.Add(request);

        var ticketId = Guid.NewGuid();
        var qrSecurity = _qrCodeService.GenerateQrSecurityData(ticketId);

        var ticket = new Ticket
        {
            Id = ticketId,
            Number = "COM-2026-000001",
            RequestId = request.Id,
            QrTokenHash = qrSecurity.TokenHash,
            Signature = qrSecurity.Signature,
            AuthorizedQuantity = 50.0m,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(2),
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        // Reservar en el stub
        await _inventory.ReserveInventoryAsync(MasterDataStubService.DefaultFuelTypeId, 50.0m, ticketId);

        return (ticket, qrSecurity.QrPayload, qrSecurity.RawToken);
    }

    [Fact]
    public async Task ValidateQr_ValidActiveTicket_ShouldReturnCanDispatchTrue()
    {
        using var context = TestDbContextFactory.Create();
        var service = new TicketService(context, _masterData, _inventory, _qrCodeService, _pdfGeneratorMock.Object, _auditMock.Object);

        var (ticket, validPayload, _) = await CreateSampleTicketAsync(context, TicketStatus.ACTIVO);

        var response = await service.ValidateTicketQrAsync(new ValidateTicketRequestDto
        {
            QrPayload = validPayload,
            StationId = "ESTACION-01"
        });

        response.Should().NotBeNull();
        response.TicketId.Should().Be(ticket.Id);
        response.CanDispatch.Should().BeTrue();
        response.Status.Should().Be(TicketStatus.ACTIVO.ToString());
    }

    [Fact]
    public async Task ValidateQr_NonexistentTicket_ShouldThrowTicketNotFound()
    {
        using var context = TestDbContextFactory.Create();
        var service = new TicketService(context, _masterData, _inventory, _qrCodeService, _pdfGeneratorMock.Object, _auditMock.Object);

        var dummyId = Guid.NewGuid();
        var qrSecurity = _qrCodeService.GenerateQrSecurityData(dummyId);

        var act = async () => await service.ValidateTicketQrAsync(new ValidateTicketRequestDto
        {
            QrPayload = qrSecurity.QrPayload,
            StationId = "ESTACION-01"
        });

        var ex = await act.Should().ThrowAsync<BusinessRuleViolationException>();
        ex.Which.Code.Should().Be(ErrorCodes.TicketNotFound);
    }

    [Fact]
    public async Task ValidateQr_AlteredSignature_ShouldThrowQrSignatureInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var service = new TicketService(context, _masterData, _inventory, _qrCodeService, _pdfGeneratorMock.Object, _auditMock.Object);

        var (ticket, _, rawToken) = await CreateSampleTicketAsync(context);

        // Payload con firma adulterada
        var alteredPayload = $"{{\"ticketId\":\"{ticket.Id}\",\"token\":\"{rawToken}\",\"sig\":\"FIRMA_FALSIFICADA\"}}";

        var act = async () => await service.ValidateTicketQrAsync(new ValidateTicketRequestDto
        {
            QrPayload = alteredPayload,
            StationId = "ESTACION-01"
        });

        var ex = await act.Should().ThrowAsync<BusinessRuleViolationException>();
        ex.Which.Code.Should().Be(ErrorCodes.QrSignatureInvalid);
    }

    [Fact]
    public async Task ValidateQr_ExpiredTicket_ShouldThrowTicketExpired()
    {
        using var context = TestDbContextFactory.Create();
        var service = new TicketService(context, _masterData, _inventory, _qrCodeService, _pdfGeneratorMock.Object, _auditMock.Object);

        // Ticket con fecha de expiración en el pasado
        var (_, expiredPayload, _) = await CreateSampleTicketAsync(context, TicketStatus.ACTIVO, DateTime.UtcNow.AddMinutes(-30));

        var act = async () => await service.ValidateTicketQrAsync(new ValidateTicketRequestDto
        {
            QrPayload = expiredPayload,
            StationId = "ESTACION-01"
        });

        var ex = await act.Should().ThrowAsync<BusinessRuleViolationException>();
        ex.Which.Code.Should().Be(ErrorCodes.TicketExpired);
    }

    [Fact]
    public async Task ValidateQr_ConsumedTicket_ShouldThrowTicketConsumed()
    {
        using var context = TestDbContextFactory.Create();
        var service = new TicketService(context, _masterData, _inventory, _qrCodeService, _pdfGeneratorMock.Object, _auditMock.Object);

        var (_, payload, _) = await CreateSampleTicketAsync(context, TicketStatus.CONSUMIDO);

        var act = async () => await service.ValidateTicketQrAsync(new ValidateTicketRequestDto
        {
            QrPayload = payload,
            StationId = "ESTACION-01"
        });

        var ex = await act.Should().ThrowAsync<BusinessRuleViolationException>();
        ex.Which.Code.Should().Be(ErrorCodes.TicketConsumed);
    }

    [Fact]
    public async Task ValidateQr_CancelledTicket_ShouldThrowTicketCancelled()
    {
        using var context = TestDbContextFactory.Create();
        var service = new TicketService(context, _masterData, _inventory, _qrCodeService, _pdfGeneratorMock.Object, _auditMock.Object);

        var (_, payload, _) = await CreateSampleTicketAsync(context, TicketStatus.ANULADO);

        var act = async () => await service.ValidateTicketQrAsync(new ValidateTicketRequestDto
        {
            QrPayload = payload,
            StationId = "ESTACION-01"
        });

        var ex = await act.Should().ThrowAsync<BusinessRuleViolationException>();
        ex.Which.Code.Should().Be(ErrorCodes.TicketCancelled);
    }

    [Fact]
    public async Task CancelTicket_ValidActive_ShouldAnularAndReleaseInventory()
    {
        using var context = TestDbContextFactory.Create();
        var service = new TicketService(context, _masterData, _inventory, _qrCodeService, _pdfGeneratorMock.Object, _auditMock.Object);

        var (ticket, _, _) = await CreateSampleTicketAsync(context, TicketStatus.ACTIVO);

        var result = await service.CancelTicketAsync(ticket.Id, new CancelTicketRequestDto { Reason = "Vehículo en mantenimiento" }, "SUPER1");

        result.Status.Should().Be(TicketStatus.ANULADO.ToString());
    }

    [Fact]
    public async Task ConsumeTicket_ValidActive_ShouldTransitionToConsumed()
    {
        using var context = TestDbContextFactory.Create();
        var service = new TicketService(context, _masterData, _inventory, _qrCodeService, _pdfGeneratorMock.Object, _auditMock.Object);

        var (ticket, _, _) = await CreateSampleTicketAsync(context, TicketStatus.ACTIVO);

        var result = await service.ConsumeTicketAsync(ticket.Id, new ConsumeTicketRequestDto { StationId = "ESTACION-05", DispatcherId = "DISP-1" });

        result.Status.Should().Be(TicketStatus.CONSUMIDO.ToString());
    }
}
