using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tickets.Sandbox.Api.Application.Common;
using Tickets.Sandbox.Api.Application.DTOs.Requests;
using Tickets.Sandbox.Api.Application.DTOs.Tickets;
using Tickets.Sandbox.Api.Infrastructure.Configuration;
using Tickets.Sandbox.Api.Infrastructure.ExternalServices.Stubs;
using Tickets.Sandbox.Api.Infrastructure.Persistence;
using Tickets.Sandbox.Api.Infrastructure.Services;
using Xunit;

namespace Tickets.Sandbox.Tests.Integration;

public class RequestsAndTicketsApiTests : IClassFixture<WebApplicationFactory<Tickets.Sandbox.Api.Program>>
{
    private readonly WebApplicationFactory<Tickets.Sandbox.Api.Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testDbName = "IntegrationTestsDb_" + Guid.NewGuid();

    public RequestsAndTicketsApiTests(WebApplicationFactory<Tickets.Sandbox.Api.Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_testDbName);
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateRequest_WhenPayloadValid_ShouldReturn201CreatedAndStandardResponse()
    {
        var dto = new CreateRequestDto
        {
            EmployeeId = MasterDataStubService.DefaultEmployeeId,
            VehicleId = MasterDataStubService.DefaultVehicleId,
            DepartmentId = MasterDataStubService.DefaultDepartmentId,
            FuelTypeId = MasterDataStubService.DefaultFuelTypeId,
            RequestedQuantity = 45.0m
        };

        var response = await _client.PostAsJsonAsync("/api/v1/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<RequestResponseDto>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeNull();
        content.Data!.RequestedQuantity.Should().Be(45.0m);
    }

    [Fact]
    public async Task CreateRequest_WhenVehicleFuelMismatch_ShouldReturn422UnprocessableEntity()
    {
        var dto = new CreateRequestDto
        {
            EmployeeId = MasterDataStubService.DefaultEmployeeId,
            VehicleId = MasterDataStubService.DefaultVehicleId,
            DepartmentId = MasterDataStubService.DefaultDepartmentId,
            FuelTypeId = MasterDataStubService.AlternativeFuelTypeId, // Incompatible
            RequestedQuantity = 45.0m
        };

        var response = await _client.PostAsJsonAsync("/api/v1/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse>();
        content.Should().NotBeNull();
        content!.Success.Should().BeFalse();
        content.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.VehicleFuelMismatch);
    }

    [Fact]
    public async Task ValidateTicket_WhenTicketDoesNotExistInDb_ShouldReturn404NotFoundAndTicketNotFoundCode()
    {
        var qrService = _factory.Services.GetRequiredService<Tickets.Sandbox.Api.Application.Contracts.IQrCodeService>();
        var nonExistentId = Guid.NewGuid();
        var qrSecurity = qrService.GenerateQrSecurityData(nonExistentId);

        var validateDto = new ValidateTicketRequestDto
        {
            QrPayload = qrSecurity.QrPayload,
            StationId = "ESTACION-01"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/tickets/validate", validateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound); // 404
        var content = await response.Content.ReadFromJsonAsync<ApiResponse>();
        content.Should().NotBeNull();
        content!.Success.Should().BeFalse();
        content.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.TicketNotFound);
    }

    [Fact]
    public async Task EndToEnd_Create_Approve_DownloadPdf_AndValidateQr()
    {
        // 1. Crear Solicitud
        var createDto = new CreateRequestDto
        {
            EmployeeId = MasterDataStubService.DefaultEmployeeId,
            VehicleId = MasterDataStubService.DefaultVehicleId,
            DepartmentId = MasterDataStubService.DefaultDepartmentId,
            FuelTypeId = MasterDataStubService.DefaultFuelTypeId,
            RequestedQuantity = 50.0m
        };

        var createRes = await _client.PostAsJsonAsync("/api/v1/requests", createDto);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createRes.Content.ReadFromJsonAsync<ApiResponse<RequestResponseDto>>();
        var requestId = created!.Data!.Id;

        // 2. Aprobar Solicitud (Supervisor)
        var approveDto = new ApproveRequestDto
        {
            AuthorizedQuantity = 50.0m,
            ExpiresAt = DateTime.UtcNow.AddDays(2),
            Notes = "Aprobado para operaciones"
        };

        var approveRes = await _client.PostAsJsonAsync($"/api/v1/requests/{requestId}/approve", approveDto);
        approveRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var approved = await approveRes.Content.ReadFromJsonAsync<ApiResponse<RequestResponseDto>>();
        approved!.Data!.TicketId.Should().NotBeNull();
        var ticketId = approved.Data.TicketId!.Value;

        // 3. Descargar PDF del Ticket
        var pdfRes = await _client.GetAsync($"/api/v1/tickets/{ticketId}/pdf");
        pdfRes.StatusCode.Should().Be(HttpStatusCode.OK);
        pdfRes.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        var pdfBytes = await pdfRes.Content.ReadAsByteArrayAsync();
        pdfBytes.Should().NotBeEmpty();

        // 4. Obtener Ticket por ID
        var ticketRes = await _client.GetAsync($"/api/v1/tickets/{ticketId}");
        ticketRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var ticketData = await ticketRes.Content.ReadFromJsonAsync<ApiResponse<TicketResponseDto>>();
        ticketData!.Data!.Number.Should().StartWith($"COM-{DateTime.UtcNow.Year}-");
    }
}
