using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

/// <summary>
/// User invitation for joining existing company
/// </summary>
[Table("UserInvitations")]
public class UserInvitation
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public int InvitedByUserId { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    [StringLength(50)]
    public string Token { get; set; } = string.Empty;

    [Required]
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    [Required]
    public InviteAuthMethod AuthMethod { get; set; } = InviteAuthMethod.Both;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime ExpiresAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    // Navigation properties
    public virtual User? InvitedBy { get; set; }
    public virtual Company? Company { get; set; }
}

/// <summary>
/// Status of user invitation
/// </summary>
public enum InvitationStatus
{
    Pending,
    Accepted,
    Expired,
    Revoked
}

/// <summary>
/// Authentication method for invited user
/// </summary>
public enum InviteAuthMethod
{
    EmailPassword,
    MicrosoftEntraId,
    Both
}
