using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

/// <summary>
/// Routing - Master template defining the manufacturing process for a product
/// Similar to Microsoft Dynamics 365 Business Central Manufacturing Routing
/// Reusable template that can be applied to multiple work orders
/// </summary>
[Table("Routings")]
public class Routing
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string RoutingCode { get; set; } = string.Empty; // "COL-BASE-FAB", "BEAM-WELD-01"

    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty; // "Column Base Fabrication Process"

    [StringLength(2000)]
    public string? Notes { get; set; }

    // Optional: Link to specific item/product (if routing is item-specific)
    public int? ItemId { get; set; } // Future: Link to Items/Products table

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Draft"; // Draft, Certified, Under Development, Closed

    [Required]
    [StringLength(20)]
    public string Type { get; set; } = "Serial"; // Serial (sequential), Parallel (concurrent)

    // Audit fields
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public int? CreatedBy { get; set; }
    public int? LastModifiedBy { get; set; }

    // Multi-tenant
    [Required]
    public int CompanyId { get; set; }

    // Navigation properties
    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("LastModifiedBy")]
    public virtual User? LastModifiedByUser { get; set; }

    public virtual ICollection<RoutingLine> RoutingLines { get; set; } = new List<RoutingLine>();
}

/// <summary>
/// RoutingLine - Individual step in a master routing
/// Specifies Work Center and/or Resource requirements for each operation
/// Business Central style: Can assign to WorkCenter, Resource, or ResourceGroup
/// </summary>
[Table("RoutingLines")]
public class RoutingLine
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int RoutingId { get; set; }

    [Required]
    public int SequenceNumber { get; set; } // 10, 20, 30, 40... (Business Central style increments of 10)

    // FLEXIBLE ASSIGNMENT (at least ONE must be specified)
    public int? WorkCenterId { get; set; }      // Optional: Which work center (location/machine)
    public int? ResourceId { get; set; }        // Optional: Specific resource (person/machine)

    [StringLength(100)]
    public string? ResourceGroup { get; set; }  // Optional: "Level 3 Welders", "CNC Operators"

    // Operation details
    [Required]
    [StringLength(20)]
    public string OperationType { get; set; } = string.Empty; // CUT, DRILL, WELD, PAINT, INSPECT, ASSEMBLE

    [StringLength(50)]
    public string? OperationCode { get; set; } // Operation code reference

    [StringLength(200)]
    public string? OperationName { get; set; } // Operation name

    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty; // "Cut steel beams to length"

    [StringLength(1000)]
    public string? DetailedInstructions { get; set; }

    // Status tracking
    [StringLength(50)]
    public string? Status { get; set; } // NotStarted, InProgress, Completed, OnHold

    public DateTime? StartDateTime { get; set; } // When operation started

    public DateTime? EndDateTime { get; set; } // When operation completed

    // Actual time tracking (for progress tracking)
    [Column(TypeName = "decimal(10,2)")]
    public decimal ActualSetupTime { get; set; } = 0;

    [Column(TypeName = "decimal(10,2)")]
    public decimal ActualRunTime { get; set; } = 0;

    [Column(TypeName = "decimal(10,2)")]
    public decimal QuantityProcessed { get; set; } = 0;

    // Time allocation (Business Central style: Setup + Run time)
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal SetupTime { get; set; } = 0; // Hours for setup (one-time per batch)

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal RunTime { get; set; } = 0; // Hours per unit (or total batch time)

    [Required]
    [StringLength(10)]
    public string RunTimeUnit { get; set; } = "Batch"; // "PerUnit", "Batch", "PerHour"

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal WaitTime { get; set; } = 0; // Queue time before this operation

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal MoveTime { get; set; } = 0; // Time to transport to next work center

    // Concurrent operations (Business Central parallel routing)
    public int? NextOperationSequence { get; set; } // NULL = sequential, value = can start in parallel

    // Send-Ahead Quantity (Business Central feature)
    [Column(TypeName = "decimal(10,2)")]
    public decimal? SendAheadQuantity { get; set; } // Can send this qty to next operation before completion

    // Quality requirements
    [Required]
    public bool RequiresInspection { get; set; } = false;

    [StringLength(50)]
    public string? InspectionType { get; set; } // Visual, Dimensional, NDT, Functional

    // Audit fields
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("RoutingId")]
    public virtual Routing Routing { get; set; } = null!;

    [ForeignKey("WorkCenterId")]
    public virtual WorkCenter? WorkCenter { get; set; }

    [ForeignKey("ResourceId")]
    public virtual Resource? Resource { get; set; }
}

/// <summary>
/// WorkOrderRouting - Instance of a routing applied to a specific WorkOrder
/// Copy of master routing that can be customized for this specific job
/// </summary>
[Table("WorkOrderRoutings")]
public class WorkOrderRouting
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int WorkOrderId { get; set; }

    public int? SourceRoutingId { get; set; } // Link to master Routing (if copied from template)

    [StringLength(200)]
    public string? Description { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Planned"; // Planned, InProgress, Finished, Closed

    // Audit fields
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("WorkOrderId")]
    public virtual WorkOrder WorkOrder { get; set; } = null!;

    [ForeignKey("SourceRoutingId")]
    public virtual Routing? SourceRouting { get; set; }

    public virtual ICollection<WorkOrderRoutingLine> RoutingLines { get; set; } = new List<WorkOrderRoutingLine>();
}

/// <summary>
/// WorkOrderRoutingLine - Actual execution of a routing step on a specific WorkOrder
/// Tracks planned vs actual time, status, and completion
/// This is where shop floor execution happens
/// </summary>
[Table("WorkOrderRoutingLines")]
public class WorkOrderRoutingLine
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int WorkOrderRoutingId { get; set; }

    [Required]
    public int SequenceNumber { get; set; }

    // Assignment (copied from master routing, can be overridden when scheduling)
    public int? WorkCenterId { get; set; }
    public int? AssignedResourceId { get; set; }  // Can assign specific person during scheduling

    [StringLength(100)]
    public string? ResourceGroup { get; set; }

    // Operation details
    [Required]
    [StringLength(20)]
    public string OperationType { get; set; } = string.Empty;

    [StringLength(50)]
    public string? OperationCode { get; set; } // Operation code reference

    [StringLength(200)]
    public string? OperationName { get; set; } // Operation name

    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? DetailedInstructions { get; set; }

    // Planned times (copied from master routing, can be adjusted for this job)
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal PlannedSetupTime { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal PlannedRunTime { get; set; } = 0;

    [Required]
    [StringLength(10)]
    public string RunTimeUnit { get; set; } = "Batch";

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal PlannedWaitTime { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal PlannedMoveTime { get; set; } = 0;

    // Actual times (tracked during shop floor execution)
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ActualSetupTime { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ActualRunTime { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ActualWaitTime { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ActualMoveTime { get; set; } = 0;

    // Quantity tracking (for partial completions)
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal QuantityToProcess { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal QuantityProcessed { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal QuantityRejected { get; set; } = 0;

    // Quality requirements
    [Required]
    public bool RequiresInspection { get; set; } = false;

    [StringLength(50)]
    public string? InspectionType { get; set; }

    [StringLength(20)]
    public string? InspectionResult { get; set; } // Pending, Passed, Failed, Waived

    // Status tracking
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Planned"; // Planned, Released, InProgress, Finished, Closed

    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }

    public int? CompletedByResourceId { get; set; } // Which worker completed it

    [StringLength(2000)]
    public string? CompletionNotes { get; set; }

    // Audit fields
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("WorkOrderRoutingId")]
    public virtual WorkOrderRouting WorkOrderRouting { get; set; } = null!;

    [ForeignKey("WorkCenterId")]
    public virtual WorkCenter? WorkCenter { get; set; }

    [ForeignKey("AssignedResourceId")]
    public virtual Resource? AssignedResource { get; set; }

    [ForeignKey("CompletedByResourceId")]
    public virtual Resource? CompletedByResource { get; set; }
}
