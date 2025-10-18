using FabOS.WebServer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FabOS.WebServer.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class SharePointSyncController : ControllerBase
{
    private readonly ISharePointSyncService _syncService;
    private readonly ILogger<SharePointSyncController> _logger;

    public SharePointSyncController(
        ISharePointSyncService syncService,
        ILogger<SharePointSyncController> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    /// <summary>
    /// Synchronizes all existing takeoffs with SharePoint folder structure
    /// Creates missing folders for takeoffs that existed before SharePoint integration
    /// </summary>
    /// <returns>Sync result with summary statistics</returns>
    [HttpPost("sync-all")]
    public async Task<ActionResult<SharePointSyncResult>> SyncAllTakeoffs()
    {
        try
        {
            _logger.LogInformation("API: Starting SharePoint sync for all takeoffs");

            var result = await _syncService.SyncAllTakeoffsAsync();

            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("API: SharePoint sync completed successfully");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error during SharePoint sync");
            return StatusCode(500, new
            {
                error = "Failed to sync SharePoint folders",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Synchronizes a specific takeoff with SharePoint
    /// </summary>
    /// <param name="takeoffId">The ID of the takeoff to sync</param>
    [HttpPost("sync-takeoff/{takeoffId}")]
    public async Task<ActionResult<SharePointSyncResult>> SyncTakeoff(int takeoffId)
    {
        try
        {
            _logger.LogInformation("API: Starting SharePoint sync for takeoff {TakeoffId}", takeoffId);

            var result = await _syncService.SyncTakeoffAsync(takeoffId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error syncing takeoff {TakeoffId}", takeoffId);
            return StatusCode(500, new
            {
                error = $"Failed to sync takeoff {takeoffId}",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Synchronizes a specific package with SharePoint
    /// </summary>
    /// <param name="packageId">The ID of the package to sync</param>
    [HttpPost("sync-package/{packageId}")]
    public async Task<ActionResult<bool>> SyncPackage(int packageId)
    {
        try
        {
            _logger.LogInformation("API: Starting SharePoint sync for package {PackageId}", packageId);

            var success = await _syncService.SyncPackageAsync(packageId);

            if (!success)
            {
                return NotFound(new { error = $"Package {packageId} not found or sync failed" });
            }

            return Ok(new { success = true, message = $"Package {packageId} synced successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error syncing package {PackageId}", packageId);
            return StatusCode(500, new
            {
                error = $"Failed to sync package {packageId}",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets the status of SharePoint synchronization
    /// Returns information about what would be synced without actually syncing
    /// </summary>
    [HttpGet("sync-status")]
    public async Task<ActionResult<object>> GetSyncStatus()
    {
        try
        {
            // This would be implemented to return info about what needs syncing
            // For now, return a simple message
            return Ok(new
            {
                message = "Sync status endpoint - to be implemented",
                endpoints = new
                {
                    syncAll = "/api/sharepointsync/sync-all",
                    syncTakeoff = "/api/sharepointsync/sync-takeoff/{takeoffId}",
                    syncPackage = "/api/sharepointsync/sync-package/{packageId}"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error getting sync status");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
