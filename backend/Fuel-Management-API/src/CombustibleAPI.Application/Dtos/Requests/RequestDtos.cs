using System.ComponentModel.DataAnnotations;
using CombustibleAPI.Application.Dtos.Dispatches;

namespace CombustibleAPI.Application.Dtos.Requests;

public class CreateRequestDto
{
    [Required] public Guid EmpleadoId { get; set; }
    [Required] public Guid VehiculoId { get; set; }
    [Required] public Guid DepartamentoId { get; set; }
    [Range(typeof(decimal), "0.01", "999999")] public decimal CantidadSolicitada { get; set; }
    [MaxLength(20)] public string TipoSolicitud { get; set; } = "MANUAL";
    [MaxLength(500)] public string? Observaciones { get; set; }
}

public class UpdateRequestDto
{
    [Range(typeof(decimal), "0.01", "999999")] public decimal? CantidadSolicitada { get; set; }
    [MaxLength(500)] public string? Observaciones { get; set; }
}

public class ApproveRequestDto
{
    [Required] public Guid EstacionId { get; set; }
    [Range(typeof(decimal), "0.01", "999999")] public decimal CantidadAutorizada { get; set; }
    public DateTime FechaExpiracion { get; set; }
    [MaxLength(500)] public string? Observaciones { get; set; }
}

public class RejectRequestDto { [Required, MinLength(3), MaxLength(300)] public string Motivo { get; set; } = string.Empty; }
public class CancelRequestDto { [MaxLength(300)] public string? Motivo { get; set; } }

public class RequestFilterDto
{
    public string? Estado { get; set; }
    public Guid? EmpleadoId { get; set; }
    public Guid? VehiculoId { get; set; }
    public Guid? DepartamentoId { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class RequestResponseDto
{
    public Guid Id { get; set; }
    public Guid EmpleadoId { get; set; }
    public string? EmpleadoNombre { get; set; }
    public Guid VehiculoId { get; set; }
    public string? VehiculoPlaca { get; set; }
    public string? VehiculoFicha { get; set; }
    public Guid DepartamentoId { get; set; }
    public string? DepartamentoNombre { get; set; }
    public short TipoCombustibleId { get; set; }
    public string? TipoCombustible { get; set; }
    public decimal CantidadSolicitada { get; set; }
    public decimal? CantidadAutorizada { get; set; }
    public string Estado { get; set; } = default!;
    public string TipoSolicitud { get; set; } = default!;
    public DateTime FechaSolicitud { get; set; }
    public DateTime? FechaRevision { get; set; }
    public string? MotivoRechazo { get; set; }
    public string? MotivoCancelacion { get; set; }
    public string? Observaciones { get; set; }
    public Guid? TicketId { get; set; }
    public string? NumeroTicket { get; set; }
}
