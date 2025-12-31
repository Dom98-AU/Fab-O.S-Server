using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

/// <summary>
/// A part that was successfully identified from the CAD file
/// </summary>
public class IdentifiedPartPreviewDto
{
    public string TempPartId { get; set; } = string.Empty;
    public string PartReference { get; set; } = string.Empty; // The identified part mark
    public string Description { get; set; } = string.Empty;
    public string PartType { get; set; } = string.Empty;
    public string? MaterialGrade { get; set; }
    public string? Profile { get; set; } // e.g., "PFC 150", "EA 100x100x10"
    public decimal? Weight { get; set; }
    public int Quantity { get; set; } = 1;
    public string? AssemblyMark { get; set; }
}
