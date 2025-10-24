namespace FabOS.WebServer.Models.Dto.Signup;

/// <summary>
/// Types of conflicts that can occur during signup validation
/// </summary>
public enum ConflictType
{
    None,
    EmailExists,
    CompanyCodeExists,
    DomainExists
}
