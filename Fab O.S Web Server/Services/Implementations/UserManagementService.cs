using FabOS.WebServer.Authentication;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Services.Implementations;

/// <summary>
/// Service for managing user accounts with multi-tenant support
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<UserManagementService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users
            .Include(u => u.Company)
            .OrderBy(u => u.Username)
            .ToListAsync();
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User> CreateUserAsync(User user, string password)
    {
        // Validation
        if (user.CompanyId <= 0)
        {
            throw new ArgumentException("CompanyId is required - every user must belong to a company");
        }

        if (string.IsNullOrWhiteSpace(user.Username))
        {
            throw new ArgumentException("Username is required");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new ArgumentException("Email is required");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required");
        }

        // Check if company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == user.CompanyId && c.IsActive);
        if (!companyExists)
        {
            throw new InvalidOperationException($"Company with ID {user.CompanyId} does not exist or is inactive");
        }

        // Check for duplicate username
        if (await UsernameExistsAsync(user.Username))
        {
            throw new InvalidOperationException($"Username '{user.Username}' already exists");
        }

        // Check for duplicate email
        if (await EmailExistsAsync(user.Email))
        {
            throw new InvalidOperationException($"Email '{user.Email}' already exists");
        }

        try
        {
            // Hash password
            user.PasswordHash = _passwordHasher.HashPassword(password);
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.CreatedDate = DateTime.UtcNow;
            user.LastModified = DateTime.UtcNow;
            user.IsEmailConfirmed = false;
            user.FailedLoginAttempts = 0;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User created successfully: {Username} (ID: {UserId}) assigned to Company {CompanyId}",
                user.Username, user.Id, user.CompanyId);

            // Reload with company info
            return (await GetUserByIdAsync(user.Id))!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Username}", user.Username);
            throw;
        }
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        // Validation
        if (user.CompanyId <= 0)
        {
            throw new ArgumentException("CompanyId is required - every user must belong to a company");
        }

        var existing = await _context.Users.FindAsync(user.Id);
        if (existing == null)
        {
            throw new InvalidOperationException($"User with ID {user.Id} not found");
        }

        // Check if company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == user.CompanyId && c.IsActive);
        if (!companyExists)
        {
            throw new InvalidOperationException($"Company with ID {user.CompanyId} does not exist or is inactive");
        }

        // Check for duplicate username (excluding current user)
        if (await UsernameExistsAsync(user.Username, user.Id))
        {
            throw new InvalidOperationException($"Username '{user.Username}' already exists");
        }

        // Check for duplicate email (excluding current user)
        if (await EmailExistsAsync(user.Email, user.Id))
        {
            throw new InvalidOperationException($"Email '{user.Email}' already exists");
        }

        try
        {
            // Update fields (preserve PasswordHash and SecurityStamp)
            existing.Username = user.Username;
            existing.Email = user.Email;
            existing.FirstName = user.FirstName;
            existing.LastName = user.LastName;
            existing.CompanyId = user.CompanyId;
            existing.JobTitle = user.JobTitle;
            existing.PhoneNumber = user.PhoneNumber;
            existing.IsActive = user.IsActive;
            existing.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User updated successfully: {Username} (ID: {UserId}), Company: {CompanyId}",
                existing.Username, existing.Id, existing.CompanyId);

            // Reload with company info
            return (await GetUserByIdAsync(existing.Id))!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", user.Id);
            throw;
        }
    }

    public async Task DeleteUserAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        try
        {
            // Soft delete - set IsActive to false
            user.IsActive = false;
            user.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User soft-deleted successfully: {Username} (ID: {UserId})",
                user.Username, user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            throw;
        }
    }

    public async Task ChangePasswordAsync(int userId, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new ArgumentException("Password cannot be empty");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        try
        {
            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            user.SecurityStamp = Guid.NewGuid().ToString(); // Invalidate existing tokens
            user.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Password changed successfully for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null)
    {
        var query = _context.Users.Where(u => u.Username.ToLower() == username.ToLower());

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
    {
        var query = _context.Users.Where(u => u.Email.ToLower() == email.ToLower());

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return await query.AnyAsync();
    }
}
