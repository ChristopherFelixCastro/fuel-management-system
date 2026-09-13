using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Tickets.Sandbox.Api.Application.Contracts;

namespace Tickets.Sandbox.Api.Infrastructure.Services;

public class QuestPdfTicketGenerator : IPdfGeneratorService
{
    static QuestPdfTicketGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateTicketPdf(TicketPdfModel model)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header()
                    .Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("SISTEMA DE GESTIÓN DE COMBUSTIBLE").Bold().FontSize(14).FontColor(Colors.Blue.Darken3);
                            col.Item().Text("TICKET DIGITAL DE COMBUSTIBLE").SemiBold().FontSize(12).FontColor(Colors.Grey.Darken2);
                        });

                        row.ConstantItem(140).Column(col =>
                        {
                            col.Item().AlignRight().Text($"TICKET: {model.Number}").Bold().FontSize(11).FontColor(Colors.Blue.Darken2);
                            col.Item().AlignRight().Text($"ESTADO: {model.Status}").Bold().FontColor(model.Status == "ACTIVO" ? Colors.Green.Darken2 : Colors.Red.Darken2);
                        });
                    });

                page.Content()
                    .PaddingVertical(10)
                    .Row(row =>
                    {
                        // Columna Izquierda: Información del Ticket y Beneficiario
                        row.RelativeItem(3).Column(col =>
                        {
                            col.Spacing(6);

                            col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(2).Text("DETALLES DE LA AUTORIZACIÓN").Bold().FontSize(10);

                            col.Item().Row(r =>
                            {
                                r.ConstantItem(120).Text("Empleado:").Bold();
                                r.RelativeItem().Text($"{model.EmployeeName} ({model.EmployeeNumber})");
                            });

                            col.Item().Row(r =>
                            {
                                r.ConstantItem(120).Text("Vehículo / Placa:").Bold();
                                r.RelativeItem().Text(model.VehiclePlate);
                            });

                            col.Item().Row(r =>
                            {
                                r.ConstantItem(120).Text("Departamento:").Bold();
                                r.RelativeItem().Text(model.DepartmentName);
                            });

                            col.Item().Row(r =>
                            {
                                r.ConstantItem(120).Text("Tipo Combustible:").Bold();
                                r.RelativeItem().Text(model.FuelTypeName);
                            });

                            col.Item().Row(r =>
                            {
                                r.ConstantItem(120).Text("Cantidad Autorizada:").Bold();
                                r.RelativeItem().Text($"{model.AuthorizedQuantity:N2} Galones").Bold().FontColor(Colors.Blue.Darken3);
                            });

                            col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(2).PaddingTop(4).Text("VIGENCIA").Bold().FontSize(10);

                            col.Item().Row(r =>
                            {
                                r.ConstantItem(120).Text("Fecha Emisión:").Bold();
                                r.RelativeItem().Text(model.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"));
                            });

                            col.Item().Row(r =>
                            {
                                r.ConstantItem(120).Text("Fecha Vencimiento:").Bold();
                                r.RelativeItem().Text(model.ExpiresAt.ToString("dd/MM/yyyy HH:mm:ss")).Bold().FontColor(Colors.Red.Darken2);
                            });
                        });

                        // Columna Derecha: Código QR de Validación
                        row.RelativeItem(2).Column(col =>
                        {
                            col.Item().AlignCenter().Text("CÓDIGO QR OFICIAL").Bold().FontSize(9);
                            if (model.QrImageBytes is { Length: > 0 })
                            {
                                col.Item().AlignCenter().PaddingTop(5).Width(130).Height(130).Image(model.QrImageBytes);
                            }
                            col.Item().AlignCenter().PaddingTop(4).Text("Escanee en estación para validación").FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                        });
                    });

                page.Footer()
                    .AlignCenter()
                    .Text("Este ticket es único, intransferible y debe ser validado en línea por el despachador. Generado por FMS API.")
                    .FontSize(7)
                    .FontColor(Colors.Grey.Darken1);
            });
        });

        return document.GeneratePdf();
    }
}
