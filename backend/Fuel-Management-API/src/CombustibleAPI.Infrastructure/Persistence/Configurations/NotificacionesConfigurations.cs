using CombustibleAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CombustibleAPI.Infrastructure.Persistence.Configurations;

public class TicketAccesoPublicoConfig : IEntityTypeConfiguration<TicketAccesoPublico>
{
    public void Configure(EntityTypeBuilder<TicketAccesoPublico> b)
    {
        b.ToTable("ticket_acceso_publico");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.TicketId).HasColumnName("ticket_id");
        b.Property(x => x.Nonce).HasColumnName("nonce").HasMaxLength(64).IsRequired();
        b.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
        b.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
        b.Property(x => x.FechaExpiracion).HasColumnName("fecha_expiracion");
        b.Property(x => x.FechaRevocacion).HasColumnName("fecha_revocacion");

        b.Ignore(x => x.EstaActivo);

        b.HasOne(x => x.Ticket)
            .WithMany()
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.TokenHash).IsUnique();
    }
}

public class NotificacionEntregaConfig : IEntityTypeConfiguration<NotificacionEntrega>
{
    public void Configure(EntityTypeBuilder<NotificacionEntrega> b)
    {
        b.ToTable("notificacion_entrega");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.TicketId).HasColumnName("ticket_id");
        b.Property(x => x.TipoEvento).HasColumnName("tipo_evento").HasMaxLength(40).IsRequired();
        b.Property(x => x.Canal).HasColumnName("canal").HasMaxLength(20).IsRequired();
        b.Property(x => x.Destinatario).HasColumnName("destinatario").HasMaxLength(200);
        b.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(30).IsRequired();
        b.Property(x => x.Intentos).HasColumnName("intentos");
        b.Property(x => x.MaxIntentos).HasColumnName("max_intentos");
        b.Property(x => x.UltimoError).HasColumnName("ultimo_error");
        b.Property(x => x.ProviderMessageId).HasColumnName("provider_message_id").HasMaxLength(120);
        b.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
        b.Property(x => x.FechaProximoIntento).HasColumnName("fecha_proximo_intento");
        b.Property(x => x.FechaEnvio).HasColumnName("fecha_envio");
        b.Property(x => x.ProcesandoDesde).HasColumnName("procesando_desde");
        b.Property(x => x.WorkerId).HasColumnName("worker_id").HasMaxLength(100);

        b.HasOne(x => x.Ticket)
            .WithMany()
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.TicketId, x.TipoEvento, x.Canal }).IsUnique();
    }
}
