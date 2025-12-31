using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

/// <summary>
/// User-provided part reference mapping
/// </summary>
public class PartReferenceMappingDto
{
    public string TempPartId { get; set; } = string.Empty;
    public string PartReference { get; set; } = string.Empty; // User-provided part mark
}
