using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

/// <summary>
/// Represents a Takeoff Card - the top-level container for a takeoff project.
/// Previously named Takeoff for historical reasons. The database table is still "TraceDrawings"
/// but will be renamed to "Takeoffs" in a future migration.
///
/// Hierarchy: Takeoff → TakeoffRevision → Package → PackageDrawing → TraceTakeoffMeasurement
/// </summary>
[Table("TraceDrawings")]
public class Takeoff
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    // DEPRECATED: ProjectId is now optional - transitioning away from Project entity
    public int? ProjectId { get; set; }

    [StringLength(50)]
    public string? TakeoffNumber { get; set; }

    [StringLength(100)]
    public string? DrawingNumber { get; set; }

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string FileType { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string BlobUrl { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ThumbnailUrl { get; set; }

    [Required]
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;

    [Required]
    public int UploadedBy { get; set; }

    public int? PageCount { get; set; }

    [Required]
    [StringLength(50)]
    public string ProcessingStatus { get; set; } = "Pending";

    [Column(TypeName = "decimal(10,4)")]
    public decimal? Scale { get; set; }

    [StringLength(20)]
    public string? ScaleUnit { get; set; } = "mm";

    public string? CalibrationData { get; set; }

    [StringLength(200)]
    public string? DrawingTitle { get; set; }

    [StringLength(50)]
    public string? DrawingType { get; set; }

    [StringLength(50)]
    public string? Discipline { get; set; }

    [StringLength(20)]
    public string? Revision { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    [Required]
    public bool IsDeleted { get; set; } = false;

    [StringLength(50)]
    public string? OCRStatus { get; set; } = "NotProcessed";

    public DateTime? OCRProcessedDate { get; set; }

    public int? OCRResultId { get; set; }

    // Customer relationship
    public int? CustomerId { get; set; }

    // Contact relationship
    public int? ContactId { get; set; }


    // Contact Person
    [StringLength(200)]
    public string? ContactPerson { get; set; }

    // OCR Enhancement Properties
    [StringLength(200)]
    public string? TraceName { get; set; }

    [StringLength(200)]
    public string? ProjectName { get; set; }

    [StringLength(200)]
    public string? ClientName { get; set; }  // Keep for backward compatibility

    // Takeoff Enhancement Properties
    [StringLength(50)]
    public string? Status { get; set; } = "Draft";

    [StringLength(1000)]
    public string? Description { get; set; }

    public int? OcrConfidence { get; set; }

    // ProjectNumber removed as per requirements
    // [StringLength(50)]
    // public string? ProjectNumber { get; set; }

    [StringLength(200)]
    public string? ProjectArchitect { get; set; }

    [StringLength(200)]
    public string? ProjectEngineer { get; set; }

    // Convenience property for UI compatibility
    [NotMapped]
    public DateTime CreatedAt => CreatedDate;

    // Navigation properties
    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey("ProjectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [ForeignKey("UploadedBy")]
    public virtual User UploadedByUser { get; set; } = null!;

    public virtual ICollection<TakeoffRevision> Revisions { get; set; } = new List<TakeoffRevision>();
    public virtual ICollection<TraceMeasurement> TraceMeasurements { get; set; } = new List<TraceMeasurement>();
    public virtual ICollection<TraceBeamDetection> TraceBeamDetections { get; set; } = new List<TraceBeamDetection>();
    public virtual ICollection<TraceTakeoffItem> TraceTakeoffItems { get; set; } = new List<TraceTakeoffItem>();
}

[Table("WeldingConnections")]
public class WeldingConnection
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal DefaultAssembleFitTack { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal DefaultWeld { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal DefaultWeldCheck { get; set; } = 0;

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public int DisplayOrder { get; set; } = 0;

    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal DefaultWeldTest { get; set; } = 0;

    [Required]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(20)]
    public string Size { get; set; } = "Small";

    // Navigation properties
    // Note: PackageId removed - replaced with TakeoffRevision system
}

[Table("TraceMeasurements")]
public class TraceMeasurement
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TraceDrawingId { get; set; }

    [StringLength(100)]
    public string? ElementType { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal? Length { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal? Width { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal? Height { get; set; }

    [StringLength(50)]
    public string? Units { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public DateTime LastModified { get; set; }

    // Navigation properties
    [ForeignKey("TraceDrawingId")]
    public virtual Takeoff Takeoff { get; set; } = null!;
}

[Table("TraceBeamDetections")]
public class TraceBeamDetection
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TraceDrawingId { get; set; }

    [StringLength(100)]
    public string? BeamType { get; set; }

    [StringLength(50)]
    public string? BeamSize { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal? Length { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal? Weight { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public DateTime LastModified { get; set; }

    // Navigation properties
    [ForeignKey("TraceDrawingId")]
    public virtual Takeoff Takeoff { get; set; } = null!;
}

[Table("TraceTakeoffItems")]
public class TraceTakeoffItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TraceDrawingId { get; set; }

    [StringLength(100)]
    public string? ItemType { get; set; }

    [StringLength(200)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Quantity { get; set; }

    [StringLength(20)]
    public string? Units { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public DateTime LastModified { get; set; }

    // Navigation properties
    [ForeignKey("TraceDrawingId")]
    public virtual Takeoff Takeoff { get; set; } = null!;
}
