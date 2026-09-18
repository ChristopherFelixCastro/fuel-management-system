using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CombustibleAPI.Application.Dtos.Reports;
using CombustibleAPI.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CombustibleAPI.Infrastructure.Services;

public class ReportExportService : IReportExportService
{
    private readonly IReportService _reportService;

    public ReportExportService(IReportService reportService)
    {
        _reportService = reportService;

        // Proyecto académico/universitario.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<ReportExportResultDto> ExportConsumptionAsync(
        ConsumptionReportFilterDto filter,
        string format,
        CancellationToken ct)
    {
        var items = await GetAllConsumptionAsync(filter, ct);
        var normalizedFormat = NormalizeFormat(format);

        return normalizedFormat switch
        {
            "csv" => BuildConsumptionCsv(items),
            "xlsx" => BuildConsumptionXlsx(items),
            "pdf" => BuildConsumptionPdf(items),
            _ => throw new ArgumentException("Formato de exportación no soportado.")
        };
    }

    public async Task<ReportExportResultDto> ExportInventoryAsync(
        InventoryReportFilterDto filter,
        string format,
        CancellationToken ct)
    {
        var items = await GetAllInventoryAsync(filter, ct);
        var normalizedFormat = NormalizeFormat(format);

        return normalizedFormat switch
        {
            "csv" => BuildInventoryCsv(items),
            "xlsx" => BuildInventoryXlsx(items),
            "pdf" => BuildInventoryPdf(items),
            _ => throw new ArgumentException("Formato de exportación no soportado.")
        };
    }

    public async Task<ReportExportResultDto> ExportTraceabilityAsync(
        TraceabilityReportFilterDto filter,
        string format,
        CancellationToken ct)
    {
        var items = await GetAllTraceabilityAsync(filter, ct);
        var normalizedFormat = NormalizeFormat(format);

        return normalizedFormat switch
        {
            "csv" => BuildTraceabilityCsv(items),
            "xlsx" => BuildTraceabilityXlsx(items),
            "pdf" => BuildTraceabilityPdf(items),
            _ => throw new ArgumentException("Formato de exportación no soportado.")
        };
    }

    private async Task<List<ConsumptionReportItemDto>> GetAllConsumptionAsync(
        ConsumptionReportFilterDto filter,
        CancellationToken ct)
    {
        var items = new List<ConsumptionReportItemDto>();
        var page = 1;

        while (true)
        {
            var pageFilter = new ConsumptionReportFilterDto
            {
                FechaDesde = filter.FechaDesde,
                FechaHasta = filter.FechaHasta,
                TanqueId = filter.TanqueId,
                Page = page,
                PageSize = 100
            };

            var result = await _reportService.GetConsumptionAsync(pageFilter, ct);
            items.AddRange(result.Items);

            if (page >= result.TotalPages || result.Items.Count == 0)
                break;

            page++;
        }

        return items;
    }

    private async Task<List<InventoryReportItemDto>> GetAllInventoryAsync(
        InventoryReportFilterDto filter,
        CancellationToken ct)
    {
        var items = new List<InventoryReportItemDto>();
        var page = 1;

        while (true)
        {
            var pageFilter = new InventoryReportFilterDto
            {
                TanqueId = filter.TanqueId,
                TipoCombustibleId = filter.TipoCombustibleId,
                EstacionId = filter.EstacionId,
                Page = page,
                PageSize = 100
            };

            var result = await _reportService.GetInventoryAsync(pageFilter, ct);
            items.AddRange(result.Items);

            if (page >= result.TotalPages || result.Items.Count == 0)
                break;

            page++;
        }

        return items;
    }

    private async Task<List<TraceabilityReportItemDto>> GetAllTraceabilityAsync(
        TraceabilityReportFilterDto filter,
        CancellationToken ct)
    {
        var items = new List<TraceabilityReportItemDto>();
        var page = 1;

        while (true)
        {
            var pageFilter = new TraceabilityReportFilterDto
            {
                FechaDesde = filter.FechaDesde,
                FechaHasta = filter.FechaHasta,
                TanqueId = filter.TanqueId,
                TipoMovimiento = filter.TipoMovimiento,
                Page = page,
                PageSize = 100
            };

            var result = await _reportService.GetTraceabilityAsync(pageFilter, ct);
            items.AddRange(result.Items);

            if (page >= result.TotalPages || result.Items.Count == 0)
                break;

            page++;
        }

        return items;
    }

    private static string NormalizeFormat(string format)
    {
        var normalized = format.Trim().ToLowerInvariant();

        if (normalized is not ("csv" or "xlsx" or "pdf"))
            throw new ArgumentException(
                "Formato no soportado. Utilice csv, xlsx o pdf.");

        return normalized;
    }

    // =========================================================
    // CSV
    // =========================================================

    private static ReportExportResultDto BuildConsumptionCsv(
        IReadOnlyCollection<ConsumptionReportItemDto> items)
    {
        var sb = new StringBuilder();

        sb.AppendLine(
            "Fecha,Tanque,Codigo,Combustible,Total Despachado (gal),Cantidad Despachos");

        foreach (var item in items)
        {
            sb.AppendLine(string.Join(",",
                Csv(item.Fecha.ToString("yyyy-MM-dd")),
                Csv(item.TanqueNombre),
                Csv(item.TanqueCodigo),
                Csv(item.Combustible),
                Csv(DecimalText(item.TotalDespachadoGalones)),
                Csv(item.CantidadDespachos.ToString(CultureInfo.InvariantCulture))));
        }

        return CsvResult(sb, "reporte-consumo");
    }

    private static ReportExportResultDto BuildInventoryCsv(
        IReadOnlyCollection<InventoryReportItemDto> items)
    {
        var sb = new StringBuilder();

        sb.AppendLine(
            "Estacion,Tanque,Codigo,Combustible,Capacidad (gal),Stock Actual (gal),Nivel Critico (gal),Ocupacion (%),Estado");

        foreach (var item in items)
        {
            sb.AppendLine(string.Join(",",
                Csv(item.Estacion),
                Csv(item.TanqueNombre),
                Csv(item.TanqueCodigo),
                Csv(item.Combustible),
                Csv(DecimalText(item.CapacidadMaximaGalones)),
                Csv(DecimalText(item.StockActualGalones)),
                Csv(DecimalText(item.NivelCriticoGalones)),
                Csv(DecimalText(item.PorcentajeOcupacion)),
                Csv(item.EstadoStock)));
        }

        return CsvResult(sb, "reporte-inventario");
    }

    private static ReportExportResultDto BuildTraceabilityCsv(
        IReadOnlyCollection<TraceabilityReportItemDto> items)
    {
        var sb = new StringBuilder();

        sb.AppendLine(
            "Fecha,Estacion,Tanque,Codigo,Combustible,Movimiento,Cantidad (gal),Saldo Anterior (gal),Saldo Posterior (gal),Registrado Por,Observaciones");

        foreach (var item in items)
        {
            sb.AppendLine(string.Join(",",
                Csv(item.FechaMovimiento.ToString("yyyy-MM-dd HH:mm:ss")),
                Csv(item.Estacion),
                Csv(item.TanqueNombre),
                Csv(item.TanqueCodigo),
                Csv(item.Combustible),
                Csv(item.TipoMovimiento),
                Csv(DecimalText(item.CantidadGalones)),
                Csv(DecimalText(item.SaldoAnteriorGalones)),
                Csv(DecimalText(item.SaldoPosteriorGalones)),
                Csv(item.RegistradoPor),
                Csv(item.Observaciones)));
        }

        return CsvResult(sb, "reporte-trazabilidad");
    }

    private static ReportExportResultDto CsvResult(
        StringBuilder sb,
        string baseName)
    {
        // BOM UTF-8 para que Excel reconozca correctamente tildes y ñ.
        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(sb.ToString());

        var bytes = new byte[preamble.Length + content.Length];

        Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
        Buffer.BlockCopy(content, 0, bytes, preamble.Length, content.Length);

        return new ReportExportResultDto
        {
            Content = bytes,
            ContentType = "text/csv; charset=utf-8",
            FileName = $"{baseName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv"
        };
    }

    private static string Csv(string? value)
    {
        var text = value ?? string.Empty;

        if (text.Contains('"'))
            text = text.Replace("\"", "\"\"");

        if (text.Contains(',') ||
            text.Contains('"') ||
            text.Contains('\n') ||
            text.Contains('\r'))
        {
            return $"\"{text}\"";
        }

        return text;
    }

    // =========================================================
    // XLSX
    // =========================================================

    private static ReportExportResultDto BuildConsumptionXlsx(
        IReadOnlyCollection<ConsumptionReportItemDto> items)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Consumo");

        var headers = new[]
        {
            "Fecha",
            "Tanque",
            "Código",
            "Combustible",
            "Total Despachado (gal)",
            "Cantidad Despachos"
        };

        WriteHeaders(sheet, headers);

        var row = 2;

        foreach (var item in items)
        {
            sheet.Cell(row, 1).Value = item.Fecha.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(row, 2).Value = item.TanqueNombre ?? string.Empty;
            sheet.Cell(row, 3).Value = item.TanqueCodigo;
            sheet.Cell(row, 4).Value = item.Combustible;
            sheet.Cell(row, 5).Value = item.TotalDespachadoGalones;
            sheet.Cell(row, 6).Value = item.CantidadDespachos;

            row++;
        }

        sheet.Column(1).Style.DateFormat.Format = "yyyy-MM-dd";

        return XlsxResult(workbook, sheet, "reporte-consumo");
    }

    private static ReportExportResultDto BuildInventoryXlsx(
        IReadOnlyCollection<InventoryReportItemDto> items)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Inventario");

        var headers = new[]
        {
            "Estación",
            "Tanque",
            "Código",
            "Combustible",
            "Capacidad (gal)",
            "Stock Actual (gal)",
            "Nivel Crítico (gal)",
            "Ocupación (%)",
            "Estado"
        };

        WriteHeaders(sheet, headers);

        var row = 2;

        foreach (var item in items)
        {
            sheet.Cell(row, 1).Value = item.Estacion;
            sheet.Cell(row, 2).Value = item.TanqueNombre ?? string.Empty;
            sheet.Cell(row, 3).Value = item.TanqueCodigo;
            sheet.Cell(row, 4).Value = item.Combustible;
            sheet.Cell(row, 5).Value = item.CapacidadMaximaGalones;
            sheet.Cell(row, 6).Value = item.StockActualGalones;
            sheet.Cell(row, 7).Value = item.NivelCriticoGalones;
            sheet.Cell(row, 8).Value = item.PorcentajeOcupacion;
            sheet.Cell(row, 9).Value = item.EstadoStock;

            row++;
        }

        return XlsxResult(workbook, sheet, "reporte-inventario");
    }

    private static ReportExportResultDto BuildTraceabilityXlsx(
        IReadOnlyCollection<TraceabilityReportItemDto> items)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Trazabilidad");

        var headers = new[]
        {
            "Fecha",
            "Estación",
            "Tanque",
            "Código",
            "Combustible",
            "Movimiento",
            "Cantidad (gal)",
            "Saldo Anterior (gal)",
            "Saldo Posterior (gal)",
            "Registrado Por",
            "Observaciones"
        };

        WriteHeaders(sheet, headers);

        var row = 2;

        foreach (var item in items)
        {
            sheet.Cell(row, 1).Value = item.FechaMovimiento;
            sheet.Cell(row, 2).Value = item.Estacion;
            sheet.Cell(row, 3).Value = item.TanqueNombre ?? string.Empty;
            sheet.Cell(row, 4).Value = item.TanqueCodigo;
            sheet.Cell(row, 5).Value = item.Combustible;
            sheet.Cell(row, 6).Value = item.TipoMovimiento;
            sheet.Cell(row, 7).Value = item.CantidadGalones;
            sheet.Cell(row, 8).Value = item.SaldoAnteriorGalones;
            sheet.Cell(row, 9).Value = item.SaldoPosteriorGalones;
            sheet.Cell(row, 10).Value = item.RegistradoPor;
            sheet.Cell(row, 11).Value = item.Observaciones ?? string.Empty;

            row++;
        }

        sheet.Column(1).Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";

        return XlsxResult(workbook, sheet, "reporte-trazabilidad");
    }

    private static void WriteHeaders(
        IXLWorksheet sheet,
        IReadOnlyList<string> headers)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        sheet.SheetView.FreezeRows(1);
    }

    private static ReportExportResultDto XlsxResult(
        XLWorkbook workbook,
        IXLWorksheet sheet,
        string baseName)
    {
        sheet.ColumnsUsed().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ReportExportResultDto
        {
            Content = stream.ToArray(),
            ContentType =
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileName = $"{baseName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx"
        };
    }

    // =========================================================
    // PDF
    // =========================================================

    private static ReportExportResultDto BuildConsumptionPdf(
        IReadOnlyCollection<ConsumptionReportItemDto> items)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page);

                page.Header()
                    .Text("Reporte de Consumo de Combustible")
                    .SemiBold()
                    .FontSize(16);

                page.Content()
                    .PaddingVertical(10)
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        PdfHeader(
                            table,
                            "Fecha",
                            "Tanque",
                            "Combustible",
                            "Galones",
                            "Despachos");

                        foreach (var item in items)
                        {
                            PdfCell(table, item.Fecha.ToString("yyyy-MM-dd"));
                            PdfCell(
                                table,
                                $"{item.TanqueCodigo} {item.TanqueNombre}".Trim());
                            PdfCell(table, item.Combustible);
                            PdfCell(
                                table,
                                DecimalText(item.TotalDespachadoGalones));
                            PdfCell(
                                table,
                                item.CantidadDespachos.ToString(
                                    CultureInfo.InvariantCulture));
                        }
                    });

                PdfFooter(page);
            });
        });

        return PdfResult(document, "reporte-consumo");
    }

    private static ReportExportResultDto BuildInventoryPdf(
        IReadOnlyCollection<InventoryReportItemDto> items)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page);

                page.Header()
                    .Text("Reporte de Inventario de Combustible")
                    .SemiBold()
                    .FontSize(16);

                page.Content()
                    .PaddingVertical(10)
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        PdfHeader(
                            table,
                            "Estación",
                            "Tanque",
                            "Combustible",
                            "Capacidad",
                            "Stock",
                            "Estado");

                        foreach (var item in items)
                        {
                            PdfCell(table, item.Estacion);
                            PdfCell(
                                table,
                                $"{item.TanqueCodigo} {item.TanqueNombre}".Trim());
                            PdfCell(table, item.Combustible);
                            PdfCell(
                                table,
                                $"{DecimalText(item.CapacidadMaximaGalones)} gal");
                            PdfCell(
                                table,
                                $"{DecimalText(item.StockActualGalones)} gal");
                            PdfCell(table, item.EstadoStock);
                        }
                    });

                PdfFooter(page);
            });
        });

        return PdfResult(document, "reporte-inventario");
    }

    private static ReportExportResultDto BuildTraceabilityPdf(
        IReadOnlyCollection<TraceabilityReportItemDto> items)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header()
                    .Text("Reporte de Trazabilidad de Inventario")
                    .SemiBold()
                    .FontSize(16);

                page.Content()
                    .PaddingVertical(10)
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn(2);
                        });

                        PdfHeader(
                            table,
                            "Fecha",
                            "Tanque",
                            "Combustible",
                            "Movimiento",
                            "Cantidad",
                            "Anterior",
                            "Posterior",
                            "Usuario");

                        foreach (var item in items)
                        {
                            PdfCell(
                                table,
                                item.FechaMovimiento.ToString(
                                    "yyyy-MM-dd HH:mm"));
                            PdfCell(table, item.TanqueCodigo);
                            PdfCell(table, item.Combustible);
                            PdfCell(table, item.TipoMovimiento);
                            PdfCell(
                                table,
                                DecimalText(item.CantidadGalones));
                            PdfCell(
                                table,
                                DecimalText(item.SaldoAnteriorGalones));
                            PdfCell(
                                table,
                                DecimalText(item.SaldoPosteriorGalones));
                            PdfCell(table, item.RegistradoPor);
                        }
                    });

                PdfFooter(page);
            });
        });

        return PdfResult(document, "reporte-trazabilidad");
    }

    private static void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(30);
        page.DefaultTextStyle(x => x.FontSize(9));
    }

    private static void PdfHeader(
        TableDescriptor table,
        params string[] headers)
    {
        table.Header(header =>
        {
            foreach (var text in headers)
            {
                header.Cell()
                    .BorderBottom(1)
                    .Padding(4)
                    .Text(text)
                    .SemiBold();
            }
        });
    }

    private static void PdfCell(
        TableDescriptor table,
        string? value)
    {
        table.Cell()
            .BorderBottom(0.5f)
            .Padding(4)
            .Text(value ?? string.Empty);
    }

    private static void PdfFooter(PageDescriptor page)
    {
        page.Footer()
            .AlignCenter()
            .Text(text =>
            {
                text.Span("Página ");
                text.CurrentPageNumber();
                text.Span(" de ");
                text.TotalPages();
            });
    }

    private static ReportExportResultDto PdfResult(
        IDocument document,
        string baseName)
    {
        return new ReportExportResultDto
        {
            Content = document.GeneratePdf(),
            ContentType = "application/pdf",
            FileName = $"{baseName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf"
        };
    }

    private static string DecimalText(decimal value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}