using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

[Table("Projects")]
public class Project
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string ProjectName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string JobNumber { get; set; } = string.Empty;

    [StringLength(200)]
    public string? CustomerName { get; set; }

    [StringLength(200)]
    public string? ProjectLocation { get; set; }

    [Required]
    [StringLength(20)]
    public string EstimationStage { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal LaborRate { get; set; }

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal ContingencyPercentage { get; set; }

    public string? Notes { get; set; }

    public int? OwnerId { get; set; }

    public int? LastModifiedBy { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public DateTime LastModified { get; set; }

    [Required]
    public bool IsDeleted { get; set; }

    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? EstimatedHours { get; set; }

    public DateTime? EstimatedCompletionDate { get; set; }

    public int? CustomerId { get; set; }

    // NEW: Workflow integration fields - TEMPORARILY DISABLED UNTIL MIGRATION
    [NotMapped]
    public int? OrderId { get; set; }

    [NotMapped]
    [StringLength(20)]
    public string ProjectType { get; set; } = "Direct"; // Direct, Estimation

    // Navigation properties
    [ForeignKey("OwnerId")]
    public virtual User? Owner { get; set; }

    [ForeignKey("LastModifiedBy")]
    public virtual User? LastModifiedByUser { get; set; }

    // NEW: Order navigation property (for workflow integration) - TEMPORARILY DISABLED
    //[ForeignKey("OrderId")]
    //public virtual Order? Order { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    public virtual ICollection<Package> Packages { get; set; } = new List<Package>();
    public virtual ICollection<TraceDrawing> TraceDrawings { get; set; } = new List<TraceDrawing>();
}