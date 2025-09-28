using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

// Enumerations for Trace Module
public enum TraceableType
{
    RawMaterial = 1,
    ProcessedMaterial = 2,
    Operation = 3,
    Assembly = 4,
    Product = 5,
    Package = 6,
    Shipment = 7
}

public enum TraceStatus
{
    Active = 1,
    InProgress = 2,
    Completed = 3,
    OnHold = 4,
    Cancelled = 5
}

public enum CertificateType
{
    Type21 = 1,  // 2.1 - Inspection certificate based on specific testing
    Type22 = 2,  // 2.2 - Inspection certificate based on routine testing
    Type31 = 3,  // 3.1 - Inspection certificate based on specific testing
    Type32 = 4   // 3.2 - Inspection certificate based on routine testing
}

public enum DocumentType
{
    MillCertificate = 1,
    TestReport = 2,
    Photo = 3,
    InspectionReport = 4,
    ComplianceCertificate = 5,
    Drawing = 6,
    WorkInstruction = 7,
    QualityRecord = 8
}

// Core Trace Entity
[Table("TraceRecords")]
public class TraceRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid TraceId { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(50)]
    public string TraceNumber { get; set; } = string.Empty; // TRC-2025-000001

    // What we're tracing
    [Required]
    public TraceableType EntityType { get; set; }

    [Required]
    public int EntityId { get; set; }

    [StringLength(100)]
    public string? EntityReference { get; set; } // Order#, WO#, etc.

    // Hierarchy
    public Guid? ParentTraceId { get; set; }
    public virtual ICollection<TraceRecord> ChildTraces { get; set; } = new List<TraceRecord>();

    // When
    [Required]
    public DateTime CaptureDateTime { get; set; }

    public DateTime? EventDateTime { get; set; }

    // Who
    public int? UserId { get; set; }

    [StringLength(100)]
    public string? OperatorName { get; set; }

    // Where
    public int? WorkCenterId { get; set; }

    [StringLength(100)]
    public string? Location { get; set; }

    [StringLength(50)]
    public string? MachineId { get; set; }

    // Status
    [Required]
    public TraceStatus Status { get; set; } = TraceStatus.Active;

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    [Required]
    public int CompanyId { get; set; }

    // Navigation properties
    public virtual User? User { get; set; }
    public virtual WorkCenter? WorkCenter { get; set; }
    public virtual Company Company { get; set; } = null!;
    public virtual ICollection<TraceMaterial> Materials { get; set; } = new List<TraceMaterial>();
    public virtual ICollection<TraceProcess> Processes { get; set; } = new List<TraceProcess>();
    public virtual ICollection<TraceAssembly> Assemblies { get; set; } = new List<TraceAssembly>();
    public virtual ICollection<TraceDocument> Documents { get; set; } = new List<TraceDocument>();
}

// Material Traceability
[Table("TraceMaterials")]
public class TraceMaterial
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TraceRecordId { get; set; }

    // Material identification
    public int? CatalogueItemId { get; set; }

    [Required]
    [StringLength(50)]
    public string MaterialCode { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    // Source tracking
    [StringLength(50)]
    public string? HeatNumber { get; set; }

    [StringLength(50)]
    public string? BatchNumber { get; set; }

    [StringLength(50)]
    public string? SerialNumber { get; set; }

    [StringLength(100)]
    public string? Supplier { get; set; }

    [StringLength(50)]
    public string? SupplierBatch { get; set; }

    // Certification
    [StringLength(100)]
    public string? MillCertificate { get; set; }

    public CertificateType? CertType { get; set; } // 2.1, 2.2, 3.1, 3.2

    public DateTime? CertDate { get; set; }

    // Quantities
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    [Required]
    [StringLength(20)]
    public string Unit { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,4)")]
    public decimal? Weight { get; set; }

    // Test data
    [StringLength(500)]
    public string? ChemicalComposition { get; set; }

    [StringLength(500)]
    public string? MechanicalProperties { get; set; }

    [StringLength(1000)]
    public string? TestResults { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; }

    // Navigation properties
    public virtual TraceRecord TraceRecord { get; set; } = null!;
    public virtual CatalogueItem? CatalogueItem { get; set; }
}

// Process Traceability
[Table("TraceProcesses")]
public class TraceProcess
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TraceRecordId { get; set; }

    // Operation details
    public int? WorkOrderOperationId { get; set; }

    [Required]
    [StringLength(50)]
    public string OperationCode { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string OperationDescription { get; set; } = string.Empty;

    // Timing
    [Required]
    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DurationMinutes { get; set; }

    // Resources
    public int? OperatorId { get; set; }

    [StringLength(100)]
    public string? OperatorName { get; set; }

    public int? MachineId { get; set; }

    [StringLength(100)]
    public string? MachineName { get; set; }

    // Quality
    public bool? PassedInspection { get; set; }

    [StringLength(500)]
    public string? InspectionNotes { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; }

    // Navigation properties
    public virtual TraceRecord TraceRecord { get; set; } = null!;
    public virtual WorkOrderOperation? WorkOrderOperation { get; set; }
    public virtual User? Operator { get; set; }
    public virtual ICollection<TraceParameter> Parameters { get; set; } = new List<TraceParameter>();
}

// Process Parameters
[Table("TraceParameters")]
public class TraceParameter
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TraceProcessId { get; set; }

    [Required]
    [StringLength(100)]
    public string ParameterName { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string ParameterValue { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Unit { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? NumericValue { get; set; }

    // Common parameters
    [StringLength(50)]
    public string? Category { get; set; } // Welding, Cutting, Painting

    [Required]
    public DateTime CreatedDate { get; set; }

    // Navigation properties
    public virtual TraceProcess TraceProcess { get; set; } = null!;
}

// Assembly Traceability (Genealogy)
[Table("TraceAssemblies")]
public class TraceAssembly
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TraceRecordId { get; set; }

    public int? AssemblyId { get; set; }

    // Assembly identification
    [Required]
    [StringLength(100)]
    public string AssemblyNumber { get; set; } = string.Empty;

    [StringLength(50)]
    public string? SerialNumber { get; set; }

    [Required]
    public DateTime AssemblyDate { get; set; }

    // Build details
    public int? BuildOperatorId { get; set; }

    [StringLength(100)]
    public string? BuildOperatorName { get; set; }

    public int? BuildWorkCenterId { get; set; }

    [StringLength(100)]
    public string? BuildLocation { get; set; }

    [StringLength(1000)]
    public string? BuildNotes { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; }

    // Navigation properties
    public virtual TraceRecord TraceRecord { get; set; } = null!;
    public virtual Assembly? Assembly { get; set; }
    public virtual User? BuildOperator { get; set; }
    public virtual WorkCenter? BuildWorkCenter { get; set; }
    public virtual ICollection<TraceComponent> Components { get; set; } = new List<TraceComponent>();
}

// Component Usage in Assembly
[Table("TraceComponents")]
public class TraceComponent
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TraceAssemblyId { get; set; }

    [Required]
    public Guid ComponentTraceId { get; set; }

    [Required]
    [StringLength(100)]
    public string ComponentReference { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal QuantityUsed { get; set; }

    [Required]
    [StringLength(20)]
    public string Unit { get; set; } = string.Empty;

    [StringLength(500)]
    public string? UsageNotes { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; }

    // Navigation properties
    public virtual TraceAssembly TraceAssembly { get; set; } = null!;
}

// Document Management
[Table("TraceDocuments")]
public class TraceDocument
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TraceRecordId { get; set; }

    [Required]
    public DocumentType DocumentType { get; set; }

    [Required]
    [StringLength(200)]
    public string DocumentName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [StringLength(100)]
    public string? FileHash { get; set; }

    [Required]
    public long FileSize { get; set; }

    [Required]
    [StringLength(100)]
    public string MimeType { get; set; } = string.Empty;

    [Required]
    public bool IsVerified { get; set; } = false;

    public DateTime? VerifiedDate { get; set; }

    public int? VerifiedBy { get; set; }

    [Required]
    public DateTime UploadDate { get; set; }

    public int? UploadedBy { get; set; }

    [Required]
    public int CompanyId { get; set; }

    // Navigation properties
    public virtual TraceRecord TraceRecord { get; set; } = null!;
    public virtual User? VerifiedByUser { get; set; }
    public virtual User? UploadedByUser { get; set; }
    public virtual Company Company { get; set; } = null!;
}
