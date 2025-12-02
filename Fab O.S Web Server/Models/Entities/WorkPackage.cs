using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

/// <summary>
/// WorkPackage - Production package for FabMate module
/// This is separate from the Trace module's Package entity
/// Links: Order → WorkPackage → WorkOrder
/// </summary>
[Table("WorkPackages")]
public class WorkPackage
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string PackageNumber { get; set; } = string.Empty; // WP-2025-0001

    [Required]
    [StringLength(200)]
    public string PackageName { get; set; } = string.Empty;

    // Link to Order (REQUIRED)
    [Required]
    public int OrderId { get; set; }

    // Package details
    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    [StringLength(20)]
    public string Priority { get; set; } = "Normal"; // Low, Normal, High, Urgent

    [StringLength(50)]
    public string? PackageType { get; set; } // PartsProcessing, AssemblyBuilding, Mixed, Finishing

    // Scheduling
    public DateTime? PlannedStartDate { get; set; }

    public DateTime? PlannedEndDate { get; set; }

    public DateTime? ActualStartDate { get; set; }

    public DateTime? ActualEndDate { get; set; }

    // Costing and Pricing
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal EstimatedHours { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ActualHours { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal EstimatedCost { get; set; } = 0; // Our internal cost estimate

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ActualCost { get; set; } = 0; // Our actual internal cost

    // NOTE: BillableValue column doesn't exist in database - marked as NotMapped
    [NotMapped]
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal BillableValue { get; set; } = 0; // What we charge the customer (price, not cost)

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal LaborRatePerHour { get; set; } = 0;

    // Progress tracking
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Planning"; // Planning, Ready, InProgress, OnHold, Complete, Cancelled

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal PercentComplete { get; set; } = 0;

    // Quality
    [Required]
    public bool RequiresITP { get; set; } = false; // Inspection Test Plan required

    [StringLength(50)]
    public string? ITPNumber { get; set; }

    // Audit fields
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    [Required]
    public bool IsDeleted { get; set; } = false;

    // Multi-tenant
    [Required]
    public int CompanyId { get; set; }

    // Navigation properties
    [ForeignKey("OrderId")]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    // Collection of work orders in this package
    // Relationship explicitly configured in ApplicationDbContext.OnModelCreating
    public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}
