using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

public class QDocsDrawingDto
{
    public int Id { get; set; }
    public string DrawingNumber { get; set; } = string.Empty;
    public string DrawingTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderId { get; set; }
    public int? WorkPackageId { get; set; }
    public int? AssemblyId { get; set; }
    public string CurrentStage { get; set; } = "IFA"; // IFA, IFC, Superseded
    public int? ActiveRevisionId { get; set; }
    public DateTime CreatedDate { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }

    public List<DrawingRevisionDto> Revisions { get; set; } = new();
}
