using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service interface for Equipment Category and Type management
/// </summary>
public interface IEquipmentCategoryService
{
    #region Category CRUD Operations

    Task<EquipmentCategory?> GetCategoryByIdAsync(int id);
    Task<EquipmentCategory?> GetCategoryByNameAsync(string name, int companyId);
    Task<IEnumerable<EquipmentCategory>> GetAllCategoriesAsync(int companyId, bool includeInactive = false);
    Task<IEnumerable<EquipmentCategory>> GetCategoriesWithTypesAsync(int companyId, bool includeInactive = false);
    Task<EquipmentCategory> CreateCategoryAsync(EquipmentCategory category, int companyId);
    Task<EquipmentCategory> UpdateCategoryAsync(EquipmentCategory category);
    Task<bool> DeleteCategoryAsync(int id);
    Task<bool> CategoryExistsAsync(int id);
    Task<bool> CategoryNameExistsAsync(string name, int companyId, int? excludeId = null);

    #endregion

    #region Category Status Operations

    Task<EquipmentCategory> ActivateCategoryAsync(int id);
    Task<EquipmentCategory> DeactivateCategoryAsync(int id);
    Task<IEnumerable<EquipmentCategory>> GetActiveCategoriesAsync(int companyId);
    Task<IEnumerable<EquipmentCategory>> GetSystemCategoriesAsync(int companyId);
    Task<IEnumerable<EquipmentCategory>> GetCustomCategoriesAsync(int companyId);

    #endregion

    #region Category Statistics

    Task<int> GetEquipmentCountForCategoryAsync(int categoryId);
    Task<int> GetTypeCountForCategoryAsync(int categoryId);
    Task<Dictionary<int, int>> GetEquipmentCountsByCategoryAsync(int companyId);

    #endregion

    #region Type CRUD Operations

    Task<EquipmentType?> GetTypeByIdAsync(int id);
    Task<EquipmentType?> GetTypeByNameAsync(string name, int categoryId);
    Task<IEnumerable<EquipmentType>> GetAllTypesAsync(int companyId, bool includeInactive = false);
    Task<IEnumerable<EquipmentType>> GetTypesByCategoryAsync(int categoryId, bool includeInactive = false);
    Task<EquipmentType> CreateTypeAsync(EquipmentType type);
    Task<EquipmentType> UpdateTypeAsync(EquipmentType type);
    Task<bool> DeleteTypeAsync(int id);
    Task<bool> TypeExistsAsync(int id);
    Task<bool> TypeNameExistsAsync(string name, int categoryId, int? excludeId = null);

    #endregion

    #region Type Status Operations

    Task<EquipmentType> ActivateTypeAsync(int id);
    Task<EquipmentType> DeactivateTypeAsync(int id);
    Task<IEnumerable<EquipmentType>> GetActiveTypesAsync(int companyId);
    Task<IEnumerable<EquipmentType>> GetActiveTypesByCategoryAsync(int categoryId);
    Task<IEnumerable<EquipmentType>> GetSystemTypesAsync(int companyId);
    Task<IEnumerable<EquipmentType>> GetCustomTypesAsync(int companyId);

    #endregion

    #region Type Statistics

    Task<int> GetEquipmentCountForTypeAsync(int typeId);
    Task<Dictionary<int, int>> GetEquipmentCountsByTypeAsync(int companyId);

    #endregion

    #region Display Order Operations

    Task UpdateCategoryDisplayOrderAsync(int categoryId, int newOrder);
    Task UpdateTypeDisplayOrderAsync(int typeId, int newOrder);
    Task ReorderCategoriesAsync(int companyId, IEnumerable<(int Id, int NewOrder)> newOrders);
    Task ReorderTypesAsync(int categoryId, IEnumerable<(int Id, int NewOrder)> newOrders);

    #endregion

    #region Seed Data

    Task SeedDefaultCategoriesAsync(int companyId);
    Task<bool> HasDefaultCategoriesAsync(int companyId);

    #endregion
}
