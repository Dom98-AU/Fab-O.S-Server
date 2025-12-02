using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces;

public interface ISurfaceCoatingService
{
    /// <summary>
    /// Get all active surface coatings for a company
    /// </summary>
    Task<List<SurfaceCoating>> GetActiveCoatingsAsync(int companyId);

    /// <summary>
    /// Get surface coating by ID
    /// </summary>
    Task<SurfaceCoating?> GetCoatingByIdAsync(int id);

    /// <summary>
    /// Create a new surface coating
    /// </summary>
    Task<SurfaceCoating> CreateCoatingAsync(SurfaceCoating coating);

    /// <summary>
    /// Update an existing surface coating
    /// </summary>
    Task<SurfaceCoating> UpdateCoatingAsync(SurfaceCoating coating);

    /// <summary>
    /// Delete a surface coating (soft delete by setting IsActive = false)
    /// </summary>
    Task<bool> DeleteCoatingAsync(int id);
}
