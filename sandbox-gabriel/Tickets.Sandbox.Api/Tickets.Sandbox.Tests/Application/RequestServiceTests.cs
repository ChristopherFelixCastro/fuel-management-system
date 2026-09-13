using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tickets.Sandbox.Api.Application.Common;
using Tickets.Sandbox.Api.Application.Contracts;
using Tickets.Sandbox.Api.Application.DTOs.Requests;
using Tickets.Sandbox.Api.Application.Services;
using Tickets.Sandbox.Api.Domain.Entities;
using Tickets.Sandbox.Api.Domain.Enums;
using Tickets.Sandbox.Api.Domain.Exceptions;
using Tickets.Sandbox.Api.Infrastructure.ExternalServices.Stubs;
using Tickets.Sandbox.Api.Infrastructure.Services;
using Tickets.Sandbox.Tests.Helpers;
using Xunit;

namespace Tickets.Sandbox.Tests.Application;

public class RequestServiceTests
{
    private readonly MasterDataStubService _masterData;
    private readonly InventoryStubService _inventory;
    private readonly Mock<IAuditService> _auditMock;
    private readonly Mock<INotificationService> _notificationMock;
    private readonly Mock<IPdfGeneratorService> _pdfGeneratorMock;
    private readonly Mock<IQrCodeService> _qrCodeMock;

    public RequestServiceTests()
    {
        _masterData = new MasterDataStubService();
        _inventory = new InventoryStubService();
        _auditMock = new Mock<IAuditService>();
        _notificationMock = new Mock<INotificationService>();
        _pdfGeneratorMock = new Mock<IPdfGeneratorService>();
        _qrCodeMock = new Mock<IQrCodeService>();

        _qrCodeMock.Setup(q => q.GenerateQrSecurityData(It.IsAny<Guid>()))
            .Returns((Guid id) => new QrSecurityResult("raw_token", "token_hash", "signature_hex", $"{id}:raw_token:signature_hex"));

        _qrCodeMock.Setup(q => q.GenerateQrImagePng(It.IsAny<string>()))
            .Returns(new byte[] { 1, 2, 3 });

        _pdfGeneratorMock.Setup(p => p.GenerateTicketPdf(It.IsAny<TicketPdfModel>()))
            .Returns(new byte[] { 10, 20, 30 });
    }

    [Fact]
    public async Task CreateRequest_ValidManual_ShouldCreateSuccessfully()
    {
        using var context = TestDbContextFactory.Create();
        var sequenceService = new TicketSequenceService(context);
        var service = new RequestService(
            context, _masterData, _inventory, sequenceService,
            _qrCodeMock.Object, _pdfGeneratorMock.Object, _notificationMock.Object, _auditMock.Object);

        var dto = new CreateRequestDto
        {
            EmployeeId = MasterDataStubService.DefaultEmployeeId,
            VehicleId = MasterDataStubService.DefaultVehicleId,
            DepartmentId = MasterDataStubService.DefaultDepartmentId,
            FuelTypeId = MasterDataStubService.DefaultFuelTypeId,
            RequestedQuantity = 50.0m,
            Source = RequestSource.MANUAL
        };

        var result = await service.CreateRequestAsync(dto, "USER1");

        result.Should().NotBeNull();
        result.Status.Should().Be(RequestStatus.PENDIENTE.ToString());
        result.RequestedQuantity.Should().Be(50.0m);
        result.TicketId.Should().BeNull();
    }

    [Fact]
    public async Task CreateRequest_VehicleFuelMismatch_ShouldThrowVehicleFuelMismatchException()
    {
        using var context = TestDbContextFactory.Create();
        var sequenceService = new TicketSequenceService(context);
        var service = new RequestService(
            context, _masterData, _inventory, sequenceService,
            _qrCodeMock.Object, _pdfGeneratorMock.Object, _notificationMock.Object, _auditMock.Object);

        // Vehicle expects DefaultFuelTypeId (Diesel), we request AlternativeFuelTypeId (Gasoline)
        var dto = new CreateRequestDto
        {
            EmployeeId = MasterDataStubService.DefaultEmployeeId,
            VehicleId = MasterDataStubService.DefaultVehicleId,
            DepartmentId = MasterDataStubService.DefaultDepartmentId,
            FuelTypeId = MasterDataStubService.AlternativeFuelTypeId,
            RequestedQuantity = 30.0m
        };

        var act = async () => await service.CreateRequestAsync(dto);

        var ex = await act.Should().ThrowAsync<BusinessRuleViolationException>();
        ex.Which.Code.Should().Be(ErrorCodes.VehicleFuelMismatch);
    }

    [Fact]
    public async Task CreateRequest_QuantityExceedsTankCapacity_ShouldThrowQuantityExceedsTankCapacityException()
    {
        using var context = TestDbContextFactory.Create();
        var sequenceService = new TicketSequenceService(context);
        var service = new RequestService(
            context, _masterData, _inventory, sequenceService,
            _qrCodeMock.Object, _pdfGeneratorMock.Object, _notificationMock.Object, _auditMock.Object);

        // Vehicle tank capacity is 80.0, we request 150.0
        var dto = new CreateRequestDto
        {
            EmployeeId = MasterDataStubService.DefaultEmployeeId,
            VehicleId = MasterDataStubService.DefaultVehicleId,
            DepartmentId = MasterDataStubService.DefaultDepartmentId,
            FuelTypeId = MasterDataStubService.DefaultFuelTypeId,
            RequestedQuantity = 150.0m
        };

        var act = async () => await service.CreateRequestAsync(dto);

        var ex = await act.Should().ThrowAsync<BusinessRuleViolationException>();
        ex.Which.Code.Should().Be(ErrorCodes.QuantityExceedsTankCapacity);
    }

    [Fact]
    public async Task CreateRequest_DepartmentIncoherent_ShouldThrowDepartmentIncoherentException()
    {
        using var context = TestDbContextFactory.Create();
        var sequenceService = new TicketSequenceService(context);
        var service = new RequestService(
            context, _masterData, _inventory, sequenceService,
            _qrCodeMock.Object, _pdfGeneratorMock.Object, _notificationMock.Object, _auditMock.Object);

        var otherDeptId = Guid.NewGuid();
        _masterData.SeedDepartment(new DepartmentDto(otherDeptId, "Otro Depto", "OTH"));

        var dto = new CreateRequestDto
        {
            EmployeeId = MasterDataStubService.DefaultEmployeeId,
            VehicleId = MasterDataStubService.DefaultVehicleId,
            DepartmentId = otherDeptId, // Employee belongs to DefaultDepartmentId
            FuelTypeId = MasterDataStubService.DefaultFuelTypeId,
            RequestedQuantity = 40.0m
        };

        var act = async () => await service.CreateRequestAsync(dto);

        var ex = await act.Should().ThrowAsync<BusinessRuleViolationException>();
        ex.Which.Code.Should().Be(ErrorCodes.DepartmentIncoherent);
    }

    [Fact]
    public async Task ApproveRequest_WhenValid_ShouldEmitTicketAndGenerateNumber()
    {
        using var context = TestDbContextFactory.Create();
        var sequenceService = new TicketSequenceService(context);
        var service = new RequestService(
            context, _masterData, _inventory, sequenceService,
            _qrCodeMock.Object, _pdfGeneratorMock.Object, _notificationMock.Object, _auditMock.Object);

        var created = await service.CreateRequestAsync(new CreateRequestDto
        {
            EmployeeId = MasterDataStubService.DefaultEmployeeId,
            VehicleId = MasterDataStubService.DefaultVehicleId,
            DepartmentId = MasterDataStubService.DefaultDepartmentId,
            FuelTypeId = MasterDataStubService.DefaultFuelTypeId,
            RequestedQuantity = 50.0m
        });

        var approveDto = new ApproveRequestDto
        {
            AuthorizedQuantity = 50.0m,
            ExpiresAt = DateTime.UtcNow.AddDays(2),
            Notes = "Aprobado para ruta norte"
        };

        var approved = await service.ApproveRequestAsync(created.Id, approveDto, "SUPERVISOR1");

        approved.Status.Should().Be(RequestStatus.APROBADA.ToString());
        approved.TicketId.Should().NotBeNull();
        approved.TicketNumber.Should().StartWith($"COM-{DateTime.UtcNow.Year}-");
    }

    [Fact]
    public async Task ApproveRequest_InsufficientInventory_ShouldThrowInsufficientInventory()
    {
        using var context = TestDbContextFactory.Create();
        var sequenceService = new TicketSequenceService(context);
        var service = new RequestService(
            context, _masterData, _inventory, sequenceService,
            _qrCodeMock.Object, _pdfGeneratorMock.Object, _notificationMock.Object, _auditMock.Object);

        // Agotar inventario en el stub
        _inventory.SetPhysicalStock(MasterDataStubService.DefaultFuelTypeId, 5.0m);

        var created = await service.CreateRequestAsync(new CreateRequestDto
        {
            EmployeeId = MasterDataStubService.DefaultEmployeeId,
            VehicleId = MasterDataStubService.DefaultVehicleId,
            DepartmentId = MasterDataStubService.DefaultDepartmentId,
            FuelTypeId = MasterDataStubService.DefaultFuelTypeId,
            RequestedQuantity = 50.0m
        });

        var approveDto = new ApproveRequestDto
        {
            AuthorizedQuantity = 50.0m,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        var act = async () => await service.ApproveRequestAsync(created.Id, approveDto);

        var ex = await act.Should().ThrowAsync<BusinessRuleViolationException>();
        ex.Which.Code.Should().Be(ErrorCodes.InsufficientAvailableInventory);
    }

    [Fact]
    public async Task RejectRequest_WhenPending_ShouldTransitionToRejected()
    {
        using var context = TestDbContextFactory.Create();
        var sequenceService = new TicketSequenceService(context);
        var service = new RequestService(
            context, _masterData, _inventory, sequenceService,
            _qrCodeMock.Object, _pdfGeneratorMock.Object, _notificationMock.Object, _auditMock.Object);

        var created = await service.CreateRequestAsync(new CreateRequestDto
        {
            EmployeeId = MasterDataStubService.DefaultEmployeeId,
            VehicleId = MasterDataStubService.DefaultVehicleId,
            DepartmentId = MasterDataStubService.DefaultDepartmentId,
            FuelTypeId = MasterDataStubService.DefaultFuelTypeId,
            RequestedQuantity = 40.0m
        });

        var rejected = await service.RejectRequestAsync(created.Id, new RejectRequestDto { Reason = "Falta justificación de ruta" }, "SUPER1");

        rejected.Status.Should().Be(RequestStatus.RECHAZADA.ToString());
        rejected.RejectionReason.Should().Be("Falta justificación de ruta");
    }

    [Fact]
    public async Task CancelRequest_AfterApprovedWithTicket_ShouldThrowRequestNotEditable()
    {
        using var context = TestDbContextFactory.Create();
        var sequenceService = new TicketSequenceService(context);
        var service = new RequestService(
            context, _masterData, _inventory, sequenceService,
            _qrCodeMock.Object, _pdfGeneratorMock.Object, _notificationMock.Object, _auditMock.Object);

        var created = await service.CreateRequestAsync(new CreateRequestDto
        {
            EmployeeId = MasterDataStubService.DefaultEmployeeId,
            VehicleId = MasterDataStubService.DefaultVehicleId,
            DepartmentId = MasterDataStubService.DefaultDepartmentId,
            FuelTypeId = MasterDataStubService.DefaultFuelTypeId,
            RequestedQuantity = 40.0m
        });

        await service.ApproveRequestAsync(created.Id, new ApproveRequestDto
        {
            AuthorizedQuantity = 40.0m,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        });

        // Intentar cancelar después de aprobada / con ticket
        var act = async () => await service.CancelRequestAsync(created.Id);

        var ex = await act.Should().ThrowAsync<BusinessRuleViolationException>();
        ex.Which.Code.Should().Be(ErrorCodes.RequestNotEditable);
    }
}
