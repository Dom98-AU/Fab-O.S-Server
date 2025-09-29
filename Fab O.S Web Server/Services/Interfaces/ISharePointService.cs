using Microsoft.Graph.Models;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Main interface for SharePoint operations
/// </summary>
public interface ISharePointService
{
    /// <summary>
    /// Checks if SharePoint is configured and connected
    /// </summary>
    Task<SharePointConnectionStatus> GetConnectionStatusAsync();

    /// <summary>
    /// Validates and saves SharePoint configuration
    /// </summary>
    Task<bool> ConfigureSharePointAsync(string tenantId, string clientId, string clientSecret, string siteUrl);

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