using Microsoft.EntityFrameworkCore;
using Inventario.Api.Models;

namespace Inventario.Api.Data
{
    public class InventoryMockContext : DbContext
    {
        public InventoryMockContext(DbContextOptions<InventoryMockContext> options)
            : base(options) { }

        public DbSet<Estacion> Estaciones => Set<Estacion>();
        public DbSet<Tanque> Tanques => Set<Tanque>();
        public DbSet<Proveedor> Proveedores => Set<Proveedor>();
        public DbSet<RecepcionCombustible> Recepciones => Set<RecepcionCombustible>();
        public DbSet<MovimientoInventario> Movimientos => Set<MovimientoInventario>();
        public DbSet<TransferenciaInventario> Transferencias => Set<TransferenciaInventario>();
        public DbSet<AjusteInventario> Ajustes => Set<AjusteInventario>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var ahora = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

            var estacion1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var estacion2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

            var tanque1Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
            var tanque2Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");
            var tanque3Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3");

            var proveedor1Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
            var proveedor2Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");

            modelBuilder.Entity<Estacion>().HasData(
                new Estacion { Id = estacion1Id, Codigo = "EST-01", Nombre = "Estación Centro", Activo = true, FechaCreacion = ahora, FechaActualizacion = ahora },
                new Estacion { Id = estacion2Id, Codigo = "EST-02", Nombre = "Estación Norte", Activo = true, FechaCreacion = ahora, FechaActualizacion = ahora }
            );

            modelBuilder.Entity<Tanque>().HasData(
                new Tanque { Id = tanque1Id, EstacionId = estacion1Id, TipoCombustibleId = 1, Codigo = "T-01", Nombre = "Tanque 1", CapacidadMaxima = 5000, StockActual = 4200, NivelCritico = 500, Activo = true, FechaCreacion = ahora, FechaActualizacion = ahora },
                new Tanque { Id = tanque2Id, EstacionId = estacion1Id, TipoCombustibleId = 1, Codigo = "T-02", Nombre = "Tanque 2", CapacidadMaxima = 3000, StockActual = 300, NivelCritico = 400, Activo = true, FechaCreacion = ahora, FechaActualizacion = ahora }, // cerca del crítico
                new Tanque { Id = tanque3Id, EstacionId = estacion2Id, TipoCombustibleId = 2, Codigo = "T-03", Nombre = "Tanque 3", CapacidadMaxima = 6000, StockActual = 5000, NivelCritico = 600, Activo = true, FechaCreacion = ahora, FechaActualizacion = ahora }
            );

            modelBuilder.Entity<Proveedor>().HasData(
                new Proveedor { Id = proveedor1Id, Rnc = "101000001", Nombre = "Proveedor Uno", Activo = true, FechaCreacion = ahora, FechaActualizacion = ahora },
                new Proveedor { Id = proveedor2Id, Rnc = "101000002", Nombre = "Proveedor Dos", Activo = true, FechaCreacion = ahora, FechaActualizacion = ahora }
            );
        }
    }
}