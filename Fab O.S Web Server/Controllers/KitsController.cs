using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Models.DTOs.Assets;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Controllers;

/// <summary>
/// API Controller for Equipment Kit management
/// </summary>
[ApiController]
[Route("api/assets/kits")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class KitsController : ControllerBase
{
    private readonly IEquipmentKitService _kitService;
    private readonly ILogger<KitsController> _logger;

    public KitsController(
        IEquipmentKitService kitService,
        ILogger<KitsController> logger)
    {
        _kitService = kitService;
        _logger = logger;
    }

    private int GetCompanyId() => int.Parse(User.FindFirst("CompanyId")?.Value ?? "1");
    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : null;
    }

    #region CRUD Endpoints

    /// <summary>
    /// Get all equipment kits with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<EquipmentKitListResponse>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] KitStatus? status = null,
        [FromQuery] int? templateId = null,
        [FromQuery] int? assignedToUserId = null)
    {
        var companyId = GetCompanyId();

        var kits = await _kitService.GetPagedAsync(companyId, page, pageSize, search, status, templateId, assignedToUserId);
        var totalCount = await _kitService.GetCountAsync(companyId, search, status, templateId, assignedToUserId);

        var response = new EquipmentKitListResponse
        {
            Items = kits.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Get equipment kit by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EquipmentKitDto>> GetById(int id)
    {
        var kit = await _kitService.GetWithItemsAsync(id);
        if (kit == null)
            return NotFound(new { message = $"Equipment kit with ID {id} not found" });

        return Ok(MapToDto(kit));
    }

    /// <summary>
    /// Get equipment kit by code
    /// </summary>
    [HttpGet("code/{kitCode}")]
    public async Task<ActionResult<EquipmentKitDto>> GetByCode(string kitCode)
    {
        var companyId = GetCompanyId();
        var kit = await _kitService.GetByCodeAsync(kitCode, companyId);

        if (kit == null)
            return NotFound(new { message = $"Equipment kit with code '{kitCode}' not found" });

        return Ok(MapToDto(kit));
    }

    /// <summary>
    /// Get equipment kit by QR code identifier
    /// </summary>
    [HttpGet("qr/{qrCodeIdentifier}")]
    public async Task<ActionResult<EquipmentKitDto>> GetByQRCode(string qrCodeIdentifier)
    {
        var kit = await _kitService.GetByQRCodeIdentifierAsync(qrCodeIdentifier);

        if (kit == null)
            return NotFound(new { message = $"Equipment kit with QR code '{qrCodeIdentifier}' not found" });

        return Ok(MapToDto(kit));
    }

    /// <summary>
    /// Create kit from template
    /// </summary>
    [HttpPost("from-template")]
    public async Task<ActionResult<EquipmentKitDto>> CreateFromTemplate([FromBody] CreateKitFromTemplateRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();

            var kit = await _kitService.CreateFromTemplateAsync(
                request.TemplateId,
                companyId,
                request.EquipmentIds,
                request.Name,
                request.Description,
                request.Location,
                userId);

            return CreatedAtAction(nameof(GetById), new { id = kit.Id }, MapToDto(kit));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Create ad-hoc kit (not based on template)
    /// </summary>
    [HttpPost("adhoc")]
    public async Task<ActionResult<EquipmentKitDto>> CreateAdHoc([FromBody] CreateAdHocKitRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();

            var kit = await _kitService.CreateAdHocAsync(
                request.Name,
                companyId,
                request.EquipmentIds,
                request.Description,
                request.Location,
                userId);

            return CreatedAtAction(nameof(GetById), new { id = kit.Id }, MapToDto(kit));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update equipment kit
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<EquipmentKitDto>> Update(int id, [FromBody] UpdateKitRequest request)
    {
        try
        {
            var userId = GetUserId();

            var existing = await _kitService.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Equipment kit with ID {id} not found" });

            existing.Name = request.Name;
            existing.Description = request.Description;
            existing.LocationLegacy = request.Location;

            var updated = await _kitService.UpdateAsync(existing, userId);

            return Ok(MapToDto(updated));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete equipment kit
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] bool hardDelete = false)
    {
        try
        {
            var result = await _kitService.DeleteAsync(id, hardDelete);
            if (!result)
                return NotFound(new { message = $"Equipment kit with ID {id} not found" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Kit Items Endpoints

    /// <summary>
    /// Get kit items
    /// </summary>
    [HttpGet("{id:int}/items")]
    public async Task<ActionResult<IEnumerable<EquipmentKitItemDto>>> GetItems(int id)
    {
        var items = await _kitService.GetKitItemsAsync(id);
        return Ok(items.Select(MapKitItemToDto));
    }

    /// <summary>
    /// Add item to kit
    /// </summary>
    [HttpPost("{id:int}/items")]
    public async Task<ActionResult<EquipmentKitItemDto>> AddItem(int id, [FromBody] AddKitItemRequest request)
    {
        try
        {
            var userId = GetUserId();
            var item = await _kitService.AddItemToKitAsync(id, request.EquipmentId, request.TemplateItemId, request.DisplayOrder, request.Notes, userId);
            return CreatedAtAction(nameof(GetItems), new { id }, MapKitItemToDto(item));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove item from kit
    /// </summary>
    [HttpDelete("{id:int}/items/{equipmentId:int}")]
    public async Task<IActionResult> RemoveItem(int id, int equipmentId)
    {
        try
        {
            var result = await _kitService.RemoveItemFromKitAsync(id, equipmentId);
            if (!result)
                return NotFound(new { message = $"Equipment {equipmentId} not found in kit {id}" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Swap item in kit
    /// </summary>
    [HttpPost("{id:int}/items/swap")]
    public async Task<IActionResult> SwapItem(int id, [FromBody] SwapKitItemRequest request)
    {
        try
        {
            var userId = GetUserId();
            await _kitService.SwapItemAsync(id, request.OldEquipmentId, request.NewEquipmentId, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reorder kit items
    /// </summary>
    [HttpPost("{id:int}/items/reorder")]
    public async Task<IActionResult> ReorderItems(int id, [FromBody] List<int> equipmentIds)
    {
        await _kitService.ReorderKitItemsAsync(id, equipmentIds);
        return NoContent();
    }

    #endregion

    #region Available Equipment

    /// <summary>
    /// Get equipment available to be added to kits
    /// </summary>
    [HttpGet("available-equipment")]
    public async Task<ActionResult<IEnumerable<EquipmentDto>>> GetAvailableEquipment([FromQuery] int? templateId = null)
    {
        var companyId = GetCompanyId();
        var equipment = await _kitService.GetAvailableEquipmentForKitAsync(companyId, templateId);

        return Ok(equipment.Select(e => new EquipmentDto
        {
            Id = e.Id,
            EquipmentCode = e.EquipmentCode,
            Name = e.Name,
            Description = e.Description,
            CategoryId = e.CategoryId,
            CategoryName = e.EquipmentCategory?.Name,
            TypeId = e.TypeId,
            TypeName = e.EquipmentType?.Name,
            Status = e.Status,
            Location = e.Location?.Name ?? e.LocationLegacy
        }));
    }

    /// <summary>
    /// Validate kit completeness (for template-based kits)
    /// </summary>
    [HttpGet("{id:int}/validate")]
    public async Task<ActionResult> ValidateCompleteness(int id)
    {
        var isComplete = await _kitService.ValidateKitCompletenessAsync(id);
        var missingItems = await _kitService.GetMissingTemplateItemsAsync(id);

        return Ok(new
        {
            IsComplete = isComplete,
            MissingItems = missingItems.Select(mi => new
            {
                mi.Id,
                mi.EquipmentTypeId,
                EquipmentTypeName = mi.EquipmentType?.Name,
                mi.Quantity,
                mi.IsMandatory
            })
        });
    }

    #endregion

    #region Status Endpoints

    /// <summary>
    /// Update kit status
    /// </summary>
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<EquipmentKitDto>> UpdateStatus(int id, [FromBody] UpdateKitStatusRequest request)
    {
        try
        {
            var userId = GetUserId();
            var kit = await _kitService.UpdateStatusAsync(id, request.Status, userId);
            return Ok(MapToDto(kit));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get available kits
    /// </summary>
    [HttpGet("available")]
    public async Task<ActionResult<IEnumerable<EquipmentKitDto>>> GetAvailable()
    {
        var companyId = GetCompanyId();
        var kits = await _kitService.GetAvailableKitsAsync(companyId);
        return Ok(kits.Select(MapToDto));
    }

    /// <summary>
    /// Get checked-out kits
    /// </summary>
    [HttpGet("checked-out")]
    public async Task<ActionResult<IEnumerable<EquipmentKitDto>>> GetCheckedOut([FromQuery] int? userId = null)
    {
        var companyId = GetCompanyId();
        var kits = await _kitService.GetCheckedOutKitsAsync(companyId, userId);
        return Ok(kits.Select(MapToDto));
    }

    /// <summary>
    /// Get overdue kits
    /// </summary>
    [HttpGet("overdue")]
    public async Task<ActionResult<IEnumerable<EquipmentKitDto>>> GetOverdue()
    {
        var companyId = GetCompanyId();
        var kits = await _kitService.GetOverdueKitsAsync(companyId);
        return Ok(kits.Select(MapToDto));
    }

    /// <summary>
    /// Get kit status counts
    /// </summary>
    [HttpGet("status-counts")]
    public async Task<ActionResult<Dictionary<KitStatus, int>>> GetStatusCounts()
    {
        var companyId = GetCompanyId();
        var counts = await _kitService.GetStatusCountsAsync(companyId);
        return Ok(counts);
    }

    #endregion

    #region Maintenance Endpoints

    /// <summary>
    /// Flag item for maintenance
    /// </summary>
    [HttpPost("{id:int}/flag-maintenance")]
    public async Task<ActionResult<EquipmentKitDto>> FlagMaintenance(int id, [FromBody] FlagMaintenanceRequest request)
    {
        try
        {
            var userId = GetUserId();
            var kit = await _kitService.FlagItemForMaintenanceAsync(id, request.EquipmentId, request.Notes, userId);
            return Ok(MapToDto(kit));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Clear maintenance flag
    /// </summary>
    [HttpPost("{id:int}/clear-maintenance")]
    public async Task<ActionResult<EquipmentKitDto>> ClearMaintenance(int id, [FromBody] FlagMaintenanceRequest request)
    {
        try
        {
            var userId = GetUserId();
            var kit = await _kitService.ClearMaintenanceFlagAsync(id, request.EquipmentId, userId);
            return Ok(MapToDto(kit));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get kits with maintenance flags
    /// </summary>
    [HttpGet("maintenance-flagged")]
    public async Task<ActionResult<IEnumerable<EquipmentKitDto>>> GetMaintenanceFlagged()
    {
        var companyId = GetCompanyId();
        var kits = await _kitService.GetKitsWithMaintenanceFlagsAsync(companyId);
        return Ok(kits.Select(MapToDto));
    }

    #endregion

    #region QR Code Endpoints

    /// <summary>
    /// Generate QR code for kit
    /// </summary>
    [HttpPost("{id:int}/qrcode/generate")]
    public async Task<ActionResult<QRCodeGenerationResult>> GenerateQRCode(int id)
    {
        var result = await _kitService.GenerateKitQRCodeAsync(id);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Regenerate QR code for kit
    /// </summary>
    [HttpPost("{id:int}/qrcode/regenerate")]
    public async Task<ActionResult<QRCodeGenerationResult>> RegenerateQRCode(int id)
    {
        var result = await _kitService.RegenerateKitQRCodeAsync(id);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    #endregion

    #region Assignment Endpoints

    /// <summary>
    /// Assign kit to user
    /// </summary>
    [HttpPost("{id:int}/assign")]
    public async Task<ActionResult<EquipmentKitDto>> Assign(int id, [FromBody] EquipmentAssignmentRequest request)
    {
        try
        {
            if (!request.UserId.HasValue)
                return BadRequest(new { message = "UserId is required" });

            var modifiedByUserId = GetUserId();
            var kit = await _kitService.AssignToUserAsync(id, request.UserId.Value, modifiedByUserId);
            return Ok(MapToDto(kit));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Unassign kit
    /// </summary>
    [HttpPost("{id:int}/unassign")]
    public async Task<ActionResult<EquipmentKitDto>> Unassign(int id)
    {
        try
        {
            var userId = GetUserId();
            var kit = await _kitService.UnassignAsync(id, userId);
            return Ok(MapToDto(kit));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get kits assigned to a user
    /// </summary>
    [HttpGet("assigned/{userId:int}")]
    public async Task<ActionResult<IEnumerable<EquipmentKitDto>>> GetByAssignedUser(int userId)
    {
        var companyId = GetCompanyId();
        var kits = await _kitService.GetByAssignedUserAsync(userId, companyId);
        return Ok(kits.Select(MapToDto));
    }

    /// <summary>
    /// Get unassigned kits
    /// </summary>
    [HttpGet("unassigned")]
    public async Task<ActionResult<IEnumerable<EquipmentKitDto>>> GetUnassigned()
    {
        var companyId = GetCompanyId();
        var kits = await _kitService.GetUnassignedAsync(companyId);
        return Ok(kits.Select(MapToDto));
    }

    #endregion

    #region Dashboard

    /// <summary>
    /// Get kit dashboard
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<KitDashboardDto>> GetDashboard()
    {
        var companyId = GetCompanyId();
        var dashboard = await _kitService.GetDashboardAsync(companyId);
        return Ok(dashboard);
    }

    #endregion

    #region Mapping Helpers

    private static EquipmentKitDto MapToDto(EquipmentKit kit)
    {
        return new EquipmentKitDto
        {
            Id = kit.Id,
            CompanyId = kit.CompanyId,
            KitTemplateId = kit.KitTemplateId,
            KitTemplateName = kit.KitTemplate?.Name,
            KitCode = kit.KitCode,
            Name = kit.Name,
            Description = kit.Description,
            Status = kit.Status,
            Location = kit.Location?.Name ?? kit.LocationLegacy,
            AssignedToUserId = kit.AssignedToUserId,
            AssignedToUserName = kit.AssignedToUserName,
            QRCodeData = kit.QRCodeData,
            QRCodeIdentifier = kit.QRCodeIdentifier,
            HasMaintenanceFlag = kit.HasMaintenanceFlag,
            MaintenanceFlagNotes = kit.MaintenanceFlagNotes,
            CreatedDate = kit.CreatedDate,
            CreatedByUserId = kit.CreatedByUserId,
            LastModified = kit.LastModified,
            LastModifiedByUserId = kit.LastModifiedByUserId,
            KitItemCount = kit.KitItems?.Count ?? 0,
            KitItems = kit.KitItems?.Select(MapKitItemToDto).ToList() ?? new()
        };
    }

    private static EquipmentKitItemDto MapKitItemToDto(EquipmentKitItem item)
    {
        return new EquipmentKitItemDto
        {
            Id = item.Id,
            KitId = item.KitId,
            EquipmentId = item.EquipmentId,
            EquipmentCode = item.Equipment?.EquipmentCode,
            EquipmentName = item.Equipment?.Name,
            EquipmentTypeName = item.Equipment?.EquipmentType?.Name,
            EquipmentCategoryName = item.Equipment?.EquipmentType?.EquipmentCategory?.Name,
            TemplateItemId = item.TemplateItemId,
            DisplayOrder = item.DisplayOrder,
            NeedsMaintenance = item.NeedsMaintenance,
            Notes = item.Notes,
            AddedDate = item.AddedDate,
            AddedByUserId = item.AddedByUserId
        };
    }

    #endregion
}
