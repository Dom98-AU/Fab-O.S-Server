using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

/// <summary>
/// Request DTO for uploading an attachment
/// </summary>
public class UploadAttachmentRequest
{
    // Parent entity (one required)
    public int? EquipmentId { get; set; }
    public int? EquipmentKitId { get; set; }
    public int? LocationId { get; set; }
    public int? MaintenanceRecordId { get; set; }

    // Type and category
    public AttachmentType Type { get; set; }
    public AttachmentCategory Category { get; set; } = AttachmentCategory.Other;

    // Optional metadata
    public string? Title { get; set; }
    public string? Description { get; set; }

    // Certificate-specific (for Certificate type)
    public DateTime? ExpiryDate { get; set; }
    public string? CertificateNumber { get; set; }
    public string? IssuingAuthority { get; set; }

    // Photo-specific
    public bool SetAsPrimaryPhoto { get; set; }
}
