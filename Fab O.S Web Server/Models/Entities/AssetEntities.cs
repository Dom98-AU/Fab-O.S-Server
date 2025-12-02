using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities.Assets;

#region Location

/// <summary>
/// Location type enumeration
/// </summary>
public enum LocationType
{
    PhysicalSite = 0,    // Warehouses, workshops, offices
    JobSite = 1,         // Temporary project/job sites
    Vehicle = 2          // Trucks, trailers, mobile containers
}

/// <summary>
/// Location entity for tracking where equipment is allocated
/// </summary>
[Table("Locations")]
public class Location
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    /// <summary>
    /// Location code (e.g., "WH-001", "SITE-123", "VEH-T01")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string LocationCode { get; set; } = string.Empty;

    /// <summary>
    /// Location name (e.g., "Main Warehouse", "Sydney CBD Project", "Truck 01")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public LocationType Type { get; set; } = LocationType.PhysicalSite;

    /// <summary>
    /// Physical address or description
    /// </summary>
    [MaxLength(500)]
    public string? Address { get; set; }

    /// <summary>
    /// Site contact person
    /// </summary>
    [MaxLength(100)]
    public string? ContactName { get; set; }

    [MaxLength(50)]
    public string? ContactPhone { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    // Audit
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    public DateTime? LastModified { get; set; }
    public int? LastModifiedByUserId { get; set; }

    // Navigation
    public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
    public virtual ICollection<EquipmentKit> Kits { get; set; } = new List<EquipmentKit>();
}

#endregion

#region Equipment Category & Type

/// <summary>
/// Equipment category (e.g., Fabrication, Workshop, Lifting)
/// </summary>
public class EquipmentCategory
{
    [Key]
    public int Id { get; set; }

    public int CompanyId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? IconClass { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// System categories cannot be deleted
    /// </summary>
    public bool IsSystemCategory { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? LastModified { get; set; }

    // Navigation
    public virtual ICollection<EquipmentType> EquipmentTypes { get; set; } = new List<EquipmentType>();
    public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
}

/// <summary>
/// Equipment type within a category (e.g., CNC Machine, Hand Drill, Forklift)
/// </summary>
public class EquipmentType
{
    [Key]
    public int Id { get; set; }

    public int CategoryId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Default maintenance interval in days
    /// </summary>
    public int? DefaultMaintenanceIntervalDays { get; set; }

    /// <summary>
    /// Required certifications for this type (comma-separated)
    /// </summary>
    [MaxLength(500)]
    public string? RequiredCertifications { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsSystemType { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? LastModified { get; set; }

    // Navigation
    [ForeignKey("CategoryId")]
    public virtual EquipmentCategory? EquipmentCategory { get; set; }

    public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
}

#endregion

#region Equipment

/// <summary>
/// Equipment status enumeration
/// </summary>
public enum EquipmentStatus
{
    Active = 0,
    InMaintenance = 1,
    OutOfService = 2,
    Retired = 3,
    Disposed = 4
}

/// <summary>
/// Main equipment entity for asset tracking
/// </summary>
public class Equipment
{
    [Key]
    public int Id { get; set; }

    public int CompanyId { get; set; }

    [Required]
    [MaxLength(50)]
    public string EquipmentCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int CategoryId { get; set; }

    public int TypeId { get; set; }

    [MaxLength(100)]
    public string? Manufacturer { get; set; }

    [MaxLength(100)]
    public string? Model { get; set; }

    [MaxLength(100)]
    public string? SerialNumber { get; set; }

    public DateTime? PurchaseDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PurchaseCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? CurrentValue { get; set; }

    public DateTime? WarrantyExpiry { get; set; }

    /// <summary>
    /// Legacy location field (deprecated - use LocationId instead)
    /// </summary>
    [MaxLength(200)]
    public string? LocationLegacy { get; set; }

    /// <summary>
    /// Foreign key to Location entity
    /// </summary>
    public int? LocationId { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    [MaxLength(100)]
    public string? AssignedTo { get; set; }

    public int? AssignedToUserId { get; set; }

    public EquipmentStatus Status { get; set; } = EquipmentStatus.Active;

    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// QR Code data URL (base64 encoded image)
    /// </summary>
    public string? QRCodeData { get; set; }

    /// <summary>
    /// Unique identifier for QR code lookup
    /// </summary>
    [MaxLength(50)]
    public string? QRCodeIdentifier { get; set; }

    public DateTime? LastMaintenanceDate { get; set; }

    public DateTime? NextMaintenanceDate { get; set; }

    /// <summary>
    /// Custom maintenance interval (overrides type default)
    /// </summary>
    public int? MaintenanceIntervalDays { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime? LastModified { get; set; }

    [MaxLength(100)]
    public string? LastModifiedBy { get; set; }

    // Navigation
    [ForeignKey("CategoryId")]
    public virtual EquipmentCategory? EquipmentCategory { get; set; }

    [ForeignKey("TypeId")]
    public virtual EquipmentType? EquipmentType { get; set; }

    [ForeignKey("LocationId")]
    public virtual Location? Location { get; set; }

    public virtual ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; } = new List<MaintenanceSchedule>();
    public virtual ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
    public virtual ICollection<EquipmentCertification> Certifications { get; set; } = new List<EquipmentCertification>();
    public virtual ICollection<EquipmentManual> Manuals { get; set; } = new List<EquipmentManual>();
}

#endregion

#region Maintenance

/// <summary>
/// Maintenance frequency type
/// </summary>
public enum MaintenanceFrequency
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2,
    Quarterly = 3,
    Yearly = 4,
    AsNeeded = 5,
    Custom = 6
}

/// <summary>
/// Maintenance schedule status
/// </summary>
public enum MaintenanceScheduleStatus
{
    Active = 0,
    Paused = 1,
    Completed = 2,
    Cancelled = 3
}

/// <summary>
/// Preventive maintenance schedule
/// </summary>
public class MaintenanceSchedule
{
    [Key]
    public int Id { get; set; }

    public int EquipmentId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public MaintenanceFrequency Frequency { get; set; }

    /// <summary>
    /// Custom interval in days (when Frequency = Custom)
    /// </summary>
    public int? CustomIntervalDays { get; set; }

    public DateTime? LastPerformed { get; set; }

    public DateTime? NextDue { get; set; }

    /// <summary>
    /// Days before due date to send reminder
    /// </summary>
    public int ReminderDaysBefore { get; set; } = 7;

    public MaintenanceScheduleStatus Status { get; set; } = MaintenanceScheduleStatus.Active;

    /// <summary>
    /// Estimated hours to complete
    /// </summary>
    [Column(TypeName = "decimal(8,2)")]
    public decimal? EstimatedHours { get; set; }

    /// <summary>
    /// Estimated cost
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? EstimatedCost { get; set; }

    [MaxLength(200)]
    public string? AssignedTo { get; set; }

    /// <summary>
    /// Checklist items (JSON array)
    /// </summary>
    public string? ChecklistItems { get; set; }

    /// <summary>
    /// Required parts/materials (JSON array)
    /// </summary>
    public string? RequiredParts { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime? LastModified { get; set; }

    // Navigation
    [ForeignKey("EquipmentId")]
    public virtual Equipment? Equipment { get; set; }

    public virtual ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
}

/// <summary>
/// Maintenance record status
/// </summary>
public enum MaintenanceRecordStatus
{
    Scheduled = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3,
    Overdue = 4
}

/// <summary>
/// Actual maintenance record (completed or scheduled work)
/// </summary>
public class MaintenanceRecord
{
    [Key]
    public int Id { get; set; }

    public int EquipmentId { get; set; }

    public int? ScheduleId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? MaintenanceType { get; set; }

    public DateTime ScheduledDate { get; set; }

    public DateTime? StartedDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public MaintenanceRecordStatus Status { get; set; } = MaintenanceRecordStatus.Scheduled;

    [MaxLength(200)]
    public string? PerformedBy { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal? ActualHours { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? LaborCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PartsCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? TotalCost { get; set; }

    /// <summary>
    /// Completed checklist (JSON)
    /// </summary>
    public string? CompletedChecklist { get; set; }

    /// <summary>
    /// Parts used (JSON array)
    /// </summary>
    public string? PartsUsed { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Document attachments (JSON array of URLs)
    /// </summary>
    public string? Attachments { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime? LastModified { get; set; }

    // Navigation
    [ForeignKey("EquipmentId")]
    public virtual Equipment? Equipment { get; set; }

    [ForeignKey("ScheduleId")]
    public virtual MaintenanceSchedule? Schedule { get; set; }
}

#endregion

#region Certifications

/// <summary>
/// Equipment certification (safety, compliance, etc.)
/// </summary>
public class EquipmentCertification
{
    [Key]
    public int Id { get; set; }

    public int EquipmentId { get; set; }

    [Required]
    [MaxLength(100)]
    public string CertificationType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? CertificateNumber { get; set; }

    [MaxLength(200)]
    public string? IssuingAuthority { get; set; }

    public DateTime IssueDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [MaxLength(500)]
    public string? DocumentUrl { get; set; }

    /// <summary>
    /// Valid, Expired, Expiring, Revoked
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = "Valid";

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? LastModified { get; set; }

    // Navigation
    [ForeignKey("EquipmentId")]
    public virtual Equipment? Equipment { get; set; }
}

#endregion

#region Manuals

/// <summary>
/// Equipment manual/documentation
/// </summary>
public class EquipmentManual
{
    [Key]
    public int Id { get; set; }

    public int EquipmentId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ManualType { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? DocumentUrl { get; set; }

    [MaxLength(50)]
    public string? Version { get; set; }

    [MaxLength(200)]
    public string? FileName { get; set; }

    public long? FileSize { get; set; }

    [MaxLength(100)]
    public string? ContentType { get; set; }

    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? UploadedBy { get; set; }

    public DateTime? LastModified { get; set; }

    // Navigation
    [ForeignKey("EquipmentId")]
    public virtual Equipment? Equipment { get; set; }
}

#endregion

#region Equipment Kits

/// <summary>
/// Kit status enumeration
/// </summary>
public enum KitStatus
{
    Available = 0,
    CheckedOut = 1,
    PartialReturn = 2,
    MaintenanceRequired = 3,
    Retired = 4
}

/// <summary>
/// Checkout status enumeration
/// </summary>
public enum CheckoutStatus
{
    Pending = 0,
    CheckedOut = 1,
    Returned = 2,
    PartialReturn = 3,
    Overdue = 4,
    Cancelled = 5
}

/// <summary>
/// Equipment condition enumeration
/// </summary>
public enum EquipmentCondition
{
    Excellent = 0,
    Good = 1,
    Fair = 2,
    Poor = 3,
    Damaged = 4
}

/// <summary>
/// Kit template - predefined composition of equipment types
/// </summary>
[Table("KitTemplates")]
public class KitTemplate
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    /// <summary>
    /// Template code (e.g., TMPL-WELD-001)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// Template name (e.g., "Welding Kit")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Category for grouping templates (e.g., "Welding", "Electrical", "Safety")
    /// </summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// FontAwesome icon class
    /// </summary>
    [MaxLength(50)]
    public string? IconClass { get; set; }

    /// <summary>
    /// Default checkout duration in days
    /// </summary>
    public int DefaultCheckoutDays { get; set; } = 7;

    /// <summary>
    /// Whether checkout requires a digital signature
    /// </summary>
    public bool RequiresSignature { get; set; } = true;

    /// <summary>
    /// Whether checkout requires condition assessment
    /// </summary>
    public bool RequiresConditionCheck { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    // Audit
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    public DateTime? LastModified { get; set; }
    public int? LastModifiedByUserId { get; set; }

    // Navigation
    public virtual ICollection<KitTemplateItem> TemplateItems { get; set; } = new List<KitTemplateItem>();
    public virtual ICollection<EquipmentKit> Kits { get; set; } = new List<EquipmentKit>();
}

/// <summary>
/// Template item - defines what equipment types are included in a template
/// </summary>
[Table("KitTemplateItems")]
public class KitTemplateItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int KitTemplateId { get; set; }

    /// <summary>
    /// Equipment type (abstract type, not specific equipment)
    /// </summary>
    [Required]
    public int EquipmentTypeId { get; set; }

    /// <summary>
    /// Number of items of this type required
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Whether this item is mandatory for the kit
    /// </summary>
    public bool IsMandatory { get; set; } = true;

    /// <summary>
    /// Display order in the template
    /// </summary>
    public int DisplayOrder { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation
    [ForeignKey("KitTemplateId")]
    public virtual KitTemplate? KitTemplate { get; set; }

    [ForeignKey("EquipmentTypeId")]
    public virtual EquipmentType? EquipmentType { get; set; }
}

/// <summary>
/// Equipment kit instance - actual grouping of equipment
/// </summary>
[Table("EquipmentKits")]
public class EquipmentKit
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    /// <summary>
    /// Template this kit was created from (NULL for ad-hoc kits)
    /// </summary>
    public int? KitTemplateId { get; set; }

    /// <summary>
    /// Kit code (e.g., KIT-2025-0001)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string KitCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public KitStatus Status { get; set; } = KitStatus.Available;

    /// <summary>
    /// Legacy location field (deprecated - use LocationId instead)
    /// </summary>
    [MaxLength(200)]
    public string? LocationLegacy { get; set; }

    /// <summary>
    /// Foreign key to Location entity
    /// </summary>
    public int? LocationId { get; set; }

    // Assignment
    public int? AssignedToUserId { get; set; }

    [MaxLength(200)]
    public string? AssignedToUserName { get; set; }

    // QR Code
    public string? QRCodeData { get; set; }

    [MaxLength(50)]
    public string? QRCodeIdentifier { get; set; }

    // Maintenance Flag (kit stays active, just flagged)
    public bool HasMaintenanceFlag { get; set; }

    [MaxLength(500)]
    public string? MaintenanceFlagNotes { get; set; }

    public bool IsDeleted { get; set; }

    // Audit
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    public DateTime? LastModified { get; set; }
    public int? LastModifiedByUserId { get; set; }

    // Navigation
    [ForeignKey("KitTemplateId")]
    public virtual KitTemplate? KitTemplate { get; set; }

    [ForeignKey("LocationId")]
    public virtual Location? Location { get; set; }

    public virtual ICollection<EquipmentKitItem> KitItems { get; set; } = new List<EquipmentKitItem>();
    public virtual ICollection<KitCheckout> Checkouts { get; set; } = new List<KitCheckout>();
}

/// <summary>
/// Equipment kit item - specific equipment in a kit
/// </summary>
[Table("EquipmentKitItems")]
public class EquipmentKitItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int KitId { get; set; }

    [Required]
    public int EquipmentId { get; set; }

    /// <summary>
    /// Which template slot this fills (NULL for ad-hoc kits)
    /// </summary>
    public int? TemplateItemId { get; set; }

    /// <summary>
    /// Display order in the kit
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this item needs maintenance
    /// </summary>
    public bool NeedsMaintenance { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime AddedDate { get; set; } = DateTime.UtcNow;
    public int? AddedByUserId { get; set; }

    // Navigation
    [ForeignKey("KitId")]
    public virtual EquipmentKit? Kit { get; set; }

    [ForeignKey("EquipmentId")]
    public virtual Equipment? Equipment { get; set; }

    [ForeignKey("TemplateItemId")]
    public virtual KitTemplateItem? TemplateItem { get; set; }
}

/// <summary>
/// Kit checkout/return transaction
/// </summary>
[Table("KitCheckouts")]
public class KitCheckout
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    public int KitId { get; set; }

    public CheckoutStatus Status { get; set; } = CheckoutStatus.Pending;

    // Checkout Details
    [Required]
    public int CheckedOutToUserId { get; set; }

    [MaxLength(200)]
    public string? CheckedOutToUserName { get; set; }

    public DateTime CheckoutDate { get; set; } = DateTime.UtcNow;

    public DateTime ExpectedReturnDate { get; set; }

    [MaxLength(500)]
    public string? CheckoutPurpose { get; set; }

    [MaxLength(100)]
    public string? ProjectReference { get; set; }

    public EquipmentCondition CheckoutOverallCondition { get; set; } = EquipmentCondition.Good;

    [MaxLength(1000)]
    public string? CheckoutNotes { get; set; }

    // Checkout Signature (base64 encoded)
    public string? CheckoutSignature { get; set; }
    public DateTime? CheckoutSignedDate { get; set; }
    public int? CheckoutProcessedByUserId { get; set; }

    // Return Details
    public DateTime? ActualReturnDate { get; set; }
    public int? ReturnedByUserId { get; set; }

    [MaxLength(200)]
    public string? ReturnedByUserName { get; set; }

    public EquipmentCondition? ReturnOverallCondition { get; set; }

    [MaxLength(1000)]
    public string? ReturnNotes { get; set; }

    // Return Signature (base64 encoded)
    public string? ReturnSignature { get; set; }
    public DateTime? ReturnSignedDate { get; set; }
    public int? ReturnProcessedByUserId { get; set; }

    // Audit
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    public DateTime? LastModified { get; set; }
    public int? LastModifiedByUserId { get; set; }

    // Navigation
    [ForeignKey("KitId")]
    public virtual EquipmentKit? Kit { get; set; }

    public virtual ICollection<KitCheckoutItem> CheckoutItems { get; set; } = new List<KitCheckoutItem>();
}

/// <summary>
/// Individual item condition tracking for checkout/return
/// </summary>
[Table("KitCheckoutItems")]
public class KitCheckoutItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int KitCheckoutId { get; set; }

    [Required]
    public int KitItemId { get; set; }

    [Required]
    public int EquipmentId { get; set; }

    // Checkout Condition
    public bool WasPresentAtCheckout { get; set; } = true;
    public EquipmentCondition? CheckoutCondition { get; set; }

    [MaxLength(500)]
    public string? CheckoutNotes { get; set; }

    // Return Condition
    public bool? WasPresentAtReturn { get; set; }
    public EquipmentCondition? ReturnCondition { get; set; }

    [MaxLength(500)]
    public string? ReturnNotes { get; set; }

    // Damage Tracking
    public bool DamageReported { get; set; }

    [MaxLength(1000)]
    public string? DamageDescription { get; set; }

    // Navigation
    [ForeignKey("KitCheckoutId")]
    public virtual KitCheckout? KitCheckout { get; set; }

    [ForeignKey("KitItemId")]
    public virtual EquipmentKitItem? KitItem { get; set; }

    [ForeignKey("EquipmentId")]
    public virtual Equipment? Equipment { get; set; }
}

#endregion
