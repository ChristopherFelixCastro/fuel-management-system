namespace CombustibleAPI.Application.Dtos.Masters;

public class RolDto
{
    public short Id { get; set; }
    public string Nombre { get; set; } = default!;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
}

public class EstacionDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = default!;
    public string Nombre { get; set; } = default!;
    public string? Ubicacion { get; set; }
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
}

public class VehiculoDto
{
    public Guid Id { get; set; }
    public string Placa { get; set; } = default!;
    public string Ficha { get; set; } = default!;
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public short? Anio { get; set; }
    public string? TipoVehiculo { get; set; }
    public decimal CapacidadTanque { get; set; }
    public decimal OdometroActual { get; set; }
    public short TipoCombustibleId { get; set; }
    public string? CombustibleNombre { get; set; }
    public Guid DepartamentoId { get; set; }
    public string? DepartamentoNombre { get; set; }
    public bool Activo { get; set; }
}

public class TanqueDto
{
    public Guid Id { get; set; }
    public Guid EstacionId { get; set; }
    public string? EstacionNombre { get; set; }
    public short TipoCombustibleId { get; set; }
    public string? CombustibleNombre { get; set; }
    public string Codigo { get; set; } = default!;
    public string? Nombre { get; set; }
    public decimal CapacidadMaxima { get; set; }
    public decimal StockActual { get; set; }
    public decimal NivelCritico { get; set; }
    public bool Activo { get; set; }
}

public class ProveedorDto
{
    public Guid Id { get; set; }
    public string Rnc { get; set; } = default!;
    public string Nombre { get; set; } = default!;
    public string? NombreComercial { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public bool Activo { get; set; }
}
