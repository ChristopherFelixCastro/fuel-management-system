using CombustibleAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CombustibleAPI.Infrastructure.Persistence;

/// <summary>
/// DbContext mapeado exactamente al esquema físico de PostgreSQL de Christopher.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<TipoCombustible> TiposCombustible => Set<TipoCombustible>();
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<Estacion> Estaciones => Set<Estacion>();
    public DbSet<Tanque> Tanques => Set<Tanque>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();

    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Despacho> Despachos => Set<Despacho>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<CierreDiario> CierresDiarios => Set<CierreDiario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
