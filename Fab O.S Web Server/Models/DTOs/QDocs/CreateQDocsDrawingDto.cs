using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

public class CreateQDocsDrawingDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string DrawingNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string DrawingTitle { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "OrderId must be a positive integer")]
    public int OrderId { get; set; }

    public int? WorkPackageId { get; set; }
}
