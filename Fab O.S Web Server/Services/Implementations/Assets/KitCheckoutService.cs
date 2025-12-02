using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Models.DTOs.Assets;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations.Assets;

/// <summary>
/// Service implementation for Kit Checkout/Return management
/// </summary>
public class KitCheckoutService : IKitCheckoutService
{
    private readonly ApplicationDbContext _context;
    private readonly IEquipmentKitService _kitService;
    private readonly IUserManagementService _userService;
    private readonly ILogger<KitCheckoutService> _logger;

    public KitCheckoutService(
        ApplicationDbContext context,
        IEquipmentKitService kitService,
        IUserManagementService userService,
        ILogger<KitCheckoutService> logger)
    {
        _context = context;
        _kitService = kitService;
        _userService = userService;
        _logger = logger;
    }

    #region Checkout Flow

    public async Task<KitCheckout> InitiateCheckoutAsync(InitiateCheckoutRequest request, int companyId, int? processedByUserId = null)
    {
        var kit = await _kitService.GetWithItemsAsync(request.KitId);
        if (kit == null)
            throw new InvalidOperationException($"Kit with ID {request.KitId} not found");

        if (kit.Status != KitStatus.Available)
            throw new InvalidOperationException($"Kit is not available for checkout. Current status: {kit.Status}");

        // Validate user exists
        var user = await _userService.GetUserByIdAsync(request.CheckedOutToUserId);
        if (user == null)
            throw new InvalidOperationException($"User with ID {request.CheckedOutToUserId} not found");

        var userName = !string.IsNullOrWhiteSpace(user.FirstName) || !string.IsNullOrWhiteSpace(user.LastName)
            ? $"{user.FirstName} {user.LastName}".Trim()
            : user.Email;

        var checkout = new KitCheckout
        {
            CompanyId = companyId,
            KitId = request.KitId,
            Status = CheckoutStatus.Pending,
            CheckedOutToUserId = request.CheckedOutToUserId,
            CheckedOutToUserName = userName,
            CheckoutDate = DateTime.UtcNow,
            ExpectedReturnDate = request.ExpectedReturnDate,
            CheckoutPurpose = request.CheckoutPurpose,
            ProjectReference = request.ProjectReference,
            CheckoutOverallCondition = request.OverallCondition,
            CheckoutNotes = request.Notes,
            CheckoutProcessedByUserId = processedByUserId,
            CreatedDate = DateTime.UtcNow,
            CreatedByUserId = processedByUserId
        };

        _context.KitCheckouts.Add(checkout);
        await _context.SaveChangesAsync();

        // Create checkout items for each kit item
        foreach (var kitItem in kit.KitItems)
        {
            var itemCondition = request.ItemConditions?.FirstOrDefault(ic => ic.KitItemId == kitItem.Id);

            var checkoutItem = new KitCheckoutItem
            {
                KitCheckoutId = checkout.Id,
                KitItemId = kitItem.Id,
                EquipmentId = kitItem.EquipmentId,
                WasPresentAtCheckout = itemCondition?.WasPresent ?? true,
                CheckoutCondition = itemCondition?.Condition ?? request.OverallCondition,
                CheckoutNotes = itemCondition?.Notes
            };

            _context.KitCheckoutItems.Add(checkoutItem);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Initiated checkout {CheckoutId} for kit {KitId} to user {UserId}",
            checkout.Id, request.KitId, request.CheckedOutToUserId);

        return checkout;
    }

    public async Task<KitCheckout> ConfirmCheckoutAsync(int checkoutId, ConfirmCheckoutRequest request, int? processedByUserId = null)
    {
        var checkout = await GetWithItemsAsync(checkoutId);
        if (checkout == null)
            throw new InvalidOperationException($"Checkout with ID {checkoutId} not found");

        if (checkout.Status != CheckoutStatus.Pending)
            throw new InvalidOperationException($"Checkout is not in pending status. Current status: {checkout.Status}");

        // Validate signature
        if (string.IsNullOrWhiteSpace(request.Signature))
            throw new InvalidOperationException("Digital signature is required to confirm checkout");

        checkout.Status = CheckoutStatus.CheckedOut;
        checkout.CheckoutSignature = request.Signature;
        checkout.CheckoutSignedDate = DateTime.UtcNow;
        checkout.CheckoutProcessedByUserId = processedByUserId;
        checkout.LastModified = DateTime.UtcNow;
        checkout.LastModifiedByUserId = processedByUserId;

        // Update kit status and assignment
        var kit = await _kitService.GetByIdAsync(checkout.KitId);
        if (kit != null)
        {
            kit.Status = KitStatus.CheckedOut;
            kit.AssignedToUserId = checkout.CheckedOutToUserId;
            kit.AssignedToUserName = checkout.CheckedOutToUserName;
            kit.LastModified = DateTime.UtcNow;
            kit.LastModifiedByUserId = processedByUserId;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Confirmed checkout {CheckoutId} for kit {KitId}", checkoutId, checkout.KitId);

        return checkout;
    }

    public async Task<KitCheckout> CancelCheckoutAsync(int checkoutId, string? reason = null, int? cancelledByUserId = null)
    {
        var checkout = await GetByIdAsync(checkoutId);
        if (checkout == null)
            throw new InvalidOperationException($"Checkout with ID {checkoutId} not found");

        if (checkout.Status == CheckoutStatus.Returned || checkout.Status == CheckoutStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel checkout with status: {checkout.Status}");

        checkout.Status = CheckoutStatus.Cancelled;
        checkout.ReturnNotes = reason;
        checkout.LastModified = DateTime.UtcNow;
        checkout.LastModifiedByUserId = cancelledByUserId;

        // If kit was checked out, make it available again
        if (checkout.Status == CheckoutStatus.CheckedOut)
        {
            var kit = await _kitService.GetByIdAsync(checkout.KitId);
            if (kit != null)
            {
                kit.Status = KitStatus.Available;
                kit.AssignedToUserId = null;
                kit.AssignedToUserName = null;
                kit.LastModified = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Cancelled checkout {CheckoutId}. Reason: {Reason}", checkoutId, reason ?? "Not provided");

        return checkout;
    }

    #endregion

    #region Return Flow

    public async Task<KitCheckout> InitiateReturnAsync(int checkoutId, InitiateReturnRequest request, int? processedByUserId = null)
    {
        var checkout = await GetWithItemsAsync(checkoutId);
        if (checkout == null)
            throw new InvalidOperationException($"Checkout with ID {checkoutId} not found");

        if (checkout.Status != CheckoutStatus.CheckedOut && checkout.Status != CheckoutStatus.PartialReturn && checkout.Status != CheckoutStatus.Overdue)
            throw new InvalidOperationException($"Cannot initiate return for checkout with status: {checkout.Status}");

        checkout.ReturnOverallCondition = request.OverallCondition;
        checkout.ReturnNotes = request.Notes;
        checkout.LastModified = DateTime.UtcNow;
        checkout.LastModifiedByUserId = processedByUserId;

        // Update each item's return condition
        foreach (var itemCondition in request.ItemConditions)
        {
            var checkoutItem = checkout.CheckoutItems.FirstOrDefault(ci => ci.KitItemId == itemCondition.KitItemId);
            if (checkoutItem != null)
            {
                checkoutItem.WasPresentAtReturn = itemCondition.WasPresent;
                checkoutItem.ReturnCondition = itemCondition.Condition;
                checkoutItem.ReturnNotes = itemCondition.Notes;
                checkoutItem.DamageReported = itemCondition.DamageReported;
                checkoutItem.DamageDescription = itemCondition.DamageDescription;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Initiated return for checkout {CheckoutId}", checkoutId);

        return checkout;
    }

    public async Task<KitCheckout> ConfirmReturnAsync(int checkoutId, ConfirmReturnRequest request, int? processedByUserId = null)
    {
        var checkout = await GetWithItemsAsync(checkoutId);
        if (checkout == null)
            throw new InvalidOperationException($"Checkout with ID {checkoutId} not found");

        if (checkout.Status != CheckoutStatus.CheckedOut && checkout.Status != CheckoutStatus.PartialReturn && checkout.Status != CheckoutStatus.Overdue)
            throw new InvalidOperationException($"Cannot confirm return for checkout with status: {checkout.Status}");

        // Validate signature
        if (string.IsNullOrWhiteSpace(request.Signature))
            throw new InvalidOperationException("Digital signature is required to confirm return");

        checkout.Status = CheckoutStatus.Returned;
        checkout.ActualReturnDate = DateTime.UtcNow;
        checkout.ReturnedByUserId = processedByUserId;
        checkout.ReturnSignature = request.Signature;
        checkout.ReturnSignedDate = DateTime.UtcNow;
        checkout.ReturnProcessedByUserId = processedByUserId;
        checkout.LastModified = DateTime.UtcNow;
        checkout.LastModifiedByUserId = processedByUserId;

        // Update kit status
        var kit = await _kitService.GetByIdAsync(checkout.KitId);
        if (kit != null)
        {
            kit.Status = KitStatus.Available;
            kit.AssignedToUserId = null;
            kit.AssignedToUserName = null;
            kit.LastModified = DateTime.UtcNow;
            kit.LastModifiedByUserId = processedByUserId;

            // Check if any items were damaged and need maintenance flagging
            foreach (var checkoutItem in checkout.CheckoutItems.Where(ci => ci.DamageReported))
            {
                var kitItem = await _context.EquipmentKitItems
                    .FirstOrDefaultAsync(ki => ki.Id == checkoutItem.KitItemId);
                if (kitItem != null)
                {
                    kitItem.NeedsMaintenance = true;
                    kit.HasMaintenanceFlag = true;
                }
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Confirmed return for checkout {CheckoutId}", checkoutId);

        return checkout;
    }

    public async Task<KitCheckout> PartialReturnAsync(int checkoutId, PartialReturnRequest request, int? processedByUserId = null)
    {
        var checkout = await GetWithItemsAsync(checkoutId);
        if (checkout == null)
            throw new InvalidOperationException($"Checkout with ID {checkoutId} not found");

        if (checkout.Status != CheckoutStatus.CheckedOut && checkout.Status != CheckoutStatus.Overdue)
            throw new InvalidOperationException($"Cannot process partial return for checkout with status: {checkout.Status}");

        // Validate signature
        if (string.IsNullOrWhiteSpace(request.Signature))
            throw new InvalidOperationException("Digital signature is required for partial return");

        checkout.Status = CheckoutStatus.PartialReturn;
        checkout.ReturnNotes = request.Notes;
        checkout.LastModified = DateTime.UtcNow;
        checkout.LastModifiedByUserId = processedByUserId;

        // Update returned items
        foreach (var returnedItem in request.ReturnedItems)
        {
            var checkoutItem = checkout.CheckoutItems.FirstOrDefault(ci => ci.KitItemId == returnedItem.KitItemId);
            if (checkoutItem != null)
            {
                checkoutItem.WasPresentAtReturn = returnedItem.WasPresent;
                checkoutItem.ReturnCondition = returnedItem.Condition;
                checkoutItem.ReturnNotes = returnedItem.Notes;
                checkoutItem.DamageReported = returnedItem.DamageReported;
                checkoutItem.DamageDescription = returnedItem.DamageDescription;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Processed partial return for checkout {CheckoutId}. {Count} items returned",
            checkoutId, request.ReturnedItems.Count);

        return checkout;
    }

    #endregion

    #region Checkout Queries

    public async Task<KitCheckout?> GetByIdAsync(int id)
    {
        return await _context.KitCheckouts
            .Include(kc => kc.Kit)
            .FirstOrDefaultAsync(kc => kc.Id == id);
    }

    public async Task<KitCheckout?> GetWithItemsAsync(int id)
    {
        return await _context.KitCheckouts
            .Include(kc => kc.Kit)
            .Include(kc => kc.CheckoutItems)
                .ThenInclude(ci => ci.Equipment)
            .FirstOrDefaultAsync(kc => kc.Id == id);
    }

    public async Task<IEnumerable<KitCheckout>> GetAllAsync(int companyId, int? limit = null)
    {
        var query = _context.KitCheckouts
            .Include(kc => kc.Kit)
            .Where(kc => kc.CompanyId == companyId)
            .OrderByDescending(kc => kc.CheckoutDate);

        if (limit.HasValue)
            query = (IOrderedQueryable<KitCheckout>)query.Take(limit.Value);

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<KitCheckout>> GetPagedAsync(int companyId, int page, int pageSize,
        string? search = null, CheckoutStatus? status = null, int? kitId = null, int? userId = null)
    {
        var query = BuildQuery(companyId, search, status, kitId, userId);

        return await query
            .Include(kc => kc.Kit)
            .OrderByDescending(kc => kc.CheckoutDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(int companyId, string? search = null, CheckoutStatus? status = null,
        int? kitId = null, int? userId = null)
    {
        var query = BuildQuery(companyId, search, status, kitId, userId);
        return await query.CountAsync();
    }

    private IQueryable<KitCheckout> BuildQuery(int companyId, string? search, CheckoutStatus? status,
        int? kitId, int? userId)
    {
        var query = _context.KitCheckouts
            .Include(kc => kc.Kit)
            .Where(kc => kc.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(kc =>
                (kc.Kit != null && kc.Kit.Name.ToLower().Contains(searchLower)) ||
                (kc.Kit != null && kc.Kit.KitCode.ToLower().Contains(searchLower)) ||
                (kc.CheckedOutToUserName != null && kc.CheckedOutToUserName.ToLower().Contains(searchLower)) ||
                (kc.ProjectReference != null && kc.ProjectReference.ToLower().Contains(searchLower)));
        }

        if (status.HasValue)
            query = query.Where(kc => kc.Status == status.Value);

        if (kitId.HasValue)
            query = query.Where(kc => kc.KitId == kitId.Value);

        if (userId.HasValue)
            query = query.Where(kc => kc.CheckedOutToUserId == userId.Value);

        return query;
    }

    #endregion

    #region Status Queries

    public async Task<IEnumerable<KitCheckout>> GetActiveCheckoutsAsync(int companyId)
    {
        return await _context.KitCheckouts
            .Include(kc => kc.Kit)
            .Where(kc => kc.CompanyId == companyId &&
                (kc.Status == CheckoutStatus.CheckedOut || kc.Status == CheckoutStatus.PartialReturn || kc.Status == CheckoutStatus.Overdue))
            .OrderByDescending(kc => kc.CheckoutDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<KitCheckout>> GetOverdueCheckoutsAsync(int companyId)
    {
        var now = DateTime.UtcNow;
        return await _context.KitCheckouts
            .Include(kc => kc.Kit)
            .Where(kc => kc.CompanyId == companyId &&
                (kc.Status == CheckoutStatus.CheckedOut || kc.Status == CheckoutStatus.PartialReturn) &&
                kc.ExpectedReturnDate < now)
            .OrderBy(kc => kc.ExpectedReturnDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<KitCheckout>> GetPendingCheckoutsAsync(int companyId)
    {
        return await _context.KitCheckouts
            .Include(kc => kc.Kit)
            .Where(kc => kc.CompanyId == companyId && kc.Status == CheckoutStatus.Pending)
            .OrderByDescending(kc => kc.CreatedDate)
            .ToListAsync();
    }

    public async Task<KitCheckout?> GetCurrentCheckoutAsync(int kitId)
    {
        return await _context.KitCheckouts
            .Include(kc => kc.CheckoutItems)
            .Where(kc => kc.KitId == kitId &&
                (kc.Status == CheckoutStatus.Pending ||
                 kc.Status == CheckoutStatus.CheckedOut ||
                 kc.Status == CheckoutStatus.PartialReturn ||
                 kc.Status == CheckoutStatus.Overdue))
            .OrderByDescending(kc => kc.CheckoutDate)
            .FirstOrDefaultAsync();
    }

    public async Task MarkOverdueCheckoutsAsync(int companyId)
    {
        var now = DateTime.UtcNow;
        var overdueCheckouts = await _context.KitCheckouts
            .Where(kc => kc.CompanyId == companyId &&
                kc.Status == CheckoutStatus.CheckedOut &&
                kc.ExpectedReturnDate < now)
            .ToListAsync();

        foreach (var checkout in overdueCheckouts)
        {
            checkout.Status = CheckoutStatus.Overdue;
            checkout.LastModified = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        if (overdueCheckouts.Any())
        {
            _logger.LogInformation("Marked {Count} checkouts as overdue for company {CompanyId}",
                overdueCheckouts.Count, companyId);
        }
    }

    #endregion

    #region Extension

    public async Task<KitCheckout> ExtendCheckoutAsync(int checkoutId, DateTime newExpectedReturnDate,
        string? reason = null, int? modifiedByUserId = null)
    {
        var checkout = await GetByIdAsync(checkoutId);
        if (checkout == null)
            throw new InvalidOperationException($"Checkout with ID {checkoutId} not found");

        if (checkout.Status != CheckoutStatus.CheckedOut && checkout.Status != CheckoutStatus.PartialReturn && checkout.Status != CheckoutStatus.Overdue)
            throw new InvalidOperationException($"Cannot extend checkout with status: {checkout.Status}");

        if (newExpectedReturnDate <= checkout.ExpectedReturnDate)
            throw new InvalidOperationException("New expected return date must be after the current expected return date");

        var oldDate = checkout.ExpectedReturnDate;
        checkout.ExpectedReturnDate = newExpectedReturnDate;

        // If was overdue and extension is valid, change back to checked out
        if (checkout.Status == CheckoutStatus.Overdue && newExpectedReturnDate > DateTime.UtcNow)
        {
            checkout.Status = CheckoutStatus.CheckedOut;
        }

        checkout.CheckoutNotes = $"{checkout.CheckoutNotes}\n[Extended from {oldDate:yyyy-MM-dd} to {newExpectedReturnDate:yyyy-MM-dd}. Reason: {reason ?? "Not provided"}]";
        checkout.LastModified = DateTime.UtcNow;
        checkout.LastModifiedByUserId = modifiedByUserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Extended checkout {CheckoutId} to {NewDate}. Reason: {Reason}",
            checkoutId, newExpectedReturnDate, reason ?? "Not provided");

        return checkout;
    }

    #endregion

    #region History

    public async Task<IEnumerable<KitCheckout>> GetKitCheckoutHistoryAsync(int kitId, int? limit = null)
    {
        var query = _context.KitCheckouts
            .Include(kc => kc.CheckoutItems)
            .Where(kc => kc.KitId == kitId)
            .OrderByDescending(kc => kc.CheckoutDate);

        if (limit.HasValue)
            query = (IOrderedQueryable<KitCheckout>)query.Take(limit.Value);

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<KitCheckout>> GetUserCheckoutHistoryAsync(int userId, int companyId, int? limit = null)
    {
        var query = _context.KitCheckouts
            .Include(kc => kc.Kit)
            .Where(kc => kc.CompanyId == companyId && kc.CheckedOutToUserId == userId)
            .OrderByDescending(kc => kc.CheckoutDate);

        if (limit.HasValue)
            query = (IOrderedQueryable<KitCheckout>)query.Take(limit.Value);

        return await query.ToListAsync();
    }

    #endregion

    #region Item Condition Tracking

    public async Task<KitCheckoutItem?> GetCheckoutItemAsync(int checkoutId, int kitItemId)
    {
        return await _context.KitCheckoutItems
            .Include(ci => ci.Equipment)
            .FirstOrDefaultAsync(ci => ci.KitCheckoutId == checkoutId && ci.KitItemId == kitItemId);
    }

    public async Task<IEnumerable<KitCheckoutItem>> GetCheckoutItemsAsync(int checkoutId)
    {
        return await _context.KitCheckoutItems
            .Include(ci => ci.Equipment)
            .Where(ci => ci.KitCheckoutId == checkoutId)
            .ToListAsync();
    }

    public async Task<KitCheckoutItem> UpdateItemConditionAsync(int checkoutId, int kitItemId,
        EquipmentCondition condition, string? notes = null)
    {
        var checkoutItem = await GetCheckoutItemAsync(checkoutId, kitItemId);
        if (checkoutItem == null)
            throw new InvalidOperationException($"Checkout item not found for checkout {checkoutId}, kit item {kitItemId}");

        checkoutItem.ReturnCondition = condition;
        checkoutItem.ReturnNotes = notes;

        await _context.SaveChangesAsync();

        return checkoutItem;
    }

    public async Task<KitCheckoutItem> ReportDamageAsync(int checkoutId, int kitItemId,
        string damageDescription, int? reportedByUserId = null)
    {
        var checkoutItem = await GetCheckoutItemAsync(checkoutId, kitItemId);
        if (checkoutItem == null)
            throw new InvalidOperationException($"Checkout item not found for checkout {checkoutId}, kit item {kitItemId}");

        checkoutItem.DamageReported = true;
        checkoutItem.DamageDescription = damageDescription;
        checkoutItem.ReturnCondition = EquipmentCondition.Damaged;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Reported damage for checkout {CheckoutId}, kit item {KitItemId}. Description: {Description}",
            checkoutId, kitItemId, damageDescription);

        return checkoutItem;
    }

    public async Task<IEnumerable<KitCheckoutItem>> GetDamagedItemsAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.KitCheckoutItems
            .Include(ci => ci.KitCheckout)
            .Include(ci => ci.Equipment)
            .Where(ci => ci.KitCheckout!.CompanyId == companyId && ci.DamageReported);

        if (fromDate.HasValue)
            query = query.Where(ci => ci.KitCheckout!.CheckoutDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(ci => ci.KitCheckout!.CheckoutDate <= toDate.Value);

        return await query.OrderByDescending(ci => ci.KitCheckout!.CheckoutDate).ToListAsync();
    }

    #endregion

    #region Statistics

    public async Task<CheckoutStatisticsDto> GetStatisticsAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.KitCheckouts.Where(kc => kc.CompanyId == companyId);

        if (fromDate.HasValue)
            query = query.Where(kc => kc.CheckoutDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(kc => kc.CheckoutDate <= toDate.Value);

        var checkouts = await query.ToListAsync();

        var completedCheckouts = checkouts.Where(kc => kc.Status == CheckoutStatus.Returned).ToList();
        var averageCheckoutDays = completedCheckouts.Any()
            ? completedCheckouts.Average(kc => (kc.ActualReturnDate!.Value - kc.CheckoutDate).TotalDays)
            : 0;

        var damageCount = await _context.KitCheckoutItems
            .Include(ci => ci.KitCheckout)
            .Where(ci => ci.KitCheckout!.CompanyId == companyId && ci.DamageReported)
            .CountAsync();

        return new CheckoutStatisticsDto
        {
            TotalCheckouts = checkouts.Count,
            ActiveCheckouts = checkouts.Count(kc => kc.Status == CheckoutStatus.CheckedOut || kc.Status == CheckoutStatus.PartialReturn),
            CompletedCheckouts = completedCheckouts.Count,
            OverdueCheckouts = checkouts.Count(kc => kc.Status == CheckoutStatus.Overdue),
            CancelledCheckouts = checkouts.Count(kc => kc.Status == CheckoutStatus.Cancelled),
            AverageCheckoutDays = Math.Round(averageCheckoutDays, 1),
            TotalDamageReports = damageCount,
            CheckoutsByUser = checkouts.GroupBy(kc => kc.CheckedOutToUserId).ToDictionary(g => g.Key, g => g.Count()),
            CheckoutsByKit = checkouts.GroupBy(kc => kc.KitId).ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<int> GetActiveCheckoutCountAsync(int companyId)
    {
        return await _context.KitCheckouts
            .CountAsync(kc => kc.CompanyId == companyId &&
                (kc.Status == CheckoutStatus.CheckedOut || kc.Status == CheckoutStatus.PartialReturn));
    }

    public async Task<int> GetOverdueCheckoutCountAsync(int companyId)
    {
        return await _context.KitCheckouts
            .CountAsync(kc => kc.CompanyId == companyId && kc.Status == CheckoutStatus.Overdue);
    }

    public async Task<double> GetAverageCheckoutDaysAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.KitCheckouts
            .Where(kc => kc.CompanyId == companyId && kc.Status == CheckoutStatus.Returned && kc.ActualReturnDate.HasValue);

        if (fromDate.HasValue)
            query = query.Where(kc => kc.CheckoutDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(kc => kc.CheckoutDate <= toDate.Value);

        var checkouts = await query.ToListAsync();

        if (!checkouts.Any())
            return 0;

        return Math.Round(checkouts.Average(kc => (kc.ActualReturnDate!.Value - kc.CheckoutDate).TotalDays), 1);
    }

    #endregion
}
