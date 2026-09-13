namespace Tickets.Sandbox.Api.Application.Contracts;

public record TicketPdfModel(
    string Number,
    string Status,
    string EmployeeName,
    string EmployeeNumber,
    string VehiclePlate,
    string DepartmentName,
    string FuelTypeName,
    decimal AuthorizedQuantity,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    byte[] QrImageBytes
);

public interface IPdfGeneratorService
{
    byte[] GenerateTicketPdf(TicketPdfModel model);
}
