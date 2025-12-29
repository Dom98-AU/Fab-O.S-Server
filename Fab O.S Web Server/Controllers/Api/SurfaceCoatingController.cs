using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FabOS.WebServer.Controllers.Api
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    [Route("api/surface-coatings")]
    public class SurfaceCoatingController : ControllerBase
    {
        private readonly ISurfaceCoatingService _coatingService;
        private readonly ILogger<SurfaceCoatingController> _logger;

        public SurfaceCoatingController(
            ISurfaceCoatingService coatingService,
            ILogger<SurfaceCoatingController> logger)
        {
            _coatingService = coatingService;
            _logger = logger;
        }

        /// <summary>
        /// Extract authenticated user context (userId and companyId) from claims
        /// Throws UnauthorizedAccessException if claims are missing
        /// </summary>
        private (int userId, int companyId) GetUserContext()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;

            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning("User ID claim not found or invalid in request");
                throw new UnauthorizedAccessException("User ID not found in authentication claims");
            }

            if (companyIdClaim == null || !int.TryParse(companyIdClaim, out int companyId))
            {
                _logger.LogWarning("Company ID claim not found or invalid for user {UserId}", userId);
                throw new UnauthorizedAccessException("Company ID not found in authentication claims");
            }

            return (userId, companyId);
        }

        /// <summary>
        /// Get all active surface coatings for the company
        /// GET /api/surface-coatings
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<SurfaceCoating>>> GetActiveCoatings()
        {
            try
            {
                var (userId, companyId) = GetUserContext();
                var coatings = await _coatingService.GetActiveCoatingsAsync(companyId);
                return Ok(coatings);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to GetActiveCoatings");
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving surface coatings");
                return StatusCode(500, new { error = "Failed to retrieve surface coatings" });
            }
        }

        /// <summary>
        /// Get a specific surface coating by ID
        /// GET /api/surface-coatings/5
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<SurfaceCoating>> GetCoatingById(int id)
        {
            try
            {
                var coating = await _coatingService.GetCoatingByIdAsync(id);

                if (coating == null)
                {
                    return NotFound(new { error = $"Surface coating {id} not found" });
                }

                return Ok(coating);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving surface coating {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve surface coating" });
            }
        }

        /// <summary>
        /// Create a new surface coating
        /// POST /api/surface-coatings
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SurfaceCoating>> CreateCoating([FromBody] CreateSurfaceCoatingRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { error = "Request body is required" });
                }

                var (userId, companyId) = GetUserContext();

                var coating = new SurfaceCoating
                {
                    CompanyId = companyId,
                    CoatingCode = request.CoatingCode,
                    CoatingName = request.CoatingName,
                    Description = request.Description,
                    IsActive = true,
                    DisplayOrder = request.DisplayOrder ?? 999
                };

                var created = await _coatingService.CreateCoatingAsync(coating);

                return CreatedAtAction(
                    nameof(GetCoatingById),
                    new { id = created.Id },
                    created);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to CreateCoating");
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating surface coating");
                return StatusCode(500, new { error = "Failed to create surface coating" });
            }
        }

        /// <summary>
        /// Update a surface coating
        /// PUT /api/surface-coatings/5
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<SurfaceCoating>> UpdateCoating(int id, [FromBody] UpdateSurfaceCoatingRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { error = "Request body is required" });
                }

                var existing = await _coatingService.GetCoatingByIdAsync(id);
                if (existing == null)
                {
                    return NotFound(new { error = $"Surface coating {id} not found" });
                }

                existing.CoatingCode = request.CoatingCode;
                existing.CoatingName = request.CoatingName;
                existing.Description = request.Description;
                existing.DisplayOrder = request.DisplayOrder ?? existing.DisplayOrder;

                var updated = await _coatingService.UpdateCoatingAsync(existing);

                return Ok(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating surface coating {Id}", id);
                return StatusCode(500, new { error = "Failed to update surface coating" });
            }
        }

        /// <summary>
        /// Delete a surface coating (soft delete)
        /// DELETE /api/surface-coatings/5
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCoating(int id)
        {
            try
            {
                var result = await _coatingService.DeleteCoatingAsync(id);

                if (!result)
                {
                    return NotFound(new { error = $"Surface coating {id} not found" });
                }

                return Ok(new { success = true, message = "Surface coating deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting surface coating {Id}", id);
                return StatusCode(500, new { error = "Failed to delete surface coating" });
            }
        }
    }

    // Request DTOs
    public class CreateSurfaceCoatingRequest
    {
        public string CoatingCode { get; set; } = string.Empty;
        public string CoatingName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? DisplayOrder { get; set; }
    }

    public class UpdateSurfaceCoatingRequest
    {
        public string CoatingCode { get; set; } = string.Empty;
        public string CoatingName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? DisplayOrder { get; set; }
    }
}
