using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace FabOS.WebServer.Services.Implementations;

/// <summary>
/// Service for synchronizing existing takeoffs with SharePoint folder structure
/// </summary>
public class SharePointSyncService : ISharePointSyncService
{
    private readonly ApplicationDbContext _context;
    private readonly ISharePointService _sharePointService;
    private readonly ILogger<SharePointSyncService> _logger;

    public SharePointSyncService(
        ApplicationDbContext context,
        ISharePointService sharePointService,
        ILogger<SharePointSyncService> logger)
    {
        _context = context;
        _sharePointService = sharePointService;
        _logger = logger;
    }

    public async Task<SharePointSyncResult> SyncAllTakeoffsAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new SharePointSyncResult { Success = true };

        try
        {
            _logger.LogInformation("Starting SharePoint sync for all takeoffs");

            // Check if SharePoint is configured
            var connectionStatus = await _sharePointService.GetConnectionStatusAsync();
            if (!connectionStatus.IsConnected)
            {
                result.Success = false;
                result.ErrorMessages.Add($"SharePoint not connected: {connectionStatus.ErrorMessage}");
                return result;
            }

            // Get all active takeoffs with their revisions and packages
            var takeoffs = await _context.TraceDrawings
                .Include(t => t.Revisions.Where(r => !r.IsDeleted))
                    .ThenInclude(r => r.Packages.Where(p => !p.IsDeleted))
                .Where(t => !t.IsDeleted && !string.IsNullOrEmpty(t.TakeoffNumber))
                .ToListAsync();

            _logger.LogInformation("Found {Count} takeoffs to sync", takeoffs.Count);

            foreach (var takeoff in takeoffs)
            {
                try
                {
                    var takeoffResult = await SyncTakeoffInternalAsync(takeoff);
                    result.TakeoffsProcessed++;
                    result.RevisionsProcessed += takeoffResult.RevisionsProcessed;
                    result.PackagesProcessed += takeoffResult.PackagesProcessed;
                    result.FoldersCreated += takeoffResult.FoldersCreated;
                    result.FoldersAlreadyExisted += takeoffResult.FoldersAlreadyExisted;
                    result.Errors += takeoffResult.Errors;
                    result.ErrorMessages.AddRange(takeoffResult.ErrorMessages);
                    result.CreatedFolders.AddRange(takeoffResult.CreatedFolders);
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    var errorMsg = $"Error syncing takeoff {takeoff.TakeoffNumber}: {ex.Message}";
                    result.ErrorMessages.Add(errorMsg);
                    _logger.LogError(ex, errorMsg);
                }
            }

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation("SharePoint sync completed. {Summary}", result.GetSummary());
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessages.Add($"Fatal error during sync: {ex.Message}");
            _logger.LogError(ex, "Fatal error during SharePoint sync");
        }

        return result;
    }

    public async Task<SharePointSyncResult> SyncTakeoffAsync(int takeoffId)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new SharePointSyncResult { Success = true };

        try
        {
            var takeoff = await _context.TraceDrawings
                .Include(t => t.Revisions.Where(r => !r.IsDeleted))
                    .ThenInclude(r => r.Packages.Where(p => !p.IsDeleted))
                .FirstOrDefaultAsync(t => t.Id == takeoffId && !t.IsDeleted);

            if (takeoff == null)
            {
                result.Success = false;
                result.ErrorMessages.Add($"Takeoff {takeoffId} not found");
                return result;
            }

            if (string.IsNullOrEmpty(takeoff.TakeoffNumber))
            {
                result.Success = false;
                result.ErrorMessages.Add($"Takeoff {takeoffId} has no takeoff number");
                return result;
            }

            result = await SyncTakeoffInternalAsync(takeoff);
            result.TakeoffsProcessed = 1;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessages.Add($"Error syncing takeoff: {ex.Message}");
            _logger.LogError(ex, "Error syncing takeoff {TakeoffId}", takeoffId);
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        return result;
    }

    public async Task<bool> SyncPackageAsync(int packageId)
    {
        try
        {
            var package = await _context.Packages
                .Include(p => p.Revision)
                    .ThenInclude(r => r!.Takeoff)
                .FirstOrDefaultAsync(p => p.Id == packageId && !p.IsDeleted);

            if (package == null || package.Revision?.Takeoff == null)
            {
                _logger.LogWarning("Package {PackageId} not found or missing revision/takeoff", packageId);
                return false;
            }

            var takeoffNumber = package.Revision.Takeoff.TakeoffNumber;
            var revisionCode = package.Revision.RevisionCode;
            var packageNumber = package.PackageNumber;

            if (string.IsNullOrEmpty(takeoffNumber))
            {
                _logger.LogWarning("Package {PackageId} has no takeoff number", packageId);
                return false;
            }

            // Check if folder already exists
            var exists = await _sharePointService.PackageFolderExistsAsync(
                takeoffNumber, revisionCode, packageNumber);

            if (exists)
            {
                _logger.LogInformation(
                    "Package folder already exists: {TakeoffNumber}/{RevisionCode}/PKG-{PackageNumber}",
                    takeoffNumber, revisionCode, packageNumber);
                return true;
            }

            // Create the folder
            await _sharePointService.EnsurePackageFolderExistsAsync(
                takeoffNumber, revisionCode, packageNumber);

            _logger.LogInformation(
                "Created package folder: {TakeoffNumber}/{RevisionCode}/PKG-{PackageNumber}",
                takeoffNumber, revisionCode, packageNumber);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing package {PackageId}", packageId);
            return false;
        }
    }

    // Internal helper to sync a single takeoff entity
    private async Task<SharePointSyncResult> SyncTakeoffInternalAsync(Models.Entities.Takeoff takeoff)
    {
        var result = new SharePointSyncResult { Success = true };

        try
        {
            foreach (var revision in takeoff.Revisions.Where(r => !r.IsDeleted))
            {
                result.RevisionsProcessed++;

                // Ensure takeoff/revision folder exists
                try
                {
                    var folderExists = await _sharePointService.TakeoffFolderExistsAsync(takeoff.TakeoffNumber!);

                    if (!folderExists)
                    {
                        await _sharePointService.EnsureTakeoffFolderExistsAsync(
                            takeoff.TakeoffNumber!, revision.RevisionCode);

                        var folderPath = $"Takeoffs/{takeoff.TakeoffNumber}/{revision.RevisionCode}";
                        result.FoldersCreated++;
                        result.CreatedFolders.Add(folderPath);

                        _logger.LogInformation("Created takeoff/revision folder: {FolderPath}", folderPath);
                    }
                    else
                    {
                        result.FoldersAlreadyExisted++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    var errorMsg = $"Error creating folder for {takeoff.TakeoffNumber}/{revision.RevisionCode}: {ex.Message}";
                    result.ErrorMessages.Add(errorMsg);
                    _logger.LogError(ex, errorMsg);
                    continue; // Skip to next revision
                }

                // Process packages
                foreach (var package in revision.Packages.Where(p => !p.IsDeleted))
                {
                    result.PackagesProcessed++;

                    try
                    {
                        var packageExists = await _sharePointService.PackageFolderExistsAsync(
                            takeoff.TakeoffNumber!, revision.RevisionCode, package.PackageNumber);

                        if (!packageExists)
                        {
                            await _sharePointService.EnsurePackageFolderExistsAsync(
                                takeoff.TakeoffNumber!, revision.RevisionCode, package.PackageNumber);

                            var folderPath = $"Takeoffs/{takeoff.TakeoffNumber}/{revision.RevisionCode}/PKG-{package.PackageNumber}";
                            result.FoldersCreated++;
                            result.CreatedFolders.Add(folderPath);

                            _logger.LogInformation("Created package folder: {FolderPath}", folderPath);
                        }
                        else
                        {
                            result.FoldersAlreadyExisted++;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors++;
                        var errorMsg = $"Error creating folder for package {package.PackageNumber}: {ex.Message}";
                        result.ErrorMessages.Add(errorMsg);
                        _logger.LogError(ex, errorMsg);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors++;
            var errorMsg = $"Error processing takeoff {takeoff.TakeoffNumber}: {ex.Message}";
            result.ErrorMessages.Add(errorMsg);
            _logger.LogError(ex, errorMsg);
        }

        return result;
    }
}
