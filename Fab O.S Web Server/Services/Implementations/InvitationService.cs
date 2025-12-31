using FabOS.WebServer.Authentication;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.DTOs.Signup;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Services.Implementations;

/// <summary>
/// Service for managing user invitations with email/password and Microsoft auth support
/// </summary>
public class InvitationService : IInvitationService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICookieAuthenticationService _cookieAuthService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InvitationService> _logger;

    public InvitationService(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ICookieAuthenticationService cookieAuthService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<InvitationService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _cookieAuthService = cookieAuthService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<UserInvitation> CreateInvitationAsync(
        string email,
        int invitedByUserId,
        int companyId,
        InviteAuthMethod authMethod)
    {
        try
        {
            // Check if invitation already exists for this email and company
            var existingInvitation = await _context.UserInvitations
                .FirstOrDefaultAsync(i => i.Email.ToLower() == email.ToLower()
                    && i.CompanyId == companyId
                    && i.Status == InvitationStatus.Pending);

            if (existingInvitation != null)
            {
                // Update existing invitation
                existingInvitation.Token = Guid.NewGuid().ToString();
                existingInvitation.CreatedAt = DateTime.UtcNow;
                existingInvitation.ExpiresAt = DateTime.UtcNow.AddDays(7);
                existingInvitation.AuthMethod = authMethod;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated existing invitation for {Email} to company {CompanyId}",
                    email, companyId);

                // Send invitation email
                await SendInvitationEmailAsync(existingInvitation);

                return existingInvitation;
            }

            // Create new invitation
            var invitation = new UserInvitation
            {
                Email = email,
                InvitedByUserId = invitedByUserId,
                CompanyId = companyId,
                Token = Guid.NewGuid().ToString(),
                Status = InvitationStatus.Pending,
                AuthMethod = authMethod,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7) // 7 days expiration
            };

            _context.UserInvitations.Add(invitation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created invitation for {Email} to company {CompanyId}",
                email, companyId);

            // Send invitation email
            await SendInvitationEmailAsync(invitation);

            return invitation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invitation for {Email}", email);
            throw;
        }
    }

    public async Task<UserInvitation?> ValidateInvitationTokenAsync(string token)
    {
        try
        {
            var invitation = await _context.UserInvitations
                .Include(i => i.Company)
                .Include(i => i.InvitedBy)
                .FirstOrDefaultAsync(i => i.Token == token);

            if (invitation == null)
            {
                _logger.LogWarning("Invitation token not found: {Token}", token);
                return null;
            }

            // Check if expired
            if (invitation.ExpiresAt < DateTime.UtcNow)
            {
                invitation.Status = InvitationStatus.Expired;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Invitation {InvitationId} expired", invitation.Id);
                return null;
            }

            // Check if already accepted
            if (invitation.Status != InvitationStatus.Pending)
            {
                _logger.LogWarning("Invitation {InvitationId} is not pending (status: {Status})",
                    invitation.Id, invitation.Status);
                return null;
            }

            return invitation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating invitation token");
            return null;
        }
    }

    public async Task<bool> AcceptInvitationAsync(InvitedSignupRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Validate invitation
            var invitation = await ValidateInvitationTokenAsync(request.Token);
            if (invitation == null)
            {
                _logger.LogWarning("Cannot accept invalid invitation token: {Token}", request.Token);
                return false;
            }

            // Check if user already exists with this email
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (existingUser != null)
            {
                _logger.LogWarning("User already exists with email {Email}", request.Email);
                return false;
            }

            // Create user account
            var user = new User
            {
                Username = request.Email.Split('@')[0],
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CompanyId = invitation.CompanyId,
                IsActive = true,
                IsEmailConfirmed = true, // Auto-confirm since they were invited
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                FailedLoginAttempts = 0,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            // Set password if using email/password auth
            if (request.AuthMethod == "EmailPassword" && !string.IsNullOrEmpty(request.Password))
            {
                user.PasswordHash = _passwordHasher.HashPassword(request.Password);
            }

            _context.Users.Add(user);

            // Mark invitation as accepted
            invitation.Status = InvitationStatus.Accepted;
            invitation.AcceptedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("User {Email} accepted invitation and joined company {CompanyId}",
                request.Email, invitation.CompanyId);

            // Auto-login the user if using email/password
            if (request.AuthMethod == "EmailPassword")
            {
                await _cookieAuthService.SignInAsync(user, invitation.Company!, rememberMe: false);
            }

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error accepting invitation for {Email}", request.Email);
            return false;
        }
    }

    public async Task<List<UserInvitation>> GetPendingInvitationsForCompanyAsync(int companyId)
    {
        try
        {
            return await _context.UserInvitations
                .Include(i => i.InvitedBy)
                .Where(i => i.CompanyId == companyId
                    && i.Status == InvitationStatus.Pending
                    && i.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending invitations for company {CompanyId}", companyId);
            return new List<UserInvitation>();
        }
    }

    public async Task<bool> RevokeInvitationAsync(int invitationId)
    {
        try
        {
            var invitation = await _context.UserInvitations.FindAsync(invitationId);
            if (invitation == null)
            {
                return false;
            }

            invitation.Status = InvitationStatus.Revoked;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Revoked invitation {InvitationId}", invitationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking invitation {InvitationId}", invitationId);
            return false;
        }
    }

    public async Task<bool> ResendInvitationAsync(int invitationId)
    {
        try
        {
            var invitation = await _context.UserInvitations
                .Include(i => i.Company)
                .Include(i => i.InvitedBy)
                .FirstOrDefaultAsync(i => i.Id == invitationId);

            if (invitation == null || invitation.Status != InvitationStatus.Pending)
            {
                return false;
            }

            // Extend expiration and regenerate token
            invitation.Token = Guid.NewGuid().ToString();
            invitation.ExpiresAt = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();

            // Resend email
            await SendInvitationEmailAsync(invitation);

            _logger.LogInformation("Resent invitation {InvitationId}", invitationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending invitation {InvitationId}", invitationId);
            return false;
        }
    }

    public async Task<UserInvitation?> ValidateByEmailAsync(string email)
    {
        try
        {
            return await _context.UserInvitations
                .Include(i => i.Company)
                .FirstOrDefaultAsync(i => i.Email.ToLower() == email.ToLower()
                    && i.Status == InvitationStatus.Pending
                    && i.ExpiresAt > DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating invitation by email {Email}", email);
            return null;
        }
    }

    private async Task SendInvitationEmailAsync(UserInvitation invitation)
    {
        try
        {
            // Load related entities if not already loaded
            if (invitation.InvitedBy == null)
            {
                await _context.Entry(invitation).Reference(i => i.InvitedBy).LoadAsync();
            }
            if (invitation.Company == null)
            {
                await _context.Entry(invitation).Reference(i => i.Company).LoadAsync();
            }

            var invitedByName = invitation.InvitedBy != null
                ? $"{invitation.InvitedBy.FirstName} {invitation.InvitedBy.LastName}"
                : "Your colleague";

            var companyName = invitation.Company?.Name ?? "the company";

            await _emailService.SendInvitationEmailAsync(
                invitation,
                invitedByName,
                companyName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invitation email for invitation {InvitationId}", invitation.Id);
            // Don't throw - invitation was created, email is optional
        }
    }
}
