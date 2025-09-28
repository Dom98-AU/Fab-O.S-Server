using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Authentication;

/// <summary>
/// JWT token service for mobile and API authentication
/// </summary>
public interface IJwtTokenService
{
    Task<AuthTokenResponse> GenerateTokenAsync(User user, Company company);
    Task<AuthTokenResponse> RefreshTokenAsync(string refreshToken);
    ClaimsPrincipal? ValidateToken(string token);
    Task RevokeTokenAsync(string refreshToken);
    Task RevokeAllUserTokensAsync(int userId);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtTokenService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public JwtTokenService(
        IConfiguration configuration, 
        ILogger<JwtTokenService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task<AuthTokenResponse> GenerateTokenAsync(User user, Company company)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured"));
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new("user_id", user.Id.ToString()),
            new("company_id", company.Id.ToString()),
            new("company_code", company.Code),
            new("company_name", company.Name),
            new(ClaimTypes.Role, "User"), // Default role for now
            new("username", user.Username),
            new("is_active", user.IsActive.ToString()),
            new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add job title if available
        if (!string.IsNullOrEmpty(user.JobTitle))
        {
            claims.Add(new Claim("job_title", user.JobTitle));
        }

        // TODO: Add module access based on company subscription or user permissions
        // For now, we'll add basic modules based on company subscription
        var modules = GetModulesForSubscription(company.SubscriptionLevel ?? "Standard");
        foreach (var module in modules)
        {
            claims.Add(new Claim("module", module));
        }
        claims.Add(new Claim("modules", string.Join(",", modules)));

        // Access token (4 hours for mobile)
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(4),
            Issuer = _configuration["JwtSettings:Issuer"],
            Audience = _configuration["JwtSettings:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = tokenHandler.WriteToken(token);

        // Generate refresh token (30 days)
        var refreshToken = GenerateRefreshToken();
        await StoreRefreshTokenAsync(user.Id, refreshToken);

        var response = new AuthTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresIn = 14400, // 4 hours in seconds
            IssuedAt = DateTimeOffset.UtcNow,
            User = new AuthUserInfo
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Role = "User", // Default role for now
                IsActive = user.IsActive
            },
            Company = new AuthCompanyInfo
            {
                Id = company.Id,
                Code = company.Code,
                Name = company.Name,
                SubscriptionLevel = company.SubscriptionLevel ?? "Standard",
                IsActive = company.IsActive
            },
            Modules = modules
        };

        _logger.LogInformation("Generated JWT token for user {UserId} ({Email}) in company {CompanyId}", 
            user.Id, user.Email, company.Id);

        return response;
    }

    public async Task<AuthTokenResponse> RefreshTokenAsync(string refreshToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.Contexts.ApplicationDbContext>();

        // Find refresh token in database (you'll need to create RefreshToken entity)
        var storedToken = await dbContext.Set<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.ExpiresAt > DateTime.UtcNow && !rt.IsRevoked);

        if (storedToken == null)
        {
            throw new SecurityTokenException("Invalid or expired refresh token");
        }

        var user = await dbContext.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == storedToken.UserId);

        if (user?.Company == null)
        {
            throw new SecurityTokenException("User or company not found");
        }

        // Revoke old refresh token
        storedToken.IsRevoked = true;
        await dbContext.SaveChangesAsync();

        // Generate new tokens
        return await GenerateTokenAsync(user, user.Company);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured"));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidAudience = _configuration["JwtSettings:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JWT token validation failed");
            return null;
        }
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.Contexts.ApplicationDbContext>();

        var token = await dbContext.Set<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token != null)
        {
            token.IsRevoked = true;
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task RevokeAllUserTokensAsync(int userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.Contexts.ApplicationDbContext>();

        var tokens = await dbContext.Set<RefreshToken>()
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
        }

        await dbContext.SaveChangesAsync();
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private async Task StoreRefreshTokenAsync(int userId, string refreshToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.Contexts.ApplicationDbContext>();

        var token = new RefreshToken
        {
            UserId = userId,
            Token = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsRevoked = false
        };

        dbContext.Set<RefreshToken>().Add(token);
        await dbContext.SaveChangesAsync();
    }

    private string[] GetModulesForSubscription(string subscriptionLevel)
    {
        // Define modules based on subscription level
        return subscriptionLevel.ToLower() switch
        {
            "premium" => new[] { "Estimate", "Trace", "Fabmate", "QDocs" },
            "professional" => new[] { "Estimate", "Trace", "Fabmate" },
            "standard" => new[] { "Estimate", "Trace" },
            "basic" => new[] { "Estimate" },
            _ => new[] { "Estimate" }
        };
    }
}

/// <summary>
/// Response model for authentication token generation
/// </summary>
public class AuthTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public AuthUserInfo User { get; set; } = new();
    public AuthCompanyInfo Company { get; set; } = new();
    public string[] Modules { get; set; } = Array.Empty<string>();
}

public class AuthUserInfo
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class AuthCompanyInfo
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SubscriptionLevel { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
