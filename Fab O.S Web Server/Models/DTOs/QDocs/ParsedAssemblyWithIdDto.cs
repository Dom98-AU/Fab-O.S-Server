using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

/// <summary>
/// Extended ParsedAssemblyDto that includes temp ID and unidentified parts
/// </summary>
public class ParsedAssemblyWithIdDto : ParsedAssemblyDto
{
    public string TempAssemblyId { get; set; } = string.Empty;
    public bool IsIdentified { get; set; } // True if has valid/meaningful AssemblyMark
    public string? SuggestedAssemblyMark { get; set; } // Auto-generated mark for reference when unidentified
    public List<ParsedPartWithIdDto> IdentifiedParts { get; set; } = new();
    public List<ParsedPartWithIdDto> UnidentifiedParts { get; set; } = new();
}
