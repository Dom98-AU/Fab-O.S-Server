using FabOS.WebServer.Models.DTOs.Signup;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service for validating new company signups with conflict detection
/// </summary>
public interface ISignupValidationService
{
    /// <summary>
    /// Validates a signup request for conflicts (email exists, code taken, domain exists)
    /// </summary>
    Task<SignupValidationResult> ValidateSignupAsync(SignupRequest request);

    /// <summary>
    /// Generates alternative company code suggestions when the requested code is taken
    /// </summary>
    Task<List<string>> GenerateCompanyCodeSuggestionsAsync(string requestedCode);

    /// <summary>
    /// Analyzes an email domain to find existing workspaces with the same domain
    /// </summary>
    Task<DomainAnalysisResult> AnalyzeDomainAsync(string email);
}
