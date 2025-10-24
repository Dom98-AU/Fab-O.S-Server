using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an invitation email to a user
    /// </summary>
    Task<bool> SendInvitationEmailAsync(UserInvitation invitation, string invitedByName, string companyName);

    /// <summary>
    /// Sends a welcome email to a new company admin after signup
    /// </summary>
    Task<bool> SendWelcomeEmailAsync(string email, string firstName, string companyName, string tenantSlug);
}
