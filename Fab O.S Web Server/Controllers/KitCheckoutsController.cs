using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Models.DTOs.Assets;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Controllers;

/// <summary>
/// API Controller for Kit Checkout/Return management
/// </summary>
[ApiController]
[Route("api/assets/kit-checkouts")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class KitCheckoutsController : ControllerBase
{
    private readonly IKitCheckoutService _checkoutService;
    private readonly ILogger<KitCheckoutsController> _logger;

    public KitCheckoutsController(
        IKitCheckoutService checkoutService,
        ILogger<KitCheckoutsController> logger)
    {
        _checkoutService = checkoutService;
        _logger = logger;
    }

    private int GetCompanyId() => int.Parse(User.FindFirst("CompanyId")?.Value ?? "1");
    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : null;
    }

    #region Checkout Flow

    /// <summary>
    /// Initiate a checkout
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<KitCheckoutDto>> InitiateCheckout([FromBody] InitiateCheckoutRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();

            var checkout = await _checkoutService.InitiateCheckoutAsync(request, companyId, userId);

            return CreatedAtAction(nameof(GetById), new { id = checkout.Id }, MapToDto(checkout));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Confirm checkout with digital signature
    /// </summary>
    [HttpPost("{id:int}/confirm")]
    public async Task<ActionResult<KitCheckoutDto>> ConfirmCheckout(int id, [FromBody] ConfirmCheckoutRequest request)
    {
        try
        {
            var userId = GetUserId();
            var checkout = await _checkoutService.ConfirmCheckoutAsync(id, request, userId);
            return Ok(MapToDto(checkout));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancel a checkout
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<KitCheckoutDto>> CancelCheckout(int id, [FromBody] CancelCheckoutRequest? request = null)
    {
        try
        {
            var userId = GetUserId();
            var checkout = await _checkoutService.CancelCheckoutAsync(id, request?.Reason, userId);
            return Ok(MapToDto(checkout));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Return Flow

    /// <summary>
    /// Initiate a return
    /// </summary>
    [HttpPost("{id:int}/return")]
    public async Task<ActionResult<KitCheckoutDto>> InitiateReturn(int id, [FromBody] InitiateReturnRequest request)
    {
        try
        {
            var userId = GetUserId();
            var checkout = await _checkoutService.InitiateReturnAsync(id, request, userId);
            return Ok(MapToDto(checkout));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Confirm return with digital signature
    /// </summary>
    [HttpPost("{id:int}/return/confirm")]
    public async Task<ActionResult<KitCheckoutDto>> ConfirmReturn(int id, [FromBody] ConfirmReturnRequest request)
    {
        try
        {
            var userId = GetUserId();
            var checkout = await _checkoutService.ConfirmReturnAsync(id, request, userId);
            return Ok(MapToDto(checkout));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Process partial return
    /// </summary>
    [HttpPost("{id:int}/partial-return")]
    public async Task<ActionResult<KitCheckoutDto>> PartialReturn(int id, [FromBody] PartialReturnRequest request)
    {
        try
        {
            var userId = GetUserId();
            var checkout = await _checkoutService.PartialReturnAsync(id, request, userId);
            return Ok(MapToDto(checkout));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Checkout Queries

    /// <summary>
    /// Get all checkouts with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<KitCheckoutListResponse>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] CheckoutStatus? status = null,
        [FromQuery] int? kitId = null,
        [FromQuery] int? userId = null)
    {
        var companyId = GetCompanyId();

        var checkouts = await _checkoutService.GetPagedAsync(companyId, page, pageSize, search, status, kitId, userId);
        var totalCount = await _checkoutService.GetCountAsync(companyId, search, status, kitId, userId);

        var response = new KitCheckoutListResponse
        {
            Items = checkouts.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Get checkout by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<KitCheckoutDto>> GetById(int id)
    {
        var checkout = await _checkoutService.GetWithItemsAsync(id);
        if (checkout == null)
            return NotFound(new { message = $"Checkout with ID {id} not found" });

        return Ok(MapToDto(checkout));
    }

    #endregion

    #region Status Queries

    /// <summary>
    /// Get active checkouts
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<KitCheckoutDto>>> GetActive()
    {
        var companyId = GetCompanyId();
        var checkouts = await _checkoutService.GetActiveCheckoutsAsync(companyId);
        return Ok(checkouts.Select(MapToDto));
    }

    /// <summary>
    /// Get overdue checkouts
    /// </summary>
    [HttpGet("overdue")]
    public async Task<ActionResult<IEnumerable<KitCheckoutDto>>> GetOverdue()
    {
        var companyId = GetCompanyId();
        var checkouts = await _checkoutService.GetOverdueCheckoutsAsync(companyId);
        return Ok(checkouts.Select(MapToDto));
    }

    /// <summary>
    /// Get pending checkouts
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<KitCheckoutDto>>> GetPending()
    {
        var companyId = GetCompanyId();
        var checkouts = await _checkoutService.GetPendingCheckoutsAsync(companyId);
        return Ok(checkouts.Select(MapToDto));
    }

    /// <summary>
    /// Get current checkout for a kit
    /// </summary>
    [HttpGet("kit/{kitId:int}/current")]
    public async Task<ActionResult<KitCheckoutDto>> GetCurrentForKit(int kitId)
    {
        var checkout = await _checkoutService.GetCurrentCheckoutAsync(kitId);
        if (checkout == null)
            return NotFound(new { message = $"No active checkout found for kit {kitId}" });

        return Ok(MapToDto(checkout));
    }

    #endregion

    #region Extension

    /// <summary>
    /// Extend checkout period
    /// </summary>
    [HttpPost("{id:int}/extend")]
    public async Task<ActionResult<KitCheckoutDto>> ExtendCheckout(int id, [FromBody] ExtendCheckoutRequest request)
    {
        try
        {
            var userId = GetUserId();
            var checkout = await _checkoutService.ExtendCheckoutAsync(id, request.NewExpectedReturnDate, request.Reason, userId);
            return Ok(MapToDto(checkout));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region History

    /// <summary>
    /// Get checkout history for a kit
    /// </summary>
    [HttpGet("kit/{kitId:int}/history")]
    public async Task<ActionResult<IEnumerable<KitCheckoutDto>>> GetKitHistory(int kitId, [FromQuery] int? limit = null)
    {
        var checkouts = await _checkoutService.GetKitCheckoutHistoryAsync(kitId, limit);
        return Ok(checkouts.Select(MapToDto));
    }

    /// <summary>
    /// Get checkout history for a user
    /// </summary>
    [HttpGet("user/{userId:int}/history")]
    public async Task<ActionResult<IEnumerable<KitCheckoutDto>>> GetUserHistory(int userId, [FromQuery] int? limit = null)
    {
        var companyId = GetCompanyId();
        var checkouts = await _checkoutService.GetUserCheckoutHistoryAsync(userId, companyId, limit);
        return Ok(checkouts.Select(MapToDto));
    }

    #endregion

    #region Item Condition

    /// <summary>
    /// Get checkout items
    /// </summary>
    [HttpGet("{id:int}/items")]
    public async Task<ActionResult<IEnumerable<KitCheckoutItemDto>>> GetCheckoutItems(int id)
    {
        var items = await _checkoutService.GetCheckoutItemsAsync(id);
        return Ok(items.Select(MapCheckoutItemToDto));
    }

    /// <summary>
    /// Update item condition
    /// </summary>
    [HttpPatch("{id:int}/items/{kitItemId:int}/condition")]
    public async Task<ActionResult<KitCheckoutItemDto>> UpdateItemCondition(int id, int kitItemId, [FromBody] UpdateItemConditionRequest request)
    {
        try
        {
            var item = await _checkoutService.UpdateItemConditionAsync(id, kitItemId, request.Condition, request.Notes);
            return Ok(MapCheckoutItemToDto(item));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Report item damage
    /// </summary>
    [HttpPost("{id:int}/items/{kitItemId:int}/damage")]
    public async Task<ActionResult<KitCheckoutItemDto>> ReportDamage(int id, int kitItemId, [FromBody] ReportDamageRequest request)
    {
        try
        {
            var userId = GetUserId();
            var item = await _checkoutService.ReportDamageAsync(id, kitItemId, request.DamageDescription, userId);
            return Ok(MapCheckoutItemToDto(item));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get damaged items
    /// </summary>
    [HttpGet("damaged-items")]
    public async Task<ActionResult<IEnumerable<KitCheckoutItemDto>>> GetDamagedItems(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var companyId = GetCompanyId();
        var items = await _checkoutService.GetDamagedItemsAsync(companyId, fromDate, toDate);
        return Ok(items.Select(MapCheckoutItemToDto));
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Get checkout statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<CheckoutStatisticsDto>> GetStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var companyId = GetCompanyId();
        var stats = await _checkoutService.GetStatisticsAsync(companyId, fromDate, toDate);
        return Ok(stats);
    }

    /// <summary>
    /// Mark overdue checkouts (background job trigger)
    /// </summary>
    [HttpPost("mark-overdue")]
    public async Task<IActionResult> MarkOverdue()
    {
        var companyId = GetCompanyId();
        await _checkoutService.MarkOverdueCheckoutsAsync(companyId);
        return NoContent();
    }

    #endregion

    #region Mapping Helpers

    private static KitCheckoutDto MapToDto(KitCheckout checkout)
    {
        return new KitCheckoutDto
        {
            Id = checkout.Id,
            CompanyId = checkout.CompanyId,
            KitId = checkout.KitId,
            KitCode = checkout.Kit?.KitCode,
            KitName = checkout.Kit?.Name,
            Status = checkout.Status,
            CheckedOutToUserId = checkout.CheckedOutToUserId,
            CheckedOutToUserName = checkout.CheckedOutToUserName,
            CheckoutDate = checkout.CheckoutDate,
            ExpectedReturnDate = checkout.ExpectedReturnDate,
            CheckoutPurpose = checkout.CheckoutPurpose,
            ProjectReference = checkout.ProjectReference,
            CheckoutOverallCondition = checkout.CheckoutOverallCondition,
            CheckoutNotes = checkout.CheckoutNotes,
            HasCheckoutSignature = !string.IsNullOrEmpty(checkout.CheckoutSignature),
            CheckoutSignedDate = checkout.CheckoutSignedDate,
            CheckoutProcessedByUserId = checkout.CheckoutProcessedByUserId,
            ActualReturnDate = checkout.ActualReturnDate,
            ReturnedByUserId = checkout.ReturnedByUserId,
            ReturnedByUserName = checkout.ReturnedByUserName,
            ReturnOverallCondition = checkout.ReturnOverallCondition,
            ReturnNotes = checkout.ReturnNotes,
            HasReturnSignature = !string.IsNullOrEmpty(checkout.ReturnSignature),
            ReturnSignedDate = checkout.ReturnSignedDate,
            ReturnProcessedByUserId = checkout.ReturnProcessedByUserId,
            CreatedDate = checkout.CreatedDate,
            CheckoutItems = checkout.CheckoutItems?.Select(MapCheckoutItemToDto).ToList() ?? new()
        };
    }

    private static KitCheckoutItemDto MapCheckoutItemToDto(KitCheckoutItem item)
    {
        return new KitCheckoutItemDto
        {
            Id = item.Id,
            KitCheckoutId = item.KitCheckoutId,
            KitItemId = item.KitItemId,
            EquipmentId = item.EquipmentId,
            EquipmentCode = item.Equipment?.EquipmentCode,
            EquipmentName = item.Equipment?.Name,
            WasPresentAtCheckout = item.WasPresentAtCheckout,
            CheckoutCondition = item.CheckoutCondition,
            CheckoutNotes = item.CheckoutNotes,
            WasPresentAtReturn = item.WasPresentAtReturn,
            ReturnCondition = item.ReturnCondition,
            ReturnNotes = item.ReturnNotes,
            DamageReported = item.DamageReported,
            DamageDescription = item.DamageDescription
        };
    }

    #endregion
}
