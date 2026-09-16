using CombustibleAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CombustibleAPI.Infrastructure.Persistence.Configurations;

public class DepartamentoConfig : IEntityTypeConfiguration<Departamento>
{
    public void Configure(EntityTypeBuilder<Departamento> b)
    {
        b.ToTable("departamento");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(20).IsRequired();
        b.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
        b.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(250);
        b.Property(x => x.Activo).HasColumnName("activo");
        b.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
        b.Property(x => x.FechaActualizacion).HasColumnName("fecha_actualizacion");

        b.HasIndex(x => x.Codigo).IsUnique();
        b.HasIndex(x => x.Nombre).IsUnique();
    }
}

public class EmpleadoConfig : IEntityTypeConfiguration<Empleado>
{
    public void Configure(EntityTypeBuilder<Empleado> b)
    {
        b.ToTable("empleado");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.DepartamentoId).HasColumnName("departamento_id");
        b.Property(x => x.CodigoEmpleado).HasColumnName("codigo_empleado").HasMaxLength(30).IsRequired();
        b.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(80).IsRequired();
        b.Property(x => x.Apellido).HasColumnName("apellido").HasMaxLength(80).IsRequired();
        b.Property(x => x.Cedula).HasColumnName("cedula").HasMaxLength(20).IsRequired();
        b.Property(x => x.Cargo).HasColumnName("cargo").HasMaxLength(100);
        b.Property(x => x.Email).HasColumnName("email").HasMaxLength(150);
        b.Property(x => x.Telefono).HasColumnName("telefono").HasMaxLength(25);
        b.Property(x => x.Activo).HasColumnName("activo");
        b.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
        b.Property(x => x.FechaActualizacion).HasColumnName("fecha_actualizacion");

        b.Ignore(x => x.NombreCompleto);

        b.HasIndex(x => x.CodigoEmpleado).IsUnique();
        b.HasIndex(x => x.Cedula).IsUnique();
        b.HasIndex(x => x.Email).IsUnique();
        b.HasOne(x => x.Departamento).WithMany().HasForeignKey(x => x.DepartamentoId);
    }
}

public class TipoCombustibleConfig : IEntityTypeConfiguration<TipoCombustible>
{
    public void Configure(EntityTypeBuilder<TipoCombustible> b)
    {
        b.ToTable("tipo_combustible");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(20).IsRequired();
        b.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(50).IsRequired();
        b.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(150);
        b.Property(x => x.Activo).HasColumnName("activo");
        b.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");

        b.HasIndex(x => x.Codigo).IsUnique();
        b.HasIndex(x => x.Nombre).IsUnique();
    }
}

public class VehiculoConfig : IEntityTypeConfiguration<Vehiculo>
{
    public void Configure(EntityTypeBuilder<Vehiculo> b)
    {
        b.ToTable("vehiculo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.DepartamentoId).HasColumnName("departamento_id");
        b.Property(x => x.TipoCombustibleId).HasColumnName("tipo_combustible_id");
        b.Property(x => x.Placa).HasColumnName("placa").HasMaxLength(20).IsRequired();
        b.Property(x => x.Ficha).HasColumnName("ficha").HasMaxLength(30).IsRequired();
        b.Property(x => x.Marca).HasColumnName("marca").HasMaxLength(50);
        b.Property(x => x.Modelo).HasColumnName("modelo").HasMaxLength(50);
        b.Property(x => x.Anio).HasColumnName("anio");
        b.Property(x => x.TipoVehiculo).HasColumnName("tipo_vehiculo").HasMaxLength(50);
        b.Property(x => x.CapacidadTanque).HasColumnName("capacidad_tanque").HasColumnType("numeric(10,2)");
        b.Property(x => x.OdometroActual).HasColumnName("odometro_actual").HasColumnType("numeric(12,2)");
        b.Property(x => x.Activo).HasColumnName("activo");
        b.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
        b.Property(x => x.FechaActualizacion).HasColumnName("fecha_actualizacion");

        b.HasIndex(x => x.Placa).IsUnique();
        b.HasIndex(x => x.Ficha).IsUnique();
        b.HasOne(x => x.Departamento).WithMany().HasForeignKey(x => x.DepartamentoId);
        b.HasOne(x => x.TipoCombustible).WithMany().HasForeignKey(x => x.TipoCombustibleId);
    }
}

public class EstacionConfig : IEntityTypeConfiguration<Estacion>
{
    public void Configure(EntityTypeBuilder<Estacion> b)
    {
        b.ToTable("estacion");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(20).IsRequired();
        b.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
        b.Property(x => x.Ubicacion).HasColumnName("ubicacion").HasMaxLength(200);
        b.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(250);
        b.Property(x => x.Activo).HasColumnName("activo");
        b.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
        b.Property(x => x.FechaActualizacion).HasColumnName("fecha_actualizacion");

        b.HasIndex(x => x.Codigo).IsUnique();
        b.HasIndex(x => x.Nombre).IsUnique();
    }
}

public class TanqueConfig : IEntityTypeConfiguration<Tanque>
{
    public void Configure(EntityTypeBuilder<Tanque> b)
    {
        b.ToTable("tanque");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.EstacionId).HasColumnName("estacion_id");
        b.Property(x => x.TipoCombustibleId).HasColumnName("tipo_combustible_id");
        b.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(30).IsRequired();
        b.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(100);
        b.Property(x => x.CapacidadMaxima).HasColumnName("capacidad_maxima").HasColumnType("numeric(12,2)");
        b.Property(x => x.StockActual).HasColumnName("stock_actual").HasColumnType("numeric(12,2)");
        b.Property(x => x.NivelCritico).HasColumnName("nivel_critico").HasColumnType("numeric(12,2)");
        b.Property(x => x.Activo).HasColumnName("activo");
        b.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
        b.Property(x => x.FechaActualizacion).HasColumnName("fecha_actualizacion");

        b.HasIndex(x => x.Codigo).IsUnique();
        b.HasOne(x => x.Estacion).WithMany(e => e.Tanques).HasForeignKey(x => x.EstacionId);
        b.HasOne(x => x.TipoCombustible).WithMany().HasForeignKey(x => x.TipoCombustibleId);
    }
}

public class ProveedorConfig : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> b)
    {
        b.ToTable("proveedor");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Rnc).HasColumnName("rnc").HasMaxLength(20).IsRequired();
        b.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(150).IsRequired();
        b.Property(x => x.NombreComercial).HasColumnName("nombre_comercial").HasMaxLength(150);
        b.Property(x => x.Email).HasColumnName("email").HasMaxLength(150);
        b.Property(x => x.Telefono).HasColumnName("telefono").HasMaxLength(25);
        b.Property(x => x.Direccion).HasColumnName("direccion").HasMaxLength(250);
        b.Property(x => x.Activo).HasColumnName("activo");
        b.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
        b.Property(x => x.FechaActualizacion).HasColumnName("fecha_actualizacion");

        b.HasIndex(x => x.Rnc).IsUnique();
    }
}
