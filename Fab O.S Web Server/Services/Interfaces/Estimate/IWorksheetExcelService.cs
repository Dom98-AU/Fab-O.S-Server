using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces.Estimate;

/// <summary>
/// Service for importing and exporting worksheets to/from Excel.
/// </summary>
public interface IWorksheetExcelService
{
    /// <summary>
    /// Export a single worksheet to Excel.
    /// </summary>
    /// <param name="worksheetId">Worksheet ID</param>
    /// <returns>Excel file bytes</returns>
    Task<byte[]> ExportWorksheetAsync(int worksheetId);

    /// <summary>
    /// Export an entire package (all worksheets) to an Excel workbook.
    /// Each worksheet becomes a sheet in the workbook.
    /// </summary>
    /// <param name="packageId">Package ID</param>
    /// <returns>Excel file bytes</returns>
    Task<byte[]> ExportPackageAsync(int packageId);

    /// <summary>
    /// Export an entire revision (all packages and worksheets) to an Excel workbook.
    /// </summary>
    /// <param name="revisionId">Revision ID</param>
    /// <returns>Excel file bytes</returns>
    Task<byte[]> ExportRevisionAsync(int revisionId);

    /// <summary>
    /// Preview import - shows mapped data before committing.
    /// </summary>
    /// <param name="excelStream">Excel file stream</param>
    /// <param name="worksheetId">Target worksheet ID</param>
    /// <returns>Preview of how data will be imported</returns>
    Task<ImportPreview> PreviewImportAsync(Stream excelStream, int worksheetId);

    /// <summary>
    /// Import Excel data into a worksheet.
    /// </summary>
    /// <param name="worksheetId">Target worksheet ID</param>
    /// <param name="excelStream">Excel file stream</param>
    /// <param name="mapping">Column mapping configuration</param>
    /// <returns>Import result with success/failure details</returns>
    Task<ImportResult> ImportWorksheetAsync(int worksheetId, Stream excelStream, ColumnMapping mapping);

    /// <summary>
    /// Export a worksheet template to Excel for reference.
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <returns>Excel file bytes with column headers and formatting</returns>
    Task<byte[]> ExportTemplateAsync(int templateId);

    /// <summary>
    /// Import a worksheet template from Excel.
    /// </summary>
    /// <param name="excelStream">Excel file stream</param>
    /// <param name="companyId">Company ID</param>
    /// <param name="templateName">Name for the new template</param>
    /// <returns>Created template ID</returns>
    Task<int> ImportTemplateAsync(Stream excelStream, int companyId, string templateName);
}

/// <summary>
/// Preview of import operation before committing.
/// </summary>
public class ImportPreview
{
    /// <summary>
    /// Detected columns from Excel file.
    /// </summary>
    public IList<ExcelColumnInfo> ExcelColumns { get; set; } = new List<ExcelColumnInfo>();

    /// <summary>
    /// Target worksheet columns.
    /// </summary>
    public IList<WorksheetColumnInfo> WorksheetColumns { get; set; } = new List<WorksheetColumnInfo>();

    /// <summary>
    /// Suggested column mappings.
    /// </summary>
    public IList<ColumnMappingItem> SuggestedMappings { get; set; } = new List<ColumnMappingItem>();

    /// <summary>
    /// Sample rows from Excel (first 10 rows).
    /// </summary>
    public IList<Dictionary<string, object?>> SampleRows { get; set; } = new List<Dictionary<string, object?>>();

    /// <summary>
    /// Total row count in Excel.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Validation warnings.
    /// </summary>
    public IList<string> Warnings { get; set; } = new List<string>();
}

/// <summary>
/// Information about a column in the Excel file.
/// </summary>
public class ExcelColumnInfo
{
    public int ColumnIndex { get; set; }
    public string ColumnLetter { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DetectedType { get; set; }
    public IList<string> SampleValues { get; set; } = new List<string>();
}

/// <summary>
/// Information about a worksheet column.
/// </summary>
public class WorksheetColumnInfo
{
    public string ColumnKey { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsComputed { get; set; }
}

/// <summary>
/// Column mapping configuration for import.
/// </summary>
public class ColumnMapping
{
    /// <summary>
    /// Map of Excel column name to worksheet column key.
    /// </summary>
    public Dictionary<string, string> Mappings { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Row number to start importing from (1-based, default 2 to skip header).
    /// </summary>
    public int StartRow { get; set; } = 2;

    /// <summary>
    /// Sheet name or index to import from.
    /// </summary>
    public string? SheetName { get; set; }

    /// <summary>
    /// Sheet index (0-based) if SheetName not provided.
    /// </summary>
    public int SheetIndex { get; set; } = 0;

    /// <summary>
    /// Whether to skip empty rows.
    /// </summary>
    public bool SkipEmptyRows { get; set; } = true;

    /// <summary>
    /// Whether to skip the first row (header row).
    /// </summary>
    public bool SkipHeaderRow { get; set; } = true;

    /// <summary>
    /// Whether to replace existing rows (clear before import).
    /// </summary>
    public bool ReplaceExisting { get; set; } = false;
}

/// <summary>
/// Single column mapping item.
/// </summary>
public class ColumnMappingItem
{
    public int ExcelColumnIndex { get; set; }
    public string ExcelColumnName { get; set; } = string.Empty;
    public string WorksheetColumnKey { get; set; } = string.Empty;
    public string? TransformExpression { get; set; }
}

/// <summary>
/// Result of import operation.
/// </summary>
public class ImportResult
{
    public bool Success { get; set; }
    public int RowsImported { get; set; }
    public int RowsSkipped { get; set; }
    public int RowsWithErrors { get; set; }
    public IList<ImportRowError> Errors { get; set; } = new List<ImportRowError>();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Error for a specific row during import.
/// </summary>
public class ImportRowError
{
    public int RowNumber { get; set; }
    public string ColumnKey { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? OriginalValue { get; set; }
}
