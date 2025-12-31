using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

/// <summary>
/// Extended ParsedPartDto that includes temp ID and identification status
/// Used internally during parsing
/// </summary>
public class ParsedPartWithIdDto : ParsedPartDto
{
    public string TempPartId { get; set; } = string.Empty;
    public string? TempAssemblyId { get; set; }
    public bool IsIdentified { get; set; } // True if has valid PartReference
    public string? IfcElementName { get; set; } // Original IFC element name
    public string? IfcObjectType { get; set; } // Original IFC object type
    public string? SuggestedReference { get; set; } // System's suggested reference
}
