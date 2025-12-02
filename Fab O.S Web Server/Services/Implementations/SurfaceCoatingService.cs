using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Services.Implementations;

public class SurfaceCoatingService : ISurfaceCoatingService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<SurfaceCoatingService> _logger;

    public SurfaceCoatingService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<SurfaceCoatingService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<List<SurfaceCoating>> GetActiveCoatingsAsync(int companyId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var coatings = await context.SurfaceCoatings
                .Where(sc => sc.CompanyId == companyId && sc.IsActive)
                .OrderBy(sc => sc.DisplayOrder)
                .ThenBy(sc => sc.CoatingName)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} active surface coatings for company {CompanyId}",
                coatings.Count, companyId);

            return coatings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active surface coatings for company {CompanyId}", companyId);
            throw;
        }
    }

    public async Task<SurfaceCoating?> GetCoatingByIdAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var coating = await context.SurfaceCoatings
                .FirstOrDefaultAsync(sc => sc.Id == id);

            return coating;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving surface coating {Id}", id);
            throw;
        }
    }

    public async Task<SurfaceCoating> CreateCoatingAsync(SurfaceCoating coating)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            coating.CreatedDate = DateTime.UtcNow;

            context.SurfaceCoatings.Add(coating);
            await context.SaveChangesAsync();

            _logger.LogInformation("Created surface coating {CoatingCode} - {CoatingName} for company {CompanyId}",
                coating.CoatingCode, coating.CoatingName, coating.CompanyId);

            return coating;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating surface coating {CoatingCode}", coating.CoatingCode);
            throw;
        }
    }

    public async Task<SurfaceCoating> UpdateCoatingAsync(SurfaceCoating coating)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            coating.ModifiedDate = DateTime.UtcNow;

            context.SurfaceCoatings.Update(coating);
            await context.SaveChangesAsync();

            _logger.LogInformation("Updated surface coating {Id} - {CoatingName}",
                coating.Id, coating.CoatingName);

            return coating;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating surface coating {Id}", coating.Id);
            throw;
        }
    }

    public async Task<bool> DeleteCoatingAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var coating = await context.SurfaceCoatings.FindAsync(id);
            if (coating == null)
            {
                _logger.LogWarning("Surface coating {Id} not found for deletion", id);
                return false;
            }

            // Soft delete - just mark as inactive
            coating.IsActive = false;
            coating.ModifiedDate = DateTime.UtcNow;

            await context.SaveChangesAsync();

            _logger.LogInformation("Deleted (soft) surface coating {Id} - {CoatingName}",
                id, coating.CoatingName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting surface coating {Id}", id);
            throw;
        }
    }
}
