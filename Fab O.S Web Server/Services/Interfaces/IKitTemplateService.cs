using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service interface for Kit Template management
/// </summary>
public interface IKitTemplateService
{
    #region CRUD Operations

    Task<KitTemplate?> GetByIdAsync(int id);
    Task<KitTemplate?> GetByCodeAsync(string templateCode, int companyId);
    Task<IEnumerable<KitTemplate>> GetAllAsync(int companyId, bool includeDeleted = false);
    Task<IEnumerable<KitTemplate>> GetPagedAsync(int companyId, int page, int pageSize, string? search = null, string? category = null, bool? isActive = null);
    Task<int> GetCountAsync(int companyId, string? search = null, string? category = null, bool? isActive = null);
    Task<KitTemplate> CreateAsync(KitTemplate template, int companyId, int? createdByUserId = null);
    Task<KitTemplate> UpdateAsync(KitTemplate template, int? modifiedByUserId = null);
    Task<bool> DeleteAsync(int id, bool hardDelete = false);
    Task<bool> ExistsAsync(int id);

    #endregion

    #region Template Items

    Task<KitTemplate?> GetWithItemsAsync(int id);
    Task<IEnumerable<KitTemplateItem>> GetTemplateItemsAsync(int templateId);
    Task<KitTemplateItem> AddTemplateItemAsync(int templateId, KitTemplateItem item);
    Task<bool> RemoveTemplateItemAsync(int templateId, int itemId);
    Task<KitTemplateItem> UpdateTemplateItemAsync(KitTemplateItem item);
    Task<bool> ReorderTemplateItemsAsync(int templateId, List<int> itemIds);

    #endregion

    #region Categories

    Task<IEnumerable<string>> GetCategoriesAsync(int companyId);
    Task<Dictionary<string, int>> GetTemplateCategoryCountsAsync(int companyId);

    #endregion

    #region Activation

    Task<KitTemplate> ActivateAsync(int id, int? modifiedByUserId = null);
    Task<KitTemplate> DeactivateAsync(int id, int? modifiedByUserId = null);
    Task<IEnumerable<KitTemplate>> GetActiveTemplatesAsync(int companyId);

    #endregion

    #region Dashboard

    Task<int> GetTotalCountAsync(int companyId);
    Task<int> GetActiveCountAsync(int companyId);
    Task<Dictionary<string, int>> GetTemplatesByCategory(int companyId);

    #endregion
}
