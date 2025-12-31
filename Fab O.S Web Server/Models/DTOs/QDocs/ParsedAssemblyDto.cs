using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

public class ParsedAssemblyDto
{
    public string AssemblyMark { get; set; } = string.Empty; // From SMLX m_strMark or IFC IFCRELAGGREGATES
    public string AssemblyName { get; set; } = string.Empty;
    public List<ParsedPartDto> Parts { get; set; } = new();
    public decimal TotalWeight { get; set; }
}
