using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.Signup;

/// <summary>
/// Request for new company signup
/// </summary>
public class SignupRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Company code must contain only lowercase letters, numbers, and hyphens")]
    public string CompanyCode { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;

    public List<string> SelectedModules { get; set; } = new();

    public bool ForceCreateSeparate { get; set; } = false;
}
