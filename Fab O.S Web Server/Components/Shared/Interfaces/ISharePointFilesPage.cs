using Microsoft.AspNetCore.Components;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Components.Shared.Interfaces;

/// <summary>
/// Interface for SharePoint file browser pages with folder navigation capabilities
/// </summary>
public interface ISharePointFilesPage<TFileInfo> : IToolbarActionProvider
{
    /// <summary>
    /// Current folder path being browsed
    /// </summary>
    string CurrentFolderPath { get; }

    /// <summary>
    /// List of currently selected files (not folders)
    /// </summary>
    List<TFileInfo> SelectedFiles { get; }

    /// <summary>
    /// Whether any files are currently selected
    /// </summary>
    bool HasSelectedFiles { get; }

    /// <summary>
    /// Whether currently in root folder
    /// </summary>
    bool IsAtRoot { get; }

    /// <summary>
    /// Navigate into a subfolder
    /// </summary>
    Task NavigateToFolderAsync(string folderPath);

    /// <summary>
    /// Navigate up one level in folder hierarchy
    /// </summary>
    Task NavigateUpAsync();

    /// <summary>
    /// Navigate to root folder
    /// </summary>
    Task NavigateToRootAsync();

    /// <summary>
    /// Refresh current folder contents
    /// </summary>
    Task RefreshAsync();

    /// <summary>
    /// Create a new folder in current location
    /// </summary>
    Task CreateFolderAsync(string folderName);

    /// <summary>
    /// Upload files to current folder
    /// </summary>
    Task UploadFilesAsync();

    /// <summary>
    /// Delete selected files
    /// </summary>
    Task DeleteSelectedFilesAsync();

    /// <summary>
    /// Clear file selection
    /// </summary>
    void ClearSelection();

    /// <summary>
    /// Select or deselect a file
    /// </summary>
    void ToggleFileSelection(TFileInfo file);

    /// <summary>
    /// Get breadcrumb path for current location
    /// </summary>
    List<SharePointBreadcrumb> GetBreadcrumbs();
}

/// <summary>
/// SharePoint folder browsing state
/// </summary>
public class SharePointFolderState
{
    public string CurrentPath { get; set; } = string.Empty;
    public List<SharePointFolderInfo> Folders { get; set; } = new();
    public List<SharePointFileInfo> Files { get; set; } = new();
    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Breadcrumb item for folder navigation
/// </summary>
public class SharePointBreadcrumb
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsRoot { get; set; }
    public bool IsCurrent { get; set; }
}

/// <summary>
/// SharePoint file browser view options
/// </summary>
public enum SharePointViewType
{
    Explorer,   // File manager style with icons
    Grid,       // Card-based grid layout
    List,       // Compact table view
    Tiles       // Large thumbnail view
}