using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations;

/// <summary>
/// Email service for sending invitations and welcome emails
/// TODO: Integrate with SendGrid/SMTP/Azure Communication Services
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendInvitationEmailAsync(
        UserInvitation invitation,
        string invitedByName,
        string companyName)
    {
        try
        {
            var baseUrl = _configuration["SignupSettings:BaseUrl"] ?? "https://localhost:5223";
            var inviteUrl = $"{baseUrl}/signup/invite/{invitation.Token}";

            // TODO: Replace with actual email sending logic (SendGrid, SMTP, etc.)
            _logger.LogInformation(
                @"
===========================================
INVITATION EMAIL (Development Mode)
===========================================
To: {Email}
Subject: You've been invited to join {CompanyName} on Fab.OS

Hi,

{InvitedByName} has invited you to join {CompanyName} on Fab.OS Platform.

Click the link below to accept the invitation:
{InviteUrl}

This invitation will expire on {ExpiresAt}.

---
Fab.OS Platform
===========================================
                ",
                invitation.Email,
                companyName,
                invitedByName,
                inviteUrl,
                invitation.ExpiresAt.ToString("MMMM dd, yyyy"));

            // Simulate email sending delay
            await Task.Delay(100);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invitation email to {Email}", invitation.Email);
            return false;
        }
    }

    public async Task<bool> SendWelcomeEmailAsync(
        string email,
        string firstName,
        string companyName,
        string tenantSlug)
    {
        try
        {
            var baseUrl = _configuration["SignupSettings:BaseUrl"] ?? "https://localhost:5223";
            var workspaceUrl = $"{baseUrl}/{tenantSlug}/home";

            // TODO: Replace with actual email sending logic
            _logger.LogInformation(
                @"
===========================================
WELCOME EMAIL (Development Mode)
===========================================
To: {Email}
Subject: Welcome to Fab.OS Platform!

Hi {FirstName},

Welcome to Fab.OS! Your workspace for {CompanyName} has been created successfully.

Get started by visiting your workspace:
{WorkspaceUrl}

Your workspace URL: {TenantSlug}.fab-os.com

Need help? Check out our documentation or contact support.

---
Fab.OS Platform Team
===========================================
                ",
                email,
                firstName,
                companyName,
                workspaceUrl,
                tenantSlug);

            // Simulate email sending delay
            await Task.Delay(100);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending welcome email to {Email}", email);
            return false;
        }
    }
}
