using FluentAssertions;
using Tickets.Sandbox.Api.Domain.Entities;
using Tickets.Sandbox.Api.Domain.Enums;
using Xunit;

namespace Tickets.Sandbox.Tests.Domain;

public class DomainEntityTests
{
    [Fact]
    public void Request_Initialization_ShouldHaveDefaultValues()
    {
        var request = new Request();

        request.Id.Should().NotBeEmpty();
        request.Status.Should().Be(RequestStatus.PENDIENTE);
        request.Source.Should().Be(RequestSource.MANUAL);
        request.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        request.Ticket.Should().BeNull();
    }

    [Fact]
    public void Ticket_Initialization_ShouldHaveDefaultValues()
    {
        var ticket = new Ticket();

        ticket.Id.Should().NotBeEmpty();
        ticket.Status.Should().Be(TicketStatus.ACTIVO);
        ticket.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
