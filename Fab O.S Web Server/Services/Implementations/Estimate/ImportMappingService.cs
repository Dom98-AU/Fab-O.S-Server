using System.Data;
using System.Text;
using System.Text.Json;
using ExcelDataReader;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace FabOS.WebServer.Services.Implementations.Estimate;

/// <summary>
/// Service for managing import mappings on worksheet templates.
/// Handles file parsing, column extraction, and mapping configuration.
/// </summary>
public class ImportMappingService : IImportMappingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ImportMappingService> _logger;
    private static bool _encodingRegistered = false;

    public ImportMappingService(
        ApplicationDbContext context,
        ILogger<ImportMappingService> logger)
    {
        _context = context;
        _logger = logger;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        // Register code page encoding provider for ExcelDataReader (required for .xls files)
        if (!_encodingRegistered)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _encodingRegistered = true;
        }
    }

    /// <summary>
    /// Determines if a file is the old .xls format (BIFF) based on extension
    /// </summary>
    private static bool IsOldExcelFormat(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return false;
        return fileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase)
               && !fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ImportFileParseResult> ParseFileHeadersAsync(Stream fileStream, string fileName, string? sheetName = null)
    {
        var result = new ImportFileParseResult();

        try
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            result.FileType = extension.TrimStart('.');

            switch (extension)
            {
                case ".xlsx":
                case ".xls":
                    result = await ParseExcelHeadersAsync(fileStream, fileName, sheetName);
                    break;
                case ".csv":
                    result = await ParseCsvHeadersAsync(fileStream);
                    break;
                case ".pdf":
                    result = await ParsePdfHeadersAsync(fileStream);
                    break;
                default:
                    result.Success = false;
                    result.ErrorMessage = $"Unsupported file type: {extension}";
                    break;
            }

            result.FileType = extension.TrimStart('.');
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing file headers for {FileName}", fileName);
            result.Success = false;
            result.ErrorMessage = $"Error parsing file: {ex.Message}";
        }

        return result;
    }

    public async Task<ImportFilePreviewResult> ParseFilePreviewAsync(Stream fileStream, string fileName, int headerRowIndex = 0, int dataStartRowIndex = 1, int rowCount = 5, string? sheetName = null)
    {
        var result = new ImportFilePreviewResult();

        try
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            switch (extension)
            {
                case ".xlsx":
                case ".xls":
                    result = await ParseExcelPreviewAsync(fileStream, fileName, headerRowIndex, dataStartRowIndex, rowCount, sheetName);
                    break;
                case ".csv":
                    result = await ParseCsvPreviewAsync(fileStream, headerRowIndex, dataStartRowIndex, rowCount);
                    break;
                case ".pdf":
                    result = await ParsePdfPreviewAsync(fileStream, headerRowIndex, dataStartRowIndex, rowCount);
                    break;
                default:
                    result.Success = false;
                    result.ErrorMessage = $"Unsupported file type: {extension}";
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing file preview for {FileName}", fileName);
            result.Success = false;
            result.ErrorMessage = $"Error parsing file: {ex.Message}";
        }

        return result;
    }

    public async Task<List<string>> GetExcelSheetNamesAsync(Stream fileStream, string? fileName = null)
    {
        var sheetNames = new List<string>();

        try
        {
            // Reset stream position if possible
            if (fileStream.CanSeek)
            {
                fileStream.Position = 0;
            }

            if (IsOldExcelFormat(fileName))
            {
                // Use ExcelDataReader for .xls files
                using var reader = ExcelReaderFactory.CreateReader(fileStream);
                var result = reader.AsDataSet();
                foreach (DataTable table in result.Tables)
                {
                    sheetNames.Add(table.TableName);
                }
            }
            else
            {
                // Use EPPlus for .xlsx files
                using var package = new ExcelPackage(fileStream);
                foreach (var sheet in package.Workbook.Worksheets)
                {
                    sheetNames.Add(sheet.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Excel sheet names");
        }

        return await Task.FromResult(sheetNames);
    }

    public async Task<TemplateImportMapping> SaveMappingAsync(int templateId, TemplateImportMappingDto mappingDto)
    {
        var existingMapping = await _context.TemplateImportMappings
            .FirstOrDefaultAsync(m => m.WorksheetTemplateId == templateId && m.Name == mappingDto.Name && !m.IsDeleted);

        if (existingMapping != null)
        {
            // Update existing mapping
            existingMapping.SampleFileName = mappingDto.SampleFileName;
            existingMapping.SampleFileType = mappingDto.SampleFileType;
            existingMapping.HeaderRowIndex = mappingDto.HeaderRowIndex;
            existingMapping.DataStartRowIndex = mappingDto.DataStartRowIndex;
            existingMapping.SheetName = mappingDto.SheetName;
            existingMapping.ColumnMappingsJson = JsonSerializer.Serialize(mappingDto.ColumnMappings);
            existingMapping.IsDefault = mappingDto.IsDefault;
            existingMapping.ModifiedDate = DateTime.UtcNow;

            // If this is marked as default, unmark others
            if (mappingDto.IsDefault)
            {
                await UnmarkOtherDefaults(templateId, existingMapping.Id);
            }

            await _context.SaveChangesAsync();
            return existingMapping;
        }
        else
        {
            // Create new mapping
            var newMapping = new TemplateImportMapping
            {
                WorksheetTemplateId = templateId,
                Name = mappingDto.Name,
                SampleFileName = mappingDto.SampleFileName,
                SampleFileType = mappingDto.SampleFileType,
                HeaderRowIndex = mappingDto.HeaderRowIndex,
                DataStartRowIndex = mappingDto.DataStartRowIndex,
                SheetName = mappingDto.SheetName,
                ColumnMappingsJson = JsonSerializer.Serialize(mappingDto.ColumnMappings),
                IsDefault = mappingDto.IsDefault,
                CreatedDate = DateTime.UtcNow
            };

            // If this is marked as default, unmark others
            if (mappingDto.IsDefault)
            {
                await UnmarkOtherDefaults(templateId, 0);
            }

            _context.TemplateImportMappings.Add(newMapping);
            await _context.SaveChangesAsync();
            return newMapping;
        }
    }

    public async Task<TemplateImportMapping?> GetMappingAsync(int templateId, string? mappingName = null)
    {
        if (string.IsNullOrEmpty(mappingName))
        {
            // Get default mapping
            return await _context.TemplateImportMappings
                .FirstOrDefaultAsync(m => m.WorksheetTemplateId == templateId && m.IsDefault && !m.IsDeleted);
        }

        return await _context.TemplateImportMappings
            .FirstOrDefaultAsync(m => m.WorksheetTemplateId == templateId && m.Name == mappingName && !m.IsDeleted);
    }

    public async Task<List<TemplateImportMapping>> GetAllMappingsAsync(int templateId)
    {
        return await _context.TemplateImportMappings
            .Where(m => m.WorksheetTemplateId == templateId && !m.IsDeleted)
            .OrderByDescending(m => m.IsDefault)
            .ThenBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<bool> DeleteMappingAsync(int mappingId)
    {
        var mapping = await _context.TemplateImportMappings.FindAsync(mappingId);
        if (mapping == null) return false;

        mapping.IsDeleted = true;
        mapping.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Dictionary<string, object>>> ApplyMappingAsync(Stream fileStream, string fileName, TemplateImportMapping mapping)
    {
        var result = new List<Dictionary<string, object>>();
        var columnMappings = JsonSerializer.Deserialize<List<ColumnMappingDto>>(mapping.ColumnMappingsJson) ?? new();

        try
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            switch (extension)
            {
                case ".xlsx":
                case ".xls":
                    result = await ApplyMappingToExcelAsync(fileStream, mapping, columnMappings);
                    break;
                case ".csv":
                    result = await ApplyMappingToCsvAsync(fileStream, mapping, columnMappings);
                    break;
                case ".pdf":
                    result = await ApplyMappingToPdfAsync(fileStream, mapping, columnMappings);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying mapping to file {FileName}", fileName);
        }

        return result;
    }

    #region Excel Parsing

    private async Task<ImportFileParseResult> ParseExcelHeadersAsync(Stream fileStream, string fileName, string? sheetName)
    {
        var result = new ImportFileParseResult { Success = true, FileType = IsOldExcelFormat(fileName) ? "xls" : "xlsx" };

        // Reset stream position if possible
        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        if (IsOldExcelFormat(fileName))
        {
            // Use ExcelDataReader for .xls files
            return await ParseExcelHeadersWithDataReader(fileStream, sheetName, result);
        }
        else
        {
            // Use EPPlus for .xlsx files
            return await ParseExcelHeadersWithEPPlus(fileStream, sheetName, result);
        }
    }

    private async Task<ImportFileParseResult> ParseExcelHeadersWithEPPlus(Stream fileStream, string? sheetName, ImportFileParseResult result)
    {
        using var package = new ExcelPackage(fileStream);

        // Get sheet names
        foreach (var sheet in package.Workbook.Worksheets)
        {
            result.SheetNames.Add(sheet.Name);
        }

        if (!result.SheetNames.Any())
        {
            result.Success = false;
            result.ErrorMessage = "No worksheets found in Excel file";
            return result;
        }

        // Select sheet
        var worksheet = string.IsNullOrEmpty(sheetName)
            ? package.Workbook.Worksheets.First()
            : package.Workbook.Worksheets[sheetName] ?? package.Workbook.Worksheets.First();

        result.DetectedSheetName = worksheet.Name;

        // Detect header row (first non-empty row)
        int headerRow = 1;
        for (int row = 1; row <= 10; row++)
        {
            var hasContent = false;
            for (int col = 1; col <= 50; col++)
            {
                if (!string.IsNullOrWhiteSpace(worksheet.Cells[row, col].Text))
                {
                    hasContent = true;
                    break;
                }
            }
            if (hasContent)
            {
                headerRow = row;
                break;
            }
        }

        result.DetectedHeaderRow = headerRow - 1; // Convert to 0-based

        // Extract column headers
        var colIndex = 0;
        for (int col = 1; col <= 100; col++)
        {
            var cellValue = worksheet.Cells[headerRow, col].Text?.Trim();
            if (string.IsNullOrEmpty(cellValue))
            {
                // Check if there are more columns after empty ones
                var hasMoreContent = false;
                for (int nextCol = col + 1; nextCol <= col + 5; nextCol++)
                {
                    if (!string.IsNullOrWhiteSpace(worksheet.Cells[headerRow, nextCol].Text))
                    {
                        hasMoreContent = true;
                        break;
                    }
                }
                if (!hasMoreContent) break;
                continue;
            }

            var sourceColumn = new SourceColumnInfo
            {
                Index = colIndex,
                Name = cellValue,
                SampleValues = new List<string>()
            };

            // Get sample values from next few rows
            for (int row = headerRow + 1; row <= headerRow + 5 && row <= worksheet.Dimension?.Rows; row++)
            {
                var sampleValue = worksheet.Cells[row, col].Text?.Trim() ?? "";
                sourceColumn.SampleValues.Add(sampleValue);
            }

            // Detect data type from sample values
            sourceColumn.DetectedDataType = DetectDataType(sourceColumn.SampleValues);

            result.Columns.Add(sourceColumn);
            colIndex++;
        }

        return await Task.FromResult(result);
    }

    private async Task<ImportFileParseResult> ParseExcelHeadersWithDataReader(Stream fileStream, string? sheetName, ImportFileParseResult result)
    {
        using var reader = ExcelReaderFactory.CreateReader(fileStream);
        var dataSet = reader.AsDataSet();

        // Get sheet names
        foreach (DataTable table in dataSet.Tables)
        {
            result.SheetNames.Add(table.TableName);
        }

        if (!result.SheetNames.Any())
        {
            result.Success = false;
            result.ErrorMessage = "No worksheets found in Excel file";
            return result;
        }

        // Select sheet
        var selectedTable = string.IsNullOrEmpty(sheetName)
            ? dataSet.Tables[0]
            : dataSet.Tables[sheetName] ?? dataSet.Tables[0];

        result.DetectedSheetName = selectedTable.TableName;

        if (selectedTable.Rows.Count == 0)
        {
            result.Success = false;
            result.ErrorMessage = "Worksheet is empty";
            return result;
        }

        // Detect header row (first non-empty row)
        int headerRow = 0;
        for (int row = 0; row < Math.Min(10, selectedTable.Rows.Count); row++)
        {
            var hasContent = false;
            for (int col = 0; col < Math.Min(50, selectedTable.Columns.Count); col++)
            {
                var cellValue = selectedTable.Rows[row][col]?.ToString();
                if (!string.IsNullOrWhiteSpace(cellValue))
                {
                    hasContent = true;
                    break;
                }
            }
            if (hasContent)
            {
                headerRow = row;
                break;
            }
        }

        result.DetectedHeaderRow = headerRow;

        // Extract column headers
        var colIndex = 0;
        for (int col = 0; col < selectedTable.Columns.Count; col++)
        {
            var cellValue = selectedTable.Rows[headerRow][col]?.ToString()?.Trim();
            if (string.IsNullOrEmpty(cellValue))
            {
                // Check if there are more columns after empty ones
                var hasMoreContent = false;
                for (int nextCol = col + 1; nextCol <= col + 5 && nextCol < selectedTable.Columns.Count; nextCol++)
                {
                    if (!string.IsNullOrWhiteSpace(selectedTable.Rows[headerRow][nextCol]?.ToString()))
                    {
                        hasMoreContent = true;
                        break;
                    }
                }
                if (!hasMoreContent) break;
                continue;
            }

            var sourceColumn = new SourceColumnInfo
            {
                Index = colIndex,
                Name = cellValue,
                SampleValues = new List<string>()
            };

            // Get sample values from next few rows
            for (int row = headerRow + 1; row <= headerRow + 5 && row < selectedTable.Rows.Count; row++)
            {
                var sampleValue = selectedTable.Rows[row][col]?.ToString()?.Trim() ?? "";
                sourceColumn.SampleValues.Add(sampleValue);
            }

            // Detect data type from sample values
            sourceColumn.DetectedDataType = DetectDataType(sourceColumn.SampleValues);

            result.Columns.Add(sourceColumn);
            colIndex++;
        }

        return await Task.FromResult(result);
    }

    private async Task<ImportFilePreviewResult> ParseExcelPreviewAsync(Stream fileStream, string fileName, int headerRowIndex, int dataStartRowIndex, int rowCount, string? sheetName)
    {
        // Reset stream position if possible
        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        if (IsOldExcelFormat(fileName))
        {
            return await ParseExcelPreviewWithDataReader(fileStream, headerRowIndex, dataStartRowIndex, rowCount, sheetName);
        }
        else
        {
            return await ParseExcelPreviewWithEPPlus(fileStream, headerRowIndex, dataStartRowIndex, rowCount, sheetName);
        }
    }

    private async Task<ImportFilePreviewResult> ParseExcelPreviewWithEPPlus(Stream fileStream, int headerRowIndex, int dataStartRowIndex, int rowCount, string? sheetName)
    {
        var result = new ImportFilePreviewResult { Success = true };

        using var package = new ExcelPackage(fileStream);

        var worksheet = string.IsNullOrEmpty(sheetName)
            ? package.Workbook.Worksheets.First()
            : package.Workbook.Worksheets[sheetName] ?? package.Workbook.Worksheets.First();

        if (worksheet.Dimension == null)
        {
            result.Success = false;
            result.ErrorMessage = "Worksheet is empty";
            return result;
        }

        int excelHeaderRow = headerRowIndex + 1; // Convert to 1-based
        int excelDataStart = dataStartRowIndex + 1;

        // Extract headers with sample values and data type detection
        var columnNames = new List<string>();
        for (int col = 1; col <= worksheet.Dimension.Columns; col++)
        {
            var cellValue = worksheet.Cells[excelHeaderRow, col].Text?.Trim();
            if (string.IsNullOrEmpty(cellValue)) continue;

            // Collect sample values from data rows for type detection
            var sampleValues = new List<string>();
            for (int sampleRow = excelDataStart; sampleRow < excelDataStart + 5 && sampleRow <= worksheet.Dimension.Rows; sampleRow++)
            {
                var sampleValue = worksheet.Cells[sampleRow, col].Text?.Trim() ?? "";
                sampleValues.Add(sampleValue);
            }

            columnNames.Add(cellValue);
            result.Columns.Add(new SourceColumnInfo
            {
                Index = col - 1,
                Name = cellValue,
                SampleValues = sampleValues,
                DetectedDataType = DetectDataType(sampleValues)
            });
        }

        // Extract preview rows
        result.TotalRowCount = worksheet.Dimension.Rows - excelDataStart + 1;

        for (int row = excelDataStart; row < excelDataStart + rowCount && row <= worksheet.Dimension.Rows; row++)
        {
            var rowData = new Dictionary<string, object>();
            int colIndex = 0;

            for (int col = 1; col <= worksheet.Dimension.Columns; col++)
            {
                var cellValue = worksheet.Cells[excelHeaderRow, col].Text?.Trim();
                if (string.IsNullOrEmpty(cellValue)) continue;

                var value = worksheet.Cells[row, col].Value;
                rowData[cellValue] = value ?? "";
                colIndex++;
            }

            result.PreviewRows.Add(rowData);
        }

        return await Task.FromResult(result);
    }

    private async Task<ImportFilePreviewResult> ParseExcelPreviewWithDataReader(Stream fileStream, int headerRowIndex, int dataStartRowIndex, int rowCount, string? sheetName)
    {
        var result = new ImportFilePreviewResult { Success = true };

        using var reader = ExcelReaderFactory.CreateReader(fileStream);
        var dataSet = reader.AsDataSet();

        var selectedTable = string.IsNullOrEmpty(sheetName)
            ? dataSet.Tables[0]
            : dataSet.Tables[sheetName] ?? dataSet.Tables[0];

        if (selectedTable.Rows.Count == 0)
        {
            result.Success = false;
            result.ErrorMessage = "Worksheet is empty";
            return result;
        }

        // Extract headers with sample values and data type detection
        var columnNames = new List<string>();
        for (int col = 0; col < selectedTable.Columns.Count; col++)
        {
            var cellValue = selectedTable.Rows[headerRowIndex][col]?.ToString()?.Trim();
            if (string.IsNullOrEmpty(cellValue)) continue;

            // Collect sample values from data rows for type detection
            var sampleValues = new List<string>();
            for (int sampleRow = dataStartRowIndex; sampleRow < dataStartRowIndex + 5 && sampleRow < selectedTable.Rows.Count; sampleRow++)
            {
                var sampleValue = selectedTable.Rows[sampleRow][col]?.ToString()?.Trim() ?? "";
                sampleValues.Add(sampleValue);
            }

            columnNames.Add(cellValue);
            result.Columns.Add(new SourceColumnInfo
            {
                Index = col,
                Name = cellValue,
                SampleValues = sampleValues,
                DetectedDataType = DetectDataType(sampleValues)
            });
        }

        // Extract preview rows
        result.TotalRowCount = selectedTable.Rows.Count - dataStartRowIndex;

        for (int row = dataStartRowIndex; row < dataStartRowIndex + rowCount && row < selectedTable.Rows.Count; row++)
        {
            var rowData = new Dictionary<string, object>();

            for (int col = 0; col < selectedTable.Columns.Count; col++)
            {
                var headerValue = selectedTable.Rows[headerRowIndex][col]?.ToString()?.Trim();
                if (string.IsNullOrEmpty(headerValue)) continue;

                var value = selectedTable.Rows[row][col];
                rowData[headerValue] = value ?? "";
            }

            result.PreviewRows.Add(rowData);
        }

        return await Task.FromResult(result);
    }

    private async Task<List<Dictionary<string, object>>> ApplyMappingToExcelAsync(Stream fileStream, TemplateImportMapping mapping, List<ColumnMappingDto> columnMappings)
    {
        var result = new List<Dictionary<string, object>>();

        using var package = new ExcelPackage(fileStream);

        var worksheet = string.IsNullOrEmpty(mapping.SheetName)
            ? package.Workbook.Worksheets.First()
            : package.Workbook.Worksheets[mapping.SheetName] ?? package.Workbook.Worksheets.First();

        if (worksheet.Dimension == null) return result;

        int dataStartRow = mapping.DataStartRowIndex + 1; // Convert to 1-based

        for (int row = dataStartRow; row <= worksheet.Dimension.Rows; row++)
        {
            var mappedRow = new Dictionary<string, object>();

            foreach (var columnMap in columnMappings)
            {
                var sourceCol = columnMap.SourceColumnIndex + 1; // Convert to 1-based
                var value = worksheet.Cells[row, sourceCol].Value;

                // Apply transformation
                var transformedValue = ApplyTransform(value, columnMap.TransformType, columnMap.DefaultValue);
                mappedRow[columnMap.TargetColumnKey] = transformedValue;
            }

            // Skip empty rows
            if (mappedRow.Values.All(v => v == null || string.IsNullOrWhiteSpace(v.ToString())))
                continue;

            result.Add(mappedRow);
        }

        return await Task.FromResult(result);
    }

    #endregion

    #region CSV Parsing

    private async Task<ImportFileParseResult> ParseCsvHeadersAsync(Stream fileStream)
    {
        var result = new ImportFileParseResult { Success = true, FileType = "csv" };

        using var reader = new StreamReader(fileStream);
        var firstLine = await reader.ReadLineAsync();

        if (string.IsNullOrEmpty(firstLine))
        {
            result.Success = false;
            result.ErrorMessage = "CSV file is empty";
            return result;
        }

        // Parse headers
        var headers = ParseCsvLine(firstLine);
        var sampleLines = new List<string[]>();

        // Read sample rows
        for (int i = 0; i < 5 && !reader.EndOfStream; i++)
        {
            var line = await reader.ReadLineAsync();
            if (line != null)
            {
                sampleLines.Add(ParseCsvLine(line));
            }
        }

        for (int i = 0; i < headers.Length; i++)
        {
            var sourceColumn = new SourceColumnInfo
            {
                Index = i,
                Name = headers[i].Trim(),
                SampleValues = sampleLines.Select(l => i < l.Length ? l[i] : "").ToList()
            };
            sourceColumn.DetectedDataType = DetectDataType(sourceColumn.SampleValues);
            result.Columns.Add(sourceColumn);
        }

        return result;
    }

    private async Task<ImportFilePreviewResult> ParseCsvPreviewAsync(Stream fileStream, int headerRowIndex, int dataStartRowIndex, int rowCount)
    {
        var result = new ImportFilePreviewResult { Success = true };

        using var reader = new StreamReader(fileStream);
        var lines = new List<string>();

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (line != null) lines.Add(line);
        }

        if (lines.Count <= headerRowIndex)
        {
            result.Success = false;
            result.ErrorMessage = "CSV file has no data";
            return result;
        }

        // Parse headers
        var headers = ParseCsvLine(lines[headerRowIndex]);
        for (int i = 0; i < headers.Length; i++)
        {
            result.Columns.Add(new SourceColumnInfo
            {
                Index = i,
                Name = headers[i].Trim()
            });
        }

        result.TotalRowCount = lines.Count - dataStartRowIndex;

        // Parse preview rows
        for (int i = dataStartRowIndex; i < dataStartRowIndex + rowCount && i < lines.Count; i++)
        {
            var values = ParseCsvLine(lines[i]);
            var rowData = new Dictionary<string, object>();

            for (int j = 0; j < headers.Length && j < values.Length; j++)
            {
                rowData[headers[j].Trim()] = values[j];
            }

            result.PreviewRows.Add(rowData);
        }

        return result;
    }

    private async Task<List<Dictionary<string, object>>> ApplyMappingToCsvAsync(Stream fileStream, TemplateImportMapping mapping, List<ColumnMappingDto> columnMappings)
    {
        var result = new List<Dictionary<string, object>>();

        using var reader = new StreamReader(fileStream);
        var lines = new List<string>();

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (line != null) lines.Add(line);
        }

        for (int i = mapping.DataStartRowIndex; i < lines.Count; i++)
        {
            var values = ParseCsvLine(lines[i]);
            var mappedRow = new Dictionary<string, object>();

            foreach (var columnMap in columnMappings)
            {
                var value = columnMap.SourceColumnIndex < values.Length ? values[columnMap.SourceColumnIndex] : null;
                var transformedValue = ApplyTransform(value, columnMap.TransformType, columnMap.DefaultValue);
                mappedRow[columnMap.TargetColumnKey] = transformedValue;
            }

            if (mappedRow.Values.All(v => v == null || string.IsNullOrWhiteSpace(v?.ToString())))
                continue;

            result.Add(mappedRow);
        }

        return result;
    }

    private string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var currentValue = "";

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentValue.Trim());
                currentValue = "";
            }
            else
            {
                currentValue += c;
            }
        }

        result.Add(currentValue.Trim());
        return result.ToArray();
    }

    #endregion

    #region PDF Parsing (Basic - Tables from PDF)

    private Task<ImportFileParseResult> ParsePdfHeadersAsync(Stream fileStream)
    {
        // PDF table extraction is complex - for now return a placeholder
        // In production, use PdfPig or similar library
        var result = new ImportFileParseResult
        {
            Success = false,
            FileType = "pdf",
            ErrorMessage = "PDF table extraction is not yet fully implemented. Please convert your PDF to Excel or CSV format."
        };

        return Task.FromResult(result);
    }

    private Task<ImportFilePreviewResult> ParsePdfPreviewAsync(Stream fileStream, int headerRowIndex, int dataStartRowIndex, int rowCount)
    {
        var result = new ImportFilePreviewResult
        {
            Success = false,
            ErrorMessage = "PDF table extraction is not yet fully implemented. Please convert your PDF to Excel or CSV format."
        };

        return Task.FromResult(result);
    }

    private Task<List<Dictionary<string, object>>> ApplyMappingToPdfAsync(Stream fileStream, TemplateImportMapping mapping, List<ColumnMappingDto> columnMappings)
    {
        // PDF mapping not yet implemented
        return Task.FromResult(new List<Dictionary<string, object>>());
    }

    #endregion

    #region Helpers

    private string DetectDataType(List<string> sampleValues)
    {
        var nonEmpty = sampleValues.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        if (!nonEmpty.Any()) return "text";

        // Check for numbers
        if (nonEmpty.All(v => decimal.TryParse(v.Replace("$", "").Replace(",", ""), out _)))
        {
            if (nonEmpty.Any(v => v.Contains("$") || v.Contains(".")))
                return "currency";
            return "number";
        }

        // Check for dates
        if (nonEmpty.All(v => DateTime.TryParse(v, out _)))
            return "date";

        return "text";
    }

    private object ApplyTransform(object? value, string transformType, string? defaultValue)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return defaultValue ?? "";
        }

        var strValue = value.ToString() ?? "";

        return transformType.ToLowerInvariant() switch
        {
            "trim" => strValue.Trim(),
            "uppercase" => strValue.ToUpperInvariant(),
            "lowercase" => strValue.ToLowerInvariant(),
            _ => value
        };
    }

    private async Task UnmarkOtherDefaults(int templateId, int excludeMappingId)
    {
        var otherMappings = await _context.TemplateImportMappings
            .Where(m => m.WorksheetTemplateId == templateId && m.Id != excludeMappingId && m.IsDefault && !m.IsDeleted)
            .ToListAsync();

        foreach (var mapping in otherMappings)
        {
            mapping.IsDefault = false;
            mapping.ModifiedDate = DateTime.UtcNow;
        }
    }

    #endregion
}
