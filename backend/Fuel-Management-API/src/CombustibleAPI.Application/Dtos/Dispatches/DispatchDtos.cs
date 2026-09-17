using System.ComponentModel.DataAnnotations;

namespace CombustibleAPI.Application.Dtos.Dispatches;

/// <summary>
/// Payload que envía la PWA a POST /dispatches (RN-08).
/// </summary>
public class DispatchRequestDto
{
    [Required] public Guid TicketId { get; set; }
    [Required] public Guid TanqueId { get; set; }

    [Range(typeof(decimal), "0.01", "999999", ErrorMessage = "Los galones deben ser mayores a cero.")]
    public decimal Galones { get; set; }

    [Range(typeof(decimal), "0", "99999999", ErrorMessage = "El odómetro no puede ser negativo.")]
    public decimal Odometro { get; set; }

    [MaxLength(500)]
    public string? Observacion { get; set; }
}

public class DispatchResultDto
{
    public Guid DespachoId { get; set; }
    public Guid TicketId { get; set; }
    public string NumeroTicket { get; set; } = default!;
    public decimal GalonesDespachados { get; set; }
    public decimal SaldoResultanteTanque { get; set; }
    public DateTime FechaHora { get; set; }
    public string Estado { get; set; } = "COMPLETADO";
}

public class DispatchDetailDto
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public string NumeroTicket { get; set; } = default!;
    public Guid TanqueId { get; set; }
    public string TanqueCodigo { get; set; } = default!;
    public string? TanqueNombre { get; set; }
    public Guid EstacionId { get; set; }
    public string EstacionNombre { get; set; } = default!;
    public Guid DespachadorUsuarioId { get; set; }
    public string DespachadorNombre { get; set; } = default!;
    public Guid? VehiculoId { get; set; }
    public string? VehiculoPlaca { get; set; }
    public string? VehiculoFicha { get; set; }
    public string? EmpleadoNombre { get; set; }
    public decimal CantidadDespachada { get; set; }
    public decimal Galones => CantidadDespachada;
    public decimal OdometroRegistrado { get; set; }
    public decimal Odometro => OdometroRegistrado;
    public DateTime FechaDespacho { get; set; }
    public DateTime FechaHora => FechaDespacho;
    public string? Observaciones { get; set; }
    public string? DireccionIp { get; set; }
}

public class DispatchesFilterDto
{
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public Guid? EstacionId { get; set; }
    public string? Ticket { get; set; }
    public string? Vehiculo { get; set; } // placa o ficha
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PaginatedList<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public PaginatedList() { }

    public PaginatedList(List<T> items, int count, int page, int pageSize)
    {
        Items = items;
        TotalCount = count;
        Page = page;
        PageSize = pageSize;
    }
}
