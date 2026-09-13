using Microsoft.EntityFrameworkCore;
using Tickets.Sandbox.Api.Application.Interfaces;
using Tickets.Sandbox.Api.Domain.Entities;
using Tickets.Sandbox.Api.Infrastructure.Persistence;

namespace Tickets.Sandbox.Api.Infrastructure.Services;

public class TicketSequenceService : ITicketSequenceService
{
    private readonly AppDbContext _context;
    private static readonly SemaphoreSlim _localLock = new(1, 1);

    public TicketSequenceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> NextTicketNumberAsync(int year, CancellationToken cancellationToken = default)
    {
        await _localLock.WaitAsync(cancellationToken);
        try
        {
            const int maxRetries = 5;
            for (var attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var sequence = await _context.TicketSequences
                        .FirstOrDefaultAsync(s => s.Year == year, cancellationToken);

                    if (sequence == null)
                    {
                        sequence = new TicketSequence
                        {
                            Year = year,
                            LastValue = 1
                        };
                        _context.TicketSequences.Add(sequence);
                    }
                    else
                    {
                        sequence.LastValue++;
                    }

                    await _context.SaveChangesAsync(cancellationToken);

                    return $"COM-{year}-{sequence.LastValue:D6}";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (attempt == maxRetries - 1)
                        throw;

                    // Reintentar si hubo colisión concurrente
                    await Task.Delay(50, cancellationToken);
                }
            }

            throw new InvalidOperationException("No se pudo generar la secuencia de ticket debido a alta concurrencia.");
        }
        finally
        {
            _localLock.Release();
        }
    }
}
