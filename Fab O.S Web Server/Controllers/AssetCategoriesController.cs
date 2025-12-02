using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FabOS.WebServer.Models.DTOs.Assets;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Controllers;

/// <summary>
/// API controller for Equipment Categories and Types in the Asset module
/// </summary>
[ApiController]
[Route("api/assets/categories")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class AssetCategoriesController : ControllerBase
{
    private readonly IEquipmentCategoryService _categoryService;
    private readonly ILogger<AssetCategoriesController> _logger;

    public AssetCategoriesController(
        IEquipmentCategoryService categoryService,
        ILogger<AssetCategoriesController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    #region Categories

    /// <summary>
    /// Get all equipment categories
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<EquipmentCategoryDto>>> GetCategories(
        [FromQuery] int companyId,
        [FromQuery] bool activeOnly = false)
    {
        try
        {
            var categories = activeOnly
                ? await _categoryService.GetActiveCategoriesAsync(companyId)
                : await _categoryService.GetAllCategoriesAsync(companyId);

            var counts = await _categoryService.GetEquipmentCountsByCategoryAsync(companyId);

            var dtos = categories.Select(c => new EquipmentCategoryDto
            {
                Id = c.Id,
                CompanyId = c.CompanyId,
                Name = c.Name,
                Description = c.Description,
                IconClass = c.IconClass,
                DisplayOrder = c.DisplayOrder,
                IsSystemCategory = c.IsSystemCategory,
                IsActive = c.IsActive,
                EquipmentCount = counts.GetValueOrDefault(c.Id, 0),
                TypeCount = c.EquipmentTypes?.Count ?? 0,
                CreatedDate = c.CreatedDate,
                LastModified = c.LastModified
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories for company {CompanyId}", companyId);
            return StatusCode(500, new { message = "Error retrieving categories" });
        }
    }

    /// <summary>
    /// Get all categories with their types
    /// </summary>
    [HttpGet("with-types")]
    public async Task<ActionResult<List<EquipmentCategoryWithTypesDto>>> GetCategoriesWithTypes([FromQuery] int companyId)
    {
        try
        {
            var categories = await _categoryService.GetCategoriesWithTypesAsync(companyId);
            var counts = await _categoryService.GetEquipmentCountsByCategoryAsync(companyId);

            var dtos = categories.Select(c => new EquipmentCategoryWithTypesDto
            {
                Id = c.Id,
                CompanyId = c.CompanyId,
                Name = c.Name,
                Description = c.Description,
                IconClass = c.IconClass,
                DisplayOrder = c.DisplayOrder,
                IsSystemCategory = c.IsSystemCategory,
                IsActive = c.IsActive,
                EquipmentCount = counts.GetValueOrDefault(c.Id, 0),
                TypeCount = c.EquipmentTypes?.Count ?? 0,
                CreatedDate = c.CreatedDate,
                LastModified = c.LastModified,
                Types = c.EquipmentTypes?.Select(t => new EquipmentTypeDto
                {
                    Id = t.Id,
                    CategoryId = t.CategoryId,
                    CategoryName = c.Name,
                    Name = t.Name,
                    Description = t.Description,
                    DefaultMaintenanceIntervalDays = t.DefaultMaintenanceIntervalDays,
                    RequiredCertifications = t.RequiredCertifications,
                    DisplayOrder = t.DisplayOrder,
                    IsSystemType = t.IsSystemType,
                    IsActive = t.IsActive,
                    CreatedDate = t.CreatedDate,
                    LastModified = t.LastModified
                }).ToList() ?? new List<EquipmentTypeDto>()
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories with types");
            return StatusCode(500, new { message = "Error retrieving categories" });
        }
    }

    /// <summary>
    /// Get category by ID with types
    /// </summary>
    [HttpGet("{id:int}/with-types")]
    public async Task<ActionResult<EquipmentCategoryWithTypesDto>> GetCategoryWithTypes(int id)
    {
        return await GetCategory(id);
    }

    /// <summary>
    /// Get category by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EquipmentCategoryWithTypesDto>> GetCategory(int id)
    {
        try
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
                return NotFound(new { message = $"Category with ID {id} not found" });

            var equipmentCount = await _categoryService.GetEquipmentCountForCategoryAsync(id);
            var typeCount = await _categoryService.GetTypeCountForCategoryAsync(id);

            // Get types for this category
            var types = await _categoryService.GetTypesByCategoryAsync(id);
            var typeCounts = await _categoryService.GetEquipmentCountsByTypeAsync(category.CompanyId);

            return Ok(new EquipmentCategoryWithTypesDto
            {
                Id = category.Id,
                CompanyId = category.CompanyId,
                Name = category.Name,
                Description = category.Description,
                IconClass = category.IconClass,
                DisplayOrder = category.DisplayOrder,
                IsSystemCategory = category.IsSystemCategory,
                IsActive = category.IsActive,
                EquipmentCount = equipmentCount,
                TypeCount = typeCount,
                CreatedDate = category.CreatedDate,
                LastModified = category.LastModified,
                Types = types.Select(t => new EquipmentTypeDto
                {
                    Id = t.Id,
                    CategoryId = t.CategoryId,
                    CategoryName = category.Name,
                    Name = t.Name,
                    Description = t.Description,
                    DefaultMaintenanceIntervalDays = t.DefaultMaintenanceIntervalDays,
                    RequiredCertifications = t.RequiredCertifications,
                    DisplayOrder = t.DisplayOrder,
                    IsSystemType = t.IsSystemType,
                    IsActive = t.IsActive,
                    EquipmentCount = typeCounts.GetValueOrDefault(t.Id, 0),
                    CreatedDate = t.CreatedDate,
                    LastModified = t.LastModified
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category {Id}", id);
            return StatusCode(500, new { message = "Error retrieving category" });
        }
    }

    /// <summary>
    /// Create new category
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<EquipmentCategoryDto>> CreateCategory(
        [FromQuery] int companyId,
        [FromBody] CreateCategoryRequest request)
    {
        try
        {
            if (await _categoryService.CategoryNameExistsAsync(request.Name, companyId))
                return Conflict(new { message = $"Category '{request.Name}' already exists" });

            var category = new EquipmentCategory
            {
                CompanyId = companyId,
                Name = request.Name,
                Description = request.Description,
                IconClass = request.IconClass,
                DisplayOrder = request.DisplayOrder,
                IsActive = true,
                IsSystemCategory = false
            };

            var created = await _categoryService.CreateCategoryAsync(category, companyId);

            return CreatedAtAction(nameof(GetCategory), new { id = created.Id },
                new EquipmentCategoryDto
                {
                    Id = created.Id,
                    CompanyId = created.CompanyId,
                    Name = created.Name,
                    Description = created.Description,
                    IconClass = created.IconClass,
                    DisplayOrder = created.DisplayOrder,
                    IsSystemCategory = created.IsSystemCategory,
                    IsActive = created.IsActive,
                    CreatedDate = created.CreatedDate,
                    LastModified = created.LastModified
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return StatusCode(500, new { message = "Error creating category" });
        }
    }

    /// <summary>
    /// Update category
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<EquipmentCategoryDto>> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
    {
        try
        {
            var existing = await _categoryService.GetCategoryByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Category with ID {id} not found" });

            if (existing.IsSystemCategory)
                return BadRequest(new { message = "Cannot modify system category" });

            if (await _categoryService.CategoryNameExistsAsync(request.Name, existing.CompanyId, id))
                return Conflict(new { message = $"Category '{request.Name}' already exists" });

            existing.Name = request.Name;
            existing.Description = request.Description;
            existing.IconClass = request.IconClass;
            existing.DisplayOrder = request.DisplayOrder;
            existing.IsActive = request.IsActive;

            var updated = await _categoryService.UpdateCategoryAsync(existing);

            return Ok(new EquipmentCategoryDto
            {
                Id = updated.Id,
                CompanyId = updated.CompanyId,
                Name = updated.Name,
                Description = updated.Description,
                IconClass = updated.IconClass,
                DisplayOrder = updated.DisplayOrder,
                IsSystemCategory = updated.IsSystemCategory,
                IsActive = updated.IsActive,
                CreatedDate = updated.CreatedDate,
                LastModified = updated.LastModified
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {Id}", id);
            return StatusCode(500, new { message = "Error updating category" });
        }
    }

    /// <summary>
    /// Delete category
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteCategory(int id)
    {
        try
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
                return NotFound(new { message = $"Category with ID {id} not found" });

            if (category.IsSystemCategory)
                return BadRequest(new { message = "Cannot delete system category" });

            var equipmentCount = await _categoryService.GetEquipmentCountForCategoryAsync(id);
            if (equipmentCount > 0)
                return BadRequest(new { message = "Cannot delete category with equipment assigned" });

            var result = await _categoryService.DeleteCategoryAsync(id);
            if (!result)
                return NotFound(new { message = $"Category with ID {id} not found" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {Id}", id);
            return StatusCode(500, new { message = "Error deleting category" });
        }
    }

    /// <summary>
    /// Activate category
    /// </summary>
    [HttpPatch("{id:int}/activate")]
    public async Task<ActionResult> ActivateCategory(int id)
    {
        try
        {
            var result = await _categoryService.ActivateCategoryAsync(id);
            return Ok(new { message = "Category activated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating category {Id}", id);
            return StatusCode(500, new { message = "Error activating category" });
        }
    }

    /// <summary>
    /// Deactivate category
    /// </summary>
    [HttpPatch("{id:int}/deactivate")]
    public async Task<ActionResult> DeactivateCategory(int id)
    {
        try
        {
            var result = await _categoryService.DeactivateCategoryAsync(id);
            return Ok(new { message = "Category deactivated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating category {Id}", id);
            return StatusCode(500, new { message = "Error deactivating category" });
        }
    }

    #endregion

    #region Types

    /// <summary>
    /// Get types for a category
    /// </summary>
    [HttpGet("{categoryId:int}/types")]
    public async Task<ActionResult<List<EquipmentTypeDto>>> GetTypes(
        int categoryId,
        [FromQuery] bool activeOnly = false)
    {
        try
        {
            var types = activeOnly
                ? await _categoryService.GetActiveTypesByCategoryAsync(categoryId)
                : await _categoryService.GetTypesByCategoryAsync(categoryId);

            var category = await _categoryService.GetCategoryByIdAsync(categoryId);
            if (category == null)
                return NotFound(new { message = $"Category with ID {categoryId} not found" });

            var counts = await _categoryService.GetEquipmentCountsByTypeAsync(category.CompanyId);

            var dtos = types.Select(t => new EquipmentTypeDto
            {
                Id = t.Id,
                CategoryId = t.CategoryId,
                CategoryName = category.Name,
                Name = t.Name,
                Description = t.Description,
                DefaultMaintenanceIntervalDays = t.DefaultMaintenanceIntervalDays,
                RequiredCertifications = t.RequiredCertifications,
                DisplayOrder = t.DisplayOrder,
                IsSystemType = t.IsSystemType,
                IsActive = t.IsActive,
                EquipmentCount = counts.GetValueOrDefault(t.Id, 0),
                CreatedDate = t.CreatedDate,
                LastModified = t.LastModified
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting types for category {CategoryId}", categoryId);
            return StatusCode(500, new { message = "Error retrieving types" });
        }
    }

    /// <summary>
    /// Get type by ID
    /// </summary>
    [HttpGet("types/{id:int}")]
    public async Task<ActionResult<EquipmentTypeDto>> GetType(int id)
    {
        try
        {
            var type = await _categoryService.GetTypeByIdAsync(id);
            if (type == null)
                return NotFound(new { message = $"Type with ID {id} not found" });

            var equipmentCount = await _categoryService.GetEquipmentCountForTypeAsync(id);
            var category = await _categoryService.GetCategoryByIdAsync(type.CategoryId);

            return Ok(new EquipmentTypeDto
            {
                Id = type.Id,
                CategoryId = type.CategoryId,
                CategoryName = category?.Name,
                Name = type.Name,
                Description = type.Description,
                DefaultMaintenanceIntervalDays = type.DefaultMaintenanceIntervalDays,
                RequiredCertifications = type.RequiredCertifications,
                DisplayOrder = type.DisplayOrder,
                IsSystemType = type.IsSystemType,
                IsActive = type.IsActive,
                EquipmentCount = equipmentCount,
                CreatedDate = type.CreatedDate,
                LastModified = type.LastModified
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting type {Id}", id);
            return StatusCode(500, new { message = "Error retrieving type" });
        }
    }

    /// <summary>
    /// Create new type
    /// </summary>
    [HttpPost("types")]
    public async Task<ActionResult<EquipmentTypeDto>> CreateType([FromBody] CreateTypeRequest request)
    {
        try
        {
            if (await _categoryService.TypeNameExistsAsync(request.Name, request.CategoryId))
                return Conflict(new { message = $"Type '{request.Name}' already exists in this category" });

            var type = new EquipmentType
            {
                CategoryId = request.CategoryId,
                Name = request.Name,
                Description = request.Description,
                DefaultMaintenanceIntervalDays = request.DefaultMaintenanceIntervalDays,
                RequiredCertifications = request.RequiredCertifications,
                DisplayOrder = request.DisplayOrder,
                IsActive = true,
                IsSystemType = false
            };

            var created = await _categoryService.CreateTypeAsync(type);
            var category = await _categoryService.GetCategoryByIdAsync(created.CategoryId);

            return CreatedAtAction(nameof(GetType), new { id = created.Id },
                new EquipmentTypeDto
                {
                    Id = created.Id,
                    CategoryId = created.CategoryId,
                    CategoryName = category?.Name,
                    Name = created.Name,
                    Description = created.Description,
                    DefaultMaintenanceIntervalDays = created.DefaultMaintenanceIntervalDays,
                    RequiredCertifications = created.RequiredCertifications,
                    DisplayOrder = created.DisplayOrder,
                    IsSystemType = created.IsSystemType,
                    IsActive = created.IsActive,
                    CreatedDate = created.CreatedDate,
                    LastModified = created.LastModified
                });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating type");
            return StatusCode(500, new { message = "Error creating type" });
        }
    }

    /// <summary>
    /// Update type
    /// </summary>
    [HttpPut("types/{id:int}")]
    public async Task<ActionResult<EquipmentTypeDto>> UpdateType(int id, [FromBody] UpdateTypeRequest request)
    {
        try
        {
            var existing = await _categoryService.GetTypeByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Type with ID {id} not found" });

            if (existing.IsSystemType)
                return BadRequest(new { message = "Cannot modify system type" });

            if (await _categoryService.TypeNameExistsAsync(request.Name, existing.CategoryId, id))
                return Conflict(new { message = $"Type '{request.Name}' already exists in this category" });

            existing.Name = request.Name;
            existing.Description = request.Description;
            existing.DefaultMaintenanceIntervalDays = request.DefaultMaintenanceIntervalDays;
            existing.RequiredCertifications = request.RequiredCertifications;
            existing.DisplayOrder = request.DisplayOrder;
            existing.IsActive = request.IsActive;

            var updated = await _categoryService.UpdateTypeAsync(existing);
            var category = await _categoryService.GetCategoryByIdAsync(updated.CategoryId);

            return Ok(new EquipmentTypeDto
            {
                Id = updated.Id,
                CategoryId = updated.CategoryId,
                CategoryName = category?.Name,
                Name = updated.Name,
                Description = updated.Description,
                DefaultMaintenanceIntervalDays = updated.DefaultMaintenanceIntervalDays,
                RequiredCertifications = updated.RequiredCertifications,
                DisplayOrder = updated.DisplayOrder,
                IsSystemType = updated.IsSystemType,
                IsActive = updated.IsActive,
                CreatedDate = updated.CreatedDate,
                LastModified = updated.LastModified
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating type {Id}", id);
            return StatusCode(500, new { message = "Error updating type" });
        }
    }

    /// <summary>
    /// Delete type
    /// </summary>
    [HttpDelete("types/{id:int}")]
    public async Task<ActionResult> DeleteType(int id)
    {
        try
        {
            var type = await _categoryService.GetTypeByIdAsync(id);
            if (type == null)
                return NotFound(new { message = $"Type with ID {id} not found" });

            if (type.IsSystemType)
                return BadRequest(new { message = "Cannot delete system type" });

            var equipmentCount = await _categoryService.GetEquipmentCountForTypeAsync(id);
            if (equipmentCount > 0)
                return BadRequest(new { message = "Cannot delete type with equipment assigned" });

            var result = await _categoryService.DeleteTypeAsync(id);
            if (!result)
                return NotFound(new { message = $"Type with ID {id} not found" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting type {Id}", id);
            return StatusCode(500, new { message = "Error deleting type" });
        }
    }

    /// <summary>
    /// Activate type
    /// </summary>
    [HttpPatch("types/{id:int}/activate")]
    public async Task<ActionResult> ActivateType(int id)
    {
        try
        {
            var result = await _categoryService.ActivateTypeAsync(id);
            return Ok(new { message = "Type activated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating type {Id}", id);
            return StatusCode(500, new { message = "Error activating type" });
        }
    }

    /// <summary>
    /// Deactivate type
    /// </summary>
    [HttpPatch("types/{id:int}/deactivate")]
    public async Task<ActionResult> DeactivateType(int id)
    {
        try
        {
            var result = await _categoryService.DeactivateTypeAsync(id);
            return Ok(new { message = "Type deactivated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating type {Id}", id);
            return StatusCode(500, new { message = "Error deactivating type" });
        }
    }

    #endregion
}
