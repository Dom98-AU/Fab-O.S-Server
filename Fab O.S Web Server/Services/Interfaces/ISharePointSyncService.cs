namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service for synchronizing existing takeoffs, revisions, and packages with SharePoint folder structure
/// </summary>
public interface ISharePointSyncService
{
    /// <summary>
    /// Synchronizes all existing takeoffs and their packages with SharePoint
    /// Creates missing folder hierarchies for takeoffs that existed before SharePoint integration
    /// </summary>
    Task<SharePointSyncResult> SyncAllTakeoffsAsync();

    /// <summary>
    /// Synchronizes a specific takeoff and all its packages with SharePoint
    /// </summary>
    Task<SharePointSyncResult> SyncTakeoffAsync(int takeoffId);

    /// <summary>
    /// Synchronizes a specific package with SharePoint
    /// </summary>
    Task<bool> SyncPackageAsync(int packageId);
}

/// <summary>
/// Result of SharePoint synchronization operation
/// </summary>
public class SharePointSyncResult
{
    public bool Success { get; set; }
    public int TakeoffsProcessed { get; set; }
    public int RevisionsProcessed { get; set; }
    public int PackagesProcessed { get; set; }
    public int FoldersCreated { get; set; }
    public int FoldersAlreadyExisted { get; set; }
    public int Errors { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
    public List<string> CreatedFolders { get; set; } = new();
    public TimeSpan Duration { get; set; }

    public string GetSummary()
    {
        return $@"SharePoint Sync Complete
Takeoffs: {TakeoffsProcessed}
Revisions: {RevisionsProcessed}
Packages: {PackagesProcessed}
Folders Created: {FoldersCreated}
Folders Already Existed: {FoldersAlreadyExisted}
Errors: {Errors}
Duration: {Duration.TotalSeconds:F2} seconds";
    }
}
