# Cloud Storage Architecture

**Status:** 🚧 In Development (Phase 1 Complete: 6/11 tasks)
**Branch:** feature-cloud-storage
**Created:** October 19, 2025
**Last Updated:** October 19, 2025

## Executive Summary

Multi-provider cloud storage architecture for Fab.OS SaaS platform. Enables customers to use their preferred cloud storage (SharePoint, Google Drive, Dropbox, Azure Blob) while maintaining database as single source of truth.

## Current SharePoint Configuration

The existing SharePoint integration uses these credentials (from `appsettings.json`):

```json
{
  "SharePoint": {
    "TenantId": "Set in appsettings.Development.json",
    "ClientId": "Set in appsettings.Development.json",
    "ClientSecret": "Set in appsettings.Development.json",
    "SiteUrl": "Set in appsettings.Development.json"
  }
}
```

**IMPORTANT:** The SharePointStorageProvider will wrap the existing `ISharePointService`, which means it will automatically use the same credentials already configured. No credential changes needed.

## Architecture Overview

```
PackageDrawingService
        ↓
CloudStorageProviderFactory
        ↓
ICloudStorageProvider ← Common interface
        ↓
   ┌────┴────┬─────────┬──────────┐
   ↓         ↓         ↓          ↓
SharePoint Google  Dropbox  AzureBlob
Provider   Drive   Provider Provider
   ↓
ISharePointService ← Uses existing credentials
```

## Database Schema Changes

### Phase 1: Add New Fields (Non-Breaking)

```sql
ALTER TABLE PackageDrawings ADD StorageProvider NVARCHAR(50) NULL;
ALTER TABLE PackageDrawings ADD ProviderFileId NVARCHAR(255) NULL;
ALTER TABLE PackageDrawings ADD ProviderMetadata NVARCHAR(MAX) NULL;
```

### Phase 2: Backfill Existing Data

```sql
UPDATE PackageDrawings
SET
    StorageProvider = 'SharePoint',
    ProviderFileId = SharePointItemId,
    ProviderMetadata = JSON_OBJECT('SharePointUrl', SharePointUrl)
WHERE SharePointItemId IS NOT NULL
  AND StorageProvider IS NULL;
```

## Core Interface

```csharp
public interface ICloudStorageProvider
{
    string ProviderName { get; }

    Task<CloudFileUploadResult> UploadFileAsync(CloudFileUploadRequest request);
    Task<Stream> DownloadFileAsync(string fileId);
    Task<CloudFileUploadResult> UpdateFileAsync(string fileId, Stream content, string contentType);
    Task<bool> DeleteFileAsync(string fileId);
    Task<CloudFileMetadata> GetFileMetadataAsync(string fileId);
    Task<string> GetFileWebUrlAsync(string fileId);
    Task<CloudUploadSession> CreateUploadSessionAsync(CloudFileUploadRequest request);
}
```

## SharePointStorageProvider Implementation

```csharp
public class SharePointStorageProvider : ICloudStorageProvider
{
    private readonly ISharePointService _sharePointService; // Uses existing credentials!
    private readonly ILogger<SharePointStorageProvider> _logger;

    public SharePointStorageProvider(
        ISharePointService sharePointService,
        ILogger<SharePointStorageProvider> logger)
    {
        _sharePointService = sharePointService;
        _logger = logger;
    }

    public string ProviderName => "SharePoint";

    public async Task<CloudFileUploadResult> UploadFileAsync(CloudFileUploadRequest request)
    {
        // Delegates to existing SharePointService
        var files = await _sharePointService.UploadMultipleFilesAsync(
            request.FolderPath,
            new List<(Stream, string, string)>
            {
                (request.Content, request.FileName, request.ContentType)
            }
        );

        var file = files.First();
        return new CloudFileUploadResult
        {
            FileId = file.Id,
            WebUrl = file.WebUrl,
            Size = file.Size,
            ProviderMetadata = new Dictionary<string, string>
            {
                { "SharePointItemId", file.Id },
                { "SharePointUrl", file.WebUrl }
            }
        };
    }

    // ... other methods delegate to ISharePointService
}
```

## Service Registration

```csharp
// Program.cs

// Existing services (keep as-is, uses existing credentials from appsettings.json)
builder.Services.AddScoped<ISharePointService, SharePointService>();

// New cloud storage providers
builder.Services.AddScoped<SharePointStorageProvider>(); // Wraps ISharePointService
builder.Services.AddScoped<GoogleDriveStorageProvider>();  // Stub for now
builder.Services.AddScoped<DropboxStorageProvider>();      // Stub for now
builder.Services.AddScoped<AzureBlobStorageProvider>();    // Stub for now

// Factory
builder.Services.AddScoped<CloudStorageProviderFactory>();

// Existing PackageDrawingService (will be refactored to use factory)
builder.Services.AddScoped<IPackageDrawingService, PackageDrawingService>();
```

## Migration Strategy

1. ⏳ Add new database fields (nullable, non-breaking)
2. ✅ Create abstraction layer interfaces
3. ✅ Implement SharePointStorageProvider wrapping existing SharePointService
4. ✅ Add stub providers for other cloud storage
5. ⏳ Refactor PackageDrawingService to use CloudStorageProviderFactory
6. ⏳ Backfill existing records with `StorageProvider = 'SharePoint'`
7. ⏳ Test with existing SharePoint data (backward compatibility)
8. 🔮 Future: Implement Google Drive provider
9. 🔮 Future: Implement Dropbox provider
10. 🔮 Future: Cloud direct upload for large files

## Backward Compatibility Guarantee

- Existing `SharePointItemId` and `SharePointUrl` fields **preserved**
- Existing SharePoint credentials **unchanged**
- SharePointStorageProvider uses existing `ISharePointService` internally
- All existing drawings continue to work without modification
- If `StorageProvider` is NULL, defaults to "SharePoint"

## Benefits

**For Business:**
- Multi-provider support attracts more customers
- Easy to add new providers as demand grows
- Reduced vendor lock-in

**For Developers:**
- Clean abstractions, easy to test
- Existing SharePoint code reused, not rewritten
- Extensible for new providers

**For Customers:**
- Use their preferred cloud storage
- Leverage existing cloud storage contracts
- Data sovereignty and compliance

## Implementation Checklist

- [x] Architecture documentation
- [x] `ICloudStorageProvider` interface
- [x] Supporting models (CloudFileUploadRequest, CloudFileUploadResult, CloudFileMetadata, CloudUploadSession)
- [x] SharePointStorageProvider (wraps existing ISharePointService)
- [x] Stub providers (GoogleDriveStorageProvider, DropboxStorageProvider, AzureBlobStorageProvider)
- [x] CloudStorageProviderFactory
- [ ] Database migration scripts
- [ ] Update PackageDrawing entity
- [ ] Refactor PackageDrawingService
- [ ] Update dependency injection
- [ ] Test with existing SharePoint data
- [ ] Unit tests

## Implementation Details

### Phase 1: Abstraction Layer (COMPLETED)

**Files Created:**

1. **Services/Interfaces/ICloudStorageProvider.cs**
   - Core abstraction interface for all cloud storage providers
   - Methods: UploadFileAsync, DownloadFileAsync, UpdateFileAsync, DeleteFileAsync, GetFileMetadataAsync, GetFileWebUrlAsync, CreateUploadSessionAsync, GetPresignedUploadUrlAsync

2. **Models/CloudStorage/CloudFileUploadRequest.cs**
   - Request model for file uploads
   - Properties: FolderPath, FileName, Content (Stream), ContentType, Metadata

3. **Models/CloudStorage/CloudFileUploadResult.cs**
   - Result model returned after successful upload
   - Properties: FileId, WebUrl, Size, ETag, ProviderMetadata (Dictionary)

4. **Models/CloudStorage/CloudFileMetadata.cs**
   - Metadata model for file information
   - Properties: FileId, Name, Size, ContentType, CreatedDate, ModifiedDate, WebUrl, ETag

5. **Models/CloudStorage/CloudUploadSession.cs**
   - Upload session model for large file uploads (>4MB)
   - Properties: SessionId, UploadUrl, ExpirationDate, ChunkSize

6. **Services/Implementations/CloudStorage/SharePointStorageProvider.cs**
   - Production-ready SharePoint implementation
   - Wraps existing ISharePointService via dependency injection
   - Automatically uses existing SharePoint credentials from appsettings.json
   - All methods delegate to ISharePointService methods

7. **Services/Implementations/CloudStorage/GoogleDriveStorageProvider.cs**
   - Stub implementation for future Google Drive support
   - All methods throw NotImplementedException

8. **Services/Implementations/CloudStorage/DropboxStorageProvider.cs**
   - Stub implementation for future Dropbox support
   - All methods throw NotImplementedException

9. **Services/Implementations/CloudStorage/AzureBlobStorageProvider.cs**
   - Stub implementation for future Azure Blob Storage support
   - All methods throw NotImplementedException

10. **Services/Implementations/CloudStorage/CloudStorageProviderFactory.cs**
    - Factory for provider selection with backward compatibility
    - Methods:
      - `GetProvider(string providerName)` - Get provider by name
      - `GetProviderForDrawing(PackageDrawing drawing)` - Backward compatibility helper (defaults to SharePoint)
      - `GetDefaultProvider()` - Returns SharePoint provider
      - `IsProviderSupported(string providerName)` - Check if provider is supported
      - `GetSupportedProviders()` - Returns array of supported provider names

### Phase 2: Database & Service Integration (PENDING)

**Next Steps:**
1. Create database migration to add `StorageProvider`, `ProviderFileId`, `ProviderMetadata` fields to PackageDrawings table
2. Update PackageDrawing entity with new properties
3. Refactor PackageDrawingService to use CloudStorageProviderFactory instead of ISharePointService
4. Register all services in Program.cs dependency injection
5. Test with existing SharePoint data to ensure backward compatibility
6. Create unit tests for providers and factory
