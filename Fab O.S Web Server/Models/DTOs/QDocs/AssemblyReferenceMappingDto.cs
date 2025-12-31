using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

/// <summary>
/// User-provided assembly reference mapping
/// </summary>
public class AssemblyReferenceMappingDto
{
    public string TempAssemblyId { get; set; } = string.Empty;
    public string AssemblyMark { get; set; } = string.Empty;
    public string? AssemblyName { get; set; }
}
