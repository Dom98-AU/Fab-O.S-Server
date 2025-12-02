using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Controllers.Api
{
    /// <summary>
    /// API controller for Nutrient DWS authentication
    /// Provides session tokens for browser-based PDF viewing
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NutrientAuthController : ControllerBase
    {
        private readonly INutrientDwsSessionService _dwsSessionService;
        private readonly ILogger<NutrientAuthController> _logger;

        public NutrientAuthController(
            INutrientDwsSessionService dwsSessionService,
            ILogger<NutrientAuthController> logger)
        {
            _dwsSessionService = dwsSessionService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a Nutrient DWS session token for a PDF document
        /// Called by browser before loading PDF viewer
        /// </summary>
        /// <param name="request">Document URL and optional user ID</param>
        /// <returns>Session token (JWT) for browser authentication</returns>
        [HttpPost("session")]
        public async Task<ActionResult<SessionTokenResponse>> CreateSessionToken([FromBody] CreateSessionTokenRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.DocumentUrl))
                {
                    return BadRequest(new { error = "DocumentUrl is required" });
                }

                _logger.LogInformation("[NutrientAuth] Creating session token for document: {DocumentUrl}", request.DocumentUrl);

                var sessionToken = await _dwsSessionService.CreateSessionTokenAsync(
                    request.DocumentUrl,
                    request.UserId);

                return Ok(new SessionTokenResponse
                {
                    SessionToken = sessionToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[NutrientAuth] Failed to create session token for {DocumentUrl}", request.DocumentUrl);
                return StatusCode(500, new { error = "Failed to create session token", details = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request model for session token creation
    /// </summary>
    public class CreateSessionTokenRequest
    {
        public string DocumentUrl { get; set; } = string.Empty;
        public string? UserId { get; set; }
    }

    /// <summary>
    /// Response model for session token creation
    /// </summary>
    public class SessionTokenResponse
    {
        public string SessionToken { get; set; } = string.Empty;
    }
}
