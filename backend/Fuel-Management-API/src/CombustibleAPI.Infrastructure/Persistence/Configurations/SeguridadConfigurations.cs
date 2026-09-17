using CombustibleAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CombustibleAPI.Infrastructure.Persistence.Configurations;

public class RolConfig : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> b)
    {
        b.ToTable("rol");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(30).IsRequired();
        b.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(150);
        b.Property(x => x.Activo).HasColumnName("activo");
        b.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");

        b.HasIndex(x => x.Nombre).IsUnique();
    }
}

public class UsuarioConfig : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("usuario");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.RolId).HasColumnName("rol_id");
        b.Property(x => x.EmpleadoId).HasColumnName("empleado_id");
        b.Property(x => x.EstacionId).HasColumnName("estacion_id");
        b.Property(x => x.NombreUsuario).HasColumnName("nombre_usuario").HasMaxLength(60).IsRequired();
        b.Property(x => x.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
        b.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
        b.Property(x => x.Activo).HasColumnName("activo");
        b.Property(x => x.Bloqueado).HasColumnName("bloqueado");
        b.Property(x => x.IntentosFallidos).HasColumnName("intentos_fallidos");
        b.Property(x => x.UltimoAcceso).HasColumnName("ultimo_acceso");
        b.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
        b.Property(x => x.FechaActualizacion).HasColumnName("fecha_actualizacion");

        b.Ignore(x => x.Username);
        b.Ignore(x => x.CreadoEn);

        b.HasIndex(x => x.NombreUsuario).IsUnique();
        b.HasIndex(x => x.Email).IsUnique();
        b.HasIndex(x => x.EmpleadoId).IsUnique();

        b.HasOne(x => x.Rol).WithMany(r => r.Usuarios).HasForeignKey(x => x.RolId);
        b.HasOne(x => x.Empleado).WithMany().HasForeignKey(x => x.EmpleadoId);
        b.HasOne(x => x.Estacion).WithMany().HasForeignKey(x => x.EstacionId);
    }
}

public class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_token");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        b.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(255).IsRequired();
        b.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
        b.Property(x => x.FechaExpiracion).HasColumnName("fecha_expiracion");
        b.Property(x => x.FechaRevocacion).HasColumnName("fecha_revocacion");
        b.Property(x => x.ReemplazadoPorId).HasColumnName("reemplazado_por_id");
        b.Property(x => x.IpCreacion).HasColumnName("ip_creacion").HasMaxLength(45);
        b.Property(x => x.IpRevocacion).HasColumnName("ip_revocacion").HasMaxLength(45);

        b.Ignore(x => x.CreadoEn);
        b.Ignore(x => x.ExpiraEn);
        b.Ignore(x => x.RevocadoEn);
        b.Ignore(x => x.EstaActivo);
        b.Ignore(x => x.EstaExpirado);

        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => x.ReemplazadoPorId).IsUnique();
        b.HasOne(x => x.Usuario).WithMany(u => u.RefreshTokens).HasForeignKey(x => x.UsuarioId);
    }
}

public class AuditLogConfig : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_log");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        b.Property(x => x.Accion).HasColumnName("accion").HasMaxLength(50).IsRequired();
        b.Property(x => x.Entidad).HasColumnName("entidad").HasMaxLength(80).IsRequired();
        b.Property(x => x.EntidadId).HasColumnName("entidad_id");
        b.Property(x => x.DatosAnteriores).HasColumnName("datos_anteriores").HasColumnType("jsonb");
        b.Property(x => x.DatosNuevos).HasColumnName("datos_nuevos").HasColumnType("jsonb");
        b.Property(x => x.DireccionIp).HasColumnName("direccion_ip").HasMaxLength(45);
        b.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        b.Property(x => x.FechaHora).HasColumnName("fecha_hora");
        b.Property(x => x.HashAnterior).HasColumnName("hash_anterior").HasMaxLength(64);
        b.Property(x => x.HashActual).HasColumnName("hash_actual").HasMaxLength(64).IsRequired();

        b.HasIndex(x => x.HashActual).IsUnique();
        b.HasIndex(x => new { x.FechaHora, x.Entidad, x.EntidadId });
    }
}
