namespace CombustibleAPI.Domain.Entities;

public class Departamento
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = default!;
    public string Nombre { get; set; } = default!;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
}

public class Empleado
{
    public Guid Id { get; set; }
    public Guid DepartamentoId { get; set; }
    public Departamento Departamento { get; set; } = default!;
    public string CodigoEmpleado { get; set; } = default!;
    public string Nombre { get; set; } = default!;
    public string Apellido { get; set; } = default!;
    public string Cedula { get; set; } = default!;
    public string? Cargo { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }

    public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
}

public class TipoCombustible
{
    public short Id { get; set; }
    public string Codigo { get; set; } = default!; // GASOLINA, DIESEL
    public string Nombre { get; set; } = default!; // Gasolina, Diésel
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

public class Vehiculo
{
    public Guid Id { get; set; }
    public Guid DepartamentoId { get; set; }
    public Departamento Departamento { get; set; } = default!;

    /// <summary>Obligatorio: RN-02, el vehículo define el único combustible autorizado.</summary>
    public short TipoCombustibleId { get; set; }
    public TipoCombustible TipoCombustible { get; set; } = default!;

    public string Placa { get; set; } = default!;
    public string Ficha { get; set; } = default!;
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public short? Anio { get; set; }
    public string? TipoVehiculo { get; set; }
    public decimal CapacidadTanque { get; set; }
    public decimal OdometroActual { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
}

public class Estacion
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = default!;
    public string Nombre { get; set; } = default!;
    public string? Ubicacion { get; set; }
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }

    public ICollection<Tanque> Tanques { get; set; } = new List<Tanque>();
}

public class Tanque
{
    public Guid Id { get; set; }
    public Guid EstacionId { get; set; }
    public Estacion Estacion { get; set; } = default!;

    public short TipoCombustibleId { get; set; }
    public TipoCombustible TipoCombustible { get; set; } = default!;

    public string Codigo { get; set; } = default!;
    public string? Nombre { get; set; }
    public decimal CapacidadMaxima { get; set; }
    public decimal StockActual { get; set; }
    public decimal NivelCritico { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
}

public class Proveedor
{
    public Guid Id { get; set; }
    public string Rnc { get; set; } = default!;
    public string Nombre { get; set; } = default!;
    public string? NombreComercial { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
}
