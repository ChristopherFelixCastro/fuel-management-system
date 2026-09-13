using Microsoft.EntityFrameworkCore;
using Tickets.Sandbox.Api.Domain.Entities;
using Tickets.Sandbox.Api.Infrastructure.Persistence.Configurations;

namespace Tickets.Sandbox.Api.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Request> Requests => Set<Request>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketSequence> TicketSequences => Set<TicketSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new RequestConfiguration());
        modelBuilder.ApplyConfiguration(new TicketConfiguration());
        modelBuilder.ApplyConfiguration(new TicketSequenceConfiguration());
    }
}
