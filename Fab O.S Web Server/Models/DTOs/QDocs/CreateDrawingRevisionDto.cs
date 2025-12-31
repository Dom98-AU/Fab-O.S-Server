using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

public class CreateDrawingRevisionDto
{
    [Required]
    [Range(1, int.MaxValue)]
    public int DrawingId { get; set; }

    [Required]
    [RegularExpression("^(IFA|IFC)$", ErrorMessage = "RevisionType must be 'IFA' or 'IFC'")]
    public string RevisionType { get; set; } = string.Empty;

    [Required]
    [Range(1, 999)]
    public int RevisionNumber { get; set; }

    [StringLength(2000)]
    public string? RevisionNotes { get; set; }

    public int? CreatedFromIFARevisionId { get; set; }
}
