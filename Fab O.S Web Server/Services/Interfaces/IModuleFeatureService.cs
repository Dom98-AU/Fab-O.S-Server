using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service for checking module availability and feature gates in the multi-tenant platform.
/// Uses the ProductLicense table to determine which modules are enabled for the current tenant.
/// </summary>
public interface IModuleFeatureService
{
    /// <summary>
    /// Checks if a specific product module is enabled for the current tenant.
    /// </summary>
    /// <param name="productName">Module name: "Estimate", "Trace", "FabMate", or "QDocs"</param>
    /// <returns>True if the module is active and not expired</returns>
    Task<bool> IsModuleEnabledAsync(string productName);

    /// <summary>
    /// Checks if QDocs module is enabled for the current tenant.
    /// </summary>
    Task<bool> IsQDocsEnabledAsync();

    /// <summary>
    /// Checks if FabMate module is enabled for the current tenant.
    /// </summary>
    Task<bool> IsFabMateEnabledAsync();

    /// <summary>
    /// Checks if Trace module is enabled for the current tenant.
    /// </summary>
    Task<bool> IsTraceEnabledAsync();

    /// <summary>
    /// Checks if Estimate module is enabled for the current tenant.
    /// </summary>
    Task<bool> IsEstimateEnabledAsync();

    /// <summary>
    /// Checks if a specific feature is enabled within a product module.
    /// Features are stored in the ProductLicense.Features JSON array.
    /// </summary>
    /// <param name="productName">Module name</param>
    /// <param name="featureName">Feature identifier (e.g., "ITP", "time-tracking")</param>
    /// <returns>True if the feature is enabled in the module's Features JSON</returns>
    Task<bool> IsFeatureEnabledAsync(string productName, string featureName);

    /// <summary>
    /// Checks if ITP (Inspection Test Plan) features are available.
    /// ITP requires BOTH FabMate AND QDocs modules to be enabled,
    /// plus the "ITP" feature flag in QDocs license.
    /// </summary>
    /// <returns>True if both modules are active and ITP feature is enabled</returns>
    Task<bool> AreITPFeaturesAvailableAsync();

    /// <summary>
    /// Gets the ProductLicense record for a specific module.
    /// </summary>
    /// <param name="productName">Module name</param>
    /// <returns>ProductLicense entity or null if not found</returns>
    Task<ProductLicense?> GetProductLicenseAsync(string productName);

    /// <summary>
    /// Gets a list of all enabled module names for the current tenant.
    /// </summary>
    /// <returns>List of enabled module names (e.g., ["Trace", "FabMate"])</returns>
    Task<List<string>> GetEnabledModulesAsync();

    /// <summary>
    /// Gets a list of all enabled features for a specific module.
    /// </summary>
    /// <param name="productName">Module name</param>
    /// <returns>List of feature identifiers from the Features JSON array</returns>
    Task<List<string>> GetEnabledFeaturesAsync(string productName);
}
