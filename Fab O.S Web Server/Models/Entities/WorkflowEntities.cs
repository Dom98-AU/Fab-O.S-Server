using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

// ==========================================
// ESTIMATION SYSTEM
// Estimation → Revision → Package → Worksheet → Row
// ==========================================

[Table("Estimations")]
public class Estimation
{
    [Key]
    public int Id { get; set; }

    // Multi-tenant support (NEW - needs migration)
    [Required]
    public int CompanyId { get; set; }

    [Required]
    [StringLength(50)]
    public string EstimationNumber { get; set; } = string.Empty; // EST-2025-0001

    // NEW - Estimation name (shorter than project name)
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    // NEW - Description
    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public int CustomerId { get; set; }

    // Project name (existing column in DB)
    [Required]
    [StringLength(200)]
    public string ProjectName { get; set; } = string.Empty;

    // NEW - Link to project entity
    public int? ProjectId { get; set; }

    // NEW - Source takeoff reference
    public int? SourceTakeoffId { get; set; }

    [Required]
    public DateTime EstimationDate { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime ValidUntil { get; set; } = DateTime.UtcNow.AddDays(30);

    [Required]
    public int RevisionNumber { get; set; } = 1;

    // NEW - Current revision letter (A, B, C...)
    [Required]
    [StringLength(10)]
    public string CurrentRevisionLetter { get; set; } = "A";

    // Cost breakdown
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalMaterialCost { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalLaborHours { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalLaborCost { get; set; }

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal OverheadPercentage { get; set; } = 15.00m;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal OverheadAmount { get; set; }

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal MarginPercentage { get; set; } = 20.00m;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal MarginAmount { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    // NEW - Current total (denormalized from current revision for quick access)
    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentTotal { get; set; }

    // Status
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    // NEW - Soft delete support
    public bool IsDeleted { get; set; } = false;

    public DateTime? ApprovedDate { get; set; }

    public int? ApprovedBy { get; set; }

    // Audit fields
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public int CreatedBy { get; set; }

    [Required]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    [Required]
    public int LastModifiedBy { get; set; }

    // Alias properties for code compatibility (map to LastModified/LastModifiedBy)
    [NotMapped]
    public DateTime? ModifiedDate { get => LastModified; set => LastModified = value ?? DateTime.UtcNow; }

    [NotMapped]
    public int? ModifiedBy { get => LastModifiedBy; set => LastModifiedBy = value ?? 0; }

    // Link to Order
    public int? OrderId { get; set; }

    // Navigation properties
    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual User CreatedByUser { get; set; } = null!;

    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }

    [ForeignKey("SourceTakeoffId")]
    public virtual Takeoff? SourceTakeoff { get; set; }

    public virtual ICollection<EstimationPackage> Packages { get; set; } = new List<EstimationPackage>();

    public virtual ICollection<EstimationRevision> Revisions { get; set; } = new List<EstimationRevision>();
}

// Note: EstimationPackage is deprecated. Use EstimationRevisionPackage from EstimationWorksheetEntities.cs instead.
// This class is kept for backward compatibility only.
[Table("EstimationPackages")]
public class EstimationPackage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int EstimationId { get; set; }

    [Required]
    [StringLength(200)]
    public string PackageName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public int SequenceNumber { get; set; }

    // Package costs
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal MaterialCost { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal LaborHours { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal LaborCost { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PackageTotal { get; set; }

    // Schedule estimates
    public DateTime? PlannedStartDate { get; set; }

    public DateTime? PlannedEndDate { get; set; }

    public int? EstimatedDuration { get; set; } // Days

    // Navigation properties
    [ForeignKey("EstimationId")]
    public virtual Estimation Estimation { get; set; } = null!;
}

// ==========================================
// UNIFIED ORDER SYSTEM
// ==========================================

[Table("Orders")]
public class Order
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string OrderNumber { get; set; } = string.Empty; // ORD-2025-0001

    [Required]
    public int CustomerId { get; set; }

    // Multi-tenant
    [Required]
    public int CompanyId { get; set; }

    // Source tracking
    [Required]
    [StringLength(20)]
    public string Source { get; set; } = string.Empty; // FromEstimation, Direct

    public int? EstimationId { get; set; } // Source estimation (can be simple or complex)

    // Order description (required for direct orders, copied from Quote/Estimation when converted)
    // NOTE: These columns don't exist in database yet - marked as NotMapped
    [NotMapped]
    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [NotMapped]
    [StringLength(200)]
    public string? ProjectName { get; set; }

    // Customer references
    [StringLength(100)]
    public string? CustomerPONumber { get; set; }

    [StringLength(200)]
    public string? CustomerReference { get; set; }

    // Order date
    [Required]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    // Note: Status, TotalValue, and Schedule are calculated from WorkPackages
    // Not stored on Order - calculated on demand from child WorkPackages

    // Audit fields
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public int? CreatedBy { get; set; }

    [Required]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    // Map to ModifiedBy column in database (not LastModifiedBy)
    [Column("ModifiedBy")]
    public int? LastModifiedBy { get; set; }

    // Navigation properties
    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey("EstimationId")]
    public virtual Estimation? Estimation { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("LastModifiedBy")]
    public virtual User? LastModifiedByUser { get; set; }

    // FabMate production packages
    public virtual ICollection<WorkPackage> WorkPackages { get; set; } = new List<WorkPackage>();
}

// ==========================================
// ENUMS FOR TYPE SAFETY
// ==========================================

public enum QuoteStatus
{
    Draft,
    Sent,
    Accepted,
    Rejected,
    Expired,
    Superseded
}

public enum EstimationStatus
{
    Draft,
    InReview,
    Sent,
    Accepted,
    Rejected,
    Expired
}

public enum OrderSource
{
    FromQuote,
    FromEstimation,
    Direct
}

public enum OrderStatus
{
    Confirmed,
    InProgress,
    OnHold,
    Complete,
    Cancelled,
    Invoiced,
    Paid
}
