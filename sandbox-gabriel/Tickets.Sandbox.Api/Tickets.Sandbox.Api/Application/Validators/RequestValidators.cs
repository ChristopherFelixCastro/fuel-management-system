using FluentValidation;
using Tickets.Sandbox.Api.Application.DTOs.Requests;
using Tickets.Sandbox.Api.Application.DTOs.Tickets;

namespace Tickets.Sandbox.Api.Application.Validators;

public class CreateRequestValidator : AbstractValidator<CreateRequestDto>
{
    public CreateRequestValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("El ID del empleado es obligatorio.");

        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("El ID del vehículo es obligatorio.");

        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("El ID del departamento es obligatorio.");

        RuleFor(x => x.FuelTypeId)
            .NotEmpty().WithMessage("El ID del tipo de combustible es obligatorio.");

        RuleFor(x => x.RequestedQuantity)
            .GreaterThan(0).WithMessage("La cantidad solicitada debe ser mayor a 0.");
    }
}

public class ApproveRequestValidator : AbstractValidator<ApproveRequestDto>
{
    public ApproveRequestValidator()
    {
        RuleFor(x => x.AuthorizedQuantity)
            .GreaterThan(0).WithMessage("La cantidad autorizada debe ser mayor a 0.");

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("La fecha de vencimiento debe ser posterior a la fecha actual.");
    }
}

public class ValidateTicketValidator : AbstractValidator<ValidateTicketRequestDto>
{
    public ValidateTicketValidator()
    {
        RuleFor(x => x.QrPayload)
            .NotEmpty().WithMessage("El payload del código QR es obligatorio.");

        RuleFor(x => x.StationId)
            .NotEmpty().WithMessage("El ID de la estación de despacho es obligatorio.");
    }
}
