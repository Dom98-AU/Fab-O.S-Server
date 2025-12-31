using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

/// <summary>
/// Result of parsing a CAD file (SMLX/IFC) before final import.
/// Contains identified parts (with references) and unidentified parts (need user input).
/// </summary>
public class CadImportPreviewDto
{
    public string ImportSessionId { get; set; } = string.Empty; // Unique ID for this import session
    public int DrawingId { get; set; }
    public int DrawingRevisionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // SMLX, IFC

    /// <summary>
    /// Status of the import:
    /// - "Ready" = all parts identified, can import directly
    /// - "PendingReview" = has unidentified parts, needs user input
    /// - "Failed" = parsing failed
    /// </summary>
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }

    // Counts for summary
    public int TotalElementCount { get; set; }
    public int IdentifiedPartCount { get; set; }
    public int UnidentifiedPartCount { get; set; }
    public int AssemblyCount { get; set; }

    // Parts that are fully identified (have part references)
    public List<CadImportAssemblyPreviewDto> Assemblies { get; set; } = new();

    // Parts that need user identification (no part reference in CAD file)
    public List<UnidentifiedPartDto> UnidentifiedParts { get; set; } = new();

    public DateTime ParsedDate { get; set; }
    public DateTime ExpiresAt { get; set; } // When this preview session expires
}
