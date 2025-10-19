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
using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Implementations;

/// <summary>
/// Implementation of SharePoint operations using Microsoft Graph with multi-tenant support
/// </summary>
public class SharePointService : ISharePointService
{
    private readonly ILogger<SharePointService> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ITenantService _tenantService;
    private GraphServiceClient? _graphClient;
    private SharePointSettings? _currentTenantSettings;
    private int? _lastLoadedCompanyId;

    public SharePointService(
        ILogger<SharePointService> logger,
        ApplicationDbContext dbContext,
        IConfiguration configuration,
        ITenantService tenantService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _configuration = configuration;
        _tenantService = tenantService;
    }

    /// <summary>
    /// Gets or loads the current tenant's SharePoint settings
    /// </summary>
    private async Task<SharePointSettings?> GetCurrentTenantSettingsAsync()
    {
        var companyId = _tenantService.GetCurrentCompanyId();

        // Check if we already have the settings for the current company loaded
        if (_currentTenantSettings != null && _lastLoadedCompanyId == companyId)
        {
            return _currentTenantSettings;
        }

        // Load settings from database for the current tenant
        var tenantSettings = await _dbContext.CompanySharePointSettings
            .FirstOrDefaultAsync(s => s.CompanyId == companyId);

        if (tenantSettings == null)
        {
            _logger.LogInformation("No SharePoint settings found for company {CompanyId}", companyId);
            _currentTenantSettings = null;
            _lastLoadedCompanyId = companyId;
            return null;
        }

        // Convert to SharePointSettings object
        _currentTenantSettings = new SharePointSettings
        {
            TenantId = tenantSettings.TenantId,
            ClientId = tenantSettings.ClientId,
            ClientSecret = tenantSettings.ClientSecret,
            SiteUrl = tenantSettings.SiteUrl,
            DocumentLibrary = tenantSettings.DocumentLibrary,
            TakeoffsRootFolder = tenantSettings.TakeoffsRootFolder,
            IsEnabled = tenantSettings.IsEnabled,
            UseMockData = tenantSettings.UseMockData,
            MaxFileSizeMB = tenantSettings.MaxFileSizeMB
        };

        _lastLoadedCompanyId = companyId;
        _graphClient = null; // Reset graph client when tenant changes

        _logger.LogInformation("SharePoint settings loaded for company {CompanyId}. IsEnabled: {IsEnabled}, UseMockData: {UseMockData}",
            companyId, tenantSettings.IsEnabled, tenantSettings.UseMockData);

        return _currentTenantSettings;
    }

    private async Task<GraphServiceClient> GetGraphClientAsync()
    {
        if (_graphClient != null)
            return _graphClient;

        var settings = await GetCurrentTenantSettingsAsync();
        if (settings == null || !settings.IsValid())
        {
            throw new InvalidOperationException($"SharePoint is not properly configured for company {_tenantService.GetCurrentCompanyId()}");
        }

        var clientSecretCredential = new ClientSecretCredential(
            settings.TenantId,
            settings.ClientId,
            settings.ClientSecret);

        _graphClient = new GraphServiceClient(clientSecretCredential);
        return _graphClient;
    }

    public async Task<CompanySharePointSettings?> GetSettingsForTenantAsync(int companyId)
    {
        return await _dbContext.CompanySharePointSettings
            .Include(s => s.Company)
            .FirstOrDefaultAsync(s => s.CompanyId == companyId);
    }

    public async Task<bool> IsTenantConfiguredAsync()
    {
        var companyId = _tenantService.GetCurrentCompanyId();
        return await _dbContext.CompanySharePointSettings
            .AnyAsync(s => s.CompanyId == companyId && s.IsEnabled);
    }

    public async Task<SharePointConnectionStatus> GetConnectionStatusAsync()
    {
        var status = new SharePointConnectionStatus();
        var settings = await GetCurrentTenantSettingsAsync();

        if (settings == null)
        {
            status.IsConfigured = false;
            status.ErrorMessage = $"SharePoint is not configured for company {_tenantService.GetCurrentCompanyId()}. Please complete the setup.";
            return status;
        }

        status.IsConfigured = settings.IsValid();

        if (!status.IsConfigured)
        {
            status.ErrorMessage = "SharePoint configuration is incomplete. Please complete the setup.";
            return status;
        }

        if (settings.UseMockData)
        {
            status.IsConnected = true;
            status.SiteName = "Mock SharePoint Site";
            status.LibraryName = settings.DocumentLibrary;
            return status;
        }

        try
        {
            var client = await GetGraphClientAsync();
            var site = await client.Sites[settings.GetSiteId()].GetAsync();

            status.IsConnected = site != null;
            status.SiteName = site?.DisplayName ?? site?.Name;
            status.LibraryName = settings.DocumentLibrary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SharePoint for company {CompanyId}", _tenantService.GetCurrentCompanyId());
            status.IsConnected = false;
            status.ErrorMessage = $"Connection failed: {ex.Message}";
        }

        return status;
    }

    public async Task<bool> ConfigureSharePointAsync(string tenantId, string clientId, string clientSecret, string siteUrl)
    {
        try
        {
            var companyId = _tenantService.GetCurrentCompanyId();
            var userId = _tenantService.GetCurrentUserId();

            // Test the connection first
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

            // Check if settings already exist for this company
            var existingSettings = await _dbContext.CompanySharePointSettings
                .FirstOrDefaultAsync(s => s.CompanyId == companyId);

            if (existingSettings != null)
            {
                // Update existing settings
                existingSettings.TenantId = tenantId;
                existingSettings.ClientId = clientId;
                existingSettings.ClientSecret = clientSecret;
                existingSettings.SiteUrl = siteUrl;
                existingSettings.IsEnabled = true;
                existingSettings.UseMockData = false;
                existingSettings.IsClientSecretEncrypted = true; // Mark for encryption
                existingSettings.LastModifiedDate = DateTime.UtcNow;
                existingSettings.LastModifiedByUserId = userId;
            }
            else
            {
                // Create new settings
                var newSettings = new CompanySharePointSettings
                {
                    CompanyId = companyId,
                    TenantId = tenantId,
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    SiteUrl = siteUrl,
                    DocumentLibrary = "Takeoff Files",
                    TakeoffsRootFolder = "Takeoffs",
                    IsEnabled = true,
                    UseMockData = false,
                    MaxFileSizeMB = 250,
                    IsClientSecretEncrypted = true,
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow,
                    CreatedByUserId = userId,
                    LastModifiedByUserId = userId
                };

                _dbContext.CompanySharePointSettings.Add(newSettings);
            }

            await _dbContext.SaveChangesAsync();

            // Reset cached settings to force reload
            _currentTenantSettings = null;
            _graphClient = null;

            _logger.LogInformation("SharePoint configured successfully for company {CompanyId}, site: {SiteName}",
                companyId, site.DisplayName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure SharePoint for company {CompanyId}", _tenantService.GetCurrentCompanyId());
            return false;
        }
    }

    public async Task<DocumentLibraryCheckResult> CheckDocumentLibraryAsync()
    {
        var settings = await GetCurrentTenantSettingsAsync();
        if (settings == null)
        {
            return new DocumentLibraryCheckResult
            {
                Exists = false,
                Message = $"SharePoint not configured for company {_tenantService.GetCurrentCompanyId()}",
                LibraryName = "Unknown"
            };
        }

        if (settings.UseMockData)
        {
            _logger.LogInformation("Mock mode: Document library exists");
            return new DocumentLibraryCheckResult
            {
                Exists = true,
                Message = "Mock mode - library simulation active",
                LibraryName = settings.DocumentLibrary
            };
        }

        try
        {
            var client = await GetGraphClientAsync();
            var site = await client.Sites[settings.GetSiteId()].GetAsync();

            if (site?.Id == null)
            {
                return new DocumentLibraryCheckResult
                {
                    Exists = false,
                    Message = "Unable to access SharePoint site. Please check your connection settings.",
                    LibraryName = settings.DocumentLibrary
                };
            }

            // Check if document library already exists
            var drives = await client.Sites[site.Id].Drives.GetAsync();
            var existingDrive = drives?.Value?.FirstOrDefault(d => d.Name == settings.DocumentLibrary);

            if (existingDrive != null)
            {
                _logger.LogInformation("Document library '{LibraryName}' already exists for company {CompanyId}",
                    settings.DocumentLibrary, _tenantService.GetCurrentCompanyId());
                return new DocumentLibraryCheckResult
                {
                    Exists = true,
                    Message = $"Document library '{settings.DocumentLibrary}' is accessible and ready",
                    LibraryName = settings.DocumentLibrary
                };
            }

            // Note: Creating a new document library requires SharePoint admin permissions
            // and is typically done through SharePoint admin center or PowerShell
            _logger.LogWarning("Document library '{LibraryName}' does not exist. Please create it manually in SharePoint.",
                settings.DocumentLibrary);
            return new DocumentLibraryCheckResult
            {
                Exists = false,
                Message = $"Document library '{settings.DocumentLibrary}' does not exist. It must be created manually in SharePoint by an administrator.",
                LibraryName = settings.DocumentLibrary,
                CanCreate = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check document library for company {CompanyId}",
                _tenantService.GetCurrentCompanyId());
            return new DocumentLibraryCheckResult
            {
                Exists = false,
                Message = $"Error checking library: {ex.Message}",
                LibraryName = settings.DocumentLibrary
            };
        }
    }

    [Obsolete("Use CheckDocumentLibraryAsync instead - libraries cannot be created programmatically")]
    public async Task<bool> EnsureDocumentLibraryExistsAsync()
    {
        var result = await CheckDocumentLibraryAsync();
        return result.Exists;
    }

    public async Task<SharePointFolderInfo> EnsureTakeoffFolderExistsAsync(string takeoffNumber, string revisionCode = "A")
    {
        var settings = await GetCurrentTenantSettingsAsync();
        if (settings == null)
        {
            throw new InvalidOperationException($"SharePoint not configured for company {_tenantService.GetCurrentCompanyId()}");
        }

        if (settings.UseMockData)
        {
            return CreateMockFolderInfo(takeoffNumber, revisionCode);
        }

        try
        {
            var client = await GetGraphClientAsync();
            var site = await client.Sites[settings.GetSiteId()].GetAsync();

            if (site?.Id == null)
            {
                throw new InvalidOperationException("Unable to get SharePoint site");
            }

            // Get the document library drive
            var drives = await client.Sites[site.Id].Drives.GetAsync();
            var drive = drives?.Value?.FirstOrDefault(d => d.Name == settings.DocumentLibrary);

            if (drive?.Id == null)
            {
                throw new InvalidOperationException($"Document library '{settings.DocumentLibrary}' not found");
            }

            // Build the folder path
            var folderPath = $"{settings.TakeoffsRootFolder}/{takeoffNumber}/{revisionCode}";

            // Try to get the folder first
            try
            {
                var existingFolder = await client.Drives[drive.Id].Root
                    .ItemWithPath(folderPath)
                    .GetAsync();

                if (existingFolder != null)
                {
                    return new SharePointFolderInfo
                    {
                        Id = existingFolder.Id ?? string.Empty,
                        Name = existingFolder.Name ?? takeoffNumber,
                        WebUrl = existingFolder.WebUrl ?? string.Empty,
                        Path = folderPath,
                        CreatedDateTime = existingFolder.CreatedDateTime?.DateTime ?? DateTime.UtcNow,
                        RevisionCode = revisionCode
                    };
                }
            }
            catch
            {
                // Folder doesn't exist, create it
            }

            // Create the folder hierarchy
            var createdFolder = await CreateFolderHierarchyAsync(client, drive.Id, folderPath);

            return new SharePointFolderInfo
            {
                Id = createdFolder.Id ?? string.Empty,
                Name = createdFolder.Name ?? takeoffNumber,
                WebUrl = createdFolder.WebUrl ?? string.Empty,
                Path = folderPath,
                CreatedDateTime = createdFolder.CreatedDateTime?.DateTime ?? DateTime.UtcNow,
                RevisionCode = revisionCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure takeoff folder exists for {TakeoffNumber} in company {CompanyId}",
                takeoffNumber, _tenantService.GetCurrentCompanyId());
            throw;
        }
    }

    public async Task<bool> TakeoffFolderExistsAsync(string takeoffNumber)
    {
        var settings = await GetCurrentTenantSettingsAsync();
        if (settings == null)
        {
            return false;
        }

        if (settings.UseMockData)
        {
            return true;
        }

        try
        {
            var client = await GetGraphClientAsync();
            var site = await client.Sites[settings.GetSiteId()].GetAsync();

            if (site?.Id == null)
                return false;

            var drives = await client.Sites[site.Id].Drives.GetAsync();
            var drive = drives?.Value?.FirstOrDefault(d => d.Name == settings.DocumentLibrary);

            if (drive?.Id == null)
                return false;

            var folderPath = $"{settings.TakeoffsRootFolder}/{takeoffNumber}";

            try
            {
                var folder = await client.Drives[drive.Id].Root
                    .ItemWithPath(folderPath)
                    .GetAsync();

                return folder != null;
            }
            catch
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if takeoff folder exists for {TakeoffNumber} in company {CompanyId}",
                takeoffNumber, _tenantService.GetCurrentCompanyId());
            return false;
        }
    }

    public async Task<SharePointFolderInfo> CreateTakeoffFolderAsync(string takeoffNumber, string revisionCode = "A")
    {
        return await EnsureTakeoffFolderExistsAsync(takeoffNumber, revisionCode);
    }

    public async Task<SharePointFolderInfo?> GetTakeoffFolderAsync(string takeoffNumber)
    {
        var settings = await GetCurrentTenantSettingsAsync();
        if (settings == null)
        {
            return null;
        }

        if (settings.UseMockData)
        {
            return CreateMockFolderInfo(takeoffNumber, "A");
        }

        try
        {
            var client = await GetGraphClientAsync();
            var site = await client.Sites[settings.GetSiteId()].GetAsync();

            if (site?.Id == null)
                return null;

            var drives = await client.Sites[site.Id].Drives.GetAsync();
            var drive = drives?.Value?.FirstOrDefault(d => d.Name == settings.DocumentLibrary);

            if (drive?.Id == null)
                return null;

            var folderPath = $"{settings.TakeoffsRootFolder}/{takeoffNumber}";

            try
            {
                var folder = await client.Drives[drive.Id].Root
                    .ItemWithPath(folderPath)
                    .GetAsync();

                if (folder == null)
                    return null;

                return new SharePointFolderInfo
                {
                    Id = folder.Id ?? string.Empty,
                    Name = folder.Name ?? takeoffNumber,
                    WebUrl = folder.WebUrl ?? string.Empty,
                    Path = folderPath,
                    CreatedDateTime = folder.CreatedDateTime?.DateTime ?? DateTime.UtcNow
                };
            }
            catch
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get takeoff folder for {TakeoffNumber} in company {CompanyId}",
                takeoffNumber, _tenantService.GetCurrentCompanyId());
            return null;
        }
    }

    public async Task<List<SharePointFileInfo>> GetTakeoffFilesAsync(string takeoffNumber)
    {
        var settings = await GetCurrentTenantSettingsAsync();
        if (settings == null)
        {
            return new List<SharePointFileInfo>();
        }

        if (settings.UseMockData)
        {
            return CreateMockFileList(takeoffNumber);
        }

        try
        {
            var client = await GetGraphClientAsync();
            var site = await client.Sites[settings.GetSiteId()].GetAsync();

            if (site?.Id == null)
                return new List<SharePointFileInfo>();

            var drives = await client.Sites[site.Id].Drives.GetAsync();
            var drive = drives?.Value?.FirstOrDefault(d => d.Name == settings.DocumentLibrary);

            if (drive?.Id == null)
                return new List<SharePointFileInfo>();

            var folderPath = $"{settings.TakeoffsRootFolder}/{takeoffNumber}";
            var files = new List<SharePointFileInfo>();

            try
            {
                var folder = await client.Drives[drive.Id].Root
                    .ItemWithPath(folderPath)
                    .Children
                    .GetAsync();

                if (folder?.Value != null)
                {
                    foreach (var item in folder.Value.Where(i => i.File != null))
                    {
                        files.Add(new SharePointFileInfo
                        {
                            Id = item.Id ?? string.Empty,
                            Name = item.Name ?? string.Empty,
                            Size = item.Size ?? 0,
                            ContentType = item.File?.MimeType ?? string.Empty,
                            WebUrl = item.WebUrl ?? string.Empty,
                            CreatedDateTime = item.CreatedDateTime?.DateTime ?? DateTime.UtcNow,
                            LastModifiedDateTime = item.LastModifiedDateTime?.DateTime ?? DateTime.UtcNow,
                            CreatedBy = item.CreatedBy?.User?.DisplayName ?? "Unknown",
                            ModifiedBy = item.LastModifiedBy?.User?.DisplayName ?? "Unknown",
                            ETag = item.ETag,
                            DownloadUrl = item.AdditionalData?.ContainsKey("@microsoft.graph.downloadUrl") == true ?
                                item.AdditionalData["@microsoft.graph.downloadUrl"]?.ToString() : null
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get files for takeoff {TakeoffNumber}", takeoffNumber);
            }

            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get takeoff files for {TakeoffNumber} in company {CompanyId}",
                takeoffNumber, _tenantService.GetCurrentCompanyId());
            return new List<SharePointFileInfo>();
        }
    }

    public async Task<SharePointFileInfo> UploadFileAsync(string takeoffNumber, Stream fileStream, string fileName, string contentType)
    {
        var settings = await GetCurrentTenantSettingsAsync();
        if (settings == null)
        {
            throw new InvalidOperationException($"SharePoint not configured for company {_tenantService.GetCurrentCompanyId()}");
        }

        if (settings.UseMockData)
        {
            return CreateMockFileInfo(fileName, fileStream.Length, contentType);
        }

        try
        {
            // Ensure the folder exists
            var folder = await EnsureTakeoffFolderExistsAsync(takeoffNumber);

            var client = await GetGraphClientAsync();
            var site = await client.Sites[settings.GetSiteId()].GetAsync();

            if (site?.Id == null)
                throw new InvalidOperationException("Unable to get SharePoint site");

            var drives = await client.Sites[site.Id].Drives.GetAsync();
            var drive = drives?.Value?.FirstOrDefault(d => d.Name == settings.DocumentLibrary);

            if (drive?.Id == null)
                throw new InvalidOperationException($"Document library '{settings.DocumentLibrary}' not found");

            var folderPath = $"{settings.TakeoffsRootFolder}/{takeoffNumber}/A"; // Default to revision A
            var filePath = $"{folderPath}/{fileName}";

            DriveItem uploadedFile;

            // For large files (> 4MB), use upload session
            if (fileStream.Length > 4 * 1024 * 1024)
            {
                var uploadSessionRequestBody = new Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession.CreateUploadSessionPostRequestBody();
                var uploadSession = await client.Drives[drive.Id].Root
                    .ItemWithPath(filePath)
                    .CreateUploadSession
                    .PostAsync(uploadSessionRequestBody);

                if (uploadSession?.UploadUrl == null)
                    throw new InvalidOperationException("Failed to create upload session");

                // Upload the file in chunks
                var fileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, fileStream);
                var uploadResult = await fileUploadTask.UploadAsync();

                if (!uploadResult.UploadSucceeded || uploadResult.ItemResponse == null)
                    throw new InvalidOperationException("File upload failed");

                uploadedFile = uploadResult.ItemResponse;
            }
            else
            {
                // For small files, upload directly
                uploadedFile = await client.Drives[drive.Id].Root
                    .ItemWithPath(filePath)
                    .Content
                    .PutAsync(fileStream);
            }

            if (uploadedFile == null)
                throw new InvalidOperationException("Upload completed but file information not returned");

            return new SharePointFileInfo
            {
                Id = uploadedFile.Id ?? string.Empty,
                Name = uploadedFile.Name ?? fileName,
                Size = uploadedFile.Size ?? fileStream.Length,
                ContentType = contentType,
                WebUrl = uploadedFile.WebUrl ?? string.Empty,
                CreatedDateTime = uploadedFile.CreatedDateTime?.DateTime ?? DateTime.UtcNow,
                LastModifiedDateTime = uploadedFile.LastModifiedDateTime?.DateTime ?? DateTime.UtcNow,
                CreatedBy = uploadedFile.CreatedBy?.User?.DisplayName ?? "Unknown",
                ModifiedBy = uploadedFile.LastModifiedBy?.User?.DisplayName ?? "Unknown",
                ETag = uploadedFile.ETag,
                DownloadUrl = uploadedFile.AdditionalData?.ContainsKey("@microsoft.graph.downloadUrl") == true ?
                    uploadedFile.AdditionalData["@microsoft.graph.downloadUrl"]?.ToString() : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {FileName} for takeoff {TakeoffNumber} in company {CompanyId}",
                fileName, takeoffNumber, _tenantService.GetCurrentCompanyId());
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(string driveItemId)
    {
        var settings = await GetCurrentTenantSettingsAsync();
        if (settings == null)
        {
            throw new InvalidOperationException($"SharePoint not configured for company {_tenantService.GetCurrentCompanyId()}");
        }

        if (settings.UseMockData)
        {
            // Return a mock stream with some test data
            var mockData = Encoding.UTF8.GetBytes($"Mock file content for item {driveItemId}");
            return new MemoryStream(mockData);
        }

        try
        {
            var client = await GetGraphClientAsync();
            var site = await client.Sites[settings.GetSiteId()].GetAsync();

            if (site?.Id == null)
                throw new InvalidOperationException("Unable to get SharePoint site");

            var drives = await client.Sites[site.Id].Drives.GetAsync();
            var drive = drives?.Value?.FirstOrDefault(d => d.Name == settings.DocumentLibrary);

            if (drive?.Id == null)
                throw new InvalidOperationException($"Document library '{settings.DocumentLibrary}' not found");

            var fileStream = await client.Drives[drive.Id].Items[driveItemId].Content.GetAsync();

            if (fileStream == null)
                throw new InvalidOperationException("File not found or unable to download");

            return fileStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file {DriveItemId} for company {CompanyId}",
                driveItemId, _tenantService.GetCurrentCompanyId());
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string driveItemId)
    {
        var settings = await GetCurrentTenantSettingsAsync();
        if (settings == null)
        {
            return false;
        }

        if (settings.UseMockData)
        {
            _logger.LogInformation("Mock mode: File {DriveItemId} deleted", driveItemId);
            return true;
        }

        try
        {
            var client = await GetGraphClientAsync();
            var site = await client.Sites[settings.GetSiteId()].GetAsync();

            if (site?.Id == null)
                return false;

            var drives = await client.Sites[site.Id].Drives.GetAsync();
            var drive = drives?.Value?.FirstOrDefault(d => d.Name == settings.DocumentLibrary);

            if (drive?.Id == null)
                return false;

            await client.Drives[drive.Id].Items[driveItemId].DeleteAsync();

            _logger.LogInformation("File {DriveItemId} deleted successfully for company {CompanyId}",
                driveItemId, _tenantService.GetCurrentCompanyId());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {DriveItemId} for company {CompanyId}",
                driveItemId, _tenantService.GetCurrentCompanyId());
            return false;
        }
    }

    public async Task<SharePointFileInfo> UpdateFileAsync(string driveItemId, Stream fileStream, string contentType)
    {
        var settings = await GetCurrentTenantSettingsAsync();
        if (settings == null)
        {
            throw new InvalidOperationException($"SharePoint not configured for company {_tenantService.GetCurrentCompanyId()}");
        }

        if (settings.UseMockData)
        {
            _logger.LogInformation("Mock mode: File {DriveItemId} updated", driveItemId);
            return CreateMockFileInfo("updated-file.pdf", fileStream.Length, contentType);
        }

        try
        {
            var client = await GetGraphClientAsync();
            var site = await client.Sites[settings.GetSiteId()].GetAsync();

            if (site?.Id == null)
                throw new InvalidOperationException("Unable to get SharePoint site");

            var drives = await client.Sites[site.Id].Drives.GetAsync();
            var drive = drives?.Value?.FirstOrDefault(d => d.Name == settings.DocumentLibrary);

            if (drive?.Id == null)
                throw new InvalidOperationException($"Document library '{settings.DocumentLibrary}' not found");

            // Update the file content using PUT to /content endpoint
            // This replaces the file content while keeping the same driveItemId
            var updatedFile = await client.Drives[drive.Id].Items[driveItemId].Content.PutAsync(fileStream);

            if (updatedFile == null)
                throw new InvalidOperationException("File update completed but file information not returned");

            _logger.LogInformation("File {DriveItemId} updated successfully for company {CompanyId}",
                driveItemId, _tenantService.GetCurrentCompanyId());

            return new SharePointFileInfo
            {
                Id = updatedFile.Id ?? driveItemId,
                Name = updatedFile.Name ?? string.Empty,
                Size = updatedFile.Size ?? fileStream.Length,
                ContentType = contentType,
                WebUrl = updatedFile.WebUrl ?? string.Empty,
                CreatedDateTime = updatedFile.CreatedDateTime?.DateTime ?? DateTime.UtcNow,
                LastModifiedDateTime = updatedFile.LastModifiedDateTime?.DateTime ?? DateTime.UtcNow,
                CreatedBy = updatedFile.CreatedBy?.User?.DisplayName ?? "Unknown",
                ModifiedBy = updatedFile.LastModifiedBy?.User?.DisplayName ?? "Unknown",
                ETag = updatedFile.ETag,
                DownloadUrl = updatedFile.AdditionalData?.ContainsKey("@microsoft.graph.downloadUrl") == true ?
                    updatedFile.AdditionalData["@microsoft.graph.downloadUrl"]?.ToString() : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update file {DriveItemId} for company {CompanyId}",
                driveItemId, _tenantService.GetCurrentCompanyId());
            throw;
        }
    }

    public async Task<SharePointFolderInfo> CreateRevisionFolderAsync(string takeoffNumber, string revisionCode)
    {
        return await EnsureTakeoffFolderExistsAsync(takeoffNumber, revisionCode);
    }

    public async Task<string> GetFileWebUrlAsync(string driveItemId)
    {
        var settings = await GetCurrentTenantSettingsAsync();
        if (settings == null)
        {
            return string.Empty;
        }

        if (settings.UseMockData)
        {
            return $"https://mock.sharepoint.com/sites/mock/_layouts/15/Doc.aspx?sourcedoc={driveItemId}";
        }

        try
        {
            var client = await GetGraphClientAsync();
            var site = await client.Sites[settings.GetSiteId()].GetAsync();

            if (site?.Id == null)
                return string.Empty;

            var drives = await client.Sites[site.Id].Drives.GetAsync();
            var drive = drives?.Value?.FirstOrDefault(d => d.Name == settings.DocumentLibrary);

            if (drive?.Id == null)
                return string.Empty;

            var item = await client.Drives[drive.Id].Items[driveItemId].GetAsync();
            return item?.WebUrl ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get web URL for file {DriveItemId} in company {CompanyId}",
                driveItemId, _tenantService.GetCurrentCompanyId());
            return string.Empty;
        }
    }

    public async Task<SharePointFolderContents> GetFolderContentsAsync(string folderPath)
    {
        var settings = await GetCurrentTenantSettingsAsync();
        if (settings == null)
        {
            return new SharePointFolderContents { CurrentPath = folderPath };
        }

        if (settings.UseMockData)
        {
            return CreateMockFolderContents(folderPath);
        }

        try
        {
            var client = await GetGraphClientAsync();
            var site = await client.Sites[settings.GetSiteId()].GetAsync();

            if (site?.Id == null)
                return new SharePointFolderContents { CurrentPath = folderPath };

            var drives = await client.Sites[site.Id].Drives.GetAsync();
            var drive = drives?.Value?.FirstOrDefault(d => d.Name == settings.DocumentLibrary);

            if (drive?.Id == null)
                return new SharePointFolderContents { CurrentPath = folderPath };

            var contents = new SharePointFolderContents
            {
                CurrentPath = folderPath,
                IsRoot = string.IsNullOrEmpty(folderPath) || folderPath == "/"
            };

            // Set parent path
            if (!contents.IsRoot)
            {
                var pathParts = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (pathParts.Length > 1)
                {
                    contents.ParentPath = string.Join("/", pathParts.Take(pathParts.Length - 1));
                }
                else
                {
                    contents.ParentPath = "/";
                }
            }

            DriveItemCollectionResponse? items;

            if (string.IsNullOrEmpty(folderPath) || folderPath == "/")
            {
                items = await client.Drives[drive.Id].Items["root"].Children.GetAsync();
            }
            else
            {
                items = await client.Drives[drive.Id].Root
                    .ItemWithPath(folderPath)
                    .Children
                    .GetAsync();
            }

            if (items?.Value != null)
            {
                foreach (var item in items.Value)
                {
                    if (item.Folder != null)
                    {
                        contents.Folders.Add(new SharePointFolderInfo
                        {
                            Id = item.Id ?? string.Empty,
                            Name = item.Name ?? string.Empty,
                            WebUrl = item.WebUrl ?? string.Empty,
                            Path = string.IsNullOrEmpty(folderPath) ? item.Name ?? string.Empty : $"{folderPath}/{item.Name}",
                            CreatedDateTime = item.CreatedDateTime?.DateTime ?? DateTime.UtcNow
                        });
                    }
                    else if (item.File != null)
                    {
                        contents.Files.Add(new SharePointFileInfo
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
                            ETag = item.ETag,
                            DownloadUrl = item.AdditionalData?.ContainsKey("@microsoft.graph.downloadUrl") == true ?
                                item.AdditionalData["@microsoft.graph.downloadUrl"]?.ToString() : null
                        });
                    }
                }
            }

            return contents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get folder contents for {FolderPath} in company {CompanyId}",
                folderPath, _tenantService.GetCurrentCompanyId());
            return new SharePointFolderContents { CurrentPath = folderPath };
        }
    }

    public async Task<SharePointFolderInfo> CreateFolderAsync(string parentPath, string folderName)
    {
        var settings = await GetCurrentTenantSettingsAsync();
        if (settings == null)
        {
            throw new InvalidOperationException($"SharePoint not configured for company {_tenantService.GetCurrentCompanyId()}");
        }

        if (settings.UseMockData)
        {
            return CreateMockFolderInfo(folderName, null);
        }

        try
        {
            var client = await GetGraphClientAsync();
            var site = await client.Sites[settings.GetSiteId()].GetAsync();

            if (site?.Id == null)
                throw new InvalidOperationException("Unable to get SharePoint site");

            var drives = await client.Sites[site.Id].Drives.GetAsync();
            var drive = drives?.Value?.FirstOrDefault(d => d.Name == settings.DocumentLibrary);

            if (drive?.Id == null)
                throw new InvalidOperationException($"Document library '{settings.DocumentLibrary}' not found");

            var fullPath = string.IsNullOrEmpty(parentPath) ? folderName : $"{parentPath}/{folderName}";
            var createdFolder = await CreateFolderHierarchyAsync(client, drive.Id, fullPath);

            return new SharePointFolderInfo
            {
                Id = createdFolder.Id ?? string.Empty,
                Name = createdFolder.Name ?? folderName,
                WebUrl = createdFolder.WebUrl ?? string.Empty,
                Path = fullPath,
                CreatedDateTime = createdFolder.CreatedDateTime?.DateTime ?? DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create folder {FolderName} in {ParentPath} for company {CompanyId}",
                folderName, parentPath, _tenantService.GetCurrentCompanyId());
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
            var pathParts = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var currentPath = "";

            for (int i = 0; i < pathParts.Length; i++)
            {
                currentPath = string.IsNullOrEmpty(currentPath) ? pathParts[i] : $"{currentPath}/{pathParts[i]}";
                breadcrumbs.Add(new SharePointBreadcrumbItem
                {
                    Name = pathParts[i],
                    Path = currentPath,
                    IsRoot = false,
                    IsCurrent = i == pathParts.Length - 1
                });
            }
        }

        return breadcrumbs;
    }

    public async Task<List<SharePointFileInfo>> UploadMultipleFilesAsync(string folderPath, List<(Stream stream, string fileName, string contentType)> files)
    {
        var uploadedFiles = new List<SharePointFileInfo>();

        foreach (var file in files)
        {
            try
            {
                // Extract takeoff number from folder path if possible
                var pathParts = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var takeoffNumber = pathParts.Length > 1 ? pathParts[1] : "General";

                var uploadedFile = await UploadFileToPathAsync(folderPath, file.stream, file.fileName, file.contentType);
                uploadedFiles.Add(uploadedFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file {FileName} to {FolderPath}", file.fileName, folderPath);
            }
        }

        return uploadedFiles;
    }

    public async Task<bool> DeleteMultipleFilesAsync(List<string> driveItemIds)
    {
        var allSuccessful = true;

        foreach (var driveItemId in driveItemIds)
        {
            var result = await DeleteFileAsync(driveItemId);
            if (!result)
            {
                allSuccessful = false;
            }
        }

        return allSuccessful;
    }

    // Helper method to upload file to a specific path
    private async Task<SharePointFileInfo> UploadFileToPathAsync(string folderPath, Stream fileStream, string fileName, string contentType)
    {
        var settings = await GetCurrentTenantSettingsAsync();
        if (settings == null)
        {
            throw new InvalidOperationException($"SharePoint not configured for company {_tenantService.GetCurrentCompanyId()}");
        }

        if (settings.UseMockData)
        {
            return CreateMockFileInfo(fileName, fileStream.Length, contentType);
        }

        try
        {
            var client = await GetGraphClientAsync();
            var site = await client.Sites[settings.GetSiteId()].GetAsync();

            if (site?.Id == null)
                throw new InvalidOperationException("Unable to get SharePoint site");

            var drives = await client.Sites[site.Id].Drives.GetAsync();
            var drive = drives?.Value?.FirstOrDefault(d => d.Name == settings.DocumentLibrary);

            if (drive?.Id == null)
                throw new InvalidOperationException($"Document library '{settings.DocumentLibrary}' not found");

            // Ensure folder exists
            await CreateFolderHierarchyAsync(client, drive.Id, folderPath);

            var filePath = $"{folderPath}/{fileName}";
            DriveItem uploadedFile;

            // For large files (> 4MB), use upload session
            if (fileStream.Length > 4 * 1024 * 1024)
            {
                var uploadSessionRequestBody = new Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession.CreateUploadSessionPostRequestBody();
                var uploadSession = await client.Drives[drive.Id].Root
                    .ItemWithPath(filePath)
                    .CreateUploadSession
                    .PostAsync(uploadSessionRequestBody);

                if (uploadSession?.UploadUrl == null)
                    throw new InvalidOperationException("Failed to create upload session");

                var fileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, fileStream);
                var uploadResult = await fileUploadTask.UploadAsync();

                if (!uploadResult.UploadSucceeded || uploadResult.ItemResponse == null)
                    throw new InvalidOperationException("File upload failed");

                uploadedFile = uploadResult.ItemResponse;
            }
            else
            {
                uploadedFile = await client.Drives[drive.Id].Root
                    .ItemWithPath(filePath)
                    .Content
                    .PutAsync(fileStream);
            }

            if (uploadedFile == null)
                throw new InvalidOperationException("Upload completed but file information not returned");

            return new SharePointFileInfo
            {
                Id = uploadedFile.Id ?? string.Empty,
                Name = uploadedFile.Name ?? fileName,
                Size = uploadedFile.Size ?? fileStream.Length,
                ContentType = contentType,
                WebUrl = uploadedFile.WebUrl ?? string.Empty,
                CreatedDateTime = uploadedFile.CreatedDateTime?.DateTime ?? DateTime.UtcNow,
                LastModifiedDateTime = uploadedFile.LastModifiedDateTime?.DateTime ?? DateTime.UtcNow,
                CreatedBy = uploadedFile.CreatedBy?.User?.DisplayName ?? "Unknown",
                ModifiedBy = uploadedFile.LastModifiedBy?.User?.DisplayName ?? "Unknown",
                ETag = uploadedFile.ETag,
                DownloadUrl = uploadedFile.AdditionalData?.ContainsKey("@microsoft.graph.downloadUrl") == true ?
                    uploadedFile.AdditionalData["@microsoft.graph.downloadUrl"]?.ToString() : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {FileName} to {FolderPath}", fileName, folderPath);
            throw;
        }
    }

    // Helper method to create folder hierarchy
    private async Task<DriveItem> CreateFolderHierarchyAsync(GraphServiceClient client, string driveId, string folderPath)
    {
        var pathParts = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        DriveItem? currentFolder = null;
        string currentPath = "";

        foreach (var part in pathParts)
        {
            currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}/{part}";

            try
            {
                // Try to get the folder first
                currentFolder = await client.Drives[driveId].Root
                    .ItemWithPath(currentPath)
                    .GetAsync();
            }
            catch
            {
                // Folder doesn't exist, create it
                var newFolder = new DriveItem
                {
                    Name = part,
                    Folder = new Folder()
                };

                if (string.IsNullOrEmpty(currentPath) || currentPath == part)
                {
                    // Create at root
                    currentFolder = await client.Drives[driveId].Items["root"].Children.PostAsync(newFolder);
                }
                else
                {
                    // Create under parent path
                    var parentPath = currentPath.Substring(0, currentPath.LastIndexOf('/'));
                    currentFolder = await client.Drives[driveId].Root
                        .ItemWithPath(parentPath)
                        .Children
                        .PostAsync(newFolder);
                }
            }
        }

        return currentFolder ?? throw new InvalidOperationException("Failed to create folder hierarchy");
    }

    // Package-level folder operations
    public async Task<string> GetPackageFolderPathAsync(string takeoffNumber, string revisionCode, string packageNumber)
    {
        var settings = await GetCurrentTenantSettingsAsync();
        if (settings == null)
        {
            throw new InvalidOperationException($"SharePoint not configured for company {_tenantService.GetCurrentCompanyId()}");
        }

        return $"{settings.TakeoffsRootFolder}/{takeoffNumber}/{revisionCode}/PKG-{packageNumber}";
    }

    public async Task<SharePointFolderInfo> EnsurePackageFolderExistsAsync(string takeoffNumber, string revisionCode, string packageNumber)
    {
        var settings = await GetCurrentTenantSettingsAsync();
        if (settings == null)
        {
            throw new InvalidOperationException($"SharePoint not configured for company {_tenantService.GetCurrentCompanyId()}");
        }

        if (settings.UseMockData)
        {
            return CreateMockFolderInfo($"PKG-{packageNumber}", revisionCode);
        }

        try
        {
            var client = await GetGraphClientAsync();
            var site = await client.Sites[settings.GetSiteId()].GetAsync();

            if (site?.Id == null)
            {
                throw new InvalidOperationException("Unable to get SharePoint site");
            }

            // Get the document library drive
            var drives = await client.Sites[site.Id].Drives.GetAsync();
            var drive = drives?.Value?.FirstOrDefault(d => d.Name == settings.DocumentLibrary);

            if (drive?.Id == null)
            {
                throw new InvalidOperationException($"Document library '{settings.DocumentLibrary}' not found");
            }

            // Build the complete folder path including package level
            var folderPath = $"{settings.TakeoffsRootFolder}/{takeoffNumber}/{revisionCode}/PKG-{packageNumber}";

            // Try to get the folder first
            try
            {
                var existingFolder = await client.Drives[drive.Id].Root
                    .ItemWithPath(folderPath)
                    .GetAsync();

                if (existingFolder != null)
                {
                    _logger.LogInformation("Package folder already exists: {FolderPath}", folderPath);
                    return new SharePointFolderInfo
                    {
                        Id = existingFolder.Id ?? string.Empty,
                        Name = $"PKG-{packageNumber}",
                        WebUrl = existingFolder.WebUrl ?? string.Empty,
                        Path = folderPath,
                        CreatedDateTime = existingFolder.CreatedDateTime?.DateTime ?? DateTime.UtcNow,
                        RevisionCode = revisionCode
                    };
                }
            }
            catch
            {
                // Folder doesn't exist, create it
            }

            // Create the complete folder hierarchy (Takeoffs -> TakeoffNumber -> RevisionCode -> Package)
            var createdFolder = await CreateFolderHierarchyAsync(client, drive.Id, folderPath);

            _logger.LogInformation("Created package folder: {FolderPath} for company {CompanyId}",
                folderPath, _tenantService.GetCurrentCompanyId());

            return new SharePointFolderInfo
            {
                Id = createdFolder.Id ?? string.Empty,
                Name = $"PKG-{packageNumber}",
                WebUrl = createdFolder.WebUrl ?? string.Empty,
                Path = folderPath,
                CreatedDateTime = createdFolder.CreatedDateTime?.DateTime ?? DateTime.UtcNow,
                RevisionCode = revisionCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure package folder exists for {TakeoffNumber}/{RevisionCode}/PKG-{PackageNumber} in company {CompanyId}",
                takeoffNumber, revisionCode, packageNumber, _tenantService.GetCurrentCompanyId());
            throw;
        }
    }

    public async Task<bool> PackageFolderExistsAsync(string takeoffNumber, string revisionCode, string packageNumber)
    {
        var settings = await GetCurrentTenantSettingsAsync();
        if (settings == null)
        {
            return false;
        }

        if (settings.UseMockData)
        {
            return true;
        }

        try
        {
            var client = await GetGraphClientAsync();
            var site = await client.Sites[settings.GetSiteId()].GetAsync();

            if (site?.Id == null)
                return false;

            var drives = await client.Sites[site.Id].Drives.GetAsync();
            var drive = drives?.Value?.FirstOrDefault(d => d.Name == settings.DocumentLibrary);

            if (drive?.Id == null)
                return false;

            var folderPath = $"{settings.TakeoffsRootFolder}/{takeoffNumber}/{revisionCode}/PKG-{packageNumber}";

            try
            {
                var folder = await client.Drives[drive.Id].Root
                    .ItemWithPath(folderPath)
                    .GetAsync();

                return folder != null;
            }
            catch
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if package folder exists for {TakeoffNumber}/{RevisionCode}/PKG-{PackageNumber} in company {CompanyId}",
                takeoffNumber, revisionCode, packageNumber, _tenantService.GetCurrentCompanyId());
            return false;
        }
    }

    // Mock data helper methods
    private SharePointFolderInfo CreateMockFolderInfo(string name, string? revisionCode)
    {
        return new SharePointFolderInfo
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            WebUrl = $"https://mock.sharepoint.com/sites/mock/Takeoffs/{name}",
            Path = $"Takeoffs/{name}",
            CreatedDateTime = DateTime.UtcNow.AddDays(-5),
            RevisionCode = revisionCode
        };
    }

    private SharePointFileInfo CreateMockFileInfo(string fileName, long size, string contentType)
    {
        return new SharePointFileInfo
        {
            Id = Guid.NewGuid().ToString(),
            Name = fileName,
            Size = size,
            ContentType = contentType,
            WebUrl = $"https://mock.sharepoint.com/sites/mock/_layouts/15/Doc.aspx?sourcedoc={Guid.NewGuid()}",
            CreatedDateTime = DateTime.UtcNow.AddHours(-2),
            LastModifiedDateTime = DateTime.UtcNow.AddHours(-1),
            CreatedBy = "Mock User",
            ModifiedBy = "Mock User",
            ETag = $"\"{Guid.NewGuid()}\"",
            DownloadUrl = $"https://mock.sharepoint.com/_api/download/{Guid.NewGuid()}"
        };
    }

    private List<SharePointFileInfo> CreateMockFileList(string takeoffNumber)
    {
        return new List<SharePointFileInfo>
        {
            CreateMockFileInfo($"{takeoffNumber}_drawing.pdf", 2048000, "application/pdf"),
            CreateMockFileInfo($"{takeoffNumber}_specs.docx", 512000, "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
            CreateMockFileInfo($"{takeoffNumber}_estimate.xlsx", 256000, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        };
    }

    private SharePointFolderContents CreateMockFolderContents(string folderPath)
    {
        var contents = new SharePointFolderContents
        {
            CurrentPath = folderPath,
            IsRoot = string.IsNullOrEmpty(folderPath) || folderPath == "/"
        };

        // Add mock folders
        contents.Folders.Add(CreateMockFolderInfo("Takeoff001", null));
        contents.Folders.Add(CreateMockFolderInfo("Takeoff002", null));
        contents.Folders.Add(CreateMockFolderInfo("Archives", null));

        // Add mock files
        contents.Files.Add(CreateMockFileInfo("general_specs.pdf", 1024000, "application/pdf"));
        contents.Files.Add(CreateMockFileInfo("readme.txt", 1024, "text/plain"));

        return contents;
    }
}