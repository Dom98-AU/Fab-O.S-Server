using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

/// <summary>
/// Request to confirm and complete an import after user has provided part references
/// </summary>
public class ConfirmCadImportRequestDto
{
    public string ImportSessionId { get; set; } = string.Empty;

    // Mappings for unidentified parts
    public List<PartReferenceMappingDto> PartMappings { get; set; } = new();

    // Mappings for unidentified assemblies (if any)
    public List<AssemblyReferenceMappingDto> AssemblyMappings { get; set; } = new();

    // Option to auto-generate references for remaining unidentified parts
    public bool AutoGenerateRemainingReferences { get; set; } = false;
}
