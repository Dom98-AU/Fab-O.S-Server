using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FabOS.WebServer.Models.DTOs.Assets;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Controllers;

/// <summary>
/// API controller for Equipment management in the Asset module
/// </summary>
[ApiController]
[Route("api/assets/equipment")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class AssetsController : ControllerBase
{
    private readonly IEquipmentService _equipmentService;
    private readonly IQRCodeService _qrCodeService;
    private readonly ILabelPrintingService _labelService;
    private readonly ILogger<AssetsController> _logger;

    public AssetsController(
        IEquipmentService equipmentService,
        IQRCodeService qrCodeService,
        ILabelPrintingService labelService,
        ILogger<AssetsController> logger)
    {
        _equipmentService = equipmentService;
        _qrCodeService = qrCodeService;
        _labelService = labelService;
        _logger = logger;
    }

    #region Equipment CRUD

    /// <summary>
    /// Get all equipment for a company
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<EquipmentListResponse>> GetAllEquipment(
        [FromQuery] int companyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] int? typeId = null,
        [FromQuery] EquipmentStatus? status = null,
        [FromQuery] string? location = null)
    {
        try
        {
            var equipment = await _equipmentService.GetPagedAsync(
                companyId, page, pageSize, search, categoryId, typeId, status, location);
            var totalCount = await _equipmentService.GetCountAsync(
                companyId, search, categoryId, typeId, status, location);

            var items = equipment.Select(e => MapToDto(e)).ToList();

            return Ok(new EquipmentListResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting equipment for company {CompanyId}", companyId);
            return StatusCode(500, new { message = "Error retrieving equipment" });
        }
    }

    /// <summary>
    /// Get equipment by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EquipmentDto>> GetEquipment(int id)
    {
        try
        {
            var equipment = await _equipmentService.GetByIdAsync(id);
            if (equipment == null)
                return NotFound(new { message = $"Equipment with ID {id} not found" });

            return Ok(MapToDto(equipment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting equipment {Id}", id);
            return StatusCode(500, new { message = "Error retrieving equipment" });
        }
    }

    /// <summary>
    /// Get equipment by equipment code
    /// </summary>
    [HttpGet("code/{equipmentCode}")]
    public async Task<ActionResult<EquipmentDto>> GetEquipmentByCode(string equipmentCode, [FromQuery] int companyId)
    {
        try
        {
            var equipment = await _equipmentService.GetByCodeAsync(equipmentCode, companyId);
            if (equipment == null)
                return NotFound(new { message = $"Equipment with code '{equipmentCode}' not found" });

            return Ok(MapToDto(equipment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting equipment by code {EquipmentCode}", equipmentCode);
            return StatusCode(500, new { message = "Error retrieving equipment" });
        }
    }

    /// <summary>
    /// Get equipment by QR code identifier
    /// </summary>
    [HttpGet("qr/{qrCodeIdentifier}")]
    public async Task<ActionResult<EquipmentDto>> GetEquipmentByQRCode(string qrCodeIdentifier)
    {
        try
        {
            var equipment = await _equipmentService.GetByQRCodeIdentifierAsync(qrCodeIdentifier);
            if (equipment == null)
                return NotFound(new { message = $"Equipment with QR code identifier '{qrCodeIdentifier}' not found" });

            return Ok(MapToDto(equipment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting equipment by QR code {QRCodeIdentifier}", qrCodeIdentifier);
            return StatusCode(500, new { message = "Error retrieving equipment" });
        }
    }

    /// <summary>
    /// Create new equipment
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<EquipmentDto>> CreateEquipment(
        [FromQuery] int companyId,
        [FromBody] CreateEquipmentRequest request)
    {
        try
        {
            var equipment = new Equipment
            {
                CompanyId = companyId,
                Name = request.Name,
                Description = request.Description,
                CategoryId = request.CategoryId,
                TypeId = request.TypeId,
                Manufacturer = request.Manufacturer,
                Model = request.Model,
                SerialNumber = request.SerialNumber,
                PurchaseDate = request.PurchaseDate,
                PurchaseCost = request.PurchaseCost,
                CurrentValue = request.CurrentValue,
                WarrantyExpiry = request.WarrantyExpiry,
                LocationLegacy = request.Location,
                Department = request.Department,
                AssignedTo = request.AssignedTo,
                AssignedToUserId = request.AssignedToUserId,
                Notes = request.Notes,
                MaintenanceIntervalDays = request.MaintenanceIntervalDays
            };

            var createdBy = User.Identity?.Name;
            var created = await _equipmentService.CreateAsync(equipment, companyId, createdBy);

            _logger.LogInformation("Created equipment {EquipmentCode} for company {CompanyId}",
                created.EquipmentCode, companyId);

            return CreatedAtAction(nameof(GetEquipment), new { id = created.Id }, MapToDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating equipment");
            return StatusCode(500, new { message = "Error creating equipment" });
        }
    }

    /// <summary>
    /// Update equipment
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<EquipmentDto>> UpdateEquipment(int id, [FromBody] UpdateEquipmentRequest request)
    {
        try
        {
            var existing = await _equipmentService.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Equipment with ID {id} not found" });

            existing.Name = request.Name;
            existing.Description = request.Description;
            existing.CategoryId = request.CategoryId;
            existing.TypeId = request.TypeId;
            existing.Manufacturer = request.Manufacturer;
            existing.Model = request.Model;
            existing.SerialNumber = request.SerialNumber;
            existing.PurchaseDate = request.PurchaseDate;
            existing.PurchaseCost = request.PurchaseCost;
            existing.CurrentValue = request.CurrentValue;
            existing.WarrantyExpiry = request.WarrantyExpiry;
            existing.LocationLegacy = request.Location;
            existing.Department = request.Department;
            existing.AssignedTo = request.AssignedTo;
            existing.AssignedToUserId = request.AssignedToUserId;
            existing.Notes = request.Notes;
            existing.MaintenanceIntervalDays = request.MaintenanceIntervalDays;

            var modifiedBy = User.Identity?.Name;
            var updated = await _equipmentService.UpdateAsync(existing, modifiedBy);

            return Ok(MapToDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating equipment {Id}", id);
            return StatusCode(500, new { message = "Error updating equipment" });
        }
    }

    /// <summary>
    /// Delete equipment (soft delete by default)
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteEquipment(int id, [FromQuery] bool hardDelete = false)
    {
        try
        {
            var result = await _equipmentService.DeleteAsync(id, hardDelete);
            if (!result)
                return NotFound(new { message = $"Equipment with ID {id} not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting equipment {Id}", id);
            return StatusCode(500, new { message = "Error deleting equipment" });
        }
    }

    #endregion

    #region Equipment Status

    /// <summary>
    /// Update equipment status
    /// </summary>
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<EquipmentDto>> UpdateStatus(int id, [FromBody] EquipmentStatusUpdateRequest request)
    {
        try
        {
            if (!Enum.TryParse<EquipmentStatus>(request.Status, true, out var status))
                return BadRequest(new { message = "Invalid status value" });

            var modifiedBy = User.Identity?.Name;
            var updated = await _equipmentService.UpdateStatusAsync(id, status, modifiedBy);

            if (updated == null)
                return NotFound(new { message = $"Equipment with ID {id} not found" });

            return Ok(MapToDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating equipment status {Id}", id);
            return StatusCode(500, new { message = "Error updating status" });
        }
    }

    /// <summary>
    /// Get equipment by status
    /// </summary>
    [HttpGet("status/{status}")]
    public async Task<ActionResult<List<EquipmentDto>>> GetByStatus([FromQuery] int companyId, EquipmentStatus status)
    {
        try
        {
            var equipment = await _equipmentService.GetByStatusAsync(companyId, status);
            return Ok(equipment.Select(e => MapToDto(e)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting equipment by status");
            return StatusCode(500, new { message = "Error retrieving equipment" });
        }
    }

    /// <summary>
    /// Get equipment status counts
    /// </summary>
    [HttpGet("status-counts")]
    public async Task<ActionResult<Dictionary<EquipmentStatus, int>>> GetStatusCounts([FromQuery] int companyId)
    {
        try
        {
            var counts = await _equipmentService.GetStatusCountsAsync(companyId);
            return Ok(counts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status counts");
            return StatusCode(500, new { message = "Error retrieving status counts" });
        }
    }

    #endregion

    #region Equipment Assignment

    /// <summary>
    /// Assign equipment to a user
    /// </summary>
    [HttpPatch("{id:int}/assign")]
    public async Task<ActionResult<EquipmentDto>> AssignEquipment(int id, [FromBody] EquipmentAssignmentRequest request)
    {
        try
        {
            Equipment updated;
            var modifiedBy = User.Identity?.Name;

            if (request.UserId.HasValue)
            {
                // For now, use userId as userName - in production, fetch from user service
                var userName = $"User_{request.UserId.Value}";
                updated = await _equipmentService.AssignToUserAsync(id, request.UserId.Value, userName, modifiedBy);
            }
            else
            {
                updated = await _equipmentService.UnassignAsync(id, modifiedBy);
            }

            if (updated == null)
                return NotFound(new { message = $"Equipment with ID {id} not found" });

            return Ok(MapToDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning equipment {Id}", id);
            return StatusCode(500, new { message = "Error updating assignment" });
        }
    }

    /// <summary>
    /// Get equipment assigned to a user
    /// </summary>
    [HttpGet("assigned/{userId:int}")]
    public async Task<ActionResult<List<EquipmentDto>>> GetByUser(int userId, [FromQuery] int companyId)
    {
        try
        {
            var equipment = await _equipmentService.GetByAssignedUserAsync(userId, companyId);
            return Ok(equipment.Select(e => MapToDto(e)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting equipment for user {UserId}", userId);
            return StatusCode(500, new { message = "Error retrieving equipment" });
        }
    }

    /// <summary>
    /// Get unassigned equipment
    /// </summary>
    [HttpGet("unassigned")]
    public async Task<ActionResult<List<EquipmentDto>>> GetUnassigned([FromQuery] int companyId)
    {
        try
        {
            var equipment = await _equipmentService.GetUnassignedAsync(companyId);
            return Ok(equipment.Select(e => MapToDto(e)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unassigned equipment");
            return StatusCode(500, new { message = "Error retrieving equipment" });
        }
    }

    #endregion

    #region Location Operations

    /// <summary>
    /// Update equipment location
    /// </summary>
    [HttpPatch("{id:int}/location")]
    public async Task<ActionResult<EquipmentDto>> UpdateLocation(int id, [FromBody] string location)
    {
        try
        {
            var modifiedBy = User.Identity?.Name;
            var updated = await _equipmentService.UpdateLocationAsync(id, location, modifiedBy);

            if (updated == null)
                return NotFound(new { message = $"Equipment with ID {id} not found" });

            return Ok(MapToDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating equipment location {Id}", id);
            return StatusCode(500, new { message = "Error updating location" });
        }
    }

    /// <summary>
    /// Get equipment by location
    /// </summary>
    [HttpGet("location/{location}")]
    public async Task<ActionResult<List<EquipmentDto>>> GetByLocation([FromQuery] int companyId, string location)
    {
        try
        {
            var equipment = await _equipmentService.GetByLocationAsync(companyId, location);
            return Ok(equipment.Select(e => MapToDto(e)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting equipment by location");
            return StatusCode(500, new { message = "Error retrieving equipment" });
        }
    }

    /// <summary>
    /// Get all locations for a company
    /// </summary>
    [HttpGet("locations")]
    public async Task<ActionResult<List<string>>> GetLocations([FromQuery] int companyId)
    {
        try
        {
            var locations = await _equipmentService.GetLocationsAsync(companyId);
            return Ok(locations.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting locations");
            return StatusCode(500, new { message = "Error retrieving locations" });
        }
    }

    /// <summary>
    /// Get equipment counts by location
    /// </summary>
    [HttpGet("location-counts")]
    public async Task<ActionResult<Dictionary<string, int>>> GetLocationCounts([FromQuery] int companyId)
    {
        try
        {
            var counts = await _equipmentService.GetEquipmentCountsByLocationAsync(companyId);
            return Ok(counts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location counts");
            return StatusCode(500, new { message = "Error retrieving location counts" });
        }
    }

    #endregion

    #region Maintenance Operations

    /// <summary>
    /// Update maintenance dates for equipment
    /// </summary>
    [HttpPatch("{id:int}/maintenance-dates")]
    public async Task<ActionResult<EquipmentDto>> UpdateMaintenanceDates(
        int id,
        [FromBody] MaintenanceDatesUpdateRequest request)
    {
        try
        {
            var modifiedBy = User.Identity?.Name;
            var updated = await _equipmentService.UpdateMaintenanceDatesAsync(
                id, request.LastMaintenanceDate, request.NextMaintenanceDate, modifiedBy);

            if (updated == null)
                return NotFound(new { message = $"Equipment with ID {id} not found" });

            return Ok(MapToDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating maintenance dates {Id}", id);
            return StatusCode(500, new { message = "Error updating maintenance dates" });
        }
    }

    /// <summary>
    /// Get equipment due for maintenance
    /// </summary>
    [HttpGet("maintenance-due")]
    public async Task<ActionResult<List<EquipmentDto>>> GetMaintenanceDue(
        [FromQuery] int companyId,
        [FromQuery] int daysAhead = 7)
    {
        try
        {
            var equipment = await _equipmentService.GetDueForMaintenanceAsync(companyId, daysAhead);
            return Ok(equipment.Select(e => MapToDto(e)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting maintenance due equipment");
            return StatusCode(500, new { message = "Error retrieving equipment" });
        }
    }

    /// <summary>
    /// Get overdue maintenance equipment
    /// </summary>
    [HttpGet("maintenance-overdue")]
    public async Task<ActionResult<List<EquipmentDto>>> GetOverdueMaintenance([FromQuery] int companyId)
    {
        try
        {
            var equipment = await _equipmentService.GetOverdueMaintenanceAsync(companyId);
            return Ok(equipment.Select(e => MapToDto(e)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overdue maintenance equipment");
            return StatusCode(500, new { message = "Error retrieving equipment" });
        }
    }

    /// <summary>
    /// Get equipment by maintenance interval
    /// </summary>
    [HttpGet("maintenance-interval/{intervalDays:int}")]
    public async Task<ActionResult<List<EquipmentDto>>> GetByMaintenanceInterval(
        [FromQuery] int companyId,
        int intervalDays)
    {
        try
        {
            var equipment = await _equipmentService.GetByMaintenanceIntervalAsync(companyId, intervalDays);
            return Ok(equipment.Select(e => MapToDto(e)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting equipment by maintenance interval");
            return StatusCode(500, new { message = "Error retrieving equipment" });
        }
    }

    #endregion

    #region Category and Type Operations

    /// <summary>
    /// Get equipment by category
    /// </summary>
    [HttpGet("category/{categoryId:int}")]
    public async Task<ActionResult<List<EquipmentDto>>> GetByCategory([FromQuery] int companyId, int categoryId)
    {
        try
        {
            var equipment = await _equipmentService.GetByCategoryAsync(companyId, categoryId);
            return Ok(equipment.Select(e => MapToDto(e)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting equipment by category");
            return StatusCode(500, new { message = "Error retrieving equipment" });
        }
    }

    /// <summary>
    /// Get equipment by type
    /// </summary>
    [HttpGet("type/{typeId:int}")]
    public async Task<ActionResult<List<EquipmentDto>>> GetByType([FromQuery] int companyId, int typeId)
    {
        try
        {
            var equipment = await _equipmentService.GetByTypeAsync(companyId, typeId);
            return Ok(equipment.Select(e => MapToDto(e)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting equipment by type");
            return StatusCode(500, new { message = "Error retrieving equipment" });
        }
    }

    /// <summary>
    /// Get equipment counts by category
    /// </summary>
    [HttpGet("category-counts")]
    public async Task<ActionResult<Dictionary<int, int>>> GetCategoryCounts([FromQuery] int companyId)
    {
        try
        {
            var counts = await _equipmentService.GetEquipmentCountsByCategoryAsync(companyId);
            return Ok(counts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category counts");
            return StatusCode(500, new { message = "Error retrieving category counts" });
        }
    }

    /// <summary>
    /// Get equipment counts by type
    /// </summary>
    [HttpGet("type-counts")]
    public async Task<ActionResult<Dictionary<int, int>>> GetTypeCounts([FromQuery] int companyId)
    {
        try
        {
            var counts = await _equipmentService.GetEquipmentCountsByTypeAsync(companyId);
            return Ok(counts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting type counts");
            return StatusCode(500, new { message = "Error retrieving type counts" });
        }
    }

    #endregion

    #region Warranty Operations

    /// <summary>
    /// Get equipment with expiring warranty
    /// </summary>
    [HttpGet("warranty-expiring")]
    public async Task<ActionResult<List<EquipmentDto>>> GetExpiringWarranty(
        [FromQuery] int companyId,
        [FromQuery] int daysAhead = 30)
    {
        try
        {
            var equipment = await _equipmentService.GetWithExpiringWarrantyAsync(companyId, daysAhead);
            return Ok(equipment.Select(e => MapToDto(e)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expiring warranty equipment");
            return StatusCode(500, new { message = "Error retrieving equipment" });
        }
    }

    /// <summary>
    /// Get equipment under warranty
    /// </summary>
    [HttpGet("warranty-active")]
    public async Task<ActionResult<List<EquipmentDto>>> GetUnderWarranty([FromQuery] int companyId)
    {
        try
        {
            var equipment = await _equipmentService.GetUnderWarrantyAsync(companyId);
            return Ok(equipment.Select(e => MapToDto(e)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting equipment under warranty");
            return StatusCode(500, new { message = "Error retrieving equipment" });
        }
    }

    /// <summary>
    /// Get equipment with expired warranty
    /// </summary>
    [HttpGet("warranty-expired")]
    public async Task<ActionResult<List<EquipmentDto>>> GetWarrantyExpired([FromQuery] int companyId)
    {
        try
        {
            var equipment = await _equipmentService.GetWarrantyExpiredAsync(companyId);
            return Ok(equipment.Select(e => MapToDto(e)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expired warranty equipment");
            return StatusCode(500, new { message = "Error retrieving equipment" });
        }
    }

    #endregion

    #region Search Operations

    /// <summary>
    /// Search equipment
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<EquipmentDto>>> SearchEquipment(
        [FromQuery] int companyId,
        [FromQuery] string term)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term))
                return BadRequest(new { message = "Search term is required" });

            var equipment = await _equipmentService.SearchAsync(companyId, term);
            return Ok(equipment.Select(e => MapToDto(e)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching equipment");
            return StatusCode(500, new { message = "Error searching equipment" });
        }
    }

    /// <summary>
    /// Search equipment by serial number
    /// </summary>
    [HttpGet("search/serial/{serialNumber}")]
    public async Task<ActionResult<List<EquipmentDto>>> SearchBySerialNumber(
        [FromQuery] int companyId,
        string serialNumber)
    {
        try
        {
            var equipment = await _equipmentService.SearchBySerialNumberAsync(companyId, serialNumber);
            return Ok(equipment.Select(e => MapToDto(e)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching equipment by serial number");
            return StatusCode(500, new { message = "Error searching equipment" });
        }
    }

    /// <summary>
    /// Search equipment by manufacturer
    /// </summary>
    [HttpGet("search/manufacturer/{manufacturer}")]
    public async Task<ActionResult<List<EquipmentDto>>> SearchByManufacturer(
        [FromQuery] int companyId,
        string manufacturer)
    {
        try
        {
            var equipment = await _equipmentService.SearchByManufacturerAsync(companyId, manufacturer);
            return Ok(equipment.Select(e => MapToDto(e)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching equipment by manufacturer");
            return StatusCode(500, new { message = "Error searching equipment" });
        }
    }

    #endregion

    #region Analytics

    /// <summary>
    /// Get total asset value for company
    /// </summary>
    [HttpGet("analytics/total-value")]
    public async Task<ActionResult<decimal>> GetTotalValue([FromQuery] int companyId)
    {
        try
        {
            var totalValue = await _equipmentService.GetTotalAssetValueAsync(companyId);
            return Ok(totalValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total asset value");
            return StatusCode(500, new { message = "Error retrieving total value" });
        }
    }

    /// <summary>
    /// Get average asset value for company
    /// </summary>
    [HttpGet("analytics/average-value")]
    public async Task<ActionResult<decimal>> GetAverageValue([FromQuery] int companyId)
    {
        try
        {
            var averageValue = await _equipmentService.GetAverageAssetValueAsync(companyId);
            return Ok(averageValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting average asset value");
            return StatusCode(500, new { message = "Error retrieving average value" });
        }
    }

    /// <summary>
    /// Get equipment dashboard summary
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<object>> GetDashboard([FromQuery] int companyId)
    {
        try
        {
            var totalEquipment = await _equipmentService.GetTotalCountAsync(companyId);
            var statusCounts = await _equipmentService.GetStatusCountsAsync(companyId);
            var categoryCounts = await _equipmentService.GetCategoryCountsAsync(companyId);
            var maintenanceDue = await _equipmentService.GetMaintenanceDueCountAsync(companyId, 30);
            var totalValue = await _equipmentService.GetTotalValueAsync(companyId);

            return Ok(new
            {
                totalEquipment,
                statusCounts,
                categoryCounts,
                maintenanceDueIn30Days = maintenanceDue,
                totalValue
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting equipment dashboard");
            return StatusCode(500, new { message = "Error retrieving dashboard" });
        }
    }

    /// <summary>
    /// Get recently added equipment
    /// </summary>
    [HttpGet("analytics/recent")]
    public async Task<ActionResult<List<EquipmentDto>>> GetRecentlyAdded(
        [FromQuery] int companyId,
        [FromQuery] int count = 10)
    {
        try
        {
            var equipment = await _equipmentService.GetRecentlyAddedAsync(companyId, count);
            return Ok(equipment.Select(e => MapToDto(e)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recently added equipment");
            return StatusCode(500, new { message = "Error retrieving equipment" });
        }
    }

    /// <summary>
    /// Get recently modified equipment
    /// </summary>
    [HttpGet("analytics/recent-modified")]
    public async Task<ActionResult<List<EquipmentDto>>> GetRecentlyModified(
        [FromQuery] int companyId,
        [FromQuery] int count = 10)
    {
        try
        {
            var equipment = await _equipmentService.GetRecentlyModifiedAsync(companyId, count);
            return Ok(equipment.Select(e => MapToDto(e)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recently modified equipment");
            return StatusCode(500, new { message = "Error retrieving equipment" });
        }
    }

    #endregion

    #region QR Code

    /// <summary>
    /// Generate QR code for equipment
    /// </summary>
    [HttpPost("{id:int}/qrcode/generate")]
    public async Task<ActionResult<EquipmentQRCodeResponse>> GenerateQRCode(int id)
    {
        return await GetQRCode(id);
    }

    /// <summary>
    /// Get QR code for equipment
    /// </summary>
    [HttpGet("{id:int}/qrcode")]
    public async Task<ActionResult<EquipmentQRCodeResponse>> GetQRCode(int id)
    {
        try
        {
            var result = await _qrCodeService.GenerateEquipmentQRCodeAsync(id);

            if (!result.Success)
                return NotFound(new { message = result.ErrorMessage ?? "Equipment not found" });

            return Ok(new EquipmentQRCodeResponse
            {
                EquipmentId = id,
                EquipmentCode = "", // Will be populated by service
                QRCodeIdentifier = result.QRCodeIdentifier ?? "",
                QRCodeDataUrl = result.QRCodeDataUrl ?? ""
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code for equipment {Id}", id);
            return StatusCode(500, new { message = "Error generating QR code" });
        }
    }

    /// <summary>
    /// Regenerate QR code for equipment
    /// </summary>
    [HttpPost("{id:int}/qrcode/regenerate")]
    public async Task<ActionResult<EquipmentQRCodeResponse>> RegenerateQRCode(int id)
    {
        try
        {
            var result = await _qrCodeService.RegenerateEquipmentQRCodeAsync(id);

            if (!result.Success)
                return NotFound(new { message = result.ErrorMessage ?? "Equipment not found" });

            return Ok(new EquipmentQRCodeResponse
            {
                EquipmentId = id,
                EquipmentCode = "", // Will be populated by service
                QRCodeIdentifier = result.QRCodeIdentifier ?? "",
                QRCodeDataUrl = result.QRCodeDataUrl ?? ""
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating QR code for equipment {Id}", id);
            return StatusCode(500, new { message = "Error regenerating QR code" });
        }
    }

    /// <summary>
    /// Get QR code size options
    /// </summary>
    [HttpGet("qrcode/sizes")]
    public ActionResult<Dictionary<string, int>> GetQRCodeSizes()
    {
        var sizes = _qrCodeService.GetQRCodeSizeOptions();
        return Ok(sizes);
    }

    #endregion

    #region Label Printing

    /// <summary>
    /// Generate label PDF for single equipment
    /// </summary>
    [HttpGet("{id:int}/label")]
    public async Task<ActionResult> GetLabel(int id, [FromQuery] string template = "Standard")
    {
        try
        {
            var result = await _labelService.GenerateLabelAsync(id, template);

            if (!result.Success)
                return NotFound(new { message = result.ErrorMessage ?? "Equipment not found" });

            if (string.IsNullOrEmpty(result.PdfBase64))
                return StatusCode(500, new { message = "Failed to generate label PDF" });

            var pdfBytes = Convert.FromBase64String(result.PdfBase64);
            return File(pdfBytes, "application/pdf", result.FileName ?? $"equipment-label-{id}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating label for equipment {Id}", id);
            return StatusCode(500, new { message = "Error generating label" });
        }
    }

    /// <summary>
    /// Generate labels PDF for multiple equipment (batch)
    /// </summary>
    [HttpPost("labels/batch")]
    public async Task<ActionResult> GetBatchLabels([FromBody] GenerateBatchLabelsRequest request)
    {
        try
        {
            if (request.EquipmentIds == null || request.EquipmentIds.Count == 0)
                return BadRequest(new { message = "At least one equipment ID is required" });

            var result = await _labelService.GenerateBatchLabelsAsync(
                request.EquipmentIds,
                request.Template,
                request.Options);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage ?? "Failed to generate labels" });

            if (string.IsNullOrEmpty(result.PdfBase64))
                return StatusCode(500, new { message = "Failed to generate labels PDF" });

            var pdfBytes = Convert.FromBase64String(result.PdfBase64);
            return File(pdfBytes, "application/pdf", result.FileName ?? "equipment-labels.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating batch labels");
            return StatusCode(500, new { message = "Error generating labels" });
        }
    }

    /// <summary>
    /// Generate labels for equipment in a category
    /// </summary>
    [HttpPost("labels/category/{categoryId:int}")]
    public async Task<ActionResult> GetLabelsByCategory(
        int categoryId,
        [FromQuery] int companyId,
        [FromQuery] string template = "Standard")
    {
        try
        {
            var result = await _labelService.GenerateLabelsByCategoryAsync(categoryId, companyId, template);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage ?? "Failed to generate labels" });

            if (string.IsNullOrEmpty(result.PdfBase64))
                return StatusCode(500, new { message = "Failed to generate labels PDF" });

            var pdfBytes = Convert.FromBase64String(result.PdfBase64);
            return File(pdfBytes, "application/pdf", result.FileName ?? $"category-{categoryId}-labels.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating labels for category {CategoryId}", categoryId);
            return StatusCode(500, new { message = "Error generating labels" });
        }
    }

    /// <summary>
    /// Generate labels for equipment in a location
    /// </summary>
    [HttpPost("labels/location/{location}")]
    public async Task<ActionResult> GetLabelsByLocation(
        string location,
        [FromQuery] int companyId,
        [FromQuery] string template = "Standard")
    {
        try
        {
            var result = await _labelService.GenerateLabelsByLocationAsync(location, companyId, template);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage ?? "Failed to generate labels" });

            if (string.IsNullOrEmpty(result.PdfBase64))
                return StatusCode(500, new { message = "Failed to generate labels PDF" });

            var pdfBytes = Convert.FromBase64String(result.PdfBase64);
            return File(pdfBytes, "application/pdf", result.FileName ?? $"location-{location}-labels.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating labels for location {Location}", location);
            return StatusCode(500, new { message = "Error generating labels" });
        }
    }

    /// <summary>
    /// Generate labels for equipment due for maintenance
    /// </summary>
    [HttpPost("labels/maintenance-due")]
    public async Task<ActionResult> GetMaintenanceDueLabels(
        [FromQuery] int companyId,
        [FromQuery] int daysAhead = 7,
        [FromQuery] string template = "Standard")
    {
        try
        {
            var result = await _labelService.GenerateMaintenanceDueLabelsAsync(companyId, daysAhead, template);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage ?? "Failed to generate labels" });

            if (string.IsNullOrEmpty(result.PdfBase64))
                return StatusCode(500, new { message = "Failed to generate labels PDF" });

            var pdfBytes = Convert.FromBase64String(result.PdfBase64);
            return File(pdfBytes, "application/pdf", result.FileName ?? "maintenance-due-labels.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating maintenance due labels");
            return StatusCode(500, new { message = "Error generating labels" });
        }
    }

    /// <summary>
    /// Get available label templates
    /// </summary>
    [HttpGet("labels/templates")]
    public ActionResult<List<LabelTemplateDto>> GetLabelTemplates()
    {
        var templates = _labelService.GetAvailableTemplates();
        return Ok(templates.ToList());
    }

    /// <summary>
    /// Get specific label template
    /// </summary>
    [HttpGet("labels/templates/{templateName}")]
    public ActionResult<LabelTemplateDto> GetLabelTemplate(string templateName)
    {
        var template = _labelService.GetTemplate(templateName);
        if (template == null)
            return NotFound(new { message = $"Template '{templateName}' not found" });

        return Ok(template);
    }

    /// <summary>
    /// Get default label options for a template
    /// </summary>
    [HttpGet("labels/templates/{templateName}/defaults")]
    public ActionResult<LabelOptionsDto> GetDefaultLabelOptions(string templateName)
    {
        var options = _labelService.GetDefaultOptions(templateName);
        return Ok(options);
    }

    #endregion

    #region Helper Methods

    private static EquipmentDto MapToDto(Equipment e)
    {
        return new EquipmentDto
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
            Manufacturer = e.Manufacturer,
            Model = e.Model,
            SerialNumber = e.SerialNumber,
            PurchaseDate = e.PurchaseDate,
            PurchaseCost = e.PurchaseCost,
            CurrentValue = e.CurrentValue,
            WarrantyExpiry = e.WarrantyExpiry,
            Location = e.Location?.Name ?? e.LocationLegacy,
            Department = e.Department,
            AssignedTo = e.AssignedTo,
            AssignedToUserId = e.AssignedToUserId,
            Status = e.Status,
            Notes = e.Notes,
            QRCodeData = e.QRCodeData,
            QRCodeIdentifier = e.QRCodeIdentifier,
            LastMaintenanceDate = e.LastMaintenanceDate,
            NextMaintenanceDate = e.NextMaintenanceDate,
            MaintenanceIntervalDays = e.MaintenanceIntervalDays,
            CreatedDate = e.CreatedDate,
            CreatedBy = e.CreatedBy,
            LastModified = e.LastModified,
            LastModifiedBy = e.LastModifiedBy,
            MaintenanceScheduleCount = e.MaintenanceSchedules?.Count ?? 0,
            MaintenanceRecordCount = e.MaintenanceRecords?.Count ?? 0,
            CertificationCount = e.Certifications?.Count ?? 0,
            ManualCount = e.Manuals?.Count ?? 0
        };
    }

    #endregion
}

/// <summary>
/// Request DTO for updating maintenance dates
/// </summary>
public class MaintenanceDatesUpdateRequest
{
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
}
