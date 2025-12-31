using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

public class DrawingRevisionDto
{
    public int Id { get; set; }
    public int DrawingId { get; set; }
    public string RevisionCode { get; set; } = string.Empty; // Generated via NumberSeries
    public string RevisionType { get; set; } = string.Empty; // IFA, IFC
    public int RevisionNumber { get; set; }
    public string? RevisionNotes { get; set; }
    public string? DrawingFileName { get; set; }
    public string? CloudProvider { get; set; }
    public string? CloudFileId { get; set; }
    public string? CloudFilePath { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, UnderReview, Approved, Rejected
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovalComments { get; set; }
    public int? SupersededById { get; set; }
    public int? CreatedFromIFARevisionId { get; set; }
    public bool IsActiveForProduction { get; set; }
    public DateTime CreatedDate { get; set; }
}
