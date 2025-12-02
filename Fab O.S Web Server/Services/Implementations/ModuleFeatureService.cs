using System.Text.Json;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Services.Implementations;

/// <summary>
/// Implementation of module feature gating service.
/// Queries ProductLicense table in tenant database to determine module availability and features.
/// </summary>
public class ModuleFeatureService : IModuleFeatureService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ModuleFeatureService> _logger;

    // TODO: Replace with actual tenant context service when multi-tenant auth is fully implemented
    // For now, we use CompanyId = 1 as a placeholder
    private const int PlaceholderCompanyId = 1;

    public ModuleFeatureService(
        ApplicationDbContext context,
        ILogger<ModuleFeatureService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> IsModuleEnabledAsync(string productName)
    {
        try
        {
            var license = await _context.ProductLicenses
                .FirstOrDefaultAsync(pl =>
                    pl.CompanyId == PlaceholderCompanyId &&
                    pl.ProductName == productName &&
                    pl.IsActive &&
                    pl.ValidFrom <= DateTime.UtcNow &&
                    pl.ValidUntil >= DateTime.UtcNow);

            return license != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if module {ProductName} is enabled", productName);
            return false;
        }
    }

    public Task<bool> IsQDocsEnabledAsync() => IsModuleEnabledAsync("QDocs");

    public Task<bool> IsFabMateEnabledAsync() => IsModuleEnabledAsync("FabMate");

    public Task<bool> IsTraceEnabledAsync() => IsModuleEnabledAsync("Trace");

    public Task<bool> IsEstimateEnabledAsync() => IsModuleEnabledAsync("Estimate");

    public async Task<bool> IsFeatureEnabledAsync(string productName, string featureName)
    {
        try
        {
            var license = await GetProductLicenseAsync(productName);
            if (license == null)
            {
                return false;
            }

            // Parse Features JSON array
            if (string.IsNullOrWhiteSpace(license.Features))
            {
                return false;
            }

            var features = JsonSerializer.Deserialize<List<string>>(license.Features);
            return features?.Contains(featureName, StringComparer.OrdinalIgnoreCase) ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if feature {FeatureName} is enabled in module {ProductName}",
                featureName, productName);
            return false;
        }
    }

    public async Task<bool> AreITPFeaturesAvailableAsync()
    {
        try
        {
            // ITP features require BOTH FabMate AND QDocs modules enabled
            var fabMateEnabled = await IsFabMateEnabledAsync();
            var qdocsEnabled = await IsQDocsEnabledAsync();

            if (!fabMateEnabled || !qdocsEnabled)
            {
                _logger.LogDebug("ITP features unavailable: FabMate={FabMate}, QDocs={QDocs}",
                    fabMateEnabled, qdocsEnabled);
                return false;
            }

            // Additionally check if ITP feature is enabled in QDocs license
            var itpFeatureEnabled = await IsFeatureEnabledAsync("QDocs", "ITP");

            _logger.LogDebug("ITP features availability: FabMate={FabMate}, QDocs={QDocs}, ITP Feature={ITPFeature}",
                fabMateEnabled, qdocsEnabled, itpFeatureEnabled);

            return itpFeatureEnabled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking ITP features availability");
            return false;
        }
    }

    public async Task<ProductLicense?> GetProductLicenseAsync(string productName)
    {
        try
        {
            return await _context.ProductLicenses
                .FirstOrDefaultAsync(pl =>
                    pl.CompanyId == PlaceholderCompanyId &&
                    pl.ProductName == productName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ProductLicense for {ProductName}", productName);
            return null;
        }
    }

    public async Task<List<string>> GetEnabledModulesAsync()
    {
        try
        {
            var enabledLicenses = await _context.ProductLicenses
                .Where(pl =>
                    pl.CompanyId == PlaceholderCompanyId &&
                    pl.IsActive &&
                    pl.ValidFrom <= DateTime.UtcNow &&
                    pl.ValidUntil >= DateTime.UtcNow)
                .Select(pl => pl.ProductName)
                .ToListAsync();

            return enabledLicenses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enabled modules");
            return new List<string>();
        }
    }

    public async Task<List<string>> GetEnabledFeaturesAsync(string productName)
    {
        try
        {
            var license = await GetProductLicenseAsync(productName);
            if (license == null || string.IsNullOrWhiteSpace(license.Features))
            {
                return new List<string>();
            }

            var features = JsonSerializer.Deserialize<List<string>>(license.Features);
            return features ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enabled features for {ProductName}", productName);
            return new List<string>();
        }
    }
}
