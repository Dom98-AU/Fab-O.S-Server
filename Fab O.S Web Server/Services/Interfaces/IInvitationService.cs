using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models.DTOs.Signup;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service for managing user invitations
/// </summary>
public interface IInvitationService
{
    /// <summary>
    /// Creates a new invitation and sends email to the invited user
    /// </summary>
    Task<UserInvitation> CreateInvitationAsync(string email, int invitedByUserId, int companyId, InviteAuthMethod authMethod);

    /// <summary>
    /// Validates an invitation token and returns the invitation if valid
    /// </summary>
    Task<UserInvitation?> ValidateInvitationTokenAsync(string token);

    /// <summary>
    /// Accepts an invitation by creating a user account and marking the invitation as accepted
    /// </summary>
    Task<bool> AcceptInvitationAsync(InvitedSignupRequest request);

    /// <summary>
    /// Gets all pending invitations for a specific company
    /// </summary>
    Task<List<UserInvitation>> GetPendingInvitationsForCompanyAsync(int companyId);

    /// <summary>
    /// Revokes (cancels) an invitation
    /// </summary>
    Task<bool> RevokeInvitationAsync(int invitationId);

    /// <summary>
    /// Resends the invitation email
    /// </summary>
    Task<bool> ResendInvitationAsync(int invitationId);

    /// <summary>
    /// Validates if an email has a pending invitation
    /// </summary>
    Task<UserInvitation?> ValidateByEmailAsync(string email);
}
