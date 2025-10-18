using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces
{
    public interface ISettingsService
    {
        // Global Settings
        Task<GlobalSettings?> GetGlobalSettingAsync(string key);
        Task<T?> GetGlobalSettingValueAsync<T>(string key);
        Task<GlobalSettings> SetGlobalSettingAsync(string key, string value, string? category = null, string? description = null);
        Task<List<GlobalSettings>> GetGlobalSettingsByCategoryAsync(string category);
        Task<List<GlobalSettings>> GetAllGlobalSettingsAsync();

        // Module Settings
        Task<ModuleSettings?> GetModuleSettingAsync(string module, string key, int? userId = null);
        Task<T?> GetModuleSettingValueAsync<T>(string module, string key, int? userId = null);
        Task<ModuleSettings> SetModuleSettingAsync(string module, string key, string value, int? userId = null, string? description = null);
        Task<List<ModuleSettings>> GetModuleSettingsByModuleAsync(string module, int? userId = null);
        Task<List<ModuleSettings>> GetUserModuleSettingsAsync(int userId);

        // Initialization
        Task InitializeDefaultSettingsAsync();

        // Cache Management
        void ClearSettingsCache();
    }
}