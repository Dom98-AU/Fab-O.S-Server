using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

/// <summary>
/// Response DTO for equipment attachment
/// </summary>
public class EquipmentAttachmentDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    // Parent entity references (polymorphic)
    public int? EquipmentId { get; set; }
    public string? EquipmentCode { get; set; }
    public string? EquipmentName { get; set; }
    public int? EquipmentKitId { get; set; }
    public string? KitCode { get; set; }
    public string? KitName { get; set; }
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public int? MaintenanceRecordId { get; set; }

    // Type and category
    public AttachmentType Type { get; set; }
    public string TypeName => Type.ToString();
    public AttachmentCategory Category { get; set; }
    public string CategoryName => Category.ToString();

    // File metadata
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FormattedFileSize => FormatFileSize(FileSizeBytes);

    // Storage info
    public string StorageProvider { get; set; } = string.Empty;
    public string? StorageUrl { get; set; }

    // Photo-specific
    public int? Width { get; set; }
    public int? Height { get; set; }
    public bool IsPrimaryPhoto { get; set; }
    public string? ThumbnailUrl { get; set; }

    // Certificate-specific
    public DateTime? ExpiryDate { get; set; }
    public string? CertificateNumber { get; set; }
    public string? IssuingAuthority { get; set; }
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;
    public bool IsExpiringSoon => ExpiryDate.HasValue &&
        ExpiryDate.Value >= DateTime.UtcNow &&
        ExpiryDate.Value <= DateTime.UtcNow.AddDays(30);
    public int? DaysUntilExpiry => ExpiryDate.HasValue
        ? (int)(ExpiryDate.Value - DateTime.UtcNow).TotalDays
        : null;

    // Audit
    public DateTime UploadedDate { get; set; }
    public string? UploadedBy { get; set; }
    public DateTime? LastModified { get; set; }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}
