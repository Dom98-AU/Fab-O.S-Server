using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

public class UpdateRevisionStatusDto
{
    [Required]
    [RegularExpression("^(Draft|UnderReview|Approved|Rejected|Superseded)$",
        ErrorMessage = "Status must be one of: Draft, UnderReview, Approved, Rejected, Superseded")]
    public string Status { get; set; } = string.Empty;

    [StringLength(200)]
    public string? ReviewedBy { get; set; }

    [StringLength(200)]
    public string? ApprovedBy { get; set; }

    [StringLength(1000)]
    public string? ApprovalComments { get; set; }
}
