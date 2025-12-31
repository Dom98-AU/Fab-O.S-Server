using FabOS.WebServer.Authentication;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.DTOs.Signup;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Services.Implementations;

/// <summary>
/// Service for provisioning new tenants (companies) with admin users
/// Integrates with existing authentication system
/// </summary>
public class TenantProvisioningService : ITenantProvisioningService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICookieAuthenticationService _cookieAuthService;
    private readonly IEmailService _emailService;
    private readonly ILogger<TenantProvisioningService> _logger;
    private readonly IConfiguration _configuration;

    public TenantProvisioningService(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ICookieAuthenticationService cookieAuthService,
        IEmailService emailService,
        ILogger<TenantProvisioningService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _cookieAuthService = cookieAuthService;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<TenantCreationResult> CreateTenantAsync(SignupRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Generate company code if not provided or sanitize if provided
            var companyCode = string.IsNullOrEmpty(request.CompanyCode)
                ? GenerateCompanyCode(request.CompanyName)
                : request.CompanyCode.ToLower();

            _logger.LogInformation("Creating new tenant: {CompanyName} with code {CompanyCode}",
                request.CompanyName, companyCode);

            // Create Company
            var company = new Company
            {
                Name = request.CompanyName,
                Code = companyCode,
                ShortName = request.CompanyName.Length > 50
                    ? request.CompanyName.Substring(0, 50)
                    : request.CompanyName,
                IsActive = true,
                SubscriptionLevel = "Standard", // Default subscription level
                MaxUsers = 10, // Default max users
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Company created with ID: {CompanyId}", company.Id);

            // Create Admin User
            var user = new User
            {
                Username = request.Email.Split('@')[0], // Use email prefix as username
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CompanyId = company.Id,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                SecurityStamp = Guid.NewGuid().ToString(),
                IsActive = true,
                IsEmailConfirmed = false, // Can be set to true or implement email verification
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                FailedLoginAttempts = 0
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin user created with ID: {UserId} for company {CompanyId}",
                user.Id, company.Id);

            // Create default module licenses
            var defaultLicenses = new[]
            {
                new ProductLicense
                {
                    CompanyId = company.Id,
                    ProductName = "Trace",
                    LicenseType = "Standard",
                    IsActive = true,
                    ValidFrom = DateTime.UtcNow,
                    ValidUntil = DateTime.UtcNow.AddYears(10),
                    MaxConcurrentUsers = 10,
                    Features = "[\"basic-tracking\",\"takeoffs\",\"measurements\"]",
                    CreatedDate = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    CreatedBy = user.Id
                },
                new ProductLicense
                {
                    CompanyId = company.Id,
                    ProductName = "FabMate",
                    LicenseType = "Standard",
                    IsActive = true,
                    ValidFrom = DateTime.UtcNow,
                    ValidUntil = DateTime.UtcNow.AddYears(10),
                    MaxConcurrentUsers = 10,
                    Features = "[\"work-orders\",\"inventory\",\"purchase-orders\"]",
                    CreatedDate = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    CreatedBy = user.Id
                }
            };

            _context.ProductLicenses.AddRange(defaultLicenses);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Default module licenses created for company {CompanyId}: Trace, FabMate",
                company.Id);

            // Commit transaction
            await transaction.CommitAsync();

            // Sign in the user automatically using existing authentication service
            var signInSuccess = await _cookieAuthService.SignInAsync(user, company, rememberMe: false);

            if (!signInSuccess)
            {
                _logger.LogWarning("Tenant created but auto-login failed for {Email}", request.Email);
            }

            // Send welcome email (fire and forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendWelcomeEmailAsync(
                        user.Email,
                        user.FirstName,
                        company.Name,
                        company.Code);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
                }
            });

            return new TenantCreationResult
            {
                Success = true,
                TenantId = company.Id.ToString(),
                TenantSlug = company.Code,
                CompanyName = company.Name,
                RedirectUrl = $"/{company.Code}/welcome"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating tenant for {Email}", request.Email);

            return new TenantCreationResult
            {
                Success = false,
                ErrorMessage = "Failed to create workspace. Please try again or contact support."
            };
        }
    }

    public string GenerateCompanyCode(string companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
        {
            throw new ArgumentException("Company name cannot be empty", nameof(companyName));
        }

        // Convert to lowercase, replace spaces with hyphens, remove special characters
        var code = companyName
            .ToLower()
            .Replace(" & ", "-and-")
            .Replace("&", "-and-")
            .Replace(" ", "-")
            .Replace("_", "-");

        // Remove any characters that aren't lowercase letters, numbers, or hyphens
        code = new string(code.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

        // Remove multiple consecutive hyphens
        while (code.Contains("--"))
        {
            code = code.Replace("--", "-");
        }

        // Trim hyphens from start and end
        code = code.Trim('-');

        // Ensure minimum length
        if (code.Length < 2)
        {
            code = $"{code}-{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        // Ensure maximum length (company code is max 50 chars)
        if (code.Length > 50)
        {
            code = code.Substring(0, 50).TrimEnd('-');
        }

        return code;
    }
}
