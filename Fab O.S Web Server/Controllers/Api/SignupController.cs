using FabOS.WebServer.Models.Dto.Signup;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FabOS.WebServer.Controllers.Api;

/// <summary>
/// API controller for company signup and invitation acceptance
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SignupController : ControllerBase
{
    private readonly ISignupValidationService _validationService;
    private readonly ITenantProvisioningService _provisioningService;
    private readonly IInvitationService _invitationService;
    private readonly ILogger<SignupController> _logger;

    public SignupController(
        ISignupValidationService validationService,
        ITenantProvisioningService provisioningService,
        IInvitationService invitationService,
        ILogger<SignupController> logger)
    {
        _validationService = validationService;
        _provisioningService = provisioningService;
        _invitationService = invitationService;
        _logger = logger;
    }

    /// <summary>
    /// Validates a signup request for conflicts (email exists, code taken, domain exists)
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<ActionResult<SignupValidationResult>> ValidateSignup([FromBody] SignupRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid request data", errors = ModelState });
            }

            var result = await _validationService.ValidateSignupAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating signup for {Email}", request.Email);
            return StatusCode(500, new { message = "Validation error occurred" });
        }
    }

    /// <summary>
    /// Creates a new tenant (company + admin user)
    /// </summary>
    [HttpPost("create")]
    [AllowAnonymous]
    public async Task<ActionResult<TenantCreationResult>> CreateTenant([FromBody] SignupRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid request data", errors = ModelState });
            }

            // Always validate before creating (unless force flag is set)
            if (!request.ForceCreateSeparate)
            {
                var validation = await _validationService.ValidateSignupAsync(request);
                if (!validation.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Validation failed",
                        validation = validation
                    });
                }
            }

            // Proceed with tenant creation
            var result = await _provisioningService.CreateTenantAsync(request);

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            _logger.LogInformation("Tenant created successfully: {TenantId} for {Email}",
                result.TenantId, request.Email);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant for {Email}", request.Email);
            return StatusCode(500, new { message = "Tenant creation failed" });
        }
    }

    /// <summary>
    /// Gets company code suggestions when the requested code is taken
    /// </summary>
    [HttpGet("suggestions/{companyCode}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<string>>> GetCompanyCodeSuggestions(string companyCode)
    {
        try
        {
            var suggestions = await _validationService.GenerateCompanyCodeSuggestionsAsync(companyCode);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating suggestions for {CompanyCode}", companyCode);
            return StatusCode(500, new { message = "Failed to generate suggestions" });
        }
    }

    /// <summary>
    /// Validates an invitation token and returns invitation details
    /// </summary>
    [HttpGet("invite/{token}")]
    [AllowAnonymous]
    public async Task<ActionResult> ValidateInvitationToken(string token)
    {
        try
        {
            var invitation = await _invitationService.ValidateInvitationTokenAsync(token);

            if (invitation == null)
            {
                return NotFound(new { message = "Invalid or expired invitation token" });
            }

            return Ok(new
            {
                email = invitation.Email,
                companyName = invitation.Company?.Name,
                companyCode = invitation.Company?.Code,
                invitedBy = invitation.InvitedBy != null
                    ? $"{invitation.InvitedBy.FirstName} {invitation.InvitedBy.LastName}"
                    : "Your colleague",
                authMethod = invitation.AuthMethod.ToString(),
                expiresAt = invitation.ExpiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating invitation token");
            return StatusCode(500, new { message = "Error validating invitation" });
        }
    }

    /// <summary>
    /// Accepts an invitation and creates a user account
    /// </summary>
    [HttpPost("accept-invite")]
    [AllowAnonymous]
    public async Task<ActionResult> AcceptInvitation([FromBody] InvitedSignupRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid request data", errors = ModelState });
            }

            var success = await _invitationService.AcceptInvitationAsync(request);

            if (!success)
            {
                return BadRequest(new { message = "Failed to accept invitation" });
            }

            // Get the invitation to retrieve company code for redirect
            var invitation = await _invitationService.ValidateInvitationTokenAsync(request.Token);

            return Ok(new
            {
                success = true,
                message = "Invitation accepted successfully",
                redirectUrl = invitation?.Company != null
                    ? $"/{invitation.Company.Code}/home"
                    : "/home"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting invitation for {Email}", request.Email);
            return StatusCode(500, new { message = "Error accepting invitation" });
        }
    }
}
