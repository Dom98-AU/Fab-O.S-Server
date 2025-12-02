using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

[Table("Packages")]
public class Package
{
    [Key]
    public int Id { get; set; }

    // Multi-tenant support
    [Required]
    public int CompanyId { get; set; }

    // DEPRECATED: ProjectId is now optional - transitioning away from Project entity
    public int? ProjectId { get; set; }

    // NEW: Direct Order relationship (for production packages)
    public int? OrderId { get; set; }

    // NEW: Revision relationship (packages belong to a specific takeoff revision)
    public int? RevisionId { get; set; }

    // NEW: Package source tracking
    [StringLength(20)]
    public string PackageSource { get; set; } = "Project"; // Project, DirectOrder, Takeoff

    [Required]
    [StringLength(50)]
    public string PackageNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string PackageName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal EstimatedHours { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal EstimatedCost { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ActualHours { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ActualCost { get; set; }

    public int? CreatedBy { get; set; }

    public int? LastModifiedBy { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public DateTime LastModified { get; set; }

    [Required]
    public bool IsDeleted { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal LaborRatePerHour { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ProcessingEfficiency { get; set; }

    public int? EfficiencyRateId { get; set; }

    public int? RoutingId { get; set; }

    // Navigation properties
    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey("ProjectId")]
    public virtual Project? Project { get; set; }

    // NEW: Order navigation property (for direct order packages)
    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }

    // NEW: Revision navigation property (for takeoff revision packages)
    [ForeignKey("RevisionId")]
    public virtual TakeoffRevision? Revision { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("LastModifiedBy")]
    public virtual User? LastModifiedByUser { get; set; }

    [ForeignKey("EfficiencyRateId")]
    public virtual EfficiencyRate? EfficiencyRate { get; set; }

    [ForeignKey("RoutingId")]
    public virtual RoutingTemplate? RoutingTemplate { get; set; }

    // NOTE: TraceDrawings removed - use TakeoffRevision.Takeoff instead
    // public virtual ICollection<Takeoff> TraceDrawings { get; set; } = new List<Takeoff>();
    public virtual ICollection<WeldingConnection> WeldingConnections { get; set; } = new List<WeldingConnection>();

    // NOTE: WorkOrders collection REMOVED - Trace Package does NOT link to WorkOrder
    // WorkOrder belongs to FabMate module and links to WorkPackage (not Package)
    // When an Estimation is converted to an Order, Packages become WorkPackages
    // public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();

    // Package drawings stored in SharePoint
    public virtual ICollection<PackageDrawing> PackageDrawings { get; set; } = new List<PackageDrawing>();
}
