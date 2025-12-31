using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

public class DrawingAssemblyDto
{
    public int Id { get; set; }
    public int DrawingId { get; set; }
    public int? DrawingRevisionId { get; set; }
    public string AssemblyMark { get; set; } = string.Empty; // "ASM1", "ASM2"
    public string AssemblyName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TotalWeight { get; set; } // kg
    public int PartCount { get; set; }
    public DateTime CreatedDate { get; set; }

    // Nested parts
    public List<DrawingPartDto> Parts { get; set; } = new();
}
