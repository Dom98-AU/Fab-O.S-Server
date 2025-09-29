using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Microsoft.Extensions.Options;
using FabOS.WebServer.Models.Configuration;
using FabOS.WebServer.Services.Interfaces;
using FabOS.WebServer.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Azure.Identity;

namespace FabOS.WebServer.Services.Implementations;

/// <summary>
/// Implementation of SharePoint operations using Microsoft Graph
/// </summary>
public class SharePointService : ISharePointService
{
    private readonly SharePointSettings _settings;
    private readonly ILogger<SharePointService> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private GraphServiceClient? _graphClient;

    public SharePointService(
        IOptions<SharePointSettings> settings,
        ILogger<SharePointService> logger,
        ApplicationDbContext dbContext,
        IConfiguration configuration)
    {
        _settings = settings.Value;
        _logger = logger;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    private async Task<GraphServiceClient> GetGraphClientAsync()
    {
        if (_graphClient != null)
            return _graphClient;

        if (!_settings.IsValid())
        {
            throw new InvalidOperationException("SharePoint is not properly configured");
        }

        var clientSecretCredential = new ClientSecretCredential(
            _settings.TenantId,
            _settings.ClientId,
            _settings.ClientSecret);

        _graphClient = new GraphServiceClient(clientSecretCredential);
        return _graphClient;
    }

    public async Task<SharePointConnectionStatus> GetConnectionStatusAsync()
    {
        var status = new SharePointConnectionStatus
        {
            IsConfigured = _settings.IsValid()
        };

        if (!status.IsConfigured)
        {
            status.ErrorMessage = "SharePoint is not configured. Please complete the setup.";
            return status;
        }

        if (_settings.UseMockData)
        {
            status.IsConnected = true;
            status.SiteName = "Mock SharePoint Site";
            status.LibraryName = _settings.DocumentLibrary;
            return status;
        }

        try
        {
            var client = await GetGraphClientAsync();
            var site = await client.Sites[_settings.GetSiteId()].GetAsync();

            status.IsConnected = site != null;
            status.SiteName = site?.DisplayName ?? site?.Name;
            status.LibraryName = _settings.DocumentLibrary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SharePoint");
            status.IsConnected = false;
            status.ErrorMessage = $"Connection failed: {ex.Message}";
        }

        return status;
    }

    public async Task<bool> ConfigureSharePointAsync(string tenantId, string clientId, string clientSecret, string siteUrl)
    {
        try
        {
            // Update configuration in memory
            _configuration["SharePoint:TenantId"] = tenantId;
            _configuration["SharePoint:ClientId"] = clientId;
            _configuration["SharePoint:ClientSecret"] = clientSecret;
            _configuration["SharePoint:SiteUrl"] = siteUrl;
            _configuration["SharePoint:IsEnabled"] = "true";

            // Test the connection
            var testSettings = new SharePointSettings
            {
                TenantId = tenantId,
                ClientId = clientId,
                ClientSecret = clientSecret,
                SiteUrl = siteUrl,
                IsEnabled = true
            };

            var clientSecretCredential = new ClientSecretCredential(
                tenantId,
                clientId,
                clientSecret);

            var testClient = new GraphServiceClient(clientSecretCredential);
            var site = await testClient.Sites[testSettings.GetSiteId()].GetAsync();

            if (site == null)
            {
                throw new Exception("Unable to access SharePoint site");
            }

            // Save to database for persistence
            // TODO: Implement secure storage in database

            _logger.LogInformation("SharePoint configured successfully for site: {SiteName}", site.DisplayName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure SharePoint");
            return false;
        }
    }

    public async Task<bool> TakeoffFolderExistsAsync(string takeoffNumber)
    {
        if (_settings.UseMockData)
        {
            // Mock: Return true for even-numbered takeoffs, false for odd
            return int.TryParse(takeoffNumber.Replace("T-", ""), out var num) && num % 2 == 0;
        }

        try
        {
            var client = await GetGraphClientAsync();
            var path = $"{_settings.TakeoffsRootFolder}/Takeoff-{takeoffNumber}";

            var drive = await GetDocumentLibraryDriveAsync(client);
            var folder = await client.Drives[drive.Id]
                .Root
                .ItemWithPath(path)
                .GetAsync();

            return folder != null;
        }
        catch (ServiceException ex) when (ex.ResponseStatusCode == 404)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if takeoff folder exists for {TakeoffNumber}", takeoffNumber);
            throw;
        }
    }

    public async Task<SharePointFolderInfo> CreateTakeoffFolderAsync(string takeoffNumber, string revisionCode = "A")
    {
        if (_settings.UseMockData)
        {
            return new SharePointFolderInfo
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Takeoff-{takeoffNumber}",
                Path = $"{_settings.TakeoffsRootFolder}/Takeoff-{takeoffNumber}/Rev-{revisionCode}",
                WebUrl = $"https://mock.sharepoint.com/sites/fabos/{_settings.TakeoffsRootFolder}/Takeoff-{takeoffNumber}",
                CreatedDateTime = DateTime.UtcNow,
                RevisionCode = revisionCode
            };
        }

        try
        {
            var client = await GetGraphClientAsync();
            var drive = await GetDocumentLibraryDriveAsync(client);

            // Create takeoff folder
            var takeoffFolderPath = $"{_settings.TakeoffsRootFolder}/Takeoff-{takeoffNumber}";
            var takeoffFolder = await CreateFolderAsync(client, drive.Id, takeoffFolderPath);

            // Create revision subfolder
            var revisionFolderPath = $"{takeoffFolderPath}/Rev-{revisionCode}";
            var revisionFolder = await CreateFolderAsync(client, drive.Id, revisionFolderPath);

            return new SharePointFolderInfo
            {
                Id = revisionFolder.Id ?? string.Empty,
                Name = revisionFolder.Name ?? string.Empty,
                Path = revisionFolderPath,
                WebUrl = revisionFolder.WebUrl ?? string.Empty,
                CreatedDateTime = revisionFolder.CreatedDateTime?.DateTime ?? DateTime.UtcNow,
                RevisionCode = revisionCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating takeoff folder for {TakeoffNumber}", takeoffNumber);
            throw;
        }
    }

    public async Task<SharePointFolderInfo?> GetTakeoffFolderAsync(string takeoffNumber)
    {
        if (_settings.UseMockData)
        {
            if (!await TakeoffFolderExistsAsync(takeoffNumber))
                return null;

            return new SharePointFolderInfo
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Takeoff-{takeoffNumber}",
                Path = $"{_settings.TakeoffsRootFolder}/Takeoff-{takeoffNumber}",
                WebUrl = $"https://mock.sharepoint.com/sites/fabos/{_settings.TakeoffsRootFolder}/Takeoff-{takeoffNumber}",
                CreatedDateTime = DateTime.UtcNow.AddDays(-5),
                RevisionCode = "A"
            };
        }

        try
        {
            var client = await GetGraphClientAsync();
            var path = $"{_settings.TakeoffsRootFolder}/Takeoff-{takeoffNumber}";

            var drive = await GetDocumentLibraryDriveAsync(client);
            var folder = await client.Drives[drive.Id]
                .Root
                .ItemWithPath(path)
                .GetAsync();

            if (folder == null)
                return null;

            // Get the latest revision folder
            var children = await client.Drives[drive.Id]
                .Items[folder.Id]
                .Children
                .GetAsync();

            var latestRevision = children?.Value?
                .Where(i => i.Folder != null && i.Name?.StartsWith("Rev-") == true)
                .OrderByDescending(i => i.Name)
                .FirstOrDefault();

            return new SharePointFolderInfo
            {
                Id = folder.Id ?? string.Empty,
                Name = folder.Name ?? string.Empty,
                Path = path,
                WebUrl = folder.WebUrl ?? string.Empty,
                CreatedDateTime = folder.CreatedDateTime?.DateTime ?? DateTime.UtcNow,
                RevisionCode = latestRevision?.Name?.Replace("Rev-", "")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting takeoff folder for {TakeoffNumber}", takeoffNumber);
            return null;
        }
    }

    public async Task<List<SharePointFileInfo>> GetTakeoffFilesAsync(string takeoffNumber)
    {
        if (_settings.UseMockData)
        {
            return new List<SharePointFileInfo>
            {
                new SharePointFileInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Drawing-001.pdf",
                    Size = 1024 * 1024 * 2,
                    ContentType = "application/pdf",
                    WebUrl = "https://mock.sharepoint.com/file1",
                    CreatedDateTime = DateTime.UtcNow.AddDays(-3),
                    LastModifiedDateTime = DateTime.UtcNow.AddDays(-1),
                    CreatedBy = "John Doe",
                    ModifiedBy = "Jane Smith"
                },
                new SharePointFileInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Specifications.docx",
                    Size = 1024 * 500,
                    ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    WebUrl = "https://mock.sharepoint.com/file2",
                    CreatedDateTime = DateTime.UtcNow.AddDays(-2),
                    LastModifiedDateTime = DateTime.UtcNow.AddHours(-5),
                    CreatedBy = "Alice Johnson",
                    ModifiedBy = "Bob Wilson"
                }
            };
        }

        try
        {
            var client = await GetGraphClientAsync();
            var drive = await GetDocumentLibraryDriveAsync(client);

            // Get all files from the latest revision folder
            var folderInfo = await GetTakeoffFolderAsync(takeoffNumber);
            if (folderInfo == null)
                return new List<SharePointFileInfo>();

            var revisionPath = $"{folderInfo.Path}/Rev-{folderInfo.RevisionCode ?? "A"}";
            var folder = await client.Drives[drive.Id]
                .Root
                .ItemWithPath(revisionPath)
                .GetAsync();

            if (folder == null)
                return new List<SharePointFileInfo>();

            var files = await client.Drives[drive.Id]
                .Items[folder.Id]
                .Children
                .GetAsync();

            var fileList = new List<SharePointFileInfo>();
            foreach (var item in files?.Value ?? new List<DriveItem>())
            {
                if (item.File != null)
                {
                    fileList.Add(new SharePointFileInfo
                    {
                        Id = item.Id ?? string.Empty,
                        Name = item.Name ?? string.Empty,
                        Size = item.Size ?? 0,
                        ContentType = item.File.MimeType ?? string.Empty,
                        WebUrl = item.WebUrl ?? string.Empty,
                        CreatedDateTime = item.CreatedDateTime?.DateTime ?? DateTime.UtcNow,
                        LastModifiedDateTime = item.LastModifiedDateTime?.DateTime ?? DateTime.UtcNow,
                        CreatedBy = item.CreatedBy?.User?.DisplayName ?? "Unknown",
                        ModifiedBy = item.LastModifiedBy?.User?.DisplayName ?? "Unknown",
                        ETag = item.ETag
                    });
                }
            }

            return fileList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting files for takeoff {TakeoffNumber}", takeoffNumber);
            return new List<SharePointFileInfo>();
        }
    }

    public async Task<SharePointFileInfo> UploadFileAsync(string takeoffNumber, Stream fileStream, string fileName, string contentType)
    {
        if (_settings.UseMockData)
        {
            return new SharePointFileInfo
            {
                Id = Guid.NewGuid().ToString(),
                Name = fileName,
                Size = fileStream.Length,
                ContentType = contentType,
                WebUrl = $"https://mock.sharepoint.com/files/{fileName}",
                CreatedDateTime = DateTime.UtcNow,
                LastModifiedDateTime = DateTime.UtcNow,
                CreatedBy = "Current User",
                ModifiedBy = "Current User"
            };
        }

        try
        {
            var client = await GetGraphClientAsync();
            var drive = await GetDocumentLibraryDriveAsync(client);

            // Get the folder path
            var folderInfo = await GetTakeoffFolderAsync(takeoffNumber);
            if (folderInfo == null)
            {
                // Create folder if it doesn't exist
                folderInfo = await CreateTakeoffFolderAsync(takeoffNumber);
            }

            var uploadPath = $"{folderInfo.Path}/Rev-{folderInfo.RevisionCode ?? "A"}/{fileName}";

            // Upload file
            DriveItem uploadedFile;
            if (fileStream.Length < 4 * 1024 * 1024) // Less than 4MB
            {
                uploadedFile = await client.Drives[drive.Id]
                    .Root
                    .ItemWithPath(uploadPath)
                    .Content
                    .PutAsync(fileStream);
            }
            else
            {
                // Use large file upload for files > 4MB
                uploadedFile = await UploadLargeFileAsync(client, drive.Id, uploadPath, fileStream);
            }

            return new SharePointFileInfo
            {
                Id = uploadedFile.Id ?? string.Empty,
                Name = uploadedFile.Name ?? string.Empty,
                Size = uploadedFile.Size ?? 0,
                ContentType = contentType,
                WebUrl = uploadedFile.WebUrl ?? string.Empty,
                CreatedDateTime = uploadedFile.CreatedDateTime?.DateTime ?? DateTime.UtcNow,
                LastModifiedDateTime = uploadedFile.LastModifiedDateTime?.DateTime ?? DateTime.UtcNow,
                CreatedBy = uploadedFile.CreatedBy?.User?.DisplayName ?? "Unknown",
                ModifiedBy = uploadedFile.LastModifiedBy?.User?.DisplayName ?? "Unknown",
                ETag = uploadedFile.ETag
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName} for takeoff {TakeoffNumber}", fileName, takeoffNumber);
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(string driveItemId)
    {
        if (_settings.UseMockData)
        {
            // Return mock file content
            var mockContent = Encoding.UTF8.GetBytes("This is mock file content for testing purposes.");
            return new MemoryStream(mockContent);
        }

        try
        {
            var client = await GetGraphClientAsync();
            var drive = await GetDocumentLibraryDriveAsync(client);

            var stream = await client.Drives[drive.Id]
                .Items[driveItemId]
                .Content
                .GetAsync();

            return stream ?? new MemoryStream();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {DriveItemId}", driveItemId);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string driveItemId)
    {
        if (_settings.UseMockData)
        {
            return true;
        }

        try
        {
            var client = await GetGraphClientAsync();
            var drive = await GetDocumentLibraryDriveAsync(client);

            await client.Drives[drive.Id]
                .Items[driveItemId]
                .DeleteAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {DriveItemId}", driveItemId);
            return false;
        }
    }

    public async Task<SharePointFolderInfo> CreateRevisionFolderAsync(string takeoffNumber, string revisionCode)
    {
        if (_settings.UseMockData)
        {
            return new SharePointFolderInfo
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Rev-{revisionCode}",
                Path = $"{_settings.TakeoffsRootFolder}/Takeoff-{takeoffNumber}/Rev-{revisionCode}",
                WebUrl = $"https://mock.sharepoint.com/sites/fabos/{_settings.TakeoffsRootFolder}/Takeoff-{takeoffNumber}/Rev-{revisionCode}",
                CreatedDateTime = DateTime.UtcNow,
                RevisionCode = revisionCode
            };
        }

        try
        {
            var client = await GetGraphClientAsync();
            var drive = await GetDocumentLibraryDriveAsync(client);

            var revisionFolderPath = $"{_settings.TakeoffsRootFolder}/Takeoff-{takeoffNumber}/Rev-{revisionCode}";
            var revisionFolder = await CreateFolderAsync(client, drive.Id, revisionFolderPath);

            return new SharePointFolderInfo
            {
                Id = revisionFolder.Id ?? string.Empty,
                Name = revisionFolder.Name ?? string.Empty,
                Path = revisionFolderPath,
                WebUrl = revisionFolder.WebUrl ?? string.Empty,
                CreatedDateTime = revisionFolder.CreatedDateTime?.DateTime ?? DateTime.UtcNow,
                RevisionCode = revisionCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating revision folder for {TakeoffNumber} Rev-{RevisionCode}", takeoffNumber, revisionCode);
            throw;
        }
    }

    public async Task<string> GetFileWebUrlAsync(string driveItemId)
    {
        if (_settings.UseMockData)
        {
            return $"https://mock.sharepoint.com/preview/{driveItemId}";
        }

        try
        {
            var client = await GetGraphClientAsync();
            var drive = await GetDocumentLibraryDriveAsync(client);

            var item = await client.Drives[drive.Id]
                .Items[driveItemId]
                .GetAsync();

            return item?.WebUrl ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting web URL for file {DriveItemId}", driveItemId);
            return string.Empty;
        }
    }

    private async Task<Drive> GetDocumentLibraryDriveAsync(GraphServiceClient client)
    {
        var site = await client.Sites[_settings.GetSiteId()].GetAsync();
        if (site?.Id == null)
        {
            throw new InvalidOperationException("Unable to get SharePoint site");
        }

        var drives = await client.Sites[site.Id].Drives.GetAsync();
        var drive = drives?.Value?.FirstOrDefault(d => d.Name == _settings.DocumentLibrary)
            ?? drives?.Value?.FirstOrDefault();

        if (drive == null)
        {
            throw new InvalidOperationException($"Document library '{_settings.DocumentLibrary}' not found");
        }

        return drive;
    }

    private async Task<DriveItem> CreateFolderAsync(GraphServiceClient client, string driveId, string folderPath)
    {
        var pathSegments = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        DriveItem? currentFolder = null;
        var currentPath = "";

        foreach (var segment in pathSegments)
        {
            currentPath = string.IsNullOrEmpty(currentPath) ? segment : $"{currentPath}/{segment}";

            try
            {
                currentFolder = await client.Drives[driveId]
                    .Root
                    .ItemWithPath(currentPath)
                    .GetAsync();
            }
            catch (ServiceException ex) when (ex.ResponseStatusCode == 404)
            {
                // Folder doesn't exist, create it
                var parentPath = currentPath.Contains('/')
                    ? currentPath.Substring(0, currentPath.LastIndexOf('/'))
                    : "";

                var newFolder = new DriveItem
                {
                    Name = segment,
                    Folder = new Folder()
                };

                if (string.IsNullOrEmpty(parentPath))
                {
                    var rootItem = await client.Drives[driveId].Root.GetAsync();
                    currentFolder = await client.Drives[driveId]
                        .Items[rootItem?.Id]
                        .Children
                        .PostAsync(newFolder);
                }
                else
                {
                    var parentFolder = await client.Drives[driveId]
                        .Root
                        .ItemWithPath(parentPath)
                        .GetAsync();

                    currentFolder = await client.Drives[driveId]
                        .Items[parentFolder?.Id]
                        .Children
                        .PostAsync(newFolder);
                }
            }
        }

        return currentFolder ?? throw new InvalidOperationException("Failed to create folder");
    }

    private async Task<DriveItem> UploadLargeFileAsync(GraphServiceClient client, string driveId, string path, Stream stream)
    {
        // Create upload session
        var uploadSession = await client.Drives[driveId]
            .Root
            .ItemWithPath(path)
            .CreateUploadSession
            .PostAsync(new Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession.CreateUploadSessionPostRequestBody());

        if (uploadSession?.UploadUrl == null)
        {
            throw new InvalidOperationException("Failed to create upload session");
        }

        // Upload file in chunks
        var maxChunkSize = 320 * 1024 * 10; // 3.2MB chunks
        var buffer = new byte[maxChunkSize];
        var bytesRead = 0;
        var offset = 0L;
        var size = stream.Length;

        using var httpClient = new HttpClient();

        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            using var content = new ByteArrayContent(buffer, 0, bytesRead);
            content.Headers.Add("Content-Range", $"bytes {offset}-{offset + bytesRead - 1}/{size}");

            var response = await httpClient.PutAsync(uploadSession.UploadUrl, content);
            response.EnsureSuccessStatusCode();

            offset += bytesRead;
        }

        // Get the uploaded file
        var uploadedFile = await client.Drives[driveId]
            .Root
            .ItemWithPath(path)
            .GetAsync();

        return uploadedFile ?? throw new InvalidOperationException("Failed to get uploaded file");
    }

    public async Task<SharePointFolderContents> GetFolderContentsAsync(string folderPath)
    {
        if (_settings.UseMockData)
        {
            // Return mock data for testing
            return new SharePointFolderContents
            {
                CurrentPath = folderPath,
                ParentPath = string.IsNullOrEmpty(folderPath) ? null : Path.GetDirectoryName(folderPath)?.Replace('\\', '/'),
                IsRoot = string.IsNullOrEmpty(folderPath) || folderPath == "/",
                Folders = new List<SharePointFolderInfo>
                {
                    new SharePointFolderInfo
                    {
                        Id = "mock-folder-1",
                        Name = "Subfolder1",
                        Path = Path.Combine(folderPath, "Subfolder1").Replace('\\', '/'),
                        WebUrl = "https://mock.sharepoint.com/folders/subfolder1",
                        CreatedDateTime = DateTime.UtcNow.AddDays(-10)
                    }
                },
                Files = new List<SharePointFileInfo>
                {
                    new SharePointFileInfo
                    {
                        Id = "mock-file-1",
                        Name = "Document.pdf",
                        Size = 1024000,
                        ContentType = "application/pdf",
                        WebUrl = "https://mock.sharepoint.com/files/document.pdf",
                        CreatedDateTime = DateTime.UtcNow.AddDays(-5),
                        LastModifiedDateTime = DateTime.UtcNow.AddDays(-2),
                        CreatedBy = "Mock User",
                        ModifiedBy = "Mock User"
                    }
                }
            };
        }

        try
        {
            var client = await GetGraphClientAsync();
            var drive = await GetDocumentLibraryDriveAsync(client);

            // Get the folder
            DriveItem? folder;
            if (string.IsNullOrEmpty(folderPath) || folderPath == "/")
            {
                folder = await client.Drives[drive.Id].Root.GetAsync();
            }
            else
            {
                folder = await client.Drives[drive.Id]
                    .Root
                    .ItemWithPath(folderPath)
                    .GetAsync();
            }

            if (folder == null)
            {
                throw new InvalidOperationException($"Folder not found: {folderPath}");
            }

            // Get children (folders and files)
            var children = await client.Drives[drive.Id]
                .Items[folder.Id]
                .Children
                .GetAsync();

            var contents = new SharePointFolderContents
            {
                CurrentPath = folderPath,
                ParentPath = string.IsNullOrEmpty(folderPath) ? null : Path.GetDirectoryName(folderPath)?.Replace('\\', '/'),
                IsRoot = string.IsNullOrEmpty(folderPath) || folderPath == "/"
            };

            if (children?.Value != null)
            {
                foreach (var item in children.Value)
                {
                    if (item.Folder != null)
                    {
                        contents.Folders.Add(new SharePointFolderInfo
                        {
                            Id = item.Id ?? string.Empty,
                            Name = item.Name ?? string.Empty,
                            Path = Path.Combine(folderPath, item.Name ?? string.Empty).Replace('\\', '/'),
                            WebUrl = item.WebUrl ?? string.Empty,
                            CreatedDateTime = item.CreatedDateTime?.DateTime ?? DateTime.MinValue
                        });
                    }
                    else if (item.File != null)
                    {
                        contents.Files.Add(new SharePointFileInfo
                        {
                            Id = item.Id ?? string.Empty,
                            Name = item.Name ?? string.Empty,
                            Size = item.Size ?? 0,
                            ContentType = item.File.MimeType ?? "application/octet-stream",
                            WebUrl = item.WebUrl ?? string.Empty,
                            CreatedDateTime = item.CreatedDateTime?.DateTime ?? DateTime.MinValue,
                            LastModifiedDateTime = item.LastModifiedDateTime?.DateTime ?? DateTime.MinValue,
                            CreatedBy = item.CreatedBy?.User?.DisplayName ?? "Unknown",
                            ModifiedBy = item.LastModifiedBy?.User?.DisplayName ?? "Unknown",
                            ETag = item.ETag
                        });
                    }
                }
            }

            return contents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting folder contents for path: {FolderPath}", folderPath);
            throw;
        }
    }

    public async Task<SharePointFolderInfo> CreateFolderAsync(string parentPath, string folderName)
    {
        if (_settings.UseMockData)
        {
            return new SharePointFolderInfo
            {
                Id = $"mock-folder-{Guid.NewGuid()}",
                Name = folderName,
                Path = Path.Combine(parentPath, folderName).Replace('\\', '/'),
                WebUrl = $"https://mock.sharepoint.com/folders/{folderName}",
                CreatedDateTime = DateTime.UtcNow
            };
        }

        try
        {
            var client = await GetGraphClientAsync();
            var drive = await GetDocumentLibraryDriveAsync(client);

            // Get parent folder
            DriveItem? parentFolder;
            if (string.IsNullOrEmpty(parentPath) || parentPath == "/")
            {
                parentFolder = await client.Drives[drive.Id].Root.GetAsync();
            }
            else
            {
                parentFolder = await client.Drives[drive.Id]
                    .Root
                    .ItemWithPath(parentPath)
                    .GetAsync();
            }

            if (parentFolder == null)
            {
                throw new InvalidOperationException($"Parent folder not found: {parentPath}");
            }

            // Create the new folder
            var newFolder = new DriveItem
            {
                Name = folderName,
                Folder = new Folder()
            };

            var createdFolder = await client.Drives[drive.Id]
                .Items[parentFolder.Id]
                .Children
                .PostAsync(newFolder);

            if (createdFolder == null)
            {
                throw new InvalidOperationException($"Failed to create folder: {folderName}");
            }

            return new SharePointFolderInfo
            {
                Id = createdFolder.Id ?? string.Empty,
                Name = createdFolder.Name ?? string.Empty,
                Path = Path.Combine(parentPath, folderName).Replace('\\', '/'),
                WebUrl = createdFolder.WebUrl ?? string.Empty,
                CreatedDateTime = createdFolder.CreatedDateTime?.DateTime ?? DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating folder {FolderName} in {ParentPath}", folderName, parentPath);
            throw;
        }
    }

    public async Task<List<SharePointBreadcrumbItem>> GetFolderBreadcrumbsAsync(string folderPath)
    {
        var breadcrumbs = new List<SharePointBreadcrumbItem>();

        // Add root
        breadcrumbs.Add(new SharePointBreadcrumbItem
        {
            Name = "Root",
            Path = "/",
            IsRoot = true,
            IsCurrent = string.IsNullOrEmpty(folderPath) || folderPath == "/"
        });

        if (!string.IsNullOrEmpty(folderPath) && folderPath != "/")
        {
            var parts = folderPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var currentPath = "";

            for (int i = 0; i < parts.Length; i++)
            {
                currentPath = string.IsNullOrEmpty(currentPath)
                    ? parts[i]
                    : currentPath + "/" + parts[i];

                breadcrumbs.Add(new SharePointBreadcrumbItem
                {
                    Name = parts[i],
                    Path = currentPath,
                    IsRoot = false,
                    IsCurrent = i == parts.Length - 1
                });
            }
        }

        return await Task.FromResult(breadcrumbs);
    }

    public async Task<List<SharePointFileInfo>> UploadMultipleFilesAsync(string folderPath, List<(Stream stream, string fileName, string contentType)> files)
    {
        if (_settings.UseMockData)
        {
            var mockResults = new List<SharePointFileInfo>();
            foreach (var file in files)
            {
                mockResults.Add(new SharePointFileInfo
                {
                    Id = $"mock-file-{Guid.NewGuid()}",
                    Name = file.fileName,
                    Size = file.stream.Length,
                    ContentType = file.contentType,
                    WebUrl = $"https://mock.sharepoint.com/files/{file.fileName}",
                    CreatedDateTime = DateTime.UtcNow,
                    LastModifiedDateTime = DateTime.UtcNow,
                    CreatedBy = "Mock User",
                    ModifiedBy = "Mock User"
                });
            }
            return mockResults;
        }

        try
        {
            var uploadedFiles = new List<SharePointFileInfo>();
            var client = await GetGraphClientAsync();
            var drive = await GetDocumentLibraryDriveAsync(client);

            // Get target folder
            DriveItem? folder;
            if (string.IsNullOrEmpty(folderPath) || folderPath == "/")
            {
                folder = await client.Drives[drive.Id].Root.GetAsync();
            }
            else
            {
                folder = await client.Drives[drive.Id]
                    .Root
                    .ItemWithPath(folderPath)
                    .GetAsync();
            }

            if (folder == null)
            {
                throw new InvalidOperationException($"Folder not found: {folderPath}");
            }

            // Upload each file
            foreach (var (stream, fileName, contentType) in files)
            {
                try
                {
                    DriveItem? uploadedItem;

                    // For all files, use the simple upload method
                    // Note: In production, implement chunked upload for large files (> 4MB)
                    // using the Graph SDK's LargeFileUploadTask
                    uploadedItem = await UploadSmallFileAsync(client, drive.Id!, folder.Id!, fileName, stream);

                    if (uploadedItem != null)
                    {
                        uploadedFiles.Add(new SharePointFileInfo
                        {
                            Id = uploadedItem.Id ?? string.Empty,
                            Name = uploadedItem.Name ?? string.Empty,
                            Size = uploadedItem.Size ?? 0,
                            ContentType = contentType,
                            WebUrl = uploadedItem.WebUrl ?? string.Empty,
                            CreatedDateTime = uploadedItem.CreatedDateTime?.DateTime ?? DateTime.UtcNow,
                            LastModifiedDateTime = uploadedItem.LastModifiedDateTime?.DateTime ?? DateTime.UtcNow,
                            CreatedBy = uploadedItem.CreatedBy?.User?.DisplayName ?? "Unknown",
                            ModifiedBy = uploadedItem.LastModifiedBy?.User?.DisplayName ?? "Unknown",
                            ETag = uploadedItem.ETag
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading file {FileName}", fileName);
                    // Continue with other files even if one fails
                }
            }

            return uploadedFiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading multiple files to {FolderPath}", folderPath);
            throw;
        }
    }

    public async Task<bool> DeleteMultipleFilesAsync(List<string> driveItemIds)
    {
        if (_settings.UseMockData)
        {
            _logger.LogInformation("Mock: Deleted {Count} files", driveItemIds.Count);
            return true;
        }

        try
        {
            var client = await GetGraphClientAsync();
            var drive = await GetDocumentLibraryDriveAsync(client);
            var failedDeletes = new List<string>();

            foreach (var itemId in driveItemIds)
            {
                try
                {
                    await client.Drives[drive.Id]
                        .Items[itemId]
                        .DeleteAsync();

                    _logger.LogInformation("Deleted file with ID: {ItemId}", itemId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete file with ID: {ItemId}", itemId);
                    failedDeletes.Add(itemId);
                }
            }

            if (failedDeletes.Any())
            {
                _logger.LogWarning("Failed to delete {Count} files: {FileIds}",
                    failedDeletes.Count, string.Join(", ", failedDeletes));
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting multiple files");
            return false;
        }
    }

    private async Task<DriveItem> UploadSmallFileAsync(GraphServiceClient client, string driveId, string folderId, string fileName, Stream stream)
    {
        // Reset stream position
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        // Upload the file
        var uploadedItem = await client.Drives[driveId]
            .Items[folderId]
            .ItemWithPath(fileName)
            .Content
            .PutAsync(stream);

        return uploadedItem ?? throw new InvalidOperationException($"Failed to upload {fileName}");
    }
}