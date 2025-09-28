using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

// ==========================================
// WORK ORDER SYSTEM
// ==========================================

[Table("WorkOrders")]
public class WorkOrder
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string WorkOrderNumber { get; set; } = string.Empty; // WO-2025-0001

    [Required]
    public int PackageId { get; set; }

    // Work order details
    [Required]
    [StringLength(20)]
    public string WorkOrderType { get; set; } = "Mixed"; // PartsProcessing, AssemblyBuilding, Mixed, Finishing, QualityControl

    [StringLength(1000)]
    public string? Description { get; set; }

    // Assignment options
    public int? WorkCenterId { get; set; } // Assigned to work center

    public int? PrimaryResourceId { get; set; } // Assigned to specific person

    // Priority and scheduling
    [Required]
    [StringLength(10)]
    public string Priority { get; set; } = "Normal"; // Low, Normal, High, Urgent

    public DateTime? ScheduledStartDate { get; set; }

    public DateTime? ScheduledEndDate { get; set; }

    public DateTime? ActualStartDate { get; set; }

    public DateTime? ActualEndDate { get; set; }

    // Time tracking
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal EstimatedHours { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ActualHours { get; set; }

    // Status
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Created"; // Created, Scheduled, Released, InProgress, OnHold, Complete, Cancelled

    [StringLength(100)]
    public string? Barcode { get; set; } // For shop floor scanning

    // Quality control
    [Required]
    public bool HasHoldPoints { get; set; }

    [Required]
    public bool RequiresInspection { get; set; }

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
    [ForeignKey("PackageId")]
    public virtual Package Package { get; set; } = null!;

    [ForeignKey("WorkCenterId")]
    public virtual WorkCenter? WorkCenter { get; set; }

    [ForeignKey("PrimaryResourceId")]
    public virtual Resource? PrimaryResource { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual User CreatedByUser { get; set; } = null!;

    [ForeignKey("LastModifiedBy")]
    public virtual User LastModifiedByUser { get; set; } = null!;

    // What we're working on
    public virtual ICollection<WorkOrderInventoryItem> InventoryItems { get; set; } = new List<WorkOrderInventoryItem>();
    public virtual ICollection<WorkOrderAssembly> Assemblies { get; set; } = new List<WorkOrderAssembly>();

    // How we're working on them
    public virtual ICollection<WorkOrderOperation> Operations { get; set; } = new List<WorkOrderOperation>();

    // Resources assigned
    public virtual ICollection<WorkOrderResource> AssignedResources { get; set; } = new List<WorkOrderResource>();
}

[Table("WorkOrderOperations")]
public class WorkOrderOperation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int WorkOrderId { get; set; }

    [Required]
    public int SequenceNumber { get; set; }

    // Operation details
    [Required]
    [StringLength(20)]
    public string OperationCode { get; set; } = string.Empty; // CUT, DRILL, WELD, PAINT, INSPECT

    [Required]
    [StringLength(200)]
    public string OperationName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    // Requirements
    [StringLength(100)]
    public string? RequiredSkill { get; set; }

    public int? RequiredSkillLevel { get; set; } // 1-5 scale

    [StringLength(100)]
    public string? RequiredMachine { get; set; }

    [StringLength(500)]
    public string? RequiredTooling { get; set; }

    // Time estimates
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal SetupTime { get; set; } // Hours

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal CycleTime { get; set; } // Hours per unit

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal EstimatedHours { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ActualHours { get; set; }

    // Quality requirements
    [Required]
    public bool RequiresInspection { get; set; }

    [StringLength(50)]
    public string? InspectionType { get; set; } // Visual, Dimensional, NDT, Functional

    public int? LinkedITPPointId { get; set; }

    // Status tracking
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Planned"; // Planned, Ready, InProgress, Complete, OnHold

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int? CompletedBy { get; set; }

    // Navigation properties
    [ForeignKey("WorkOrderId")]
    public virtual WorkOrder WorkOrder { get; set; } = null!;

    [ForeignKey("CompletedBy")]
    public virtual User? CompletedByUser { get; set; }
}

[Table("WorkOrderInventoryItem")]
public class WorkOrderInventoryItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int WorkOrderId { get; set; }

    [Required]
    public int PackageItemId { get; set; }

    [Required]
    public int CatalogueItemId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal RequiredQuantity { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal IssuedQuantity { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal ProcessedQuantity { get; set; }

    [Required]
    [StringLength(20)]
    public string Unit { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string RequiredOperations { get; set; } = string.Empty; // "CUT,DRILL,PAINT"

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Planned"; // Planned, Issued, InProgress, Complete

    // Traceability
    [StringLength(100)]
    public string? HeatNumber { get; set; }

    [StringLength(200)]
    public string? Certificate { get; set; }

    public int? InventoryItemId { get; set; }

    // Navigation properties
    [ForeignKey("WorkOrderId")]
    public virtual WorkOrder WorkOrder { get; set; } = null!;

    // Note: PackageItem and CatalogueItem foreign keys will be added when those systems are implemented
}

[Table("WorkOrderAssembly")]
public class WorkOrderAssembly
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int WorkOrderId { get; set; }

    [Required]
    public int PackageAssemblyId { get; set; }

    [Required]
    public int AssemblyId { get; set; }

    [Required]
    public int QuantityToBuild { get; set; }

    [Required]
    public int QuantityCompleted { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Planned"; // Planned, InProgress, Complete, OnHold

    // Navigation properties
    [ForeignKey("WorkOrderId")]
    public virtual WorkOrder WorkOrder { get; set; } = null!;

    // Note: PackageAssembly and Assembly foreign keys will be added when those systems are implemented
}

// ==========================================
// RESOURCE MANAGEMENT
// ==========================================

[Table("Resources")]
public class Resource
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;

    public int? UserId { get; set; } // Link to Users table if they have system access

    // Personal details
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string JobTitle { get; set; } = string.Empty;

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    // Resource type and skills
    [Required]
    [StringLength(20)]
    public string ResourceType { get; set; } = "Direct"; // Direct, Indirect, Contract, Supervisor

    [StringLength(100)]
    public string? PrimarySkill { get; set; }

    [Required]
    public int SkillLevel { get; set; } = 1; // 1-5 scale

    [StringLength(50)]
    public string? CertificationLevel { get; set; }

    // Availability
    [Required]
    [Column(TypeName = "decimal(4,2)")]
    public decimal StandardHoursPerDay { get; set; } = 8.00m;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal HourlyRate { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    // Assignment
    public int? PrimaryWorkCenterId { get; set; }

    // Audit fields
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    [ForeignKey("PrimaryWorkCenterId")]
    public virtual WorkCenter? PrimaryWorkCenter { get; set; }

    public virtual ICollection<WorkOrderResource> WorkOrderAssignments { get; set; } = new List<WorkOrderResource>();
}

[Table("WorkOrderResources")]
public class WorkOrderResource
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int WorkOrderId { get; set; }

    [Required]
    public int ResourceId { get; set; }

    // Assignment details
    [Required]
    [StringLength(20)]
    public string AssignmentType { get; set; } = "Primary"; // Primary, Secondary, Support

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal EstimatedHours { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ActualHours { get; set; }

    // Time tracking
    [Required]
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

    public DateTime? StartedDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    // Navigation properties
    [ForeignKey("WorkOrderId")]
    public virtual WorkOrder WorkOrder { get; set; } = null!;

    [ForeignKey("ResourceId")]
    public virtual Resource Resource { get; set; } = null!;
}

// ==========================================
// ENUMS FOR TYPE SAFETY
// ==========================================

public enum WorkOrderType
{
    PartsProcessing,
    AssemblyBuilding,
    Mixed,
    Finishing,
    QualityControl
}

public enum WorkOrderStatus
{
    Created,
    Scheduled,
    Released,
    InProgress,
    OnHold,
    Complete,
    Cancelled
}

public enum WorkOrderPriority
{
    Low,
    Normal,
    High,
    Urgent
}

public enum OperationStatus
{
    Planned,
    Ready,
    InProgress,
    Complete,
    OnHold
}

public enum ItemProcessStatus
{
    Planned,
    Issued,
    InProgress,
    Complete
}

public enum AssemblyBuildStatus
{
    Planned,
    InProgress,
    Complete,
    OnHold
}

public enum ResourceType
{
    Direct,
    Indirect,
    Contract,
    Supervisor
}

public enum AssignmentType
{
    Primary,
    Secondary,
    Support
}
