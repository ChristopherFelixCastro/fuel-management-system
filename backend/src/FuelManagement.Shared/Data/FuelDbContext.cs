using FuelManagement.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace FuelManagement.Shared.Data;

/// <summary>
/// DbContext principal. Mapea las entidades al esquema PostgreSQL existente.
/// No se usan Migrations — el DDL esta en /database/ddl.
/// </summary>
public class FuelDbContext : DbContext
{
    public FuelDbContext(DbContextOptions<FuelDbContext> options) : base(options) { }

    // ---------------------------------------------------------------- Tables
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<TipoCombustible> TiposCombustible => Set<TipoCombustible>();
    public DbSet<Estacion> Estaciones => Set<Estacion>();
    public DbSet<Tanque> Tanques => Set<Tanque>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<CierreDiario> CierresDiarios => Set<CierreDiario>();
    public DbSet<AlertaOperativa> AlertasOperativas => Set<AlertaOperativa>();
    public DbSet<AjusteInventario> AjustesInventario => Set<AjusteInventario>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();

    // ---------------------------------------------------------------- Views
    public DbSet<VwCierresResumen> VwCierresResumen => Set<VwCierresResumen>();
    public DbSet<VwStockDisponible> VwStockDisponible => Set<VwStockDisponible>();
    public DbSet<VwMovimientosTanque> VwMovimientosTanque => Set<VwMovimientosTanque>();
    public DbSet<VwTicketsOperativos> VwTicketsOperativos => Set<VwTicketsOperativos>();
    public DbSet<FnCalcCierreRow> FnCalcCierreRows => Set<FnCalcCierreRow>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);

        // ---- rol -----------------------------------------------------------
        m.Entity<Rol>(e =>
        {
            e.ToTable("rol");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.Descripcion).HasColumnName("descripcion");
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
        });

        // ---- departamento --------------------------------------------------
        m.Entity<Departamento>(e =>
        {
            e.ToTable("departamento");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Codigo).HasColumnName("codigo");
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.Descripcion).HasColumnName("descripcion");
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
            e.Property(x => x.FechaActualizacion).HasColumnName("fecha_actualizacion");
        });

        // ---- tipo_combustible ----------------------------------------------
        m.Entity<TipoCombustible>(e =>
        {
            e.ToTable("tipo_combustible");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Codigo).HasColumnName("codigo");
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.Descripcion).HasColumnName("descripcion");
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
        });

        // ---- estacion ------------------------------------------------------
        m.Entity<Estacion>(e =>
        {
            e.ToTable("estacion");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Codigo).HasColumnName("codigo");
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.Ubicacion).HasColumnName("ubicacion");
            e.Property(x => x.Descripcion).HasColumnName("descripcion");
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
            e.Property(x => x.FechaActualizacion).HasColumnName("fecha_actualizacion");
        });

        // ---- tanque --------------------------------------------------------
        m.Entity<Tanque>(e =>
        {
            e.ToTable("tanque");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.EstacionId).HasColumnName("estacion_id");
            e.Property(x => x.TipoCombustibleId).HasColumnName("tipo_combustible_id");
            e.Property(x => x.Codigo).HasColumnName("codigo");
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.CapacidadMaxima).HasColumnName("capacidad_maxima").HasPrecision(12, 2);
            e.Property(x => x.StockActual).HasColumnName("stock_actual").HasPrecision(12, 2);
            e.Property(x => x.NivelCritico).HasColumnName("nivel_critico").HasPrecision(12, 2);
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
            e.Property(x => x.FechaActualizacion).HasColumnName("fecha_actualizacion");
            e.HasOne(x => x.Estacion).WithMany().HasForeignKey(x => x.EstacionId);
            e.HasOne(x => x.TipoCombustible).WithMany().HasForeignKey(x => x.TipoCombustibleId);
        });

        // ---- usuario -------------------------------------------------------
        m.Entity<Usuario>(e =>
        {
            e.ToTable("usuario");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.RolId).HasColumnName("rol_id");
            e.Property(x => x.EmpleadoId).HasColumnName("empleado_id");
            e.Property(x => x.EstacionId).HasColumnName("estacion_id");
            e.Property(x => x.NombreUsuario).HasColumnName("nombre_usuario");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.PasswordHash).HasColumnName("password_hash");
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.Bloqueado).HasColumnName("bloqueado");
            e.Property(x => x.IntentosFallidos).HasColumnName("intentos_fallidos");
            e.Property(x => x.UltimoAcceso).HasColumnName("ultimo_acceso");
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
            e.Property(x => x.FechaActualizacion).HasColumnName("fecha_actualizacion");
        });

        // ---- cierre_diario -------------------------------------------------
        m.Entity<CierreDiario>(e =>
        {
            e.ToTable("cierre_diario");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TanqueId).HasColumnName("tanque_id");
            e.Property(x => x.CreadoPorUsuarioId).HasColumnName("creado_por_usuario_id");
            e.Property(x => x.RevisadoPorUsuarioId).HasColumnName("revisado_por_usuario_id");
            e.Property(x => x.FechaCierre).HasColumnName("fecha_cierre");
            e.Property(x => x.StockInicial).HasColumnName("stock_inicial").HasPrecision(12, 2);
            e.Property(x => x.TotalRecepciones).HasColumnName("total_recepciones").HasPrecision(12, 2);
            e.Property(x => x.TotalTransferenciasEntrada).HasColumnName("total_transferencias_entrada").HasPrecision(12, 2);
            e.Property(x => x.TotalTransferenciasSalida).HasColumnName("total_transferencias_salida").HasPrecision(12, 2);
            e.Property(x => x.TotalDespachos).HasColumnName("total_despachos").HasPrecision(12, 2);
            e.Property(x => x.TotalAjustesPositivos).HasColumnName("total_ajustes_positivos").HasPrecision(12, 2);
            e.Property(x => x.TotalAjustesNegativos).HasColumnName("total_ajustes_negativos").HasPrecision(12, 2);
            e.Property(x => x.StockTeoricoFinal).HasColumnName("stock_teorico_final").HasPrecision(12, 2);
            e.Property(x => x.StockFisicoFinal).HasColumnName("stock_fisico_final").HasPrecision(12, 2);
            e.Property(x => x.Diferencia).HasColumnName("diferencia").HasPrecision(12, 2);
            e.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(30);
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
            e.Property(x => x.FechaRevision).HasColumnName("fecha_revision");
            e.Property(x => x.MotivoDiferencia).HasColumnName("motivo_diferencia").HasMaxLength(500);
            e.Property(x => x.MotivoRechazo).HasColumnName("motivo_rechazo").HasMaxLength(300);
            e.Property(x => x.Observaciones).HasColumnName("observaciones").HasMaxLength(500);
            e.HasOne(x => x.Tanque).WithMany().HasForeignKey(x => x.TanqueId);
            e.HasOne(x => x.CreadoPor).WithMany().HasForeignKey(x => x.CreadoPorUsuarioId);
            e.HasOne(x => x.RevisadoPor).WithMany().HasForeignKey(x => x.RevisadoPorUsuarioId);
        });

        // ---- alerta_operativa ---------------------------------------------
        m.Entity<AlertaOperativa>(e =>
        {
            e.ToTable("alerta_operativa");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(40);
            e.Property(x => x.Severidad).HasColumnName("severidad").HasMaxLength(20);
            e.Property(x => x.EntidadOrigen).HasColumnName("entidad_origen").HasMaxLength(80);
            e.Property(x => x.EntidadId).HasColumnName("entidad_id");
            e.Property(x => x.Mensaje).HasColumnName("mensaje").HasMaxLength(500);
            e.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
            e.Property(x => x.FechaResolucion).HasColumnName("fecha_resolucion");
            e.Property(x => x.ResueltaPorUsuarioId).HasColumnName("resuelta_por_usuario_id");
            e.HasOne(x => x.ResueltaPor).WithMany().HasForeignKey(x => x.ResueltaPorUsuarioId);
        });

        // ---- ajuste_inventario --------------------------------------------
        m.Entity<AjusteInventario>(e =>
        {
            e.ToTable("ajuste_inventario");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TanqueId).HasColumnName("tanque_id");
            e.Property(x => x.ReportadoPorUsuarioId).HasColumnName("reportado_por_usuario_id");
            e.Property(x => x.RevisadoPorUsuarioId).HasColumnName("revisado_por_usuario_id");
            e.Property(x => x.TipoAjuste).HasColumnName("tipo_ajuste").HasMaxLength(20);
            e.Property(x => x.Cantidad).HasColumnName("cantidad").HasPrecision(12, 2);
            e.Property(x => x.Motivo).HasColumnName("motivo").HasMaxLength(300);
            e.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(25);
            e.Property(x => x.FechaReporte).HasColumnName("fecha_reporte");
            e.Property(x => x.FechaRevision).HasColumnName("fecha_revision");
            e.Property(x => x.MotivoRechazo).HasColumnName("motivo_rechazo").HasMaxLength(300);
            e.Property(x => x.Observaciones).HasColumnName("observaciones").HasMaxLength(500);
            e.HasOne(x => x.Tanque).WithMany().HasForeignKey(x => x.TanqueId);
        });

        // ---- ticket -------------------------------------------------------
        m.Entity<Ticket>(e =>
        {
            e.ToTable("ticket");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SolicitudId).HasColumnName("solicitud_id");
            e.Property(x => x.EstacionId).HasColumnName("estacion_id");
            e.Property(x => x.TipoCombustibleId).HasColumnName("tipo_combustible_id");
            e.Property(x => x.NumeroTicket).HasColumnName("numero_ticket").HasMaxLength(25);
            e.Property(x => x.CantidadAutorizada).HasColumnName("cantidad_autorizada").HasPrecision(10, 2);
            e.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(25);
            e.Property(x => x.FechaEmision).HasColumnName("fecha_emision");
            e.Property(x => x.FechaExpiracion).HasColumnName("fecha_expiracion");
            e.Property(x => x.FechaEnvio).HasColumnName("fecha_envio");
            e.Property(x => x.FechaAnulacion).HasColumnName("fecha_anulacion");
        });

        // ---- movimiento_inventario ----------------------------------------
        m.Entity<MovimientoInventario>(e =>
        {
            e.ToTable("movimiento_inventario");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TanqueId).HasColumnName("tanque_id");
            e.Property(x => x.RegistradoPorUsuarioId).HasColumnName("registrado_por_usuario_id");
            e.Property(x => x.TipoMovimiento).HasColumnName("tipo_movimiento").HasMaxLength(30);
            e.Property(x => x.Cantidad).HasColumnName("cantidad").HasPrecision(12, 2);
            e.Property(x => x.SaldoAnterior).HasColumnName("saldo_anterior").HasPrecision(12, 2);
            e.Property(x => x.SaldoPosterior).HasColumnName("saldo_posterior").HasPrecision(12, 2);
            e.Property(x => x.RecepcionId).HasColumnName("recepcion_id");
            e.Property(x => x.DespachoId).HasColumnName("despacho_id");
            e.Property(x => x.TransferenciaId).HasColumnName("transferencia_id");
            e.Property(x => x.AjusteId).HasColumnName("ajuste_id");
            e.Property(x => x.FechaMovimiento).HasColumnName("fecha_movimiento");
            e.Property(x => x.Observaciones).HasColumnName("observaciones").HasMaxLength(500);
            e.HasOne(x => x.Tanque).WithMany().HasForeignKey(x => x.TanqueId);
        });

        // ---- vistas (read-only, sin PK de BDs) ----------------------------
        m.Entity<VwCierresResumen>(e =>
        {
            e.ToView("vw_cierres_resumen");
            e.HasKey(x => x.CierreId);
            e.Property(x => x.CierreId).HasColumnName("cierre_id");
            e.Property(x => x.FechaCierre).HasColumnName("fecha_cierre");
            e.Property(x => x.Estado).HasColumnName("estado");
            e.Property(x => x.TanqueId).HasColumnName("tanque_id");
            e.Property(x => x.TanqueCodigo).HasColumnName("tanque_codigo");
            e.Property(x => x.TanqueNombre).HasColumnName("tanque_nombre");
            e.Property(x => x.EstacionId).HasColumnName("estacion_id");
            e.Property(x => x.EstacionCodigo).HasColumnName("estacion_codigo");
            e.Property(x => x.EstacionNombre).HasColumnName("estacion_nombre");
            e.Property(x => x.TipoCombustibleId).HasColumnName("tipo_combustible_id");
            e.Property(x => x.CombustibleCodigo).HasColumnName("combustible_codigo");
            e.Property(x => x.CombustibleNombre).HasColumnName("combustible_nombre");
            e.Property(x => x.StockInicial).HasColumnName("stock_inicial");
            e.Property(x => x.TotalRecepciones).HasColumnName("total_recepciones");
            e.Property(x => x.TotalTransferenciasEntrada).HasColumnName("total_transferencias_entrada");
            e.Property(x => x.TotalTransferenciasSalida).HasColumnName("total_transferencias_salida");
            e.Property(x => x.TotalDespachos).HasColumnName("total_despachos");
            e.Property(x => x.TotalAjustesPositivos).HasColumnName("total_ajustes_positivos");
            e.Property(x => x.TotalAjustesNegativos).HasColumnName("total_ajustes_negativos");
            e.Property(x => x.StockTeoricoFinal).HasColumnName("stock_teorico_final");
            e.Property(x => x.StockFisicoFinal).HasColumnName("stock_fisico_final");
            e.Property(x => x.Diferencia).HasColumnName("diferencia");
            e.Property(x => x.CreadoPorUsuarioId).HasColumnName("creado_por_usuario_id");
            e.Property(x => x.CreadoPor).HasColumnName("creado_por");
            e.Property(x => x.RevisadoPorUsuarioId).HasColumnName("revisado_por_usuario_id");
            e.Property(x => x.RevisadoPor).HasColumnName("revisado_por");
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
            e.Property(x => x.FechaRevision).HasColumnName("fecha_revision");
            e.Property(x => x.MotivoDiferencia).HasColumnName("motivo_diferencia");
            e.Property(x => x.MotivoRechazo).HasColumnName("motivo_rechazo");
            e.Property(x => x.Observaciones).HasColumnName("observaciones");
        });

        m.Entity<VwStockDisponible>(e =>
        {
            e.ToView("vw_stock_disponible_estacion_combustible");
            e.HasNoKey();
            e.Property(x => x.EstacionId).HasColumnName("estacion_id");
            e.Property(x => x.EstacionCodigo).HasColumnName("estacion_codigo");
            e.Property(x => x.EstacionNombre).HasColumnName("estacion_nombre");
            e.Property(x => x.TipoCombustibleId).HasColumnName("tipo_combustible_id");
            e.Property(x => x.CombustibleCodigo).HasColumnName("combustible_codigo");
            e.Property(x => x.CombustibleNombre).HasColumnName("combustible_nombre");
            e.Property(x => x.StockFisico).HasColumnName("stock_fisico");
            e.Property(x => x.StockReservado).HasColumnName("stock_reservado");
            e.Property(x => x.StockDisponible).HasColumnName("stock_disponible");
        });

        m.Entity<VwMovimientosTanque>(e =>
        {
            e.ToView("vw_movimientos_tanque");
            e.HasNoKey();
            e.Property(x => x.MovimientoId).HasColumnName("movimiento_id");
            e.Property(x => x.TanqueId).HasColumnName("tanque_id");
            e.Property(x => x.TanqueCodigo).HasColumnName("tanque_codigo");
            e.Property(x => x.TanqueNombre).HasColumnName("tanque_nombre");
            e.Property(x => x.EstacionId).HasColumnName("estacion_id");
            e.Property(x => x.EstacionCodigo).HasColumnName("estacion_codigo");
            e.Property(x => x.EstacionNombre).HasColumnName("estacion_nombre");
            e.Property(x => x.TipoCombustibleId).HasColumnName("tipo_combustible_id");
            e.Property(x => x.CombustibleCodigo).HasColumnName("combustible_codigo");
            e.Property(x => x.CombustibleNombre).HasColumnName("combustible_nombre");
            e.Property(x => x.TipoMovimiento).HasColumnName("tipo_movimiento");
            e.Property(x => x.Cantidad).HasColumnName("cantidad");
            e.Property(x => x.SaldoAnterior).HasColumnName("saldo_anterior");
            e.Property(x => x.SaldoPosterior).HasColumnName("saldo_posterior");
            e.Property(x => x.RegistradoPorUsuarioId).HasColumnName("registrado_por_usuario_id");
            e.Property(x => x.RegistradoPor).HasColumnName("registrado_por");
            e.Property(x => x.RecepcionId).HasColumnName("recepcion_id");
            e.Property(x => x.DespachoId).HasColumnName("despacho_id");
            e.Property(x => x.TransferenciaId).HasColumnName("transferencia_id");
            e.Property(x => x.AjusteId).HasColumnName("ajuste_id");
            e.Property(x => x.FechaMovimiento).HasColumnName("fecha_movimiento");
            e.Property(x => x.Observaciones).HasColumnName("observaciones");
        });

        m.Entity<VwTicketsOperativos>(e =>
        {
            e.ToView("vw_tickets_operativos");
            e.HasNoKey();
            e.Property(x => x.TicketId).HasColumnName("ticket_id");
            e.Property(x => x.NumeroTicket).HasColumnName("numero_ticket");
            e.Property(x => x.SolicitudId).HasColumnName("solicitud_id");
            e.Property(x => x.EmpleadoId).HasColumnName("empleado_id");
            e.Property(x => x.CodigoEmpleado).HasColumnName("codigo_empleado");
            e.Property(x => x.EmpleadoNombre).HasColumnName("empleado_nombre");
            e.Property(x => x.EmpleadoApellido).HasColumnName("empleado_apellido");
            e.Property(x => x.VehiculoId).HasColumnName("vehiculo_id");
            e.Property(x => x.Placa).HasColumnName("placa");
            e.Property(x => x.EstacionId).HasColumnName("estacion_id");
            e.Property(x => x.EstacionCodigo).HasColumnName("estacion_codigo");
            e.Property(x => x.EstacionNombre).HasColumnName("estacion_nombre");
            e.Property(x => x.TipoCombustibleId).HasColumnName("tipo_combustible_id");
            e.Property(x => x.CombustibleCodigo).HasColumnName("combustible_codigo");
            e.Property(x => x.CantidadAutorizada).HasColumnName("cantidad_autorizada");
            e.Property(x => x.EstadoAlmacenado).HasColumnName("estado_almacenado");
            e.Property(x => x.EstadoEfectivo).HasColumnName("estado_efectivo");
            e.Property(x => x.FechaEmision).HasColumnName("fecha_emision");
            e.Property(x => x.FechaExpiracion).HasColumnName("fecha_expiracion");
        });

        m.Entity<FnCalcCierreRow>(e =>
        {
            e.HasNoKey();
            e.ToFunction("fn_calcular_cierre_diario");
        });
    }
}
