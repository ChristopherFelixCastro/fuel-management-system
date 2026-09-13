using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tickets.Sandbox.Api.Domain.Entities;

namespace Tickets.Sandbox.Api.Infrastructure.Persistence.Configurations;

public class RequestConfiguration : IEntityTypeConfiguration<Request>
{
    public void Configure(EntityTypeBuilder<Request> builder)
    {
        builder.ToTable("requests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RequestedQuantity)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.Source)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.Notes)
            .HasMaxLength(500);

        builder.Property(r => r.CreatedBy)
            .HasMaxLength(100);

        builder.Property(r => r.AuthorApprovedBy)
            .HasMaxLength(100);

        builder.Property(r => r.RejectionReason)
            .HasMaxLength(500);

        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.EmployeeId);
        builder.HasIndex(r => r.VehicleId);
        builder.HasIndex(r => r.DepartmentId);
        builder.HasIndex(r => r.CreatedAt);
    }
}

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");

        builder.HasKey(t => t.Id);

        // Restricción única en BD para el número de ticket
        builder.Property(t => t.Number)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(t => t.Number)
            .IsUnique();

        // Relación 1 a 1 obligatoria y única entre Request y Ticket
        builder.HasOne(t => t.Request)
            .WithOne(r => r.Ticket)
            .HasForeignKey<Ticket>(t => t.RequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.RequestId)
            .IsUnique();

        builder.Property(t => t.QrTokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(t => t.Signature)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(t => t.AuthorizedQuantity)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.ConsumedByStationId)
            .HasMaxLength(50);

        builder.Property(t => t.CancellationReason)
            .HasMaxLength(500);

        builder.Property(t => t.CancelledBy)
            .HasMaxLength(100);

        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.ExpiresAt);
        builder.HasIndex(t => t.CreatedAt);
    }
}

public class TicketSequenceConfiguration : IEntityTypeConfiguration<TicketSequence>
{
    public void Configure(EntityTypeBuilder<TicketSequence> builder)
    {
        builder.ToTable("ticket_sequences");

        builder.HasKey(s => s.Year);

        builder.Property(s => s.Year)
            .ValueGeneratedNever();

        builder.Property(s => s.LastValue)
            .IsRequired();

        builder.Property(s => s.ConcurrencyToken)
            .IsRowVersion();
    }
}
