using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Models.DTOs.Assets;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service interface for Kit Checkout/Return management
/// </summary>
public interface IKitCheckoutService
{
    #region Checkout Flow

    /// <summary>
    /// Initiate a checkout for a kit (creates pending checkout record)
    /// </summary>
    Task<KitCheckout> InitiateCheckoutAsync(InitiateCheckoutRequest request, int companyId, int? processedByUserId = null);

    /// <summary>
    /// Confirm checkout with digital signature (completes checkout)
    /// </summary>
    Task<KitCheckout> ConfirmCheckoutAsync(int checkoutId, ConfirmCheckoutRequest request, int? processedByUserId = null);

    /// <summary>
    /// Cancel a pending or active checkout
    /// </summary>
    Task<KitCheckout> CancelCheckoutAsync(int checkoutId, string? reason = null, int? cancelledByUserId = null);

    #endregion

    #region Return Flow

    /// <summary>
    /// Initiate a return (records return conditions)
    /// </summary>
    Task<KitCheckout> InitiateReturnAsync(int checkoutId, InitiateReturnRequest request, int? processedByUserId = null);

    /// <summary>
    /// Confirm return with digital signature (completes return)
    /// </summary>
    Task<KitCheckout> ConfirmReturnAsync(int checkoutId, ConfirmReturnRequest request, int? processedByUserId = null);

    /// <summary>
    /// Process a partial return (some items returned, others still out)
    /// </summary>
    Task<KitCheckout> PartialReturnAsync(int checkoutId, PartialReturnRequest request, int? processedByUserId = null);

    #endregion

    #region Checkout Queries

    Task<KitCheckout?> GetByIdAsync(int id);
    Task<KitCheckout?> GetWithItemsAsync(int id);
    Task<IEnumerable<KitCheckout>> GetAllAsync(int companyId, int? limit = null);
    Task<IEnumerable<KitCheckout>> GetPagedAsync(int companyId, int page, int pageSize,
        string? search = null, CheckoutStatus? status = null, int? kitId = null, int? userId = null);
    Task<int> GetCountAsync(int companyId, string? search = null, CheckoutStatus? status = null,
        int? kitId = null, int? userId = null);

    #endregion

    #region Status Queries

    Task<IEnumerable<KitCheckout>> GetActiveCheckoutsAsync(int companyId);
    Task<IEnumerable<KitCheckout>> GetOverdueCheckoutsAsync(int companyId);
    Task<IEnumerable<KitCheckout>> GetPendingCheckoutsAsync(int companyId);
    Task<KitCheckout?> GetCurrentCheckoutAsync(int kitId);

    /// <summary>
    /// Background job to mark overdue checkouts
    /// </summary>
    Task MarkOverdueCheckoutsAsync(int companyId);

    #endregion

    #region Extension

    /// <summary>
    /// Extend the expected return date of a checkout
    /// </summary>
    Task<KitCheckout> ExtendCheckoutAsync(int checkoutId, DateTime newExpectedReturnDate,
        string? reason = null, int? modifiedByUserId = null);

    #endregion

    #region History

    Task<IEnumerable<KitCheckout>> GetKitCheckoutHistoryAsync(int kitId, int? limit = null);
    Task<IEnumerable<KitCheckout>> GetUserCheckoutHistoryAsync(int userId, int companyId, int? limit = null);

    #endregion

    #region Item Condition Tracking

    Task<KitCheckoutItem?> GetCheckoutItemAsync(int checkoutId, int kitItemId);
    Task<IEnumerable<KitCheckoutItem>> GetCheckoutItemsAsync(int checkoutId);
    Task<KitCheckoutItem> UpdateItemConditionAsync(int checkoutId, int kitItemId,
        EquipmentCondition condition, string? notes = null);
    Task<KitCheckoutItem> ReportDamageAsync(int checkoutId, int kitItemId,
        string damageDescription, int? reportedByUserId = null);
    Task<IEnumerable<KitCheckoutItem>> GetDamagedItemsAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null);

    #endregion

    #region Statistics

    Task<CheckoutStatisticsDto> GetStatisticsAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<int> GetActiveCheckoutCountAsync(int companyId);
    Task<int> GetOverdueCheckoutCountAsync(int companyId);
    Task<double> GetAverageCheckoutDaysAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null);

    #endregion
}
