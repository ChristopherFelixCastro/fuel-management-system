namespace Tickets.Sandbox.Api.Domain.Entities;

public class TicketSequence
{
    public int Year { get; set; }
    public int LastValue { get; set; }
    public byte[]? ConcurrencyToken { get; set; }
}
