using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

public class CreateDrawingAssemblyDto
{
    [Required]
    [Range(1, int.MaxValue)]
    public int DrawingId { get; set; }

    public int? DrawingRevisionId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string AssemblyMark { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string AssemblyName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalWeight { get; set; }

    [Range(0, int.MaxValue)]
    public int PartCount { get; set; }
}
