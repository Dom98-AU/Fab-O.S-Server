using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FabOS.WebServer.Controllers.Api;

/// <summary>
/// API controller for managing catalogues and catalogue items
/// Supports multi-tenant catalogues with system (Database Catalogue) and custom catalogues
/// </summary>
[Authorize]
[ApiController]
[Route("api/catalogues")]
public class CatalogueController : ControllerBase
{
    private readonly ICatalogueService _catalogueService;
    private readonly ILogger<CatalogueController> _logger;

    public CatalogueController(
        ICatalogueService catalogueService,
        ILogger<CatalogueController> logger)
    {
        _catalogueService = catalogueService;
        _logger = logger;
    }

    /// <summary>
    /// Extract authenticated user context (userId and companyId) from claims
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

    // ====================
    // CATALOGUE ENDPOINTS
    // ====================

    /// <summary>
    /// Get all catalogues for the current company
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Catalogue>>> GetCatalogues()
    {
        try
        {
            var (userId, companyId) = GetUserContext();
            var catalogues = await _catalogueService.GetCataloguesAsync(companyId);
            return Ok(catalogues);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving catalogues");
            return StatusCode(500, new { error = "Failed to retrieve catalogues" });
        }
    }

    /// <summary>
    /// Get a specific catalogue by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Catalogue>> GetCatalogue(int id)
    {
        try
        {
            var (userId, companyId) = GetUserContext();
            var catalogue = await _catalogueService.GetCatalogueByIdAsync(id, companyId);

            if (catalogue == null)
            {
                return NotFound(new { error = $"Catalogue {id} not found" });
            }

            return Ok(catalogue);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving catalogue {Id}", id);
            return StatusCode(500, new { error = "Failed to retrieve catalogue" });
        }
    }

    /// <summary>
    /// Get the system (Database) catalogue for the current company
    /// </summary>
    [HttpGet("system")]
    public async Task<ActionResult<Catalogue>> GetSystemCatalogue()
    {
        try
        {
            var (userId, companyId) = GetUserContext();
            var catalogue = await _catalogueService.GetSystemCatalogueAsync(companyId);

            if (catalogue == null)
            {
                return NotFound(new { error = "System catalogue not found" });
            }

            return Ok(catalogue);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system catalogue");
            return StatusCode(500, new { error = "Failed to retrieve system catalogue" });
        }
    }

    /// <summary>
    /// Create a new custom catalogue
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Catalogue>> CreateCatalogue([FromBody] CreateCatalogueRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "Catalogue name is required" });
            }

            var (userId, companyId) = GetUserContext();
            var catalogue = await _catalogueService.CreateCatalogueAsync(request.Name, request.Description, companyId, userId);

            return CreatedAtAction(nameof(GetCatalogue), new { id = catalogue.Id }, catalogue);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating catalogue");
            return StatusCode(500, new { error = "Failed to create catalogue" });
        }
    }

    /// <summary>
    /// Update an existing catalogue
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<Catalogue>> UpdateCatalogue(int id, [FromBody] UpdateCatalogueRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "Catalogue name is required" });
            }

            var (userId, companyId) = GetUserContext();
            var catalogue = await _catalogueService.UpdateCatalogueAsync(id, request.Name, request.Description, companyId, userId);

            if (catalogue == null)
            {
                return NotFound(new { error = $"Catalogue {id} not found" });
            }

            return Ok(catalogue);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating catalogue {Id}", id);
            return StatusCode(500, new { error = "Failed to update catalogue" });
        }
    }

    /// <summary>
    /// Delete a catalogue (only if it's not a system catalogue and has no items)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCatalogue(int id)
    {
        try
        {
            var (userId, companyId) = GetUserContext();
            var success = await _catalogueService.DeleteCatalogueAsync(id, companyId, userId);

            if (!success)
            {
                return NotFound(new { error = $"Catalogue {id} not found" });
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting catalogue {Id}", id);
            return StatusCode(500, new { error = "Failed to delete catalogue" });
        }
    }

    /// <summary>
    /// Duplicate a catalogue
    /// </summary>
    [HttpPost("{id}/duplicate")]
    public async Task<ActionResult<Catalogue>> DuplicateCatalogue(int id, [FromBody] DuplicateCatalogueRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.NewName))
            {
                return BadRequest(new { error = "New catalogue name is required" });
            }

            var (userId, companyId) = GetUserContext();
            var catalogue = await _catalogueService.DuplicateCatalogueAsync(id, request.NewName, companyId, userId);

            return CreatedAtAction(nameof(GetCatalogue), new { id = catalogue.Id }, catalogue);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating catalogue {Id}", id);
            return StatusCode(500, new { error = "Failed to duplicate catalogue" });
        }
    }

    /// <summary>
    /// Get catalogue statistics
    /// </summary>
    [HttpGet("{id}/statistics")]
    public async Task<ActionResult> GetCatalogueStatistics(int id)
    {
        try
        {
            var (userId, companyId) = GetUserContext();
            var itemCount = await _catalogueService.GetCatalogueItemCountAsync(id, companyId);
            var canModify = await _catalogueService.CanModifyCatalogueAsync(id, companyId);

            return Ok(new
            {
                ItemCount = itemCount,
                CanModify = canModify
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving catalogue statistics for {Id}", id);
            return StatusCode(500, new { error = "Failed to retrieve statistics" });
        }
    }

    // ===========================
    // CATALOGUE ITEM ENDPOINTS
    // ===========================

    /// <summary>
    /// Get all items in a catalogue
    /// </summary>
    [HttpGet("{catalogueId}/items")]
    public async Task<ActionResult<List<CatalogueItem>>> GetCatalogueItems(int catalogueId, [FromQuery] string? category = null)
    {
        try
        {
            var (userId, companyId) = GetUserContext();

            List<CatalogueItem> items;
            if (!string.IsNullOrWhiteSpace(category))
            {
                items = await _catalogueService.GetItemsByCatalogueAndCategoryAsync(catalogueId, category, companyId);
            }
            else
            {
                items = await _catalogueService.GetItemsByCatalogueAsync(catalogueId, companyId);
            }

            return Ok(items);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving items for catalogue {CatalogueId}", catalogueId);
            return StatusCode(500, new { error = "Failed to retrieve catalogue items" });
        }
    }

    /// <summary>
    /// Get a specific catalogue item
    /// </summary>
    [HttpGet("items/{itemId}")]
    public async Task<ActionResult<CatalogueItem>> GetCatalogueItem(int itemId)
    {
        try
        {
            var (userId, companyId) = GetUserContext();
            var item = await _catalogueService.GetCatalogueItemByIdAsync(itemId, companyId);

            if (item == null)
            {
                return NotFound(new { error = $"Catalogue item {itemId} not found" });
            }

            return Ok(item);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving catalogue item {ItemId}", itemId);
            return StatusCode(500, new { error = "Failed to retrieve catalogue item" });
        }
    }

    /// <summary>
    /// Create a new catalogue item
    /// </summary>
    [HttpPost("{catalogueId}/items")]
    public async Task<ActionResult<CatalogueItem>> CreateCatalogueItem(int catalogueId, [FromBody] CatalogueItem item)
    {
        try
        {
            var (userId, companyId) = GetUserContext();
            var createdItem = await _catalogueService.CreateCatalogueItemAsync(catalogueId, item, companyId);

            return CreatedAtAction(nameof(GetCatalogueItem), new { itemId = createdItem.Id }, createdItem);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating catalogue item in catalogue {CatalogueId}", catalogueId);
            return StatusCode(500, new { error = "Failed to create catalogue item" });
        }
    }

    /// <summary>
    /// Update a catalogue item
    /// </summary>
    [HttpPut("items/{itemId}")]
    public async Task<ActionResult<CatalogueItem>> UpdateCatalogueItem(int itemId, [FromBody] CatalogueItem item)
    {
        try
        {
            var (userId, companyId) = GetUserContext();
            var updatedItem = await _catalogueService.UpdateCatalogueItemAsync(itemId, item, companyId);

            if (updatedItem == null)
            {
                return NotFound(new { error = $"Catalogue item {itemId} not found" });
            }

            return Ok(updatedItem);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating catalogue item {ItemId}", itemId);
            return StatusCode(500, new { error = "Failed to update catalogue item" });
        }
    }

    /// <summary>
    /// Delete a catalogue item
    /// </summary>
    [HttpDelete("items/{itemId}")]
    public async Task<ActionResult> DeleteCatalogueItem(int itemId)
    {
        try
        {
            var (userId, companyId) = GetUserContext();
            var success = await _catalogueService.DeleteCatalogueItemAsync(itemId, companyId);

            if (!success)
            {
                return NotFound(new { error = $"Catalogue item {itemId} not found" });
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting catalogue item {ItemId}", itemId);
            return StatusCode(500, new { error = "Failed to delete catalogue item" });
        }
    }

    /// <summary>
    /// Search catalogue items
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<CatalogueItem>>> SearchItems([FromQuery] string q, [FromQuery] int? catalogueId = null, [FromQuery] int maxResults = 50)
    {
        try
        {
            var (userId, companyId) = GetUserContext();
            var items = await _catalogueService.SearchCatalogueItemsAsync(q, companyId, catalogueId, maxResults);

            return Ok(items);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching catalogue items");
            return StatusCode(500, new { error = "Failed to search catalogue items" });
        }
    }

    /// <summary>
    /// Get all categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories([FromQuery] int? catalogueId = null)
    {
        try
        {
            var (userId, companyId) = GetUserContext();
            var categories = await _catalogueService.GetAllCategoriesAsync(companyId, catalogueId);

            return Ok(categories);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories");
            return StatusCode(500, new { error = "Failed to retrieve categories" });
        }
    }

    /// <summary>
    /// Get all materials
    /// </summary>
    [HttpGet("materials")]
    public async Task<ActionResult<List<string>>> GetMaterials([FromQuery] int? catalogueId = null)
    {
        try
        {
            var (userId, companyId) = GetUserContext();
            var materials = await _catalogueService.GetAllMaterialsAsync(companyId, catalogueId);

            return Ok(materials);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving materials");
            return StatusCode(500, new { error = "Failed to retrieve materials" });
        }
    }
}

// Request DTOs
public record CreateCatalogueRequest(string Name, string? Description);
public record UpdateCatalogueRequest(string Name, string? Description);
public record DuplicateCatalogueRequest(string NewName);
