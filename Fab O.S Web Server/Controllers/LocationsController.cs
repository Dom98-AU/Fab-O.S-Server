using FabOS.WebServer.Models.DTOs.Assets;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FabOS.WebServer.Controllers;

/// <summary>
/// API controller for managing tenant locations
/// </summary>
[Authorize(AuthenticationSchemes = "Bearer")]
[ApiController]
[Route("api/assets/locations")]
[Produces("application/json")]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(
        ILocationService locationService,
        ILogger<LocationsController> logger)
    {
        _locationService = locationService;
        _logger = logger;
    }

    #region Helper Methods

    private int GetCompanyId()
    {
        var companyIdClaim = User.FindFirst("company_id")?.Value;
        if (int.TryParse(companyIdClaim, out int companyId))
            return companyId;

        // Default to company 1 if not found in claims
        return 1;
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst("user_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
            return userId;

        return null;
    }

    #endregion

    #region CRUD Endpoints

    /// <summary>
    /// Get all locations (paged, filterable)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<LocationListResponse>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] LocationType? type = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var companyId = GetCompanyId();
            var items = await _locationService.GetPagedAsync(companyId, page, pageSize, search, type, isActive);
            var totalCount = await _locationService.GetCountAsync(companyId, search, type, isActive);

            return Ok(new LocationListResponse
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting locations");
            return StatusCode(500, new { message = "Error retrieving locations" });
        }
    }

    /// <summary>
    /// Get location by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<LocationDto>> GetById(int id)
    {
        try
        {
            var location = await _locationService.GetByIdAsync(id);
            if (location == null)
                return NotFound(new { message = $"Location with ID {id} not found" });

            return Ok(MapToDto(location));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location {Id}", id);
            return StatusCode(500, new { message = "Error retrieving location" });
        }
    }

    /// <summary>
    /// Get location by code
    /// </summary>
    [HttpGet("code/{locationCode}")]
    public async Task<ActionResult<LocationDto>> GetByCode(string locationCode)
    {
        try
        {
            var companyId = GetCompanyId();
            var location = await _locationService.GetByCodeAsync(locationCode, companyId);
            if (location == null)
                return NotFound(new { message = $"Location with code '{locationCode}' not found" });

            return Ok(MapToDto(location));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location by code {Code}", locationCode);
            return StatusCode(500, new { message = "Error retrieving location" });
        }
    }

    /// <summary>
    /// Create a new location
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<LocationDto>> Create([FromBody] CreateLocationRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();

            var location = new Location
            {
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,
                Address = request.Address,
                ContactName = request.ContactName,
                ContactPhone = request.ContactPhone,
                IsActive = request.IsActive
            };

            var created = await _locationService.CreateAsync(location, companyId, userId);

            _logger.LogInformation("Created location {LocationCode} for company {CompanyId}",
                created.LocationCode, companyId);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating location");
            return StatusCode(500, new { message = "Error creating location" });
        }
    }

    /// <summary>
    /// Update a location
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<LocationDto>> Update(int id, [FromBody] UpdateLocationRequest request)
    {
        try
        {
            var userId = GetUserId();
            var existing = await _locationService.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Location with ID {id} not found" });

            existing.Name = request.Name;
            existing.Description = request.Description;
            existing.Type = request.Type;
            existing.Address = request.Address;
            existing.ContactName = request.ContactName;
            existing.ContactPhone = request.ContactPhone;
            existing.IsActive = request.IsActive;

            var updated = await _locationService.UpdateAsync(existing, userId);

            return Ok(MapToDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location {Id}", id);
            return StatusCode(500, new { message = "Error updating location" });
        }
    }

    /// <summary>
    /// Delete a location
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] bool hardDelete = false)
    {
        try
        {
            var result = await _locationService.DeleteAsync(id, hardDelete);
            if (!result)
                return NotFound(new { message = $"Location with ID {id} not found" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting location {Id}", id);
            return StatusCode(500, new { message = "Error deleting location" });
        }
    }

    #endregion

    #region Query Endpoints

    /// <summary>
    /// Get available location types
    /// </summary>
    [HttpGet("types")]
    public ActionResult<IEnumerable<LocationTypeDto>> GetTypes()
    {
        var types = Enum.GetValues<LocationType>()
            .Select(t => new LocationTypeDto
            {
                Value = (int)t,
                Name = t.ToString(),
                Description = GetLocationTypeDescription(t)
            });

        return Ok(types);
    }

    private static string GetLocationTypeDescription(LocationType type) => type switch
    {
        LocationType.PhysicalSite => "Warehouses, workshops, offices - permanent physical locations",
        LocationType.JobSite => "Temporary project/job sites",
        LocationType.Vehicle => "Trucks, trailers, mobile containers",
        _ => type.ToString()
    };

    /// <summary>
    /// Get locations by type
    /// </summary>
    [HttpGet("type/{type}")]
    public async Task<ActionResult<IEnumerable<LocationDto>>> GetByType(LocationType type)
    {
        try
        {
            var companyId = GetCompanyId();
            var locations = await _locationService.GetByTypeAsync(companyId, type);
            return Ok(locations.Select(MapToDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting locations by type {Type}", type);
            return StatusCode(500, new { message = "Error retrieving locations" });
        }
    }

    /// <summary>
    /// Get active locations only
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<LocationDto>>> GetActive()
    {
        try
        {
            var companyId = GetCompanyId();
            var locations = await _locationService.GetActiveLocationsAsync(companyId);
            return Ok(locations.Select(MapToDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active locations");
            return StatusCode(500, new { message = "Error retrieving locations" });
        }
    }

    #endregion

    #region Activation Endpoints

    /// <summary>
    /// Activate a location
    /// </summary>
    [HttpPost("{id:int}/activate")]
    public async Task<ActionResult<LocationDto>> Activate(int id)
    {
        try
        {
            var userId = GetUserId();
            var location = await _locationService.ActivateAsync(id, userId);
            return Ok(MapToDto(location));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating location {Id}", id);
            return StatusCode(500, new { message = "Error activating location" });
        }
    }

    /// <summary>
    /// Deactivate a location
    /// </summary>
    [HttpPost("{id:int}/deactivate")]
    public async Task<ActionResult<LocationDto>> Deactivate(int id)
    {
        try
        {
            var userId = GetUserId();
            var location = await _locationService.DeactivateAsync(id, userId);
            return Ok(MapToDto(location));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating location {Id}", id);
            return StatusCode(500, new { message = "Error deactivating location" });
        }
    }

    #endregion

    #region Equipment/Kit Assignment Endpoints

    /// <summary>
    /// Get equipment at a location
    /// </summary>
    [HttpGet("{id:int}/equipment")]
    public async Task<ActionResult<IEnumerable<EquipmentDto>>> GetEquipment(int id)
    {
        try
        {
            var location = await _locationService.GetByIdAsync(id);
            if (location == null)
                return NotFound(new { message = $"Location with ID {id} not found" });

            var equipment = await _locationService.GetEquipmentAtLocationAsync(id);
            return Ok(equipment.Select(e => new EquipmentDto
            {
                Id = e.Id,
                CompanyId = e.CompanyId,
                EquipmentCode = e.EquipmentCode,
                Name = e.Name,
                Description = e.Description,
                CategoryId = e.CategoryId,
                CategoryName = e.EquipmentCategory?.Name,
                TypeId = e.TypeId,
                TypeName = e.EquipmentType?.Name,
                SerialNumber = e.SerialNumber,
                Status = e.Status,
                Location = location.Name
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting equipment at location {Id}", id);
            return StatusCode(500, new { message = "Error retrieving equipment" });
        }
    }

    /// <summary>
    /// Get kits at a location
    /// </summary>
    [HttpGet("{id:int}/kits")]
    public async Task<ActionResult<IEnumerable<EquipmentKitDto>>> GetKits(int id)
    {
        try
        {
            var location = await _locationService.GetByIdAsync(id);
            if (location == null)
                return NotFound(new { message = $"Location with ID {id} not found" });

            var kits = await _locationService.GetKitsAtLocationAsync(id);
            return Ok(kits.Select(k => new EquipmentKitDto
            {
                Id = k.Id,
                CompanyId = k.CompanyId,
                KitTemplateId = k.KitTemplateId,
                KitTemplateName = k.KitTemplate?.Name,
                KitCode = k.KitCode,
                Name = k.Name,
                Description = k.Description,
                Status = k.Status,
                Location = location.Name,
                KitItemCount = k.KitItems?.Count ?? 0
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting kits at location {Id}", id);
            return StatusCode(500, new { message = "Error retrieving kits" });
        }
    }

    /// <summary>
    /// Assign equipment to a location
    /// </summary>
    [HttpPost("{id:int}/assign-equipment")]
    public async Task<IActionResult> AssignEquipment(int id, [FromBody] AssignEquipmentRequest request)
    {
        try
        {
            var userId = GetUserId();
            await _locationService.AssignEquipmentToLocationAsync(id, request.EquipmentIds, userId);
            return Ok(new { message = $"Assigned {request.EquipmentIds.Count} equipment items to location" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning equipment to location {Id}", id);
            return StatusCode(500, new { message = "Error assigning equipment" });
        }
    }

    /// <summary>
    /// Assign kits to a location
    /// </summary>
    [HttpPost("{id:int}/assign-kits")]
    public async Task<IActionResult> AssignKits(int id, [FromBody] AssignKitsRequest request)
    {
        try
        {
            var userId = GetUserId();
            await _locationService.AssignKitsToLocationAsync(id, request.KitIds, userId);
            return Ok(new { message = $"Assigned {request.KitIds.Count} kits to location" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning kits to location {Id}", id);
            return StatusCode(500, new { message = "Error assigning kits" });
        }
    }

    #endregion

    #region Dashboard Endpoint

    /// <summary>
    /// Get location dashboard statistics
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<LocationDashboardDto>> GetDashboard()
    {
        try
        {
            var companyId = GetCompanyId();
            var data = await _locationService.GetDashboardAsync(companyId);

            return Ok(new LocationDashboardDto
            {
                TotalLocations = data.TotalLocations,
                ActiveLocations = data.ActiveLocations,
                PhysicalSites = data.PhysicalSites,
                JobSites = data.JobSites,
                Vehicles = data.Vehicles,
                TotalEquipmentAllocated = data.TotalEquipmentAllocated,
                TotalKitsAllocated = data.TotalKitsAllocated
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location dashboard");
            return StatusCode(500, new { message = "Error retrieving dashboard" });
        }
    }

    #endregion

    #region Mapping

    private static LocationDto MapToDto(Location location)
    {
        return new LocationDto
        {
            Id = location.Id,
            CompanyId = location.CompanyId,
            LocationCode = location.LocationCode,
            Name = location.Name,
            Description = location.Description,
            Type = location.Type,
            Address = location.Address,
            ContactName = location.ContactName,
            ContactPhone = location.ContactPhone,
            IsActive = location.IsActive,
            EquipmentCount = location.Equipment?.Count(e => !e.IsDeleted) ?? 0,
            KitCount = location.Kits?.Count(k => !k.IsDeleted) ?? 0,
            CreatedDate = location.CreatedDate,
            CreatedByUserId = location.CreatedByUserId,
            LastModified = location.LastModified,
            LastModifiedByUserId = location.LastModifiedByUserId
        };
    }

    #endregion
}
