using CombustibleAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CombustibleAPI.Infrastructure.Persistence.Configurations;

public class SolicitudConfig : IEntityTypeConfiguration<Solicitud>
{
    public void Configure(EntityTypeBuilder<Solicitud> b)
    {
        b.ToTable("solicitud");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.EmpleadoId).HasColumnName("empleado_id");
        b.Property(x => x.VehiculoId).HasColumnName("vehiculo_id");
        b.Property(x => x.DepartamentoId).HasColumnName("departamento_id");
        b.Property(x => x.CreadaPorUsuarioId).HasColumnName("creada_por_usuario_id");
        b.Property(x => x.RevisadaPorUsuarioId).HasColumnName("revisada_por_usuario_id");
        b.Property(x => x.TipoSolicitud).HasColumnName("tipo_solicitud").HasMaxLength(20).IsRequired();
        b.Property(x => x.CantidadSolicitada).HasColumnName("cantidad_solicitada").HasColumnType("numeric(10,2)");
        b.Property(x => x.CantidadAutorizada).HasColumnName("cantidad_autorizada").HasColumnType("numeric(10,2)");
        b.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20).IsRequired();
        b.Property(x => x.FechaSolicitud).HasColumnName("fecha_solicitud");
        b.Property(x => x.FechaExpiracion).HasColumnName("fecha_expiracion");
        b.Property(x => x.FechaRevision).HasColumnName("fecha_revision");
        b.Property(x => x.MotivoRechazo).HasColumnName("motivo_rechazo").HasMaxLength(300);
        b.Property(x => x.MotivoCancelacion).HasColumnName("motivo_cancelacion").HasMaxLength(300);
        b.Property(x => x.Observaciones).HasColumnName("observaciones").HasMaxLength(500);

        b.Ignore(x => x.CreadoEn);

        b.HasOne(x => x.Empleado).WithMany().HasForeignKey(x => x.EmpleadoId);
        b.HasOne(x => x.Vehiculo).WithMany().HasForeignKey(x => x.VehiculoId);
        b.HasOne(x => x.Departamento).WithMany().HasForeignKey(x => x.DepartamentoId);
        b.HasOne(x => x.CreadaPorUsuario).WithMany().HasForeignKey(x => x.CreadaPorUsuarioId);
        b.HasOne(x => x.RevisadaPorUsuario).WithMany().HasForeignKey(x => x.RevisadaPorUsuarioId);
    }
}

public class TicketConfig : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> b)
    {
        b.ToTable("ticket");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.SolicitudId).HasColumnName("solicitud_id");
        b.Property(x => x.EstacionId).HasColumnName("estacion_id");
        b.Property(x => x.TipoCombustibleId).HasColumnName("tipo_combustible_id");
        b.Property(x => x.NumeroTicket).HasColumnName("numero_ticket").HasMaxLength(25).IsRequired();
        b.Property(x => x.CantidadAutorizada).HasColumnName("cantidad_autorizada").HasColumnType("numeric(10,2)");
        b.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(25).IsRequired();
        b.Property(x => x.TokenQrHash).HasColumnName("token_qr_hash").HasMaxLength(255).IsRequired();
        b.Property(x => x.FirmaQr).HasColumnName("firma_qr").HasMaxLength(255).IsRequired();
        b.Property(x => x.FechaEmision).HasColumnName("fecha_emision");
        b.Property(x => x.FechaExpiracion).HasColumnName("fecha_expiracion");
        b.Property(x => x.FechaEnvio).HasColumnName("fecha_envio");
        b.Property(x => x.FechaAnulacion).HasColumnName("fecha_anulacion");
        b.Property(x => x.AnuladoPorUsuarioId).HasColumnName("anulado_por_usuario_id");
        b.Property(x => x.MotivoAnulacion).HasColumnName("motivo_anulacion").HasMaxLength(300);

        b.Ignore(x => x.Numero);
        b.Ignore(x => x.CantidadReservada);
        b.Ignore(x => x.EmitidoEn);
        b.Ignore(x => x.VenceEn);
        b.Ignore(x => x.TokenHash);
        b.Ignore(x => x.FirmaHash);

        b.HasIndex(x => x.SolicitudId).IsUnique();
        b.HasIndex(x => x.NumeroTicket).IsUnique();
        b.HasIndex(x => x.TokenQrHash).IsUnique();

        b.HasOne(x => x.Solicitud).WithOne().HasForeignKey<Ticket>(x => x.SolicitudId);
        b.HasOne(x => x.Estacion).WithMany().HasForeignKey(x => x.EstacionId);
        b.HasOne(x => x.TipoCombustible).WithMany().HasForeignKey(x => x.TipoCombustibleId);
        b.HasOne(x => x.AnuladoPorUsuario).WithMany().HasForeignKey(x => x.AnuladoPorUsuarioId);
    }
}

public class DespachoConfig : IEntityTypeConfiguration<Despacho>
{
    public void Configure(EntityTypeBuilder<Despacho> b)
    {
        b.ToTable("despacho");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.TicketId).HasColumnName("ticket_id");
        b.Property(x => x.DespachadorUsuarioId).HasColumnName("despachador_usuario_id");
        b.Property(x => x.TanqueId).HasColumnName("tanque_id");
        b.Property(x => x.CantidadDespachada).HasColumnName("cantidad_despachada").HasColumnType("numeric(10,2)");
        b.Property(x => x.OdometroRegistrado).HasColumnName("odometro_registrado").HasColumnType("numeric(12,2)");
        b.Property(x => x.FechaDespacho).HasColumnName("fecha_despacho");
        b.Property(x => x.Observaciones).HasColumnName("observaciones").HasMaxLength(500);
        b.Property(x => x.DireccionIp).HasColumnName("direccion_ip").HasMaxLength(45);

        b.Ignore(x => x.Galones);
        b.Ignore(x => x.Odometro);
        b.Ignore(x => x.FechaHora);
        b.Ignore(x => x.Observacion);
        b.Ignore(x => x.DespachadorId);

        b.HasIndex(x => x.TicketId).IsUnique();
        b.HasOne(x => x.Ticket).WithOne().HasForeignKey<Despacho>(x => x.TicketId);
        b.HasOne(x => x.Despachador).WithMany().HasForeignKey(x => x.DespachadorUsuarioId);
        b.HasOne(x => x.Tanque).WithMany().HasForeignKey(x => x.TanqueId);
    }
}

public class MovimientoInventarioConfig : IEntityTypeConfiguration<MovimientoInventario>
{
    public void Configure(EntityTypeBuilder<MovimientoInventario> b)
    {
        b.ToTable("movimiento_inventario");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.TanqueId).HasColumnName("tanque_id");
        b.Property(x => x.RegistradoPorUsuarioId).HasColumnName("registrado_por_usuario_id");
        b.Property(x => x.TipoMovimiento).HasColumnName("tipo_movimiento").HasMaxLength(30).IsRequired();
        b.Property(x => x.Cantidad).HasColumnName("cantidad").HasColumnType("numeric(12,2)");
        b.Property(x => x.SaldoAnterior).HasColumnName("saldo_anterior").HasColumnType("numeric(12,2)");
        b.Property(x => x.SaldoPosterior).HasColumnName("saldo_posterior").HasColumnType("numeric(12,2)");
        b.Property(x => x.RecepcionId).HasColumnName("recepcion_id");
        b.Property(x => x.DespachoId).HasColumnName("despacho_id");
        b.Property(x => x.TransferenciaId).HasColumnName("transferencia_id");
        b.Property(x => x.AjusteId).HasColumnName("ajuste_id");
        b.Property(x => x.FechaMovimiento).HasColumnName("fecha_movimiento");
        b.Property(x => x.Observaciones).HasColumnName("observaciones").HasMaxLength(500);

        b.Ignore(x => x.FechaHora);
        b.Ignore(x => x.Observacion);

        b.HasOne(x => x.Tanque).WithMany().HasForeignKey(x => x.TanqueId);
        b.HasOne(x => x.RegistradoPorUsuario).WithMany().HasForeignKey(x => x.RegistradoPorUsuarioId);
    }
}

public class CierreDiarioConfig : IEntityTypeConfiguration<CierreDiario>
{
    public void Configure(EntityTypeBuilder<CierreDiario> b)
    {
        b.ToTable("cierre_diario");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.TanqueId).HasColumnName("tanque_id");
        b.Property(x => x.CreadoPorUsuarioId).HasColumnName("creado_por_usuario_id");
        b.Property(x => x.RevisadoPorUsuarioId).HasColumnName("revisado_por_usuario_id");
        b.Property(x => x.FechaCierre).HasColumnName("fecha_cierre");
        b.Property(x => x.StockInicial).HasColumnName("stock_inicial").HasColumnType("numeric(12,2)");
        b.Property(x => x.TotalRecepciones).HasColumnName("total_recepciones").HasColumnType("numeric(12,2)");
        b.Property(x => x.TotalTransferenciasEntrada).HasColumnName("total_transferencias_entrada").HasColumnType("numeric(12,2)");
        b.Property(x => x.TotalTransferenciasSalida).HasColumnName("total_transferencias_salida").HasColumnType("numeric(12,2)");
        b.Property(x => x.TotalDespachos).HasColumnName("total_despachos").HasColumnType("numeric(12,2)");
        b.Property(x => x.TotalAjustesPositivos).HasColumnName("total_ajustes_positivos").HasColumnType("numeric(12,2)");
        b.Property(x => x.TotalAjustesNegativos).HasColumnName("total_ajustes_negativos").HasColumnType("numeric(12,2)");
        b.Property(x => x.StockTeoricoFinal).HasColumnName("stock_teorico_final").HasColumnType("numeric(12,2)");
        b.Property(x => x.StockFisicoFinal).HasColumnName("stock_fisico_final").HasColumnType("numeric(12,2)");
        b.Property(x => x.Diferencia).HasColumnName("diferencia").HasColumnType("numeric(12,2)");
        b.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(30).IsRequired();
        b.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
        b.Property(x => x.FechaRevision).HasColumnName("fecha_revision");
        b.Property(x => x.MotivoDiferencia).HasColumnName("motivo_diferencia").HasMaxLength(500);
        b.Property(x => x.MotivoRechazo).HasColumnName("motivo_rechazo").HasMaxLength(300);
        b.Property(x => x.Observaciones).HasColumnName("observaciones").HasMaxLength(500);

        b.Ignore(x => x.Fecha);
        b.Ignore(x => x.InventarioTeorico);
        b.Ignore(x => x.InventarioFisico);
        b.Ignore(x => x.DespachadorId);
        b.Ignore(x => x.AprobadorId);
        b.Ignore(x => x.CreadoEn);

        b.HasOne(x => x.Tanque).WithMany().HasForeignKey(x => x.TanqueId);
        b.HasOne(x => x.CreadoPorUsuario).WithMany().HasForeignKey(x => x.CreadoPorUsuarioId);
        b.HasOne(x => x.RevisadoPorUsuario).WithMany().HasForeignKey(x => x.RevisadoPorUsuarioId);
    }
}
