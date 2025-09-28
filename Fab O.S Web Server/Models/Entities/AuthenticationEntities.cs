using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

/// <summary>
/// Refresh token entity for JWT authentication
/// </summary>
[Table("RefreshTokens")]
public class RefreshToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(500)]
    public string Token { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; } = false;

    [StringLength(50)]
    public string? DeviceInfo { get; set; }

    [StringLength(45)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    // Navigation property
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// User authentication methods tracking (for social login support)
/// </summary>
[Table("UserAuthMethods")]
public class UserAuthMethod
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(50)]
    public string Provider { get; set; } = string.Empty; // "Local", "Microsoft", "Google", "LinkedIn"

    [StringLength(200)]
    public string? ExternalId { get; set; }

    [StringLength(200)]
    public string? ExternalEmail { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    // Navigation property
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// Authentication audit log for security tracking
/// </summary>
[Table("AuthAuditLogs")]
public class AuthAuditLog
{
    [Key]
    public int Id { get; set; }

    public int? UserId { get; set; }

    [Required]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty; // "Login", "Logout", "FailedLogin", "TokenRefresh", etc.

    [Required]
    [StringLength(50)]
    public string AuthMethod { get; set; } = string.Empty; // "Cookie", "JWT", "Microsoft", "Google", etc.

    public bool Success { get; set; }

    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    [StringLength(45)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    public DateTime Timestamp { get; set; }

    [StringLength(100)]
    public string? SessionId { get; set; }

    // Navigation property
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
