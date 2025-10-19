using FabOS.WebServer.Models.CloudStorage;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Common interface for all cloud storage providers (SharePoint, Google Drive, Dropbox, Azure Blob, etc.)
/// Provides abstraction layer for file upload, download, update, and delete operations
/// </summary>
public interface ICloudStorageProvider
{
    /// <summary>
    /// Provider identifier (e.g., "SharePoint", "GoogleDrive", "Dropbox", "AzureBlob")
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Upload a file to cloud storage
    /// </summary>
    /// <param name="request">Upload request containing file stream, metadata, and destination</param>
    /// <returns>Upload result with file ID, URL, and provider-specific metadata</returns>
    Task<CloudFileUploadResult> UploadFileAsync(CloudFileUploadRequest request);

    /// <summary>
    /// Download a file from cloud storage
    /// </summary>
    /// <param name="fileId">Provider-specific file identifier</param>
    /// <returns>Stream containing file content</returns>
    Task<Stream> DownloadFileAsync(string fileId);

    /// <summary>
    /// Update/replace an existing file in cloud storage
    /// </summary>
    /// <param name="fileId">Provider-specific file identifier</param>
    /// <param name="content">New file content stream</param>
    /// <param name="contentType">MIME type of the content</param>
    /// <returns>Upload result with updated file information</returns>
    Task<CloudFileUploadResult> UpdateFileAsync(string fileId, Stream content, string contentType);

    /// <summary>
    /// Delete a file from cloud storage
    /// </summary>
    /// <param name="fileId">Provider-specific file identifier</param>
    /// <returns>True if deletion successful, false otherwise</returns>
    Task<bool> DeleteFileAsync(string fileId);

    /// <summary>
    /// Get file metadata without downloading the file
    /// </summary>
    /// <param name="fileId">Provider-specific file identifier</param>
    /// <returns>File metadata including size, content type, dates, etc.</returns>
    Task<CloudFileMetadata> GetFileMetadataAsync(string fileId);

    /// <summary>
    /// Get web URL for viewing/previewing file in browser
    /// </summary>
    /// <param name="fileId">Provider-specific file identifier</param>
    /// <returns>Web URL for file preview/viewing</returns>
    Task<string> GetFileWebUrlAsync(string fileId);

    /// <summary>
    /// Create upload session for large files (>4MB)
    /// Enables chunked upload for improved reliability
    /// </summary>
    /// <param name="request">Upload request for large file</param>
    /// <returns>Upload session with session ID and upload URL</returns>
    Task<CloudUploadSession> CreateUploadSessionAsync(CloudFileUploadRequest request);

    /// <summary>
    /// Get presigned URL for client-side direct upload (future enhancement for cloud direct upload)
    /// Enables JavaScript to upload directly to cloud storage, bypassing server
    /// </summary>
    /// <param name="folderPath">Destination folder path</param>
    /// <param name="fileName">Name of file to upload</param>
    /// <param name="expiration">URL expiration duration</param>
    /// <returns>Presigned URL for direct upload</returns>
    Task<string> GetPresignedUploadUrlAsync(string folderPath, string fileName, TimeSpan expiration);
}
