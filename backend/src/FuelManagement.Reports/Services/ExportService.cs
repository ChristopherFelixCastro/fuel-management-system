using ClosedXML.Excel;
using CsvHelper;
using FuelManagement.Reports.Dtos;
using FuelManagement.Shared.Data;
using FuelManagement.Shared.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace FuelManagement.Reports.Services;

public class ExportService : IExportService
{
    private readonly IReportService _reportService;
    private readonly FuelDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public ExportService(
        IReportService reportService,
        FuelDbContext db,
        ICurrentUserService currentUserService)
    {
        _reportService = reportService;
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<(byte[] FileBytes, string ContentType, string FileName)> ExportAsync(
        string reportType,
        string format,
        Guid? tanqueId,
        DateOnly? fechaDesde,
        DateOnly? fechaHasta,
        string? equipoCodigo,
        CancellationToken ct = default)
    {
        var normReport = reportType.ToLowerInvariant();
        var normFormat = format.ToLowerInvariant();

        byte[] bytes;
        string contentType;
        string fileName = $"reporte-{normReport}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{normFormat}";

        if (normReport == "consumption")
        {
            var filter = new ConsumptionReportFilter { TanqueId = tanqueId, FechaDesde = fechaDesde, FechaHasta = fechaHasta, EquipoCodigo = equipoCodigo };
            var data = await _reportService.GetConsumptionDataAsync(filter, ct);

            (bytes, contentType) = normFormat switch
            {
                "pdf" => (GenerateConsumptionPdf(data), "application/pdf"),
                "xlsx" => (GenerateConsumptionXlsx(data), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
                "csv" => (GenerateCsv(data), "text/csv"),
                _ => throw new ArgumentException($"Formato '{format}' no soportado")
            };
        }
        else if (normReport == "inventory")
        {
            var filter = new InventoryReportFilter { TanqueId = tanqueId };
            var data = await _reportService.GetInventoryDataAsync(filter, ct);

            (bytes, contentType) = normFormat switch
            {
                "pdf" => (GenerateInventoryPdf(data), "application/pdf"),
                "xlsx" => (GenerateInventoryXlsx(data), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
                "csv" => (GenerateCsv(data), "text/csv"),
                _ => throw new ArgumentException($"Formato '{format}' no soportado")
            };
        }
        else if (normReport == "traceability")
        {
            var filter = new TraceabilityReportFilter { TanqueId = tanqueId, FechaDesde = fechaDesde, FechaHasta = fechaHasta };
            var data = await _reportService.GetTraceabilityDataAsync(filter, ct);

            (bytes, contentType) = normFormat switch
            {
                "pdf" => (GenerateTraceabilityPdf(data), "application/pdf"),
                "xlsx" => (GenerateTraceabilityXlsx(data), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
                "csv" => (GenerateCsv(data), "text/csv"),
                _ => throw new ArgumentException($"Formato '{format}' no soportado")
            };
        }
        else
        {
            throw new ArgumentException($"Tipo de reporte '{reportType}' no reconocido");
        }

        // Registrar auditoría
        try
        {
            await _db.Database.ExecuteSqlRawAsync(
                "SELECT fn_registrar_auditoria({0}, {1}, {2}, {3}, {4}, {5})",
                _currentUserService.UserId,
                _currentUserService.UserName,
                "EXPORT_GENERATED",
                "REPORTES",
                Guid.Empty,
                $"{{\"reporte\":\"{normReport}\",\"formato\":\"{normFormat}\",\"filas\":{bytes.Length}}}"
            );
        }
        catch
        {
            // audit non-blocking
        }

        return (bytes, contentType, fileName);
    }

    // --- CSV GENERATOR ---
    private static byte[] GenerateCsv<T>(IEnumerable<T> records)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(records);
        writer.Flush();
        return stream.ToArray();
    }

    // --- EXCEL GENERATORS ---
    private static byte[] GenerateConsumptionXlsx(List<Shared.Domain.VwConsumoDiario> data)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Consumo Diario");

        ws.Cell(1, 1).Value = "Fecha";
        ws.Cell(1, 2).Value = "Tanque";
        ws.Cell(1, 3).Value = "Combustible";
        ws.Cell(1, 4).Value = "Total Despachos (L)";
        ws.Cell(1, 5).Value = "Nº Despachos";

        var headerRow = ws.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
        headerRow.Style.Font.FontColor = XLColor.White;

        for (int i = 0; i < data.Count; i++)
        {
            var r = data[i];
            ws.Cell(i + 2, 1).Value = r.Fecha.ToString("yyyy-MM-dd");
            ws.Cell(i + 2, 2).Value = r.TanqueNombre;
            ws.Cell(i + 2, 3).Value = r.CombustibleTipo;
            ws.Cell(i + 2, 4).Value = r.TotalDespachadoLitros;
            ws.Cell(i + 2, 5).Value = r.CantidadDespachos;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static byte[] GenerateInventoryXlsx(List<Shared.Domain.VwTanqueResumen> data)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Resumen Inventario");

        ws.Cell(1, 1).Value = "Tanque";
        ws.Cell(1, 2).Value = "Código";
        ws.Cell(1, 3).Value = "Combustible";
        ws.Cell(1, 4).Value = "Capacidad (L)";
        ws.Cell(1, 5).Value = "Stock Actual (L)";
        ws.Cell(1, 6).Value = "Ocupación (%)";
        ws.Cell(1, 7).Value = "Estado";

        var headerRow = ws.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
        headerRow.Style.Font.FontColor = XLColor.White;

        for (int i = 0; i < data.Count; i++)
        {
            var r = data[i];
            ws.Cell(i + 2, 1).Value = r.TanqueNombre;
            ws.Cell(i + 2, 2).Value = r.Codigo;
            ws.Cell(i + 2, 3).Value = r.CombustibleTipo;
            ws.Cell(i + 2, 4).Value = r.CapacidadTotal;
            ws.Cell(i + 2, 5).Value = r.StockActual;
            ws.Cell(i + 2, 6).Value = r.PorcentajeOcupacion;
            ws.Cell(i + 2, 7).Value = r.EstadoStock;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static byte[] GenerateTraceabilityXlsx(List<Shared.Domain.VwMovimientosTanque> data)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Trazabilidad");

        ws.Cell(1, 1).Value = "Fecha/Hora";
        ws.Cell(1, 2).Value = "Tanque";
        ws.Cell(1, 3).Value = "Combustible";
        ws.Cell(1, 4).Value = "Tipo Movimiento";
        ws.Cell(1, 5).Value = "Cantidad (L)";
        ws.Cell(1, 6).Value = "Stock Anterior (L)";
        ws.Cell(1, 7).Value = "Stock Resultante (L)";
        ws.Cell(1, 8).Value = "Origen";

        var headerRow = ws.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
        headerRow.Style.Font.FontColor = XLColor.White;

        for (int i = 0; i < data.Count; i++)
        {
            var r = data[i];
            ws.Cell(i + 2, 1).Value = r.FechaMovimiento.ToString("yyyy-MM-dd HH:mm");
            ws.Cell(i + 2, 2).Value = r.TanqueNombre ?? r.TanqueCodigo;
            ws.Cell(i + 2, 3).Value = r.CombustibleNombre;
            ws.Cell(i + 2, 4).Value = r.TipoMovimiento;
            ws.Cell(i + 2, 5).Value = r.Cantidad;
            ws.Cell(i + 2, 6).Value = r.SaldoAnterior;
            ws.Cell(i + 2, 7).Value = r.SaldoPosterior;
            ws.Cell(i + 2, 8).Value = GetOrigen(r);
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // --- PDF GENERATORS (QuestPDF) ---
    private static byte[] GenerateConsumptionPdf(List<Shared.Domain.VwConsumoDiario> data)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.Header().Text("REPORTE DE CONSUMO DIARIO DE COMBUSTIBLE")
                    .FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);
                        c.RelativeColumn(3);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("Fecha").Bold();
                        h.Cell().Text("Tanque").Bold();
                        h.Cell().Text("Combustible").Bold();
                        h.Cell().AlignRight().Text("Total Despachado (L)").Bold();
                    });
                    foreach (var row in data)
                    {
                        table.Cell().Text(row.Fecha.ToString("yyyy-MM-dd"));
                        table.Cell().Text(row.TanqueNombre ?? "-");
                        table.Cell().Text(row.CombustibleTipo ?? "-");
                        table.Cell().AlignRight().Text((row.TotalDespachadoLitros ?? 0m).ToString("N2"));
                    }
                });
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                });
            });
        });
        return doc.GeneratePdf();
    }

    private static byte[] GenerateInventoryPdf(List<Shared.Domain.VwTanqueResumen> data)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.Header().Text("REPORTE DE ESTADO DE INVENTARIO Y TANQUES")
                    .FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("Tanque").Bold();
                        h.Cell().Text("Combustible").Bold();
                        h.Cell().AlignRight().Text("Capacidad (L)").Bold();
                        h.Cell().AlignRight().Text("Stock (L)").Bold();
                        h.Cell().AlignRight().Text("Ocupación %").Bold();
                    });
                    foreach (var row in data)
                    {
                        table.Cell().Text(row.TanqueNombre);
                        table.Cell().Text(row.CombustibleTipo);
                        table.Cell().AlignRight().Text(row.CapacidadTotal.ToString("N0"));
                        table.Cell().AlignRight().Text(row.StockActual.ToString("N2"));
                        table.Cell().AlignRight().Text($"{row.PorcentajeOcupacion:F1}%");
                    }
                });
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                });
            });
        });
        return doc.GeneratePdf();
    }

    private static byte[] GenerateTraceabilityPdf(List<Shared.Domain.VwMovimientosTanque> data)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.Header().Text("REPORTE DE TRAZABILIDAD Y MOVIMIENTOS KARDEX")
                    .FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);
                        c.RelativeColumn(3);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("Fecha/Hora").Bold();
                        h.Cell().Text("Tanque").Bold();
                        h.Cell().Text("Combustible").Bold();
                        h.Cell().Text("Tipo").Bold();
                        h.Cell().AlignRight().Text("Cantidad (L)").Bold();
                        h.Cell().AlignRight().Text("Anterior/Posterior (L)").Bold();
                    });
                    foreach (var row in data)
                    {
                        table.Cell().Text(row.FechaMovimiento.ToString("yyyy-MM-dd HH:mm"));
                        table.Cell().Text(row.TanqueNombre ?? row.TanqueCodigo);
                        table.Cell().Text(row.CombustibleNombre);
                        table.Cell().Text(row.TipoMovimiento);
                        table.Cell().AlignRight().Text(row.Cantidad.ToString("N2"));
                        table.Cell().AlignRight().Text($"{row.SaldoAnterior:N2} / {row.SaldoPosterior:N2}");
                    }
                });
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                });
            });
        });
        return doc.GeneratePdf();
    }

    private static string GetOrigen(Shared.Domain.VwMovimientosTanque r)
    {
        if (r.DespachoId.HasValue) return "DESPACHO";
        if (r.RecepcionId.HasValue) return "RECEPCION";
        if (r.TransferenciaId.HasValue) return "TRANSFERENCIA";
        if (r.AjusteId.HasValue) return "AJUSTE";
        return "-";
    }
}
