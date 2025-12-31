using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

/// <summary>
/// A part that could not be identified from the CAD file.
/// User must provide a part reference/mark before import can complete.
/// </summary>
public class UnidentifiedPartDto
{
    public string TempPartId { get; set; } = string.Empty; // Unique ID for this unidentified part

    // Information extracted from CAD to help user identify the part
    public string PartType { get; set; } = string.Empty; // "Beam", "Plate", "Bolt", "Nut"
    public string? Profile { get; set; } // "PFC 150", "EA 100x100x10", etc.
    public string? Description { get; set; } // Additional description from CAD
    public string? MaterialGrade { get; set; }
    public string? ObjectType { get; set; } // IFC ObjectType if available
    public string? ElementName { get; set; } // IFC element Name if available

    // Geometry info to help identification
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Thickness { get; set; }
    public decimal? Weight { get; set; }

    // Assembly context
    public string? TempAssemblyId { get; set; } // Which assembly this part belongs to (if any)
    public string? AssemblyMark { get; set; } // Parent assembly mark (if identified)

    public int Quantity { get; set; } = 1;

    // Suggested reference (system's best guess based on profile/type)
    public string? SuggestedReference { get; set; }

    // User-provided reference (set during review)
    public string? UserProvidedReference { get; set; }
}
