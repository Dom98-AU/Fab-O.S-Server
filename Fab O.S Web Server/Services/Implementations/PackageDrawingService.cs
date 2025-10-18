using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Services.Implementations;

public class PackageDrawingService : IPackageDrawingService
{
    private readonly ApplicationDbContext _context;
    private readonly ISharePointService _sharePointService;
    private readonly ILogger<PackageDrawingService> _logger;

    public PackageDrawingService(
        ApplicationDbContext context,
        ISharePointService sharePointService,
        ILogger<PackageDrawingService> logger)
    {
        _context = context;
        _sharePointService = sharePointService;
        _logger = logger;
    }

    public async Task<PackageDrawing?> GetDrawingAsync(int drawingId)
    {
        try
        {
            return await _context.PackageDrawings
                .Include(pd => pd.Package)
                .Include(pd => pd.UploadedByUser)
                .FirstOrDefaultAsync(pd => pd.Id == drawingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting drawing {DrawingId}", drawingId);
            return null;
        }
    }

    public async Task<List<PackageDrawing>> GetPackageDrawingsAsync(int packageId)
    {
        try
        {
            return await _context.PackageDrawings
                .Where(pd => pd.PackageId == packageId && pd.IsActive)
                .OrderByDescending(pd => pd.UploadedDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting drawings for package {PackageId}", packageId);
            return new List<PackageDrawing>();
        }
    }

    public async Task<PackageDrawing> UploadDrawingAsync(
        int packageId,
        Stream fileStream,
        string fileName,
        string drawingNumber,
        string drawingTitle,
        int uploadedBy)
    {
        try
        {
            // Get package with revision and takeoff information
            var package = await _context.Packages
                .Include(p => p.Revision)
                    .ThenInclude(r => r!.Takeoff)
                .FirstOrDefaultAsync(p => p.Id == packageId);

            if (package == null)
            {
                throw new InvalidOperationException($"Package {packageId} not found");
            }

            if (package.Revision?.Takeoff == null)
            {
                throw new InvalidOperationException($"Package {packageId} is not associated with a takeoff and revision");
            }

            var takeoffNumber = package.Revision.Takeoff.TakeoffNumber;
            var revisionCode = package.Revision.RevisionCode;
            var packageNumber = package.PackageNumber;

            if (string.IsNullOrEmpty(takeoffNumber))
            {
                throw new InvalidOperationException($"Takeoff number is missing for package {packageId}");
            }

            // Ensure the complete folder hierarchy exists (Takeoffs/TakeoffNumber/RevisionCode/PKG-PackageNumber)
            await _sharePointService.EnsurePackageFolderExistsAsync(takeoffNumber, revisionCode, packageNumber);

            // Get the complete folder path
            var folderPath = await _sharePointService.GetPackageFolderPathAsync(takeoffNumber, revisionCode, packageNumber);

            // Upload to SharePoint using the helper method that accepts folder paths
            var sharePointFile = await _sharePointService.UploadMultipleFilesAsync(
                folderPath,
                new List<(Stream stream, string fileName, string contentType)>
                {
                    (fileStream, fileName, "application/pdf")
                }
            );

            if (sharePointFile == null || !sharePointFile.Any())
            {
                throw new InvalidOperationException("File upload failed");
            }

            var uploadedFile = sharePointFile.First();

            // Save reference in database
            var drawing = new PackageDrawing
            {
                PackageId = packageId,
                DrawingNumber = drawingNumber,
                DrawingTitle = drawingTitle,
                SharePointItemId = uploadedFile.Id,
                SharePointUrl = uploadedFile.WebUrl,
                FileType = "PDF",
                FileSize = uploadedFile.Size,
                UploadedDate = DateTime.UtcNow,
                UploadedBy = uploadedBy,
                IsActive = true
            };

            _context.PackageDrawings.Add(drawing);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Uploaded drawing {DrawingNumber} for package {PackageId} to {FolderPath}",
                drawingNumber,
                packageId,
                folderPath
            );

            return drawing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error uploading drawing for package {PackageId}",
                packageId
            );
            throw;
        }
    }

    public async Task<Stream> GetDrawingContentAsync(int drawingId)
    {
        try
        {
            var drawing = await _context.PackageDrawings.FindAsync(drawingId);
            if (drawing == null)
            {
                throw new InvalidOperationException($"Drawing {drawingId} not found");
            }

            // Download from SharePoint
            return await _sharePointService.DownloadFileAsync(drawing.SharePointItemId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting drawing content for {DrawingId}", drawingId);
            throw;
        }
    }

    public async Task<bool> DeleteDrawingAsync(int drawingId)
    {
        try
        {
            var drawing = await _context.PackageDrawings.FindAsync(drawingId);
            if (drawing == null)
            {
                return false;
            }

            // Soft delete - mark as inactive
            drawing.IsActive = false;
            await _context.SaveChangesAsync();

            // Optionally delete from SharePoint
            // await _sharePointService.DeleteFileAsync(drawing.SharePointItemId);

            _logger.LogInformation("Deleted drawing {DrawingId}", drawingId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting drawing {DrawingId}", drawingId);
            return false;
        }
    }

    public async Task<string> GetDrawingPreviewUrlAsync(int drawingId)
    {
        try
        {
            var drawing = await _context.PackageDrawings.FindAsync(drawingId);
            if (drawing == null)
            {
                throw new InvalidOperationException($"Drawing {drawingId} not found");
            }

            // Get SharePoint preview URL
            return await _sharePointService.GetFileWebUrlAsync(drawing.SharePointItemId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preview URL for drawing {DrawingId}", drawingId);
            throw;
        }
    }

    public async Task<int> GetDrawingCountForPackageAsync(int packageId)
    {
        try
        {
            return await _context.PackageDrawings
                .CountAsync(pd => pd.PackageId == packageId && pd.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting drawing count for package {PackageId}", packageId);
            return 0;
        }
    }

    public async Task<bool> DrawingNumberExistsAsync(int packageId, string drawingNumber)
    {
        try
        {
            return await _context.PackageDrawings
                .AnyAsync(pd => pd.PackageId == packageId &&
                               pd.DrawingNumber == drawingNumber &&
                               pd.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking if drawing number exists for package {PackageId}",
                packageId
            );
            return false;
        }
    }
}