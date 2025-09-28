using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

[Table("MachineCenters")]
public class MachineCenter
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string MachineCode { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string MachineName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public int WorkCenterId { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [StringLength(100)]
    public string? Manufacturer { get; set; }

    [StringLength(100)]
    public string? Model { get; set; }

    [StringLength(50)]
    public string? SerialNumber { get; set; }

    public DateTime? PurchaseDate { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? PurchasePrice { get; set; }

    [Required]
    [StringLength(50)]
    public string MachineType { get; set; } = string.Empty;

    [StringLength(100)]
    public string? MachineSubType { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal MaxCapacity { get; set; } = 0;

    [StringLength(20)]
    public string? CapacityUnit { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal SetupTimeMinutes { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal WarmupTimeMinutes { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal CooldownTimeMinutes { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal HourlyRate { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal PowerConsumptionKwh { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal PowerCostPerKwh { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal EfficiencyPercentage { get; set; } = 85;

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal QualityRate { get; set; } = 95;

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal AvailabilityRate { get; set; } = 90;

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public bool IsDeleted { get; set; } = false;

    [Required]
    [StringLength(50)]
    public string CurrentStatus { get; set; } = "Available";

    public DateTime? LastMaintenanceDate { get; set; }

    public DateTime? NextMaintenanceDate { get; set; }

    [Required]
    public int MaintenanceIntervalHours { get; set; } = 500;

    [Required]
    public int CurrentOperatingHours { get; set; } = 0;

    [Required]
    public bool RequiresTooling { get; set; } = false;

    [StringLength(500)]
    public string? ToolingRequirements { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public int? CreatedByUserId { get; set; }

    [Required]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public int? LastModifiedByUserId { get; set; }

    // Navigation properties
    [ForeignKey("WorkCenterId")]
    public virtual WorkCenter WorkCenter { get; set; } = null!;

    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey("CreatedByUserId")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("LastModifiedByUserId")]
    public virtual User? LastModifiedByUser { get; set; }

    public virtual ICollection<MachineCapability> MachineCapabilities { get; set; } = new List<MachineCapability>();
    public virtual ICollection<MachineOperator> MachineOperators { get; set; } = new List<MachineOperator>();
}

[Table("WorkCenters")]
public class WorkCenter
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string WorkCenterCode { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string WorkCenterName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public DateTime LastModified { get; set; }

    // Navigation properties
    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<MachineCenter> MachineCenters { get; set; } = new List<MachineCenter>();
    public virtual ICollection<WorkCenterShift> WorkCenterShifts { get; set; } = new List<WorkCenterShift>();
}

[Table("MachineCapabilities")]
public class MachineCapability
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int MachineCenterId { get; set; }

    [Required]
    [StringLength(100)]
    public string CapabilityName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? MinValue { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? MaxValue { get; set; }

    [StringLength(20)]
    public string? Units { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public DateTime LastModified { get; set; }

    // Navigation properties
    [ForeignKey("MachineCenterId")]
    public virtual MachineCenter MachineCenter { get; set; } = null!;
}

[Table("MachineOperators")]
public class MachineOperator
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int MachineCenterId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(50)]
    public string SkillLevel { get; set; } = string.Empty;

    [Column(TypeName = "decimal(5,2)")]
    public decimal? EfficiencyRating { get; set; }

    [Required]
    public DateTime CertificationDate { get; set; }

    public DateTime? CertificationExpiryDate { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public DateTime LastModified { get; set; }

    // Navigation properties
    [ForeignKey("MachineCenterId")]
    public virtual MachineCenter MachineCenter { get; set; } = null!;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}

[Table("WorkCenterShifts")]
public class WorkCenterShift
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int WorkCenterId { get; set; }

    [Required]
    [StringLength(50)]
    public string ShiftName { get; set; } = string.Empty;

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public DateTime LastModified { get; set; }

    // Navigation properties
    [ForeignKey("WorkCenterId")]
    public virtual WorkCenter WorkCenter { get; set; } = null!;
}
