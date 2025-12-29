using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service for managing import mappings on worksheet templates.
/// Handles file parsing, column extraction, and mapping configuration.
/// </summary>
public interface IImportMappingService
{
    /// <summary>
    /// Parse headers from an uploaded file (Excel, CSV, or PDF)
    /// </summary>
    Task<ImportFileParseResult> ParseFileHeadersAsync(Stream fileStream, string fileName, string? sheetName = null);

    /// <summary>
    /// Parse preview data from an uploaded file
    /// </summary>
    Task<ImportFilePreviewResult> ParseFilePreviewAsync(Stream fileStream, string fileName, int headerRowIndex = 0, int dataStartRowIndex = 1, int rowCount = 5, string? sheetName = null);

    /// <summary>
    /// Get available sheet names from an Excel file
    /// </summary>
    /// <param name="fileStream">The Excel file stream</param>
    /// <param name="fileName">Optional filename to detect .xls vs .xlsx format</param>
    Task<List<string>> GetExcelSheetNamesAsync(Stream fileStream, string? fileName = null);

    /// <summary>
    /// Save an import mapping configuration
    /// </summary>
    Task<TemplateImportMapping> SaveMappingAsync(int templateId, TemplateImportMappingDto mappingDto);

    /// <summary>
    /// Get import mapping for a template
    /// </summary>
    Task<TemplateImportMapping?> GetMappingAsync(int templateId, string? mappingName = null);

    /// <summary>
    /// Get all import mappings for a template
    /// </summary>
    Task<List<TemplateImportMapping>> GetAllMappingsAsync(int templateId);

    /// <summary>
    /// Delete an import mapping
    /// </summary>
    Task<bool> DeleteMappingAsync(int mappingId);

    /// <summary>
    /// Apply a saved mapping to transform imported data
    /// </summary>
    Task<List<Dictionary<string, object>>> ApplyMappingAsync(Stream fileStream, string fileName, TemplateImportMapping mapping);
}

/// <summary>
/// Result of parsing file headers
/// </summary>
public class ImportFileParseResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string FileType { get; set; } = string.Empty;
    public List<SourceColumnInfo> Columns { get; set; } = new();
    public List<string> SheetNames { get; set; } = new();
    public string? DetectedSheetName { get; set; }
    public int DetectedHeaderRow { get; set; }
}

/// <summary>
/// Information about a source column from the imported file
/// </summary>
public class SourceColumnInfo
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DetectedDataType { get; set; }
    public List<string> SampleValues { get; set; } = new();
}

/// <summary>
/// Result of parsing file with preview data
/// </summary>
public class ImportFilePreviewResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<SourceColumnInfo> Columns { get; set; } = new();
    public List<Dictionary<string, object>> PreviewRows { get; set; } = new();
    public int TotalRowCount { get; set; }
}

/// <summary>
/// DTO for saving/updating import mapping
/// </summary>
public class TemplateImportMappingDto
{
    public string Name { get; set; } = "Default";
    public string? SampleFileName { get; set; }
    public string? SampleFileType { get; set; }
    public int HeaderRowIndex { get; set; } = 0;
    public int DataStartRowIndex { get; set; } = 1;
    public string? SheetName { get; set; }
    public List<ColumnMappingDto> ColumnMappings { get; set; } = new();
    public bool IsDefault { get; set; } = true;
}

/// <summary>
/// Individual column mapping configuration
/// </summary>
public class ColumnMappingDto
{
    public int SourceColumnIndex { get; set; }
    public string SourceColumnName { get; set; } = string.Empty;
    public string TargetColumnKey { get; set; } = string.Empty;
    public string TargetColumnName { get; set; } = string.Empty;
    public string TransformType { get; set; } = "none"; // none, trim, uppercase, lowercase, format
    public string? DefaultValue { get; set; }
}
