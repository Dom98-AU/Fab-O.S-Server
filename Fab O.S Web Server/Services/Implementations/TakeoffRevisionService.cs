using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FabOS.WebServer.Services.Implementations;

public class TakeoffRevisionService : ITakeoffRevisionService
{
    private readonly ApplicationDbContext _context;
    private readonly ISharePointService _sharePointService;
    private readonly ILogger<TakeoffRevisionService> _logger;

    public TakeoffRevisionService(
        ApplicationDbContext context,
        ISharePointService sharePointService,
        ILogger<TakeoffRevisionService> logger)
    {
        _context = context;
        _sharePointService = sharePointService;
        _logger = logger;
    }

    public async Task<TakeoffRevision> CreateRevisionAsync(int takeoffId, string? description = null, int? userId = null)
    {
        var takeoff = await _context.TraceDrawings.FindAsync(takeoffId);
        if (takeoff == null)
        {
            throw new ArgumentException($"Takeoff with ID {takeoffId} not found");
        }

        var revisionCode = await GetNextRevisionCodeAsync(takeoffId);

        var revision = new TakeoffRevision
        {
            TakeoffId = takeoffId,
            RevisionCode = revisionCode,
            IsActive = true,  // New revisions are active by default
            Description = description ?? $"Revision {revisionCode}",
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            IsDeleted = false
        };

        // Deactivate all other revisions for this takeoff
        var existingRevisions = await _context.TakeoffRevisions
            .Where(r => r.TakeoffId == takeoffId && !r.IsDeleted)
            .ToListAsync();

        foreach (var existing in existingRevisions)
        {
            existing.IsActive = false;
            existing.LastModified = DateTime.UtcNow;
        }

        _context.TakeoffRevisions.Add(revision);
        await _context.SaveChangesAsync();

        // Create SharePoint folder for this revision
        try
        {
            if (!string.IsNullOrEmpty(takeoff.TakeoffNumber))
            {
                await _sharePointService.CreateRevisionFolderAsync(takeoff.TakeoffNumber, revisionCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create SharePoint folder for revision {RevisionCode} of takeoff {TakeoffId}", revisionCode, takeoffId);
        }

        _logger.LogInformation("Created revision {RevisionCode} for takeoff {TakeoffId}", revisionCode, takeoffId);
        return revision;
    }

    public async Task<TakeoffRevision?> GetActiveRevisionAsync(int takeoffId)
    {
        return await _context.TakeoffRevisions
            .Include(r => r.Packages)
            .FirstOrDefaultAsync(r => r.TakeoffId == takeoffId && r.IsActive && !r.IsDeleted);
    }

    public async Task<List<TakeoffRevision>> GetRevisionsByTakeoffAsync(int takeoffId)
    {
        return await _context.TakeoffRevisions
            .Include(r => r.Packages)
            .Where(r => r.TakeoffId == takeoffId && !r.IsDeleted)
            .OrderBy(r => r.RevisionCode)
            .ToListAsync();
    }

    public async Task<TakeoffRevision?> GetRevisionByIdAsync(int revisionId)
    {
        return await _context.TakeoffRevisions
            .Include(r => r.Takeoff)
            .Include(r => r.Packages)
            .Include(r => r.CreatedByUser)
            .FirstOrDefaultAsync(r => r.Id == revisionId && !r.IsDeleted);
    }

    public async Task<bool> SetActiveRevisionAsync(int revisionId)
    {
        var revision = await _context.TakeoffRevisions.FindAsync(revisionId);
        if (revision == null || revision.IsDeleted)
        {
            return false;
        }

        // Deactivate all other revisions for this takeoff
        var otherRevisions = await _context.TakeoffRevisions
            .Where(r => r.TakeoffId == revision.TakeoffId && r.Id != revisionId && !r.IsDeleted)
            .ToListAsync();

        foreach (var other in otherRevisions)
        {
            other.IsActive = false;
            other.LastModified = DateTime.UtcNow;
        }

        revision.IsActive = true;
        revision.LastModified = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Set revision {RevisionId} as active for takeoff {TakeoffId}", revisionId, revision.TakeoffId);
        return true;
    }

    public async Task<TakeoffRevision> CopyRevisionAsync(int sourceRevisionId, string? newDescription = null, int? userId = null)
    {
        var sourceRevision = await _context.TakeoffRevisions
            .Include(r => r.Takeoff)
            .Include(r => r.Packages)
                .ThenInclude(p => p.PackageDrawings)
            .FirstOrDefaultAsync(r => r.Id == sourceRevisionId && !r.IsDeleted);

        if (sourceRevision == null)
        {
            throw new ArgumentException($"Source revision with ID {sourceRevisionId} not found");
        }

        var newRevisionCode = await GetNextRevisionCodeAsync(sourceRevision.TakeoffId);

        // Create new revision
        var newRevision = new TakeoffRevision
        {
            TakeoffId = sourceRevision.TakeoffId,
            RevisionCode = newRevisionCode,
            IsActive = true,  // New revision becomes active
            Description = newDescription ?? $"Copied from Revision {sourceRevision.RevisionCode}",
            CopiedFromRevisionId = sourceRevisionId,
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            IsDeleted = false
        };

        // Deactivate source and other revisions
        sourceRevision.IsActive = false;
        sourceRevision.LastModified = DateTime.UtcNow;

        var otherRevisions = await _context.TakeoffRevisions
            .Where(r => r.TakeoffId == sourceRevision.TakeoffId && r.Id != sourceRevisionId && !r.IsDeleted)
            .ToListAsync();

        foreach (var other in otherRevisions)
        {
            other.IsActive = false;
            other.LastModified = DateTime.UtcNow;
        }

        _context.TakeoffRevisions.Add(newRevision);
        await _context.SaveChangesAsync();

        // Copy packages
        foreach (var sourcePackage in sourceRevision.Packages.Where(p => !p.IsDeleted))
        {
            var newPackage = new Package
            {
                ProjectId = sourcePackage.ProjectId,
                OrderId = sourcePackage.OrderId,
                RevisionId = newRevision.Id,  // Link to new revision
                PackageSource = sourcePackage.PackageSource,
                PackageNumber = sourcePackage.PackageNumber + "-" + newRevisionCode,  // Append revision to number
                PackageName = sourcePackage.PackageName,
                Description = sourcePackage.Description,
                Status = sourcePackage.Status,
                StartDate = sourcePackage.StartDate,
                EndDate = sourcePackage.EndDate,
                EstimatedHours = sourcePackage.EstimatedHours,
                EstimatedCost = sourcePackage.EstimatedCost,
                ActualHours = 0,  // Reset actuals for new revision
                ActualCost = 0,
                CreatedBy = userId,
                LastModifiedBy = userId,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                IsDeleted = false,
                LaborRatePerHour = sourcePackage.LaborRatePerHour,
                ProcessingEfficiency = sourcePackage.ProcessingEfficiency,
                EfficiencyRateId = sourcePackage.EfficiencyRateId,
                RoutingId = sourcePackage.RoutingId
            };

            _context.Packages.Add(newPackage);
        }

        await _context.SaveChangesAsync();

        // Copy SharePoint files
        try
        {
            if (!string.IsNullOrEmpty(sourceRevision.Takeoff.TakeoffNumber))
            {
                await CopyRevisionFilesAsync(
                    sourceRevision.Takeoff.TakeoffNumber,
                    sourceRevision.RevisionCode,
                    newRevisionCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy SharePoint files from revision {SourceRevision} to {NewRevision}",
                sourceRevision.RevisionCode, newRevisionCode);
        }

        _logger.LogInformation("Copied revision {SourceId} to new revision {NewId} with code {RevisionCode}",
            sourceRevisionId, newRevision.Id, newRevisionCode);

        return newRevision;
    }

    public async Task<bool> DeleteRevisionAsync(int revisionId)
    {
        var revision = await _context.TakeoffRevisions.FindAsync(revisionId);
        if (revision == null || revision.IsDeleted)
        {
            return false;
        }

        if (revision.IsActive)
        {
            throw new InvalidOperationException("Cannot delete the active revision. Please set another revision as active first.");
        }

        revision.IsDeleted = true;
        revision.LastModified = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Deleted revision {RevisionId}", revisionId);
        return true;
    }

    public async Task<string> GetNextRevisionCodeAsync(int takeoffId)
    {
        var existingRevisions = await _context.TakeoffRevisions
            .Where(r => r.TakeoffId == takeoffId && !r.IsDeleted)
            .OrderBy(r => r.RevisionCode)
            .Select(r => r.RevisionCode)
            .ToListAsync();

        if (!existingRevisions.Any())
        {
            return "A";
        }

        var lastCode = existingRevisions.Last();
        return GetNextCode(lastCode);
    }

    public async Task<TakeoffRevision> GetOrCreateActiveRevisionAsync(int takeoffId, int? userId = null)
    {
        var activeRevision = await GetActiveRevisionAsync(takeoffId);
        if (activeRevision != null)
        {
            return activeRevision;
        }

        // Create initial revision A
        return await CreateRevisionAsync(takeoffId, "Initial revision", userId);
    }

    // Helper method to generate next revision code (A -> B -> C -> ... -> Z -> AA -> AB -> ...)
    private static string GetNextCode(string currentCode)
    {
        if (string.IsNullOrEmpty(currentCode))
        {
            return "A";
        }

        var chars = currentCode.ToCharArray();
        for (int i = chars.Length - 1; i >= 0; i--)
        {
            if (chars[i] < 'Z')
            {
                chars[i]++;
                return new string(chars);
            }
            chars[i] = 'A';
        }

        // If we've rolled over all characters, add a new one
        return new string('A', chars.Length + 1);
    }

    // Helper method to copy SharePoint files between revisions
    private async Task CopyRevisionFilesAsync(string takeoffNumber, string fromRevision, string toRevision)
    {
        try
        {
            // Get the source revision folder path
            var sourceFolderPath = $"Takeoffs/{takeoffNumber}/{fromRevision}";
            var targetFolderPath = $"Takeoffs/{takeoffNumber}/{toRevision}";

            // Get contents of source revision folder (which contains package folders)
            var sourceFolderContents = await _sharePointService.GetFolderContentsAsync(sourceFolderPath);

            // Iterate through each package folder in the source revision
            foreach (var packageFolder in sourceFolderContents.Folders)
            {
                // Package folders are named like "PKG-001", "PKG-002", etc.
                if (packageFolder.Name.StartsWith("PKG-", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // Get the package number (e.g., "PKG-001" -> "001")
                        var packageNumber = packageFolder.Name;

                        // Ensure the package folder exists in the target revision
                        await _sharePointService.EnsurePackageFolderExistsAsync(takeoffNumber, toRevision, packageNumber.Replace("PKG-", ""));

                        // Get files from the source package folder
                        var packageFolderPath = $"{sourceFolderPath}/{packageNumber}";
                        var packageContents = await _sharePointService.GetFolderContentsAsync(packageFolderPath);

                        // Copy each file from source package to target package
                        foreach (var file in packageContents.Files)
                        {
                            try
                            {
                                // Download file from source
                                var fileStream = await _sharePointService.DownloadFileAsync(file.Id);

                                // Upload to new package folder in target revision
                                var targetPackagePath = $"{targetFolderPath}/{packageNumber}";
                                await _sharePointService.UploadMultipleFilesAsync(
                                    targetPackagePath,
                                    new List<(Stream stream, string fileName, string contentType)>
                                    {
                                        (fileStream, file.Name, file.ContentType ?? "application/octet-stream")
                                    }
                                );

                                _logger.LogInformation(
                                    "Copied file {FileName} from package {PackageFolder} in revision {FromRevision} to revision {ToRevision}",
                                    file.Name, packageNumber, fromRevision, toRevision);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex,
                                    "Failed to copy file {FileName} from package {PackageFolder} in revision {FromRevision} to {ToRevision}",
                                    file.Name, packageNumber, fromRevision, toRevision);
                            }
                        }

                        _logger.LogInformation(
                            "Completed copying {FileCount} files from package {PackageFolder} (revision {FromRevision} to {ToRevision})",
                            packageContents.Files.Count, packageNumber, fromRevision, toRevision);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to copy package folder {PackageFolder} from revision {FromRevision} to {ToRevision}",
                            packageFolder.Name, fromRevision, toRevision);
                    }
                }
            }

            _logger.LogInformation(
                "Completed copying all package files from revision {FromRevision} to {ToRevision} for takeoff {TakeoffNumber}",
                fromRevision, toRevision, takeoffNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to copy SharePoint files from revision {FromRevision} to {ToRevision} for takeoff {TakeoffNumber}",
                fromRevision, toRevision, takeoffNumber);
            throw;
        }
    }
}
