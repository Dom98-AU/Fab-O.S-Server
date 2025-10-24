using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.Dto.Signup;

/// <summary>
/// Request for invited user signup
/// </summary>
public class InvitedSignupRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Password (required if AuthMethod is EmailPassword)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Auth method: EmailPassword or MicrosoftEntraId
    /// </summary>
    [Required]
    public string AuthMethod { get; set; } = "EmailPassword";
}
