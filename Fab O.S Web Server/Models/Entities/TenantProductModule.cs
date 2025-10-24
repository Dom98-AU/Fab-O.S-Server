using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

[Table("Users")]
public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string SecurityStamp { get; set; } = string.Empty;

    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [StringLength(200)]
    public string? CompanyName { get; set; }

    [StringLength(100)]
    public string? JobTitle { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [Required]
    public bool IsActive { get; set; }

    [Required]
    public bool IsEmailConfirmed { get; set; }

    public string? EmailConfirmationToken { get; set; }

    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetExpiry { get; set; }

    public DateTime? LastLoginDate { get; set; }

    public DateTime? LastLoginAt { get; set; } // For authentication service

    [Required]
    public int FailedLoginAttempts { get; set; }

    public DateTime? LockedOutUntil { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public DateTime LastModified { get; set; }

    [Required]
    public int CompanyId { get; set; } = 1; // Required - defaults to default company

    [StringLength(100)]
    public string? PasswordSalt { get; set; }

    [StringLength(50)]
    public string? AuthProvider { get; set; }

    [StringLength(256)]
    public string? ExternalUserId { get; set; }

    // Navigation properties
    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!; // Required - every user belongs to a company

    public virtual ICollection<Project> OwnedProjects { get; set; } = new List<Project>();
    public virtual ICollection<Project> ModifiedProjects { get; set; } = new List<Project>();
    public virtual ICollection<Package> CreatedPackages { get; set; } = new List<Package>();
    public virtual ICollection<Package> ModifiedPackages { get; set; } = new List<Package>();
    public virtual ICollection<TraceDrawing> UploadedTraceDrawings { get; set; } = new List<TraceDrawing>();
    public virtual ICollection<MachineCenter> CreatedMachineCenters { get; set; } = new List<MachineCenter>();
    public virtual ICollection<MachineCenter> ModifiedMachineCenters { get; set; } = new List<MachineCenter>();
}