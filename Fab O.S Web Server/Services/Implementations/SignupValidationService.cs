using Dapper;
using FabOS.WebServer.Models.DTOs.Signup;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace FabOS.WebServer.Services.Implementations;

/// <summary>
/// Service for validating new company signups with conflict detection
/// </summary>
public class SignupValidationService : ISignupValidationService
{
    private readonly string _connectionString;
    private readonly ILogger<SignupValidationService> _logger;
    private readonly IConfiguration _configuration;

    public SignupValidationService(
        IConfiguration configuration,
        ILogger<SignupValidationService> logger)
    {
        _configuration = configuration;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured");
        _logger = logger;
    }

    public async Task<SignupValidationResult> ValidateSignupAsync(SignupRequest request)
    {
        using var connection = new SqlConnection(_connectionString);
        var result = new SignupValidationResult();

        try
        {
            // Step 1: Check if exact email already exists as a user
            var existingUser = await connection.QueryFirstOrDefaultAsync<ExistingTenantInfo>(@"
                SELECT TOP 1
                    c.Id as TenantId,
                    c.Name as CompanyName,
                    c.Code as CompanyCode,
                    u.Email as AdminEmail,
                    c.CreatedDate as CreatedAt
                FROM Users u
                INNER JOIN Companies c ON u.CompanyId = c.Id
                WHERE LOWER(u.Email) = LOWER(@Email)
                AND u.IsActive = 1
                AND c.IsActive = 1
            ", new { Email = request.Email });

            if (existingUser != null)
            {
                result.ConflictType = ConflictType.EmailExists;
                result.IsValid = false;
                result.Message = $"This email is already registered for {existingUser.CompanyName}";
                result.ExistingTenant = existingUser;
                result.SuggestedActions = new[]
                {
                    new SuggestedAction
                    {
                        Type = "SignIn",
                        Text = "Sign in to existing account",
                        Url = $"/{existingUser.CompanyCode}/login",
                        IsPrimary = true
                    },
                    new SuggestedAction
                    {
                        Type = "Support",
                        Text = "Contact support for help",
                        Url = "/support",
                        IsPrimary = false
                    }
                };
                return result;
            }

            // Step 2: Check if company code is available
            var companyCodeExists = await connection.QueryFirstOrDefaultAsync<ExistingTenantInfo>(@"
                SELECT TOP 1
                    Id as TenantId,
                    Name as CompanyName,
                    Code as CompanyCode,
                    '' as AdminEmail,
                    CreatedDate as CreatedAt
                FROM Companies
                WHERE LOWER(Code) = LOWER(@CompanyCode)
                AND IsActive = 1
            ", new { CompanyCode = request.CompanyCode });

            if (companyCodeExists != null)
            {
                result.ConflictType = ConflictType.CompanyCodeExists;
                result.IsValid = false;
                result.Message = $"Company code '{request.CompanyCode}' is already taken by {companyCodeExists.CompanyName}";
                result.CodeSuggestions = await GenerateCompanyCodeSuggestionsAsync(request.CompanyCode);
                return result;
            }

            // Step 3: Analyze email domain for potential existing workspaces (unless ForceCreateSeparate is true)
            if (!request.ForceCreateSeparate)
            {
                var domainAnalysis = await AnalyzeDomainAsync(request.Email);
                if (domainAnalysis.HasExistingTenants)
                {
                    result.ConflictType = ConflictType.DomainExists;
                    result.IsValid = false;
                    result.Message = $"Your company domain (@{domainAnalysis.Domain}) already has existing workspaces";
                    result.DomainAnalysis = domainAnalysis;
                    result.SuggestedActions = new[]
                    {
                        new SuggestedAction
                        {
                            Type = "JoinExisting",
                            Text = $"Request to join {domainAnalysis.ExistingTenants.First().CompanyName}",
                            Url = $"/{domainAnalysis.ExistingTenants.First().CompanyCode}/request-access",
                            IsPrimary = true
                        },
                        new SuggestedAction
                        {
                            Type = "CreateSeparate",
                            Text = "Create separate workspace anyway",
                            Url = "/signup?force=true",
                            IsPrimary = false
                        }
                    };
                    return result;
                }
            }

            // Step 4: All validations passed
            result.IsValid = true;
            result.Message = "Ready to create new workspace";
            result.PreviewUrl = $"https://{request.CompanyCode}.fab-os.com";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating signup for {Email}", request.Email);
            throw;
        }
    }

    public async Task<DomainAnalysisResult> AnalyzeDomainAsync(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
        {
            return new DomainAnalysisResult { Domain = "", HasExistingTenants = false };
        }

        var domain = email.Split('@')[1].ToLower();
        using var connection = new SqlConnection(_connectionString);

        try
        {
            var existingTenants = await connection.QueryAsync<ExistingTenantInfo>(@"
                SELECT DISTINCT
                    c.Id as TenantId,
                    c.Name as CompanyName,
                    c.Code as CompanyCode,
                    u.Email as AdminEmail,
                    c.CreatedDate as CreatedAt
                FROM Companies c
                INNER JOIN Users u ON u.CompanyId = c.Id
                WHERE LOWER(u.Email) LIKE @DomainPattern
                AND c.IsActive = 1
                AND u.IsActive = 1
                ORDER BY c.CreatedDate DESC
            ", new { DomainPattern = $"%@{domain}" });

            var tenantList = existingTenants.ToList();

            return new DomainAnalysisResult
            {
                Domain = domain,
                HasExistingTenants = tenantList.Any(),
                ExistingTenants = tenantList,
                TenantCount = tenantList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing domain {Domain}", domain);
            return new DomainAnalysisResult { Domain = domain, HasExistingTenants = false };
        }
    }

    public async Task<List<string>> GenerateCompanyCodeSuggestionsAsync(string requestedCode)
    {
        using var connection = new SqlConnection(_connectionString);
        var suggestions = new List<string>();

        try
        {
            // Generate potential alternatives
            var currentYear = DateTime.UtcNow.Year;
            var candidates = new[]
            {
                $"{requestedCode}-{currentYear}",
                $"{requestedCode}-works",
                $"{requestedCode}-ltd",
                $"{requestedCode}-inc",
                $"{requestedCode}-corp",
                $"{requestedCode}-group",
                $"{requestedCode}-team",
                $"{requestedCode}-co"
            };

            foreach (var candidate in candidates)
            {
                var exists = await connection.QueryFirstOrDefaultAsync<int>(@"
                    SELECT COUNT(*) FROM Companies
                    WHERE LOWER(Code) = LOWER(@Code) AND IsActive = 1
                ", new { Code = candidate });

                if (exists == 0)
                {
                    suggestions.Add(candidate);
                }

                if (suggestions.Count >= 3) break;
            }

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating suggestions for {Code}", requestedCode);
            return suggestions;
        }
    }
}
