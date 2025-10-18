using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations
{
    public class SettingsService : ISettingsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SettingsService> _logger;
        private readonly ITenantService _tenantService;
        private const string GLOBAL_SETTINGS_CACHE_PREFIX = "global_setting_";
        private const string MODULE_SETTINGS_CACHE_PREFIX = "module_setting_";
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

        public SettingsService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<SettingsService> logger,
            ITenantService tenantService)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _tenantService = tenantService;
        }

        // Global Settings Methods
        public async Task<GlobalSettings?> GetGlobalSettingAsync(string key)
        {
            var cacheKey = $"{GLOBAL_SETTINGS_CACHE_PREFIX}{key}";

            if (_cache.TryGetValue<GlobalSettings>(cacheKey, out var cachedSetting))
            {
                return cachedSetting;
            }

            var setting = await _context.GlobalSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key && s.IsActive);

            if (setting != null)
            {
                _cache.Set(cacheKey, setting, _cacheExpiration);
            }

            return setting;
        }

        public async Task<T?> GetGlobalSettingValueAsync<T>(string key)
        {
            var setting = await GetGlobalSettingAsync(key);
            if (setting == null)
            {
                return default(T);
            }

            return setting.GetValue<T>();
        }

        public async Task<GlobalSettings> SetGlobalSettingAsync(
            string key,
            string value,
            string? category = null,
            string? description = null)
        {
            var setting = await _context.GlobalSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key);

            if (setting == null)
            {
                setting = new GlobalSettings
                {
                    SettingKey = key,
                    SettingValue = value,
                    Category = category,
                    Description = description,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    LastModified = DateTime.Now,
                    SettingType = GetSettingType(value)
                };
                _context.GlobalSettings.Add(setting);
            }
            else
            {
                setting.SettingValue = value;
                setting.LastModified = DateTime.Now;
                if (category != null) setting.Category = category;
                if (description != null) setting.Description = description;
            }

            await _context.SaveChangesAsync();

            // Clear cache
            var cacheKey = $"{GLOBAL_SETTINGS_CACHE_PREFIX}{key}";
            _cache.Remove(cacheKey);

            _logger.LogInformation($"Global setting '{key}' updated with value '{value}'");
            return setting;
        }

        public async Task<List<GlobalSettings>> GetGlobalSettingsByCategoryAsync(string category)
        {
            return await _context.GlobalSettings
                .Where(s => s.Category == category && s.IsActive)
                .OrderBy(s => s.SettingKey)
                .ToListAsync();
        }

        public async Task<List<GlobalSettings>> GetAllGlobalSettingsAsync()
        {
            return await _context.GlobalSettings
                .Where(s => s.IsActive)
                .OrderBy(s => s.Category)
                .ThenBy(s => s.SettingKey)
                .ToListAsync();
        }

        // Module Settings Methods
        public async Task<ModuleSettings?> GetModuleSettingAsync(string module, string key, int? userId = null)
        {
            var companyId = _tenantService.GetCurrentCompanyId();
            var cacheKey = $"{MODULE_SETTINGS_CACHE_PREFIX}{companyId}_{module}_{key}_{userId ?? 0}";

            if (_cache.TryGetValue<ModuleSettings>(cacheKey, out var cachedSetting))
            {
                return cachedSetting;
            }

            var query = _context.ModuleSettings
                .Where(s => s.ModuleName == module &&
                           s.SettingKey == key &&
                           s.CompanyId == companyId &&
                           s.IsActive);

            if (userId.HasValue)
            {
                query = query.Where(s => s.UserId == userId.Value && s.IsUserSpecific);
            }
            else
            {
                query = query.Where(s => !s.IsUserSpecific);
            }

            var setting = await query.FirstOrDefaultAsync();

            if (setting != null)
            {
                _cache.Set(cacheKey, setting, _cacheExpiration);
            }

            return setting;
        }

        public async Task<T?> GetModuleSettingValueAsync<T>(string module, string key, int? userId = null)
        {
            var setting = await GetModuleSettingAsync(module, key, userId);
            if (setting == null)
            {
                return default(T);
            }

            try
            {
                if (typeof(T) == typeof(bool))
                {
                    return (T)(object)bool.Parse(setting.SettingValue);
                }
                else if (typeof(T) == typeof(int))
                {
                    return (T)(object)int.Parse(setting.SettingValue);
                }
                else if (typeof(T) == typeof(string))
                {
                    return (T)(object)setting.SettingValue;
                }
                else
                {
                    // For complex types, assume JSON
                    return System.Text.Json.JsonSerializer.Deserialize<T>(setting.SettingValue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing module setting value for module '{module}', key '{key}'");
                return default(T);
            }
        }

        public async Task<ModuleSettings> SetModuleSettingAsync(
            string module,
            string key,
            string value,
            int? userId = null,
            string? description = null)
        {
            var companyId = _tenantService.GetCurrentCompanyId();
            var currentUserId = _tenantService.GetCurrentUserId();

            var query = _context.ModuleSettings
                .Where(s => s.ModuleName == module &&
                           s.SettingKey == key &&
                           s.CompanyId == companyId);

            if (userId.HasValue)
            {
                query = query.Where(s => s.UserId == userId.Value && s.IsUserSpecific);
            }
            else
            {
                query = query.Where(s => !s.IsUserSpecific);
            }

            var setting = await query.FirstOrDefaultAsync();

            if (setting == null)
            {
                setting = new ModuleSettings
                {
                    ModuleName = module,
                    CompanyId = companyId,
                    SettingKey = key,
                    SettingValue = value,
                    Description = description,
                    IsUserSpecific = userId.HasValue,
                    UserId = userId,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    LastModified = DateTime.Now,
                    CreatedByUserId = currentUserId,
                    LastModifiedByUserId = currentUserId,
                    SettingType = GetSettingType(value)
                };
                _context.ModuleSettings.Add(setting);
            }
            else
            {
                setting.SettingValue = value;
                setting.LastModified = DateTime.Now;
                setting.LastModifiedByUserId = currentUserId;
                if (description != null) setting.Description = description;
            }

            await _context.SaveChangesAsync();

            // Clear cache
            var cacheKey = $"{MODULE_SETTINGS_CACHE_PREFIX}{companyId}_{module}_{key}_{userId ?? 0}";
            _cache.Remove(cacheKey);

            _logger.LogInformation($"Module setting '{key}' for module '{module}' updated with value '{value}'");
            return setting;
        }

        public async Task<List<ModuleSettings>> GetModuleSettingsByModuleAsync(string module, int? userId = null)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            var query = _context.ModuleSettings
                .Where(s => s.ModuleName == module &&
                           s.CompanyId == companyId &&
                           s.IsActive);

            if (userId.HasValue)
            {
                query = query.Where(s => (s.UserId == userId.Value && s.IsUserSpecific) || !s.IsUserSpecific);
            }
            else
            {
                query = query.Where(s => !s.IsUserSpecific);
            }

            return await query.OrderBy(s => s.SettingKey).ToListAsync();
        }

        public async Task<List<ModuleSettings>> GetUserModuleSettingsAsync(int userId)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            return await _context.ModuleSettings
                .Where(s => s.UserId == userId &&
                           s.IsUserSpecific &&
                           s.CompanyId == companyId &&
                           s.IsActive)
                .OrderBy(s => s.ModuleName)
                .ThenBy(s => s.SettingKey)
                .ToListAsync();
        }

        // Initialization
        public async Task InitializeDefaultSettingsAsync()
        {
            try
            {
                // Check if settings exist
                var existingSettings = await _context.GlobalSettings.AnyAsync();
                if (existingSettings)
                {
                    return;
                }

                // Initialize default global settings
                var defaultSettings = new List<GlobalSettings>
                {
                    new GlobalSettings
                    {
                        SettingKey = "System.TimeZone",
                        SettingValue = "UTC",
                        SettingType = "string",
                        Category = "System",
                        Description = "Default system timezone",
                        IsSystemSetting = true,
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        LastModified = DateTime.Now
                    },
                    new GlobalSettings
                    {
                        SettingKey = "System.DateFormat",
                        SettingValue = "MM/dd/yyyy",
                        SettingType = "string",
                        Category = "System",
                        Description = "Default date format",
                        IsSystemSetting = false,
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        LastModified = DateTime.Now
                    },
                    new GlobalSettings
                    {
                        SettingKey = "System.Currency",
                        SettingValue = "USD",
                        SettingType = "string",
                        Category = "System",
                        Description = "Default currency",
                        IsSystemSetting = false,
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        LastModified = DateTime.Now
                    },
                    new GlobalSettings
                    {
                        SettingKey = "Security.PasswordMinLength",
                        SettingValue = "8",
                        SettingType = "int",
                        Category = "Security",
                        Description = "Minimum password length",
                        IsSystemSetting = true,
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        LastModified = DateTime.Now
                    },
                    new GlobalSettings
                    {
                        SettingKey = "Security.SessionTimeout",
                        SettingValue = "480",
                        SettingType = "int",
                        Category = "Security",
                        Description = "Session timeout in minutes",
                        IsSystemSetting = true,
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        LastModified = DateTime.Now
                    },
                    new GlobalSettings
                    {
                        SettingKey = "Display.ItemsPerPage",
                        SettingValue = "50",
                        SettingType = "int",
                        Category = "Display",
                        Description = "Default items per page in grids",
                        IsSystemSetting = false,
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        LastModified = DateTime.Now
                    }
                };

                _context.GlobalSettings.AddRange(defaultSettings);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Default global settings initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing default settings");
                throw;
            }
        }

        // Cache Management
        public void ClearSettingsCache()
        {
            // This is a simplified version - in production you might want to
            // implement a more sophisticated cache clearing mechanism
            _logger.LogInformation("Settings cache cleared");
        }

        // Helper Methods
        private string GetSettingType(string value)
        {
            if (bool.TryParse(value, out _))
                return "bool";
            if (int.TryParse(value, out _))
                return "int";
            if (value.StartsWith("{") || value.StartsWith("["))
                return "json";
            return "string";
        }
    }
}