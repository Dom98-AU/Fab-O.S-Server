using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service for managing user accounts, roles, and company assignments
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Get all users with their company information
    /// </summary>
    Task<List<User>> GetAllUsersAsync();

    /// <summary>
    /// Get user by ID with company information
    /// </summary>
    Task<User?> GetUserByIdAsync(int userId);

    /// <summary>
    /// Create a new user with required company assignment
    /// </summary>
    /// <param name="user">User entity (must have CompanyId set)</param>
    /// <param name="password">Plain text password (will be hashed)</param>
    Task<User> CreateUserAsync(User user, string password);

    /// <summary>
    /// Update existing user information
    /// </summary>
    Task<User> UpdateUserAsync(User user);

    /// <summary>
    /// Delete user (soft delete - set IsActive = false)
    /// </summary>
    Task DeleteUserAsync(int userId);

    /// <summary>
    /// Change user password
    /// </summary>
    Task ChangePasswordAsync(int userId, string newPassword);

    /// <summary>
    /// Check if username already exists
    /// </summary>
    Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null);

    /// <summary>
    /// Check if email already exists
    /// </summary>
    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);
}
