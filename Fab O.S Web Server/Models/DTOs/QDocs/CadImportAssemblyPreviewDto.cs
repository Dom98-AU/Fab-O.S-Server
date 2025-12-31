using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

/// <summary>
/// Assembly preview during import, showing both identified and unidentified parts
/// </summary>
public class CadImportAssemblyPreviewDto
{
    public string TempAssemblyId { get; set; } = string.Empty; // Temporary ID for mapping
    public string? AssemblyMark { get; set; } // May be null if assembly itself needs identification
    public string? AssemblyName { get; set; }
    public bool NeedsIdentification { get; set; } // True if assembly mark is missing/unclear
    public string? SuggestedAssemblyMark { get; set; } // Auto-generated or original mark for reference when unidentified

    // Parts within this assembly
    public List<IdentifiedPartPreviewDto> IdentifiedParts { get; set; } = new();
    public List<UnidentifiedPartDto> UnidentifiedParts { get; set; } = new();

    public decimal TotalWeight { get; set; }
    public int TotalPartCount { get; set; }
}
