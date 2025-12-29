using System.Text.Json;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces.Estimate;
using FabOS.WebServer.Services.Estimate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace FabOS.WebServer.Services.Implementations.Estimate;

/// <summary>
/// Service for importing and exporting worksheets to/from Excel using EPPlus.
/// </summary>
public class WorksheetExcelService : IWorksheetExcelService
{
    private readonly ApplicationDbContext _context;
    private readonly IEstimationCalculationService _calculationService;
    private readonly ICatalogueMatchingService _catalogueMatchingService;
    private readonly ILogger<WorksheetExcelService> _logger;

    public WorksheetExcelService(
        ApplicationDbContext context,
        IEstimationCalculationService calculationService,
        ICatalogueMatchingService catalogueMatchingService,
        ILogger<WorksheetExcelService> logger)
    {
        _context = context;
        _calculationService = calculationService;
        _catalogueMatchingService = catalogueMatchingService;
        _logger = logger;

        // Set EPPlus license context (non-commercial use)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<byte[]> ExportWorksheetAsync(int worksheetId)
    {
        var worksheet = await _context.EstimationWorksheets
            .Include(w => w.WorksheetTemplate)
                .ThenInclude(t => t!.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder))
            .Include(w => w.Rows.Where(r => !r.IsDeleted).OrderBy(r => r.RowNumber))
            .Include(w => w.EstimationRevisionPackage)
            .FirstOrDefaultAsync(w => w.Id == worksheetId);

        if (worksheet == null)
            throw new ArgumentException($"Worksheet {worksheetId} not found");

        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add(worksheet.Name);

        await PopulateExcelSheet(sheet, worksheet);

        return await package.GetAsByteArrayAsync();
    }

    public async Task<byte[]> ExportPackageAsync(int packageId)
    {
        var pkg = await _context.EstimationRevisionPackages
            .Include(p => p.Worksheets.OrderBy(w => w.DisplayOrder))
                .ThenInclude(w => w.WorksheetTemplate)
                    .ThenInclude(t => t!.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder))
            .Include(p => p.Worksheets)
                .ThenInclude(w => w.Rows.Where(r => !r.IsDeleted).OrderBy(r => r.RowNumber))
            .Include(p => p.EstimationRevision)
            .FirstOrDefaultAsync(p => p.Id == packageId);

        if (pkg == null)
            throw new ArgumentException($"Package {packageId} not found");

        using var package = new ExcelPackage();

        // Add summary sheet
        var summarySheet = package.Workbook.Worksheets.Add("Summary");
        await PopulateSummarySheet(summarySheet, pkg);

        // Add each worksheet as a sheet
        foreach (var worksheet in pkg.Worksheets.OrderBy(w => w.DisplayOrder))
        {
            var sheetName = GetValidSheetName(worksheet.Name, package.Workbook.Worksheets);
            var sheet = package.Workbook.Worksheets.Add(sheetName);
            await PopulateExcelSheet(sheet, worksheet);
        }

        return await package.GetAsByteArrayAsync();
    }

    public async Task<byte[]> ExportRevisionAsync(int revisionId)
    {
        var revision = await _context.EstimationRevisions
            .Include(r => r.Estimation)
            .Include(r => r.Packages.OrderBy(p => p.SequenceNumber))
                .ThenInclude(p => p.Worksheets.OrderBy(w => w.DisplayOrder))
                    .ThenInclude(w => w.WorksheetTemplate)
                        .ThenInclude(t => t!.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder))
            .Include(r => r.Packages)
                .ThenInclude(p => p.Worksheets)
                    .ThenInclude(w => w.Rows.Where(r => !r.IsDeleted).OrderBy(r => r.RowNumber))
            .FirstOrDefaultAsync(r => r.Id == revisionId);

        if (revision == null)
            throw new ArgumentException($"Revision {revisionId} not found");

        using var package = new ExcelPackage();

        // Add revision summary sheet
        var summarySheet = package.Workbook.Worksheets.Add("Revision Summary");
        await PopulateRevisionSummarySheet(summarySheet, revision);

        // Add each package and its worksheets
        foreach (var pkg in revision.Packages.OrderBy(p => p.SequenceNumber))
        {
            foreach (var worksheet in pkg.Worksheets.OrderBy(w => w.DisplayOrder))
            {
                var sheetName = GetValidSheetName($"{pkg.PackageName}-{worksheet.Name}", package.Workbook.Worksheets);
                var sheet = package.Workbook.Worksheets.Add(sheetName);
                await PopulateExcelSheet(sheet, worksheet);
            }
        }

        return await package.GetAsByteArrayAsync();
    }

    public async Task<ImportPreview> PreviewImportAsync(Stream excelStream, int worksheetId)
    {
        var worksheet = await _context.EstimationWorksheets
            .Include(w => w.WorksheetTemplate)
                .ThenInclude(t => t!.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder))
            .FirstOrDefaultAsync(w => w.Id == worksheetId);

        if (worksheet == null)
            throw new ArgumentException($"Worksheet {worksheetId} not found");

        var preview = new ImportPreview();

        using var package = new ExcelPackage(excelStream);
        var sheet = package.Workbook.Worksheets.FirstOrDefault();

        if (sheet == null)
        {
            preview.Warnings.Add("No worksheets found in Excel file");
            return preview;
        }

        // Detect columns from header row
        var headerRow = 1;
        var lastColumn = sheet.Dimension?.End.Column ?? 0;
        var lastRow = sheet.Dimension?.End.Row ?? 0;

        preview.TotalRows = Math.Max(0, lastRow - 1); // Exclude header

        for (int col = 1; col <= lastColumn; col++)
        {
            var headerValue = sheet.Cells[headerRow, col].Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(headerValue))
                continue;

            var colInfo = new ExcelColumnInfo
            {
                ColumnIndex = col,
                ColumnLetter = GetColumnLetter(col),
                Name = headerValue,
                SampleValues = new List<string>()
            };

            // Get sample values (first 5 non-empty)
            for (int row = 2; row <= Math.Min(lastRow, 10); row++)
            {
                var value = sheet.Cells[row, col].Text;
                if (!string.IsNullOrEmpty(value))
                    colInfo.SampleValues.Add(value);
            }

            // Detect type from values
            colInfo.DetectedType = DetectColumnType(colInfo.SampleValues);

            preview.ExcelColumns.Add(colInfo);
        }

        // Map worksheet columns from template
        var columns = worksheet.WorksheetTemplate?.Columns.Where(c => !c.IsDeleted) ?? new List<EstimationWorksheetColumn>();
        preview.WorksheetColumns = columns
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new WorksheetColumnInfo
            {
                ColumnKey = c.ColumnKey,
                ColumnName = c.DisplayName,
                DataType = c.DataType,
                IsRequired = c.IsRequired,
                IsComputed = !string.IsNullOrEmpty(c.Formula)
            })
            .ToList();

        // Suggest mappings based on name similarity
        foreach (var excelCol in preview.ExcelColumns)
        {
            var bestMatch = preview.WorksheetColumns
                .Where(wc => !wc.IsComputed) // Don't map to computed columns
                .Select(wc => new
                {
                    Column = wc,
                    Score = CalculateSimilarity(excelCol.Name, wc.ColumnName)
                })
                .Where(x => x.Score > 0.5)
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            if (bestMatch != null)
            {
                preview.SuggestedMappings.Add(new ColumnMappingItem
                {
                    ExcelColumnIndex = excelCol.ColumnIndex,
                    ExcelColumnName = excelCol.Name,
                    WorksheetColumnKey = bestMatch.Column.ColumnKey
                });
            }
        }

        // Get sample rows
        for (int row = 2; row <= Math.Min(lastRow, 11); row++)
        {
            var rowData = new Dictionary<string, object?>();
            foreach (var colInfo in preview.ExcelColumns)
            {
                rowData[colInfo.Name] = sheet.Cells[row, colInfo.ColumnIndex].Value;
            }
            preview.SampleRows.Add(rowData);
        }

        return preview;
    }

    public async Task<ImportResult> ImportWorksheetAsync(int worksheetId, Stream excelStream, ColumnMapping mapping)
    {
        var worksheet = await _context.EstimationWorksheets
            .Include(w => w.WorksheetTemplate)
                .ThenInclude(t => t!.Columns.Where(c => !c.IsDeleted))
            .Include(w => w.Rows.Where(r => !r.IsDeleted))
            .Include(w => w.EstimationRevisionPackage)
                .ThenInclude(p => p!.EstimationRevision)
            .FirstOrDefaultAsync(w => w.Id == worksheetId);

        if (worksheet == null)
            throw new ArgumentException($"Worksheet {worksheetId} not found");

        if (worksheet.EstimationRevisionPackage?.EstimationRevision?.Status != "Draft")
            return new ImportResult
            {
                Success = false,
                ErrorMessage = "Can only import to worksheets in draft revisions"
            };

        var result = new ImportResult { Success = true };

        try
        {
            using var package = new ExcelPackage(excelStream);
            ExcelWorksheet? sheet;

            if (!string.IsNullOrEmpty(mapping.SheetName))
            {
                sheet = package.Workbook.Worksheets[mapping.SheetName];
            }
            else
            {
                sheet = package.Workbook.Worksheets.ElementAtOrDefault(mapping.SheetIndex);
            }

            if (sheet == null)
            {
                result.Success = false;
                result.ErrorMessage = "Specified worksheet not found in Excel file";
                return result;
            }

            var lastRow = sheet.Dimension?.End.Row ?? 0;

            // Clear existing if requested
            if (mapping.ReplaceExisting)
            {
                foreach (var row in worksheet.Rows)
                {
                    row.IsDeleted = true;
                }
            }

            var nextRowNumber = worksheet.Rows.Any()
                ? worksheet.Rows.Max(r => r.RowNumber) + 1
                : 1;

            var columns = worksheet.WorksheetTemplate?.Columns
                .Where(c => !c.IsDeleted)
                .ToDictionary(c => c.ColumnKey, StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, EstimationWorksheetColumn>(StringComparer.OrdinalIgnoreCase);

            // Identify catalogue-linked columns for auto-population
            var catalogueLinkedColumns = columns.Values
                .Where(c => c.LinkToCatalogue && c.AutoPopulateFromCatalogue && !string.IsNullOrEmpty(c.CatalogueField))
                .ToList();

            // Build Excel column name to index mapping from header row
            var headerRow = 1;
            var excelColumnNameToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int col = 1; col <= (sheet.Dimension?.End.Column ?? 0); col++)
            {
                var headerValue = sheet.Cells[headerRow, col].Text?.Trim();
                if (!string.IsNullOrEmpty(headerValue))
                {
                    excelColumnNameToIndex[headerValue] = col;
                }
            }

            // Find the Description/Profile column indices for catalogue matching
            int? descriptionColIndex = null;
            int? profileColIndex = null;
            foreach (var kvp in mapping.Mappings)
            {
                if (kvp.Value.Equals("description", StringComparison.OrdinalIgnoreCase) &&
                    excelColumnNameToIndex.TryGetValue(kvp.Key, out var descIdx))
                {
                    descriptionColIndex = descIdx;
                }
                if (kvp.Value.Equals("profile", StringComparison.OrdinalIgnoreCase) &&
                    excelColumnNameToIndex.TryGetValue(kvp.Key, out var profIdx))
                {
                    profileColIndex = profIdx;
                }
            }

            // Import rows
            var newRows = new List<(EstimationWorksheetRow Row, string? MatchValue)>();

            for (int rowNum = mapping.StartRow; rowNum <= lastRow; rowNum++)
            {
                try
                {
                    var rowData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    var isEmpty = true;

                    // Iterate over mappings (excel column name -> worksheet column key)
                    foreach (var kvp in mapping.Mappings)
                    {
                        var excelColumnName = kvp.Key;
                        var worksheetColumnKey = kvp.Value;

                        if (!excelColumnNameToIndex.TryGetValue(excelColumnName, out var colIndex))
                            continue;

                        var cellValue = sheet.Cells[rowNum, colIndex].Value;

                        if (cellValue != null && !string.IsNullOrWhiteSpace(cellValue.ToString()))
                            isEmpty = false;

                        // Convert to appropriate type based on column definition
                        if (columns.TryGetValue(worksheetColumnKey, out var column))
                        {
                            var convertedValue = ConvertCellValue(cellValue, column.DataType);
                            rowData[worksheetColumnKey] = convertedValue;
                        }
                    }

                    if (isEmpty && mapping.SkipEmptyRows)
                    {
                        result.RowsSkipped++;
                        continue;
                    }

                    // Get match value for catalogue matching
                    string? matchValue = null;
                    if (descriptionColIndex.HasValue)
                    {
                        matchValue = sheet.Cells[rowNum, descriptionColIndex.Value].Value?.ToString();
                    }
                    if (string.IsNullOrEmpty(matchValue) && profileColIndex.HasValue)
                    {
                        matchValue = sheet.Cells[rowNum, profileColIndex.Value].Value?.ToString();
                    }

                    var newRow = new EstimationWorksheetRow
                    {
                        CompanyId = worksheet.CompanyId,
                        EstimationWorksheetId = worksheetId,
                        RowNumber = nextRowNumber++,
                        RowData = JsonSerializer.Serialize(rowData),
                        CreatedDate = DateTime.UtcNow,
                        MatchStatus = catalogueLinkedColumns.Any() ? "Unmatched" : null
                    };

                    _context.EstimationWorksheetRows.Add(newRow);
                    newRows.Add((newRow, matchValue));
                    result.RowsImported++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNum,
                        Message = ex.Message
                    });
                    result.RowsWithErrors++;
                }
            }

            await _context.SaveChangesAsync();

            // Attempt catalogue matching and auto-population
            if (catalogueLinkedColumns.Any() && newRows.Any())
            {
                var matchedCount = 0;
                foreach (var (row, matchValue) in newRows)
                {
                    if (!string.IsNullOrEmpty(matchValue))
                    {
                        var matchResult = await _catalogueMatchingService.MatchSingleAsync(
                            worksheet.CompanyId, matchValue, "Description");

                        if (matchResult.MatchedItem != null && matchResult.MatchConfidence >= 0.7)
                        {
                            row.CatalogueItemId = matchResult.MatchedItem.Id;
                            row.MatchStatus = matchResult.IsExactMatch ? "Matched" : "Matched";
                            row.MatchConfidence = (decimal)matchResult.MatchConfidence;

                            // Auto-populate catalogue-linked columns
                            var rowData = JsonSerializer.Deserialize<Dictionary<string, object?>>(row.RowData ?? "{}")
                                ?? new Dictionary<string, object?>();

                            foreach (var col in catalogueLinkedColumns)
                            {
                                var fieldValue = _catalogueMatchingService.GetFieldValue(matchResult.MatchedItem, col.CatalogueField!);
                                if (fieldValue != null)
                                {
                                    rowData[col.ColumnKey] = fieldValue;
                                }
                            }

                            row.RowData = JsonSerializer.Serialize(rowData);
                            matchedCount++;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Auto-matched {MatchedCount} of {TotalCount} rows to catalogue items",
                    matchedCount, newRows.Count);
            }

            // Recalculate worksheet
            await _calculationService.RecalculateWorksheetAsync(worksheetId);

            _logger.LogInformation("Imported {RowsImported} rows to worksheet {WorksheetId}",
                result.RowsImported, worksheetId);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error importing to worksheet {WorksheetId}", worksheetId);
        }

        return result;
    }

    public async Task<byte[]> ExportTemplateAsync(int templateId)
    {
        var template = await _context.EstimationWorksheetTemplates
            .Include(t => t.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder))
            .FirstOrDefaultAsync(t => t.Id == templateId && !t.IsDeleted);

        if (template == null)
            throw new ArgumentException($"Template {templateId} not found");

        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add(template.Name);

        // Write header row
        var colIndex = 1;
        foreach (var column in template.Columns.Where(c => !c.IsDeleted && c.IsVisible).OrderBy(c => c.DisplayOrder))
        {
            var cell = sheet.Cells[1, colIndex];
            cell.Value = column.DisplayName;

            // Style header
            cell.Style.Font.Bold = true;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
            cell.Style.Font.Color.SetColor(Color.White);
            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            // Set column width
            sheet.Column(colIndex).Width = column.Width / 7.0; // Approximate pixel to width conversion

            // Add comment with column info
            var comment = $"Type: {column.DataType}";
            if (column.IsRequired) comment += "\nRequired: Yes";
            if (!string.IsNullOrEmpty(column.Formula)) comment += $"\nFormula: {column.Formula}";
            if (!string.IsNullOrEmpty(column.SelectOptions)) comment += $"\nOptions: {column.SelectOptions}";
            if (column.LinkToCatalogue) comment += $"\nCatalogue Field: {column.CatalogueField}";

            sheet.Cells[1, colIndex].AddComment(comment, "Template Info");

            colIndex++;
        }

        // Add validation and formatting info sheet
        var infoSheet = package.Workbook.Worksheets.Add("Column Info");
        infoSheet.Cells[1, 1].Value = "Column Key";
        infoSheet.Cells[1, 2].Value = "Column Name";
        infoSheet.Cells[1, 3].Value = "Data Type";
        infoSheet.Cells[1, 4].Value = "Required";
        infoSheet.Cells[1, 5].Value = "Formula";
        infoSheet.Cells[1, 6].Value = "Options";
        infoSheet.Cells[1, 7].Value = "Catalogue Field";

        var row = 2;
        foreach (var column in template.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder))
        {
            infoSheet.Cells[row, 1].Value = column.ColumnKey;
            infoSheet.Cells[row, 2].Value = column.DisplayName;
            infoSheet.Cells[row, 3].Value = column.DataType;
            infoSheet.Cells[row, 4].Value = column.IsRequired ? "Yes" : "No";
            infoSheet.Cells[row, 5].Value = column.Formula;
            infoSheet.Cells[row, 6].Value = column.SelectOptions;
            infoSheet.Cells[row, 7].Value = column.CatalogueField;
            row++;
        }

        infoSheet.Cells[1, 1, 1, 7].Style.Font.Bold = true;
        infoSheet.Cells.AutoFitColumns();

        return await package.GetAsByteArrayAsync();
    }

    public async Task<int> ImportTemplateAsync(Stream excelStream, int companyId, string templateName)
    {
        using var package = new ExcelPackage(excelStream);
        var sheet = package.Workbook.Worksheets.FirstOrDefault();

        if (sheet == null)
            throw new ArgumentException("No worksheets found in Excel file");

        // Create template
        var template = new EstimationWorksheetTemplate
        {
            CompanyId = companyId,
            Name = templateName,
            WorksheetType = "Custom",
            CreatedDate = DateTime.UtcNow
        };

        _context.EstimationWorksheetTemplates.Add(template);
        await _context.SaveChangesAsync();

        // Read header row and create columns
        var lastColumn = sheet.Dimension?.End.Column ?? 0;
        var displayOrder = 0;

        for (int col = 1; col <= lastColumn; col++)
        {
            var headerValue = sheet.Cells[1, col].Text?.Trim();
            if (string.IsNullOrEmpty(headerValue))
                continue;

            // Generate column key from header
            var columnKey = GenerateColumnKey(headerValue);

            // Detect type from data in column
            var sampleValues = new List<string>();
            var lastRow = Math.Min(sheet.Dimension?.End.Row ?? 1, 100);
            for (int row = 2; row <= lastRow; row++)
            {
                var value = sheet.Cells[row, col].Text;
                if (!string.IsNullOrEmpty(value))
                    sampleValues.Add(value);
            }

            var dataType = DetectColumnType(sampleValues);

            var column = new EstimationWorksheetColumn
            {
                WorksheetTemplateId = template.Id,
                ColumnKey = columnKey,
                DisplayName = headerValue,
                DataType = dataType,
                Width = (int)(sheet.Column(col).Width * 7), // Approximate width to pixel
                DisplayOrder = displayOrder++,
                IsVisible = true,
                IsEditable = true
            };

            _context.EstimationWorksheetColumns.Add(column);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Imported template '{TemplateName}' with {ColumnCount} columns",
            templateName, displayOrder);

        return template.Id;
    }

    #region Private Helpers

    private async Task PopulateExcelSheet(ExcelWorksheet sheet, EstimationWorksheet worksheet)
    {
        var columns = worksheet.WorksheetTemplate?.Columns
            .Where(c => !c.IsDeleted && c.IsVisible)
            .OrderBy(c => c.DisplayOrder)
            .ToList() ?? new List<EstimationWorksheetColumn>();
        var rows = worksheet.Rows.Where(r => !r.IsDeleted).OrderBy(r => r.RowNumber).ToList();

        // Write header row
        var colIndex = 1;
        foreach (var column in columns)
        {
            var cell = sheet.Cells[1, colIndex];
            cell.Value = column.DisplayName;

            // Style header
            cell.Style.Font.Bold = true;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
            cell.Style.Font.Color.SetColor(Color.White);
            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            // Set column width
            sheet.Column(colIndex).Width = Math.Max(10, column.Width / 7.0);

            colIndex++;
        }

        // Write data rows
        var rowIndex = 2;
        foreach (var row in rows)
        {
            if (row.IsGroupHeader)
            {
                // Style group header - get group name from row data
                var groupName = GetGroupNameFromRowData(row.RowData);
                sheet.Cells[rowIndex, 1].Value = groupName;
                sheet.Cells[rowIndex, 1, rowIndex, columns.Count].Merge = true;
                sheet.Cells[rowIndex, 1].Style.Font.Bold = true;
                sheet.Cells[rowIndex, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[rowIndex, 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }
            else
            {
                var rowData = ParseRowData(row.RowData);

                colIndex = 1;
                foreach (var column in columns)
                {
                    var cell = sheet.Cells[rowIndex, colIndex];

                    if (rowData.TryGetValue(column.ColumnKey, out var value))
                    {
                        if (value != null)
                        {
                            // Set value and format based on data type
                            switch (column.DataType.ToLower())
                            {
                                case "number":
                                case "computed":
                                    if (decimal.TryParse(value.ToString(), out var numValue))
                                    {
                                        cell.Value = numValue;
                                        cell.Style.Numberformat.Format = column.DecimalPlaces.HasValue
                                            ? $"#,##0.{new string('0', column.DecimalPlaces.Value)}"
                                            : "#,##0.00";
                                    }
                                    break;

                                case "currency":
                                    if (decimal.TryParse(value.ToString(), out var currValue))
                                    {
                                        cell.Value = currValue;
                                        cell.Style.Numberformat.Format = "$#,##0.00";
                                    }
                                    break;

                                case "date":
                                    if (DateTime.TryParse(value.ToString(), out var dateValue))
                                    {
                                        cell.Value = dateValue;
                                        cell.Style.Numberformat.Format = "yyyy-mm-dd";
                                    }
                                    break;

                                default:
                                    cell.Value = value.ToString();
                                    break;
                            }
                        }
                    }

                    colIndex++;
                }
            }

            rowIndex++;
        }

        // Add totals row
        var totalsRow = rowIndex;
        sheet.Cells[totalsRow, 1].Value = "TOTALS";
        sheet.Cells[totalsRow, 1].Style.Font.Bold = true;

        colIndex = 1;
        foreach (var column in columns)
        {
            if (column.DataType.ToLower() is "number" or "currency" or "computed")
            {
                var columnLetter = GetColumnLetter(colIndex);
                sheet.Cells[totalsRow, colIndex].Formula = $"SUM({columnLetter}2:{columnLetter}{rowIndex - 1})";
                sheet.Cells[totalsRow, colIndex].Style.Font.Bold = true;

                if (column.DataType.ToLower() == "currency")
                    sheet.Cells[totalsRow, colIndex].Style.Numberformat.Format = "$#,##0.00";
                else
                    sheet.Cells[totalsRow, colIndex].Style.Numberformat.Format = "#,##0.00";
            }
            colIndex++;
        }

        // Style totals row
        if (columns.Count > 0)
        {
            sheet.Cells[totalsRow, 1, totalsRow, columns.Count].Style.Border.Top.Style = ExcelBorderStyle.Double;
            sheet.Cells[totalsRow, 1, totalsRow, columns.Count].Style.Fill.PatternType = ExcelFillStyle.Solid;
            sheet.Cells[totalsRow, 1, totalsRow, columns.Count].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(217, 225, 242));
        }

        // Auto-fit columns (with max width)
        for (int i = 1; i <= columns.Count; i++)
        {
            sheet.Column(i).AutoFit();
            if (sheet.Column(i).Width > 50)
                sheet.Column(i).Width = 50;
        }

        await Task.CompletedTask;
    }

    private string GetGroupNameFromRowData(string rowDataJson)
    {
        if (string.IsNullOrWhiteSpace(rowDataJson) || rowDataJson == "{}")
            return "Group";

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(rowDataJson);
            if (data == null)
                return "Group";

            // Try to get group_name or description
            if (data.TryGetValue("group_name", out var groupNameElement))
                return groupNameElement.GetString() ?? "Group";

            if (data.TryGetValue("description", out var descElement))
                return descElement.GetString() ?? "Group";

            return "Group";
        }
        catch
        {
            return "Group";
        }
    }

    private async Task PopulateSummarySheet(ExcelWorksheet sheet, EstimationRevisionPackage package)
    {
        sheet.Cells[1, 1].Value = "Package Summary";
        sheet.Cells[1, 1].Style.Font.Bold = true;
        sheet.Cells[1, 1].Style.Font.Size = 14;

        sheet.Cells[3, 1].Value = "Package Name:";
        sheet.Cells[3, 2].Value = package.PackageName;

        sheet.Cells[4, 1].Value = "Revision:";
        sheet.Cells[4, 2].Value = package.EstimationRevision?.RevisionLetter;

        sheet.Cells[6, 1].Value = "Worksheet";
        sheet.Cells[6, 2].Value = "Type";
        sheet.Cells[6, 3].Value = "Rows";
        sheet.Cells[6, 4].Value = "Total Cost";
        sheet.Cells[6, 1, 6, 4].Style.Font.Bold = true;

        var row = 7;
        foreach (var ws in package.Worksheets)
        {
            sheet.Cells[row, 1].Value = ws.Name;
            sheet.Cells[row, 2].Value = ws.WorksheetType;
            sheet.Cells[row, 3].Value = ws.Rows.Count(r => !r.IsDeleted && !r.IsGroupHeader);
            sheet.Cells[row, 4].Value = ws.TotalCost;
            sheet.Cells[row, 4].Style.Numberformat.Format = "$#,##0.00";
            row++;
        }

        sheet.Cells[row + 1, 3].Value = "Package Total:";
        sheet.Cells[row + 1, 3].Style.Font.Bold = true;
        sheet.Cells[row + 1, 4].Value = package.PackageTotal;
        sheet.Cells[row + 1, 4].Style.Numberformat.Format = "$#,##0.00";
        sheet.Cells[row + 1, 4].Style.Font.Bold = true;

        sheet.Cells.AutoFitColumns();

        await Task.CompletedTask;
    }

    private async Task PopulateRevisionSummarySheet(ExcelWorksheet sheet, EstimationRevision revision)
    {
        sheet.Cells[1, 1].Value = $"Estimation: {revision.Estimation?.Name}";
        sheet.Cells[1, 1].Style.Font.Bold = true;
        sheet.Cells[1, 1].Style.Font.Size = 16;

        sheet.Cells[2, 1].Value = $"Revision {revision.RevisionLetter}";
        sheet.Cells[2, 1].Style.Font.Size = 14;

        var row = 4;
        sheet.Cells[row, 1].Value = "Cost Summary";
        sheet.Cells[row, 1].Style.Font.Bold = true;
        row++;

        sheet.Cells[row, 1].Value = "Material Cost:";
        sheet.Cells[row, 2].Value = revision.TotalMaterialCost;
        sheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
        row++;

        sheet.Cells[row, 1].Value = "Labor Cost:";
        sheet.Cells[row, 2].Value = revision.TotalLaborCost;
        sheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
        row++;

        // Calculate subtotal
        var subtotal = revision.TotalMaterialCost + revision.TotalLaborCost;
        sheet.Cells[row, 1].Value = "Subtotal:";
        sheet.Cells[row, 2].Value = subtotal;
        sheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
        sheet.Cells[row, 1, row, 2].Style.Font.Bold = true;
        row++;

        sheet.Cells[row, 1].Value = $"Overhead ({revision.OverheadPercentage}%):";
        sheet.Cells[row, 2].Value = revision.OverheadAmount;
        sheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
        row++;

        sheet.Cells[row, 1].Value = $"Margin ({revision.MarginPercentage}%):";
        sheet.Cells[row, 2].Value = revision.MarginAmount;
        sheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
        row++;

        sheet.Cells[row, 1].Value = "TOTAL:";
        sheet.Cells[row, 2].Value = revision.TotalAmount;
        sheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
        sheet.Cells[row, 1, row, 2].Style.Font.Bold = true;
        sheet.Cells[row, 1, row, 2].Style.Font.Size = 12;

        sheet.Cells.AutoFitColumns();

        await Task.CompletedTask;
    }

    private Dictionary<string, object?> ParseRowData(string rowDataJson)
    {
        if (string.IsNullOrWhiteSpace(rowDataJson) || rowDataJson == "{}")
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(rowDataJson);
            if (data == null)
                return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in data)
            {
                result[kvp.Key] = GetJsonElementValue(kvp.Value);
            }
            return result;
        }
        catch
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private object? GetJsonElementValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.GetDecimal(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }

    private object? ConvertCellValue(object? value, string dataType)
    {
        if (value == null)
            return null;

        var stringValue = value.ToString()?.Trim();
        if (string.IsNullOrEmpty(stringValue))
            return null;

        return dataType.ToLower() switch
        {
            "number" or "currency" or "computed" =>
                decimal.TryParse(stringValue, out var d) ? d : (object?)null,
            "date" =>
                DateTime.TryParse(stringValue, out var dt) ? dt.ToString("yyyy-MM-dd") : stringValue,
            "checkbox" =>
                bool.TryParse(stringValue, out var b) ? b : stringValue.ToLower() is "yes" or "1" or "true",
            _ => stringValue
        };
    }

    private string GetColumnLetter(int columnNumber)
    {
        var result = "";
        while (columnNumber > 0)
        {
            columnNumber--;
            result = (char)('A' + columnNumber % 26) + result;
            columnNumber /= 26;
        }
        return result;
    }

    private string GetValidSheetName(string name, ExcelWorksheets worksheets)
    {
        // Excel sheet name max length is 31 characters
        var baseName = name.Length > 28 ? name.Substring(0, 28) : name;

        // Remove invalid characters
        baseName = new string(baseName.Where(c => c != '[' && c != ']' && c != '*' && c != '/' && c != '\\' && c != '?' && c != ':').ToArray());

        var sheetName = baseName;
        var counter = 1;

        while (worksheets.Any(ws => ws.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase)))
        {
            sheetName = $"{baseName}_{counter}";
            counter++;
        }

        return sheetName;
    }

    private string DetectColumnType(IList<string> sampleValues)
    {
        if (!sampleValues.Any())
            return "text";

        var allNumbers = sampleValues.All(v => decimal.TryParse(v, out _));
        var allDates = sampleValues.All(v => DateTime.TryParse(v, out _));
        var hasCurrencySymbols = sampleValues.Any(v => v.Contains("$") || v.Contains("€") || v.Contains("£"));

        if (allNumbers && hasCurrencySymbols)
            return "currency";
        if (allNumbers)
            return "number";
        if (allDates)
            return "date";

        return "text";
    }

    private double CalculateSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0;

        s1 = s1.ToLower().Replace("_", " ").Replace("-", " ");
        s2 = s2.ToLower().Replace("_", " ").Replace("-", " ");

        if (s1 == s2)
            return 1;

        if (s1.Contains(s2) || s2.Contains(s1))
            return 0.8;

        // Simple word overlap score
        var words1 = s1.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var words2 = s2.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var commonWords = words1.Intersect(words2, StringComparer.OrdinalIgnoreCase).Count();
        var totalWords = words1.Length + words2.Length;

        return totalWords > 0 ? (2.0 * commonWords) / totalWords : 0;
    }

    private string GenerateColumnKey(string headerName)
    {
        // Convert to snake_case
        var key = headerName.ToLower()
            .Replace(" ", "_")
            .Replace("-", "_")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("/", "_")
            .Replace("\\", "_");

        // Remove consecutive underscores
        while (key.Contains("__"))
            key = key.Replace("__", "_");

        // Remove leading/trailing underscores
        key = key.Trim('_');

        // Ensure it starts with a letter
        if (!char.IsLetter(key[0]))
            key = "col_" + key;

        return key;
    }

    #endregion
}
