using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

// ==========================================
// QUOTE SYSTEM (Simple Path)
// ==========================================

[Table("Quotes")]
public class Quote
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string QuoteNumber { get; set; } = string.Empty; // QTE-2025-0001

    [Required]
    public int CustomerId { get; set; }

    [Required]
    public DateTime QuoteDate { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime ValidUntil { get; set; }

    // Single package concept for simple jobs
    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal MaterialCost { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal LaborHours { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal LaborRate { get; set; }

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal OverheadPercentage { get; set; } = 15.00m;

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal MarginPercentage { get; set; } = 20.00m;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Draft"; // Draft, Sent, Accepted, Rejected, Expired

    // Audit fields
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public int? CreatedBy { get; set; }

    [Required]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public int? LastModifiedBy { get; set; }

    // Conversion tracking
    public int? OrderId { get; set; }

    // Navigation properties
    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("LastModifiedBy")]
    public virtual User? LastModifiedByUser { get; set; }

    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }

    public virtual ICollection<QuoteLineItem> LineItems { get; set; } = new List<QuoteLineItem>();
}

[Table("QuoteLineItems")]
public class QuoteLineItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int QuoteId { get; set; }

    [Required]
    public int LineNumber { get; set; }

    // Item details
    [Required]
    [StringLength(500)]
    public string ItemDescription { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    [Required]
    [StringLength(20)]
    public string Unit { get; set; } = string.Empty; // EA, KG, M, M2, HR

    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitPrice { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal LineTotal { get; set; }

    // Optional catalog reference
    public int? CatalogueItemId { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    // Navigation properties
    [ForeignKey("QuoteId")]
    public virtual Quote Quote { get; set; } = null!;

    // Note: CatalogueItem foreign key will be added when catalog system is implemented
}

// ==========================================
// ESTIMATION SYSTEM (Complex Path)
// ==========================================

[Table("Estimations")]
public class Estimation
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string EstimationNumber { get; set; } = string.Empty; // EST-2025-0001

    [Required]
    public int CustomerId { get; set; }

    [Required]
    [StringLength(200)]
    public string ProjectName { get; set; } = string.Empty;

    [Required]
    public DateTime EstimationDate { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime ValidUntil { get; set; }

    [Required]
    public int RevisionNumber { get; set; } = 1;

    // Detailed cost breakdown
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

    // Status and approval
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Draft"; // Draft, InReview, Sent, Accepted, Rejected, Expired

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

    // Conversion tracking
    public int? OrderId { get; set; }

    // Navigation properties
    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual User CreatedByUser { get; set; } = null!;

    [ForeignKey("LastModifiedBy")]
    public virtual User LastModifiedByUser { get; set; } = null!;

    [ForeignKey("ApprovedBy")]
    public virtual User? ApprovedByUser { get; set; }

    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }

    public virtual ICollection<EstimationPackage> Packages { get; set; } = new List<EstimationPackage>();
}

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

    // Source tracking
    [Required]
    [StringLength(20)]
    public string Source { get; set; } = string.Empty; // FromQuote, FromEstimation, Direct

    public int? QuoteId { get; set; } // Source quote for simple orders

    public int? EstimationId { get; set; } // Source estimation for complex orders

    // Customer references
    [StringLength(100)]
    public string? CustomerPONumber { get; set; }

    [StringLength(200)]
    public string? CustomerReference { get; set; }

    // Commercial details
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalValue { get; set; }

    [Required]
    [StringLength(100)]
    public string PaymentTerms { get; set; } = "NET30";

    [Required]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    public DateTime? RequiredDate { get; set; }

    public DateTime? PromisedDate { get; set; }

    // Status tracking
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Confirmed"; // Confirmed, InProgress, OnHold, Complete, Cancelled, Invoiced, Paid

    // Audit fields
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public int CreatedBy { get; set; }

    [Required]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    [Required]
    public int LastModifiedBy { get; set; }

    // Navigation properties
    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey("QuoteId")]
    public virtual Quote? Quote { get; set; }

    [ForeignKey("EstimationId")]
    public virtual Estimation? Estimation { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual User CreatedByUser { get; set; } = null!;

    [ForeignKey("LastModifiedBy")]
    public virtual User LastModifiedByUser { get; set; } = null!;

    // For simple orders: direct packages (from quotes)
    public virtual ICollection<Package> DirectPackages { get; set; } = new List<Package>();

    // For complex orders: project container (from estimations)
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
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
