using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tickets.Sandbox.Api.Application.Common;
using Tickets.Sandbox.Api.Application.Contracts;
using Tickets.Sandbox.Api.Application.DTOs.Requests;
using Tickets.Sandbox.Api.Application.DTOs.Tickets;
using Tickets.Sandbox.Api.Application.Services;
using Tickets.Sandbox.Api.Domain.Entities;
using Tickets.Sandbox.Api.Domain.Enums;
using Tickets.Sandbox.Api.Domain.Exceptions;
using Tickets.Sandbox.Api.Infrastructure.ExternalServices.Stubs;
using Tickets.Sandbox.Api.Infrastructure.Persistence;
using Tickets.Sandbox.Api.Infrastructure.Services;
using Tickets.Sandbox.Tests.Helpers;
using Xunit;

namespace Tickets.Sandbox.Tests.Application;

public class ConcurrencyAndIdempotencyTests : IClassFixture<WebApplicationFactory<Tickets.Sandbox.Api.Program>>
{
    private readonly WebApplicationFactory<Tickets.Sandbox.Api.Program> _factory;

    public ConcurrencyAndIdempotencyTests(WebApplicationFactory<Tickets.Sandbox.Api.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ApproveRequest_SimultaneousConcurrentCalls_OneSucceedsAndOtherFailsWithConcurrencyConflict()
    {
        // Usamos una base de datos con DbContext para probar concurrencia de inserción en Tickets con índice único
        var dbName = "ConcurrencyDb_" + Guid.NewGuid();
        var context1 = TestDbContextFactory.Create(dbName);
        var context2 = TestDbContextFactory.Create(dbName);

        var masterData = new MasterDataStubService();
        var inventory = new InventoryStubService();
        var audit = new Mock<IAuditService>();
        var notification = new Mock<INotificationService>();
        var pdf = new Mock<IPdfGeneratorService>();
        var qr = new Mock<IQrCodeService>();

        qr.Setup(q => q.GenerateQrSecurityData(It.IsAny<Guid>()))
            .Returns((Guid id) => new QrSecurityResult("raw", "hash", "sig", $"{id}:raw:sig"));

        var sequence1 = new TicketSequenceService(context1);
        var sequence2 = new TicketSequenceService(context2);

        var service1 = new RequestService(context1, masterData, inventory, sequence1, qr.Object, pdf.Object, notification.Object, audit.Object);
        var service2 = new RequestService(context2, masterData, inventory, sequence2, qr.Object, pdf.Object, notification.Object, audit.Object);

        // Crear la solicitud
        var created = await service1.CreateRequestAsync(new CreateRequestDto
        {
            EmployeeId = MasterDataStubService.DefaultEmployeeId,
            VehicleId = MasterDataStubService.DefaultVehicleId,
            DepartmentId = MasterDataStubService.DefaultDepartmentId,
            FuelTypeId = MasterDataStubService.DefaultFuelTypeId,
            RequestedQuantity = 50.0m
        });

        var approveDto1 = new ApproveRequestDto { AuthorizedQuantity = 50.0m, ExpiresAt = DateTime.UtcNow.AddDays(1) };
        var approveDto2 = new ApproveRequestDto { AuthorizedQuantity = 50.0m, ExpiresAt = DateTime.UtcNow.AddDays(1) };

        // Lanzar dos aprobaciones en paralelo sobre la MISMA solicitud
        var task1 = service1.ApproveRequestAsync(created.Id, approveDto1, "SUPER1");
        var task2 = service2.ApproveRequestAsync(created.Id, approveDto2, "SUPER2");

        var results = await Task.WhenAll(
            Task.Run(async () => {
                try { return (Success: true, Result: await task1, ErrorCode: (string?)null); }
                catch (BusinessRuleViolationException ex) { return (Success: false, Result: null, ErrorCode: ex.Code); }
            }),
            Task.Run(async () => {
                try { return (Success: true, Result: await task2, ErrorCode: (string?)null); }
                catch (BusinessRuleViolationException ex) { return (Success: false, Result: null, ErrorCode: ex.Code); }
            })
        );

        // Exactamente una debió tener éxito
        var successes = results.Count(r => r.Success);
        var failures = results.Count(r => !r.Success);

        successes.Should().Be(1, "solo una aprobación concurrente debe ser procesada");
        failures.Should().Be(1, "la aprobación competidora debe ser rechazada");
        
        var failedResult = results.First(r => !r.Success);
        failedResult.ErrorCode.Should().BeOneOf(ErrorCodes.ConcurrencyConflict, ErrorCodes.TicketAlreadyExists, ErrorCodes.RequestNotPending);
    }

    [Fact]
    public async Task Idempotency_SameKeyWithSameBody_ShouldReturnCachedResultWithoutDuplicatingSideEffects()
    {
        var testDbName = "IdempotencyDb_" + Guid.NewGuid();
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(testDbName));
            });
        }).CreateClient();

        // 1. Crear Solicitud
        var createDto = new CreateRequestDto
        {
            EmployeeId = MasterDataStubService.DefaultEmployeeId,
            VehicleId = MasterDataStubService.DefaultVehicleId,
            DepartmentId = MasterDataStubService.DefaultDepartmentId,
            FuelTypeId = MasterDataStubService.DefaultFuelTypeId,
            RequestedQuantity = 50.0m
        };
        var createRes = await client.PostAsJsonAsync("/api/v1/requests", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<ApiResponse<RequestResponseDto>>();
        var requestId = created!.Data!.Id;

        // 2. Primera llamada con Idempotency-Key
        var idempotencyKey = Guid.NewGuid().ToString();
        var approveDto = new ApproveRequestDto { AuthorizedQuantity = 50.0m, ExpiresAt = DateTime.UtcNow.AddDays(2) };

        var requestMsg1 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/requests/{requestId}/approve")
        {
            Content = JsonContent.Create(approveDto)
        };
        requestMsg1.Headers.Add("Idempotency-Key", idempotencyKey);

        var res1 = await client.SendAsync(requestMsg1);
        res1.StatusCode.Should().Be(HttpStatusCode.OK);
        var body1 = await res1.Content.ReadFromJsonAsync<ApiResponse<RequestResponseDto>>();

        // 3. Segunda llamada con la MISMA Idempotency-Key y MISMO body
        var requestMsg2 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/requests/{requestId}/approve")
        {
            Content = JsonContent.Create(approveDto)
        };
        requestMsg2.Headers.Add("Idempotency-Key", idempotencyKey);

        var res2 = await client.SendAsync(requestMsg2);
        res2.StatusCode.Should().Be(HttpStatusCode.OK);
        var body2 = await res2.Content.ReadFromJsonAsync<ApiResponse<RequestResponseDto>>();

        body2!.Data!.TicketId.Should().Be(body1!.Data!.TicketId);
        body2.Data.TicketNumber.Should().Be(body1.Data.TicketNumber);
    }

    [Fact]
    public async Task Idempotency_SameKeyWithDifferentBody_ShouldReturn409IdempotencyKeyReused()
    {
        var testDbName = "IdempotencyReusedDb_" + Guid.NewGuid();
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(testDbName));
            });
        }).CreateClient();

        var idempotencyKey = "KEY-SHARED-12345";

        var createDto1 = new CreateRequestDto
        {
            EmployeeId = MasterDataStubService.DefaultEmployeeId,
            VehicleId = MasterDataStubService.DefaultVehicleId,
            DepartmentId = MasterDataStubService.DefaultDepartmentId,
            FuelTypeId = MasterDataStubService.DefaultFuelTypeId,
            RequestedQuantity = 30.0m
        };

        var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/requests")
        {
            Content = JsonContent.Create(createDto1)
        };
        req1.Headers.Add("Idempotency-Key", idempotencyKey);
        var res1 = await client.SendAsync(req1);
        res1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Enviar MISMA clave con payload DIFERENTE
        var createDto2 = new CreateRequestDto
        {
            EmployeeId = MasterDataStubService.DefaultEmployeeId,
            VehicleId = MasterDataStubService.DefaultVehicleId,
            DepartmentId = MasterDataStubService.DefaultDepartmentId,
            FuelTypeId = MasterDataStubService.DefaultFuelTypeId,
            RequestedQuantity = 75.0m // Cantidad diferente
        };

        var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/requests")
        {
            Content = JsonContent.Create(createDto2)
        };
        req2.Headers.Add("Idempotency-Key", idempotencyKey);
        var res2 = await client.SendAsync(req2);

        res2.StatusCode.Should().Be(HttpStatusCode.Conflict); // 409
        var body2 = await res2.Content.ReadFromJsonAsync<ApiResponse>();
        body2!.Success.Should().BeFalse();
        body2.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.IdempotencyKeyReused);
    }
}
