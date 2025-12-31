using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

public class UpdateQDocsDrawingDto
{
    public string? DrawingNumber { get; set; }
    public string? DrawingTitle { get; set; }
    public string? Description { get; set; }
    public int? WorkPackageId { get; set; }
}
