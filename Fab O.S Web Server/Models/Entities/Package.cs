using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

[Table("Packages")]
public class Package
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }

    // NEW: Direct Order relationship (for simple quote-based packages)
    public int? OrderId { get; set; }

    // NEW: Package source tracking
    [StringLength(20)]
    public string PackageSource { get; set; } = "Project"; // Project, DirectOrder

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
    [ForeignKey("ProjectId")]
    public virtual Project Project { get; set; } = null!;

    // NEW: Order navigation property (for direct order packages)
    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("LastModifiedBy")]
    public virtual User? LastModifiedByUser { get; set; }

    [ForeignKey("EfficiencyRateId")]
    public virtual EfficiencyRate? EfficiencyRate { get; set; }

    [ForeignKey("RoutingId")]
    public virtual RoutingTemplate? RoutingTemplate { get; set; }

    public virtual ICollection<TraceDrawing> TraceDrawings { get; set; } = new List<TraceDrawing>();
    public virtual ICollection<WeldingConnection> WeldingConnections { get; set; } = new List<WeldingConnection>();
    
    // NEW: Work orders collection (production management)
    public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}
