using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FabOS.WebServer.Controllers.Api;

/// <summary>
/// API controller for managing user invitations (admin only)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires authentication
public class InvitationController : ControllerBase
{
    private readonly IInvitationService _invitationService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<InvitationController> _logger;

    public InvitationController(
        IInvitationService invitationService,
        ITenantService tenantService,
        ILogger<InvitationController> logger)
    {
        _invitationService = invitationService;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Sends a new invitation to a user
    /// </summary>
    [HttpPost("send")]
    public async Task<ActionResult> SendInvitation([FromBody] SendInvitationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid request data", errors = ModelState });
            }

            var currentUserId = _tenantService.GetCurrentUserId();
            var currentCompanyId = _tenantService.GetCurrentCompanyId();

            if (currentUserId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            // Parse auth method
            if (!Enum.TryParse<InviteAuthMethod>(request.AuthMethod, out var authMethod))
            {
                authMethod = InviteAuthMethod.Both;
            }

            var invitation = await _invitationService.CreateInvitationAsync(
                request.Email,
                currentUserId.Value,
                currentCompanyId,
                authMethod);

            _logger.LogInformation("User {UserId} sent invitation to {Email}",
                currentUserId, request.Email);

            return Ok(new
            {
                success = true,
                message = "Invitation sent successfully",
                invitationId = invitation.Id,
                expiresAt = invitation.ExpiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invitation to {Email}", request.Email);
            return StatusCode(500, new { message = "Failed to send invitation" });
        }
    }

    /// <summary>
    /// Gets all pending invitations for the current company
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult> GetPendingInvitations()
    {
        try
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            var invitations = await _invitationService.GetPendingInvitationsForCompanyAsync(companyId);

            var result = invitations.Select(i => new
            {
                id = i.Id,
                email = i.Email,
                invitedBy = i.InvitedBy != null
                    ? $"{i.InvitedBy.FirstName} {i.InvitedBy.LastName}"
                    : "Unknown",
                authMethod = i.AuthMethod.ToString(),
                createdAt = i.CreatedAt,
                expiresAt = i.ExpiresAt,
                status = i.Status.ToString()
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending invitations");
            return StatusCode(500, new { message = "Failed to get invitations" });
        }
    }

    /// <summary>
    /// Revokes (cancels) an invitation
    /// </summary>
    [HttpPost("revoke/{id}")]
    public async Task<ActionResult> RevokeInvitation(int id)
    {
        try
        {
            var success = await _invitationService.RevokeInvitationAsync(id);

            if (!success)
            {
                return NotFound(new { message = "Invitation not found" });
            }

            _logger.LogInformation("Invitation {InvitationId} revoked", id);

            return Ok(new { success = true, message = "Invitation revoked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking invitation {InvitationId}", id);
            return StatusCode(500, new { message = "Failed to revoke invitation" });
        }
    }

    /// <summary>
    /// Resends an invitation email
    /// </summary>
    [HttpPost("resend/{id}")]
    public async Task<ActionResult> ResendInvitation(int id)
    {
        try
        {
            var success = await _invitationService.ResendInvitationAsync(id);

            if (!success)
            {
                return NotFound(new { message = "Invitation not found or already accepted" });
            }

            _logger.LogInformation("Invitation {InvitationId} resent", id);

            return Ok(new { success = true, message = "Invitation resent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending invitation {InvitationId}", id);
            return StatusCode(500, new { message = "Failed to resend invitation" });
        }
    }
}

/// <summary>
/// Request model for sending invitations
/// </summary>
public class SendInvitationRequest
{
    public string Email { get; set; } = string.Empty;
    public string AuthMethod { get; set; } = "Both"; // EmailPassword, MicrosoftEntraId, Both
}
