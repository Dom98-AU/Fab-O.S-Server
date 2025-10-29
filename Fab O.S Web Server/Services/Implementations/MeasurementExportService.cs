using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace FabOS.WebServer.Services.Implementations;

/// <summary>
/// Service for exporting takeoff measurements to Excel material lists
/// </summary>
public class MeasurementExportService : IMeasurementExportService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<MeasurementExportService> _logger;

    public MeasurementExportService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<MeasurementExportService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<byte[]> ExportMeasurementsToExcelAsync(int packageDrawingId, bool groupByItem = true)
    {
        try
        {
            _logger.LogInformation("[MeasurementExport] Starting export for PackageDrawingId={PackageDrawingId}, GroupByItem={GroupByItem}",
                packageDrawingId, groupByItem);

            // Create new DbContext for this operation
            await using var dbContext = await _contextFactory.CreateDbContextAsync();

            // Load package drawing with all related data
            var packageDrawing = await dbContext.PackageDrawings
                .Include(pd => pd.Package)
                    .ThenInclude(p => p!.Revision)
                .AsNoTracking()
                .FirstOrDefaultAsync(pd => pd.Id == packageDrawingId);

            if (packageDrawing == null)
            {
                _logger.LogWarning("[MeasurementExport] PackageDrawing not found: {PackageDrawingId}", packageDrawingId);
                throw new InvalidOperationException($"Package drawing with ID {packageDrawingId} not found");
            }

            // Load measurements with catalogue items
            var measurements = await dbContext.TraceTakeoffMeasurements
                .Include(m => m.CatalogueItem)
                .Where(m => m.PackageDrawingId == packageDrawingId)
                .OrderBy(m => m.CatalogueItem!.Category)
                    .ThenBy(m => m.CatalogueItem!.Material)
                    .ThenBy(m => m.CatalogueItem!.ItemCode)
                    .ThenBy(m => m.CreatedDate)
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation("[MeasurementExport] Found {Count} measurements", measurements.Count);

            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Material List");

                int currentRow = 1;

                // ===== HEADER SECTION =====
                currentRow = AddHeaderSection(worksheet, packageDrawing, currentRow);
                currentRow += 2; // Add spacing

                // ===== COLUMN HEADERS =====
                currentRow = AddColumnHeaders(worksheet, currentRow);

                // ===== DATA ROWS =====
                if (groupByItem)
                {
                    currentRow = AddGroupedMeasurements(worksheet, measurements, currentRow);
                }
                else
                {
                    currentRow = AddIndividualMeasurements(worksheet, measurements, currentRow);
                }

                // ===== FOOTER / TOTALS =====
                currentRow = AddFooterSection(worksheet, measurements, currentRow);

                // ===== FORMATTING =====
                FormatWorksheet(worksheet);

                _logger.LogInformation("[MeasurementExport] Export completed successfully");
                return package.GetAsByteArray();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MeasurementExport] Error exporting measurements for PackageDrawingId={PackageDrawingId}", packageDrawingId);
            throw;
        }
    }

    private int AddHeaderSection(ExcelWorksheet worksheet, Models.Entities.PackageDrawing packageDrawing, int startRow)
    {
        int row = startRow;

        // Title
        worksheet.Cells[row, 1].Value = "MATERIAL LIST - TAKEOFF MEASUREMENTS";
        worksheet.Cells[row, 1, row, 5].Merge = true;
        worksheet.Cells[row, 1].Style.Font.Size = 16;
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        row++;

        row++; // Blank line

        // Package Information
        if (packageDrawing.Package != null)
        {
            worksheet.Cells[row, 1].Value = "Package Number:";
            worksheet.Cells[row, 2].Value = packageDrawing.Package.PackageNumber;
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row++;

            worksheet.Cells[row, 1].Value = "Package Name:";
            worksheet.Cells[row, 2].Value = packageDrawing.Package.PackageName;
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row++;

            if (!string.IsNullOrEmpty(packageDrawing.Package.Description))
            {
                worksheet.Cells[row, 1].Value = "Package Description:";
                worksheet.Cells[row, 2].Value = packageDrawing.Package.Description;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row++;
            }

            // Revision Information
            if (packageDrawing.Package.Revision != null)
            {
                worksheet.Cells[row, 1].Value = "Revision:";
                worksheet.Cells[row, 2].Value = packageDrawing.Package.Revision.RevisionCode;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row++;

                if (!string.IsNullOrEmpty(packageDrawing.Package.Revision.Description))
                {
                    worksheet.Cells[row, 1].Value = "Revision Notes:";
                    worksheet.Cells[row, 2].Value = packageDrawing.Package.Revision.Description;
                    worksheet.Cells[row, 1].Style.Font.Bold = true;
                    row++;
                }
            }
        }

        // Drawing Information
        worksheet.Cells[row, 1].Value = "Drawing Number:";
        worksheet.Cells[row, 2].Value = packageDrawing.DrawingNumber;
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        row++;

        if (!string.IsNullOrEmpty(packageDrawing.DrawingTitle))
        {
            worksheet.Cells[row, 1].Value = "Drawing Title:";
            worksheet.Cells[row, 2].Value = packageDrawing.DrawingTitle;
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row++;
        }

        // Export Information
        worksheet.Cells[row, 1].Value = "Export Date:";
        worksheet.Cells[row, 2].Value = DateTime.Now.ToString("dd-MMM-yyyy HH:mm");
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        row++;

        return row;
    }

    private int AddColumnHeaders(ExcelWorksheet worksheet, int startRow)
    {
        worksheet.Cells[startRow, 1].Value = "Item #";
        worksheet.Cells[startRow, 2].Value = "Item Code";
        worksheet.Cells[startRow, 3].Value = "Description";
        worksheet.Cells[startRow, 4].Value = "Category";
        worksheet.Cells[startRow, 5].Value = "Material";
        worksheet.Cells[startRow, 6].Value = "Profile/Size";
        worksheet.Cells[startRow, 7].Value = "Measurement Type";
        worksheet.Cells[startRow, 8].Value = "Quantity";
        worksheet.Cells[startRow, 9].Value = "Unit";
        worksheet.Cells[startRow, 10].Value = "Weight (kg)";
        worksheet.Cells[startRow, 11].Value = "Label/Notes";
        worksheet.Cells[startRow, 12].Value = "Page #";
        worksheet.Cells[startRow, 13].Value = "Date Measured";

        // Format header row
        using (var range = worksheet.Cells[startRow, 1, startRow, 13])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196)); // Dark blue
            range.Style.Font.Color.SetColor(Color.White);
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }

        return startRow + 1;
    }

    private int AddGroupedMeasurements(ExcelWorksheet worksheet, List<Models.Entities.TraceTakeoffMeasurement> measurements, int startRow)
    {
        int row = startRow;
        int itemNumber = 1;

        // Group by catalogue item
        var grouped = measurements
            .Where(m => m.CatalogueItem != null)
            .GroupBy(m => new
            {
                m.CatalogueItem!.ItemCode,
                m.CatalogueItem.Description,
                m.CatalogueItem.Category,
                m.CatalogueItem.Material,
                m.CatalogueItem.Profile,
                m.MeasurementType,
                m.Unit
            })
            .OrderBy(g => g.Key.Category)
            .ThenBy(g => g.Key.Material)
            .ThenBy(g => g.Key.ItemCode);

        foreach (var group in grouped)
        {
            decimal totalQuantity = group.Sum(m => m.Value);
            decimal? totalWeight = group.Sum(m => m.CalculatedWeight);
            string labels = string.Join(", ", group.Where(m => !string.IsNullOrEmpty(m.Label)).Select(m => m.Label).Distinct());
            var earliestDate = group.Min(m => m.CreatedDate);

            worksheet.Cells[row, 1].Value = itemNumber;
            worksheet.Cells[row, 2].Value = group.Key.ItemCode;
            worksheet.Cells[row, 3].Value = group.Key.Description;
            worksheet.Cells[row, 4].Value = group.Key.Category;
            worksheet.Cells[row, 5].Value = group.Key.Material;
            worksheet.Cells[row, 6].Value = group.Key.Profile;
            worksheet.Cells[row, 7].Value = group.Key.MeasurementType;
            worksheet.Cells[row, 8].Value = totalQuantity;
            worksheet.Cells[row, 8].Style.Numberformat.Format = "#,##0.00";
            worksheet.Cells[row, 9].Value = group.Key.Unit;
            worksheet.Cells[row, 10].Value = totalWeight;
            worksheet.Cells[row, 10].Style.Numberformat.Format = "#,##0.00";
            worksheet.Cells[row, 11].Value = labels;
            worksheet.Cells[row, 12].Value = group.Count() > 1 ? "Multiple" : group.First().PageNumber?.ToString();
            worksheet.Cells[row, 13].Value = earliestDate.ToString("dd-MMM-yyyy HH:mm");

            // Alternating row colors
            if (itemNumber % 2 == 0)
            {
                using (var range = worksheet.Cells[row, 1, row, 13])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242)); // Light gray
                }
            }

            row++;
            itemNumber++;
        }

        // Add measurements without catalogue items (if any)
        var uncategorized = measurements.Where(m => m.CatalogueItem == null);
        foreach (var measurement in uncategorized)
        {
            worksheet.Cells[row, 1].Value = itemNumber;
            worksheet.Cells[row, 2].Value = "N/A";
            worksheet.Cells[row, 3].Value = measurement.Description ?? measurement.Label ?? "Uncategorized";
            worksheet.Cells[row, 4].Value = "-";
            worksheet.Cells[row, 5].Value = "-";
            worksheet.Cells[row, 6].Value = "-";
            worksheet.Cells[row, 7].Value = measurement.MeasurementType;
            worksheet.Cells[row, 8].Value = measurement.Value;
            worksheet.Cells[row, 8].Style.Numberformat.Format = "#,##0.00";
            worksheet.Cells[row, 9].Value = measurement.Unit;
            worksheet.Cells[row, 10].Value = measurement.CalculatedWeight;
            worksheet.Cells[row, 10].Style.Numberformat.Format = "#,##0.00";
            worksheet.Cells[row, 11].Value = measurement.Label;
            worksheet.Cells[row, 12].Value = measurement.PageNumber;
            worksheet.Cells[row, 13].Value = measurement.CreatedDate.ToString("dd-MMM-yyyy HH:mm");

            if (itemNumber % 2 == 0)
            {
                using (var range = worksheet.Cells[row, 1, row, 13])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                }
            }

            row++;
            itemNumber++;
        }

        return row;
    }

    private int AddIndividualMeasurements(ExcelWorksheet worksheet, List<Models.Entities.TraceTakeoffMeasurement> measurements, int startRow)
    {
        int row = startRow;
        int itemNumber = 1;

        foreach (var measurement in measurements)
        {
            worksheet.Cells[row, 1].Value = itemNumber;
            worksheet.Cells[row, 2].Value = measurement.CatalogueItem?.ItemCode ?? "N/A";
            worksheet.Cells[row, 3].Value = measurement.CatalogueItem?.Description ?? measurement.Description ?? "N/A";
            worksheet.Cells[row, 4].Value = measurement.CatalogueItem?.Category ?? "-";
            worksheet.Cells[row, 5].Value = measurement.CatalogueItem?.Material ?? "-";
            worksheet.Cells[row, 6].Value = measurement.CatalogueItem?.Profile ?? "-";
            worksheet.Cells[row, 7].Value = measurement.MeasurementType;
            worksheet.Cells[row, 8].Value = measurement.Value;
            worksheet.Cells[row, 8].Style.Numberformat.Format = "#,##0.00";
            worksheet.Cells[row, 9].Value = measurement.Unit;
            worksheet.Cells[row, 10].Value = measurement.CalculatedWeight;
            worksheet.Cells[row, 10].Style.Numberformat.Format = "#,##0.00";
            worksheet.Cells[row, 11].Value = measurement.Label;
            worksheet.Cells[row, 12].Value = measurement.PageNumber;
            worksheet.Cells[row, 13].Value = measurement.CreatedDate.ToString("dd-MMM-yyyy HH:mm");

            // Alternating row colors
            if (itemNumber % 2 == 0)
            {
                using (var range = worksheet.Cells[row, 1, row, 13])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                }
            }

            row++;
            itemNumber++;
        }

        return row;
    }

    private int AddFooterSection(ExcelWorksheet worksheet, List<Models.Entities.TraceTakeoffMeasurement> measurements, int startRow)
    {
        int row = startRow + 1; // Add blank line

        // Total items
        worksheet.Cells[row, 1].Value = "Total Items:";
        worksheet.Cells[row, 2].Value = measurements.Count;
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        worksheet.Cells[row, 2].Style.Font.Bold = true;

        // Total weight
        decimal totalWeight = measurements.Sum(m => m.CalculatedWeight ?? 0);
        worksheet.Cells[row, 9].Value = "Total Weight (kg):";
        worksheet.Cells[row, 10].Value = totalWeight;
        worksheet.Cells[row, 9].Style.Font.Bold = true;
        worksheet.Cells[row, 10].Style.Font.Bold = true;
        worksheet.Cells[row, 10].Style.Numberformat.Format = "#,##0.00";

        // Format footer row
        using (var range = worksheet.Cells[row, 1, row, 13])
        {
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(146, 208, 80)); // Green
            range.Style.Font.Bold = true;
        }

        return row + 1;
    }

    private void FormatWorksheet(ExcelWorksheet worksheet)
    {
        // Auto-fit all columns
        worksheet.Cells.AutoFitColumns();

        // Set minimum column widths
        for (int i = 1; i <= 13; i++)
        {
            if (worksheet.Column(i).Width < 10)
            {
                worksheet.Column(i).Width = 10;
            }
        }

        // Description column should be wider
        if (worksheet.Column(3).Width < 40)
        {
            worksheet.Column(3).Width = 40;
        }

        // Add borders to all data cells
        int lastRow = worksheet.Dimension?.End.Row ?? 1;
        using (var range = worksheet.Cells[1, 1, lastRow, 13])
        {
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }

        // Freeze header row (find the column header row)
        // Typically it's around row 10-12 depending on metadata
        int headerRow = 10; // Approximate, adjust based on actual layout
        for (int i = 1; i <= 20; i++)
        {
            if (worksheet.Cells[i, 1].Value?.ToString() == "Item #")
            {
                headerRow = i;
                break;
            }
        }
        worksheet.View.FreezePanes(headerRow + 1, 1);
    }
}
