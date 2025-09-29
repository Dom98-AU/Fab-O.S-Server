using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces;

public interface IPackageDrawingService
{
    /// <summary>
    /// Get a specific drawing by ID
    /// </summary>
    Task<PackageDrawing?> GetDrawingAsync(int drawingId);

    /// <summary>
    /// Get all active drawings for a package
    /// </summary>
    Task<List<PackageDrawing>> GetPackageDrawingsAsync(int packageId);

    /// <summary>
    /// Upload a new drawing to SharePoint and save reference in database
    /// </summary>
    Task<PackageDrawing> UploadDrawingAsync(
        int packageId,
        Stream fileStream,
        string fileName,
        string drawingNumber,
        string drawingTitle,
        int uploadedBy);

    /// <summary>
    /// Get drawing content from SharePoint
    /// </summary>
    Task<Stream> GetDrawingContentAsync(int drawingId);

    /// <summary>
    /// Delete a drawing (soft delete)
    /// </summary>
    Task<bool> DeleteDrawingAsync(int drawingId);

    /// <summary>
    /// Get SharePoint preview URL for a drawing
    /// </summary>
    Task<string> GetDrawingPreviewUrlAsync(int drawingId);

    /// <summary>
    /// Get count of active drawings for a package
    /// </summary>
    Task<int> GetDrawingCountForPackageAsync(int packageId);

    /// <summary>
    /// Check if a drawing number already exists for a package
    /// </summary>
    Task<bool> DrawingNumberExistsAsync(int packageId, string drawingNumber);
}