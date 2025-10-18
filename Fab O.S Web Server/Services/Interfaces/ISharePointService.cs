using Microsoft.Graph.Models;
using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Main interface for SharePoint operations
/// </summary>
public interface ISharePointService
{
    /// <summary>
    /// Checks if SharePoint is configured and connected for the current tenant
    /// </summary>
    Task<SharePointConnectionStatus> GetConnectionStatusAsync();

    /// <summary>
    /// Gets SharePoint settings for a specific tenant (for admin purposes)
    /// </summary>
    Task<CompanySharePointSettings?> GetSettingsForTenantAsync(int companyId);

    /// <summary>
    /// Checks if the current tenant has SharePoint configured
    /// </summary>
    Task<bool> IsTenantConfiguredAsync();

    /// <summary>
    /// Validates and saves SharePoint configuration for the current tenant
    /// </summary>
    Task<bool> ConfigureSharePointAsync(string tenantId, string clientId, string clientSecret, string siteUrl);

    /// <summary>
    /// Checks if the document library exists (cannot create - requires SharePoint admin)
    /// </summary>
    Task<DocumentLibraryCheckResult> CheckDocumentLibraryAsync();

    /// <summary>
    /// Ensures the document library exists, creates if it doesn't
    /// </summary>
    [Obsolete("Use CheckDocumentLibraryAsync instead - libraries cannot be created programmatically")]
    Task<bool> EnsureDocumentLibraryExistsAsync();

    /// <summary>
    /// Ensures takeoff folder structure exists (lazy creation), creates if it doesn't
    /// </summary>
    Task<SharePointFolderInfo> EnsureTakeoffFolderExistsAsync(string takeoffNumber, string revisionCode = "A");

    /// <summary>
    /// Checks if a folder exists for a takeoff
    /// </summary>
    Task<bool> TakeoffFolderExistsAsync(string takeoffNumber);

    /// <summary>
    /// Creates the folder structure for a new takeoff
    /// </summary>
    Task<SharePointFolderInfo> CreateTakeoffFolderAsync(string takeoffNumber, string revisionCode = "A");

    /// <summary>
    /// Gets folder information for a takeoff
    /// </summary>
    Task<SharePointFolderInfo?> GetTakeoffFolderAsync(string takeoffNumber);

    /// <summary>
    /// Lists all files in a takeoff folder
    /// </summary>
    Task<List<SharePointFileInfo>> GetTakeoffFilesAsync(string takeoffNumber);

    /// <summary>
    /// Uploads a file to a takeoff folder
    /// </summary>
    Task<SharePointFileInfo> UploadFileAsync(string takeoffNumber, Stream fileStream, string fileName, string contentType);

    /// <summary>
    /// Downloads a file from SharePoint
    /// </summary>
    Task<Stream> DownloadFileAsync(string driveItemId);

    /// <summary>
    /// Deletes a file from SharePoint
    /// </summary>
    Task<bool> DeleteFileAsync(string driveItemId);

    /// <summary>
    /// Creates a new revision folder
    /// </summary>
    Task<SharePointFolderInfo> CreateRevisionFolderAsync(string takeoffNumber, string revisionCode);

    /// <summary>
    /// Gets SharePoint web URL for a file (for preview)
    /// </summary>
    Task<string> GetFileWebUrlAsync(string driveItemId);

    /// <summary>
    /// Gets mixed folder and file contents for a given path
    /// </summary>
    Task<SharePointFolderContents> GetFolderContentsAsync(string folderPath);

    /// <summary>
    /// Creates a new folder at the specified path
    /// </summary>
    Task<SharePointFolderInfo> CreateFolderAsync(string parentPath, string folderName);

    /// <summary>
    /// Gets the full folder path hierarchy for breadcrumb navigation
    /// </summary>
    Task<List<SharePointBreadcrumbItem>> GetFolderBreadcrumbsAsync(string folderPath);

    /// <summary>
    /// Uploads multiple files to a specific folder path
    /// </summary>
    Task<List<SharePointFileInfo>> UploadMultipleFilesAsync(string folderPath, List<(Stream stream, string fileName, string contentType)> files);

    /// <summary>
    /// Deletes multiple files by their drive item IDs
    /// </summary>
    Task<bool> DeleteMultipleFilesAsync(List<string> driveItemIds);

    /// <summary>
    /// Gets the complete folder path for a package within the SharePoint hierarchy
    /// </summary>
    /// <param name="takeoffNumber">The takeoff number (e.g., "TK-2025-001")</param>
    /// <param name="revisionCode">The revision code (e.g., "A", "B")</param>
    /// <param name="packageNumber">The package number (e.g., "PKG-001")</param>
    /// <returns>Full folder path: Takeoffs/{TakeoffNumber}/{RevisionCode}/PKG-{PackageNumber}</returns>
    Task<string> GetPackageFolderPathAsync(string takeoffNumber, string revisionCode, string packageNumber);

    /// <summary>
    /// Ensures the complete folder hierarchy exists for a package (Takeoff > Revision > Package)
    /// </summary>
    /// <param name="takeoffNumber">The takeoff number</param>
    /// <param name="revisionCode">The revision code</param>
    /// <param name="packageNumber">The package number</param>
    /// <returns>Folder information for the package folder</returns>
    Task<SharePointFolderInfo> EnsurePackageFolderExistsAsync(string takeoffNumber, string revisionCode, string packageNumber);

    /// <summary>
    /// Checks if a package folder exists in SharePoint
    /// </summary>
    Task<bool> PackageFolderExistsAsync(string takeoffNumber, string revisionCode, string packageNumber);
}

/// <summary>
/// SharePoint connection status information
/// </summary>
public class SharePointConnectionStatus
{
    public bool IsConfigured { get; set; }
    public bool IsConnected { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SiteName { get; set; }
    public string? LibraryName { get; set; }
}

/// <summary>
/// SharePoint folder information
/// </summary>
public class SharePointFolderInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string WebUrl { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public DateTime CreatedDateTime { get; set; }
    public string? RevisionCode { get; set; }
}

/// <summary>
/// SharePoint file information
/// </summary>
public class SharePointFileInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string WebUrl { get; set; } = string.Empty;
    public DateTime CreatedDateTime { get; set; }
    public DateTime LastModifiedDateTime { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string ModifiedBy { get; set; } = string.Empty;
    public string? ETag { get; set; }
    public string? DownloadUrl { get; set; }
}

/// <summary>
/// Mixed folder and file contents for folder browsing
/// </summary>
public class SharePointFolderContents
{
    public List<SharePointFolderInfo> Folders { get; set; } = new();
    public List<SharePointFileInfo> Files { get; set; } = new();
    public string CurrentPath { get; set; } = string.Empty;
    public string? ParentPath { get; set; }
    public bool IsRoot { get; set; }
}

/// <summary>
/// Breadcrumb item for folder navigation
/// </summary>
public class SharePointBreadcrumbItem
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsRoot { get; set; }
    public bool IsCurrent { get; set; }
}

/// <summary>
/// Result of document library existence check
/// </summary>
public class DocumentLibraryCheckResult
{
    public bool Exists { get; set; }
    public string Message { get; set; } = string.Empty;
    public string LibraryName { get; set; } = string.Empty;
    public bool CanCreate { get; set; } = false; // Always false - requires SharePoint admin
}