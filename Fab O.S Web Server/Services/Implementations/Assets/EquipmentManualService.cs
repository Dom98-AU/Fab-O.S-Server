using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations.Assets;

/// <summary>
/// Service implementation for Equipment Manual management
/// </summary>
public class EquipmentManualService : IEquipmentManualService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EquipmentManualService> _logger;

    public EquipmentManualService(ApplicationDbContext context, ILogger<EquipmentManualService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region CRUD Operations

    public async Task<EquipmentManual?> GetManualByIdAsync(int id)
    {
        return await _context.EquipmentManuals
            .Include(m => m.Equipment)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<EquipmentManual>> GetManualsByEquipmentAsync(int equipmentId)
    {
        return await _context.EquipmentManuals
            .Include(m => m.Equipment)
            .Where(m => m.EquipmentId == equipmentId)
            .OrderBy(m => m.ManualType)
            .ThenBy(m => m.Title)
            .ToListAsync();
    }

    public async Task<IEnumerable<EquipmentManual>> GetManualsPagedAsync(int companyId, int page, int pageSize, int? equipmentId = null, string? type = null)
    {
        var query = _context.EquipmentManuals
            .Include(m => m.Equipment)
            .Where(m => m.Equipment!.CompanyId == companyId);

        if (equipmentId.HasValue)
            query = query.Where(m => m.EquipmentId == equipmentId.Value);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(m => m.ManualType == type);

        return await query
            .OrderBy(m => m.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetManualsCountAsync(int companyId, int? equipmentId = null, string? type = null)
    {
        var query = _context.EquipmentManuals
            .Where(m => m.Equipment!.CompanyId == companyId);

        if (equipmentId.HasValue)
            query = query.Where(m => m.EquipmentId == equipmentId.Value);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(m => m.ManualType == type);

        return await query.CountAsync();
    }

    public async Task<EquipmentManual> CreateManualAsync(EquipmentManual manual)
    {
        manual.UploadedDate = DateTime.UtcNow;

        _context.EquipmentManuals.Add(manual);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created manual {Title} for equipment {EquipmentId}", manual.Title, manual.EquipmentId);
        return manual;
    }

    public async Task<EquipmentManual> UpdateManualAsync(EquipmentManual manual)
    {
        manual.LastModified = DateTime.UtcNow;
        _context.EquipmentManuals.Update(manual);
        await _context.SaveChangesAsync();
        return manual;
    }

    public async Task<bool> DeleteManualAsync(int id)
    {
        var manual = await _context.EquipmentManuals.FindAsync(id);
        if (manual == null)
            return false;

        _context.EquipmentManuals.Remove(manual);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Type Operations

    public async Task<IEnumerable<string>> GetManualTypesAsync(int companyId)
    {
        return await _context.EquipmentManuals
            .Where(m => m.Equipment!.CompanyId == companyId)
            .Select(m => m.ManualType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetManualTypesForEquipmentAsync(int equipmentId)
    {
        return await _context.EquipmentManuals
            .Where(m => m.EquipmentId == equipmentId)
            .Select(m => m.ManualType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }

    public async Task<IEnumerable<EquipmentManual>> GetManualsByTypeAsync(int equipmentId, string manualType)
    {
        return await _context.EquipmentManuals
            .Include(m => m.Equipment)
            .Where(m => m.EquipmentId == equipmentId && m.ManualType == manualType)
            .OrderBy(m => m.Title)
            .ToListAsync();
    }

    #endregion

    #region Search Operations

    public async Task<IEnumerable<EquipmentManual>> SearchManualsAsync(int companyId, string searchTerm)
    {
        searchTerm = searchTerm.ToLower();
        return await _context.EquipmentManuals
            .Include(m => m.Equipment)
            .Where(m => m.Equipment!.CompanyId == companyId &&
                       (m.Title.ToLower().Contains(searchTerm) ||
                        (m.Description != null && m.Description.ToLower().Contains(searchTerm)) ||
                        m.ManualType.ToLower().Contains(searchTerm)))
            .OrderBy(m => m.Title)
            .ToListAsync();
    }

    public async Task<IEnumerable<EquipmentManual>> SearchManualsByTitleAsync(int companyId, string title)
    {
        title = title.ToLower();
        return await _context.EquipmentManuals
            .Include(m => m.Equipment)
            .Where(m => m.Equipment!.CompanyId == companyId &&
                       m.Title.ToLower().Contains(title))
            .OrderBy(m => m.Title)
            .ToListAsync();
    }

    #endregion

    #region Bulk Operations

    public async Task<bool> CopyManualsToEquipmentAsync(int sourceEquipmentId, int targetEquipmentId)
    {
        var sourceManuals = await GetManualsByEquipmentAsync(sourceEquipmentId);

        if (!sourceManuals.Any())
            return false;

        foreach (var sourceManual in sourceManuals)
        {
            var newManual = new EquipmentManual
            {
                EquipmentId = targetEquipmentId,
                ManualType = sourceManual.ManualType,
                Title = sourceManual.Title,
                Description = sourceManual.Description,
                DocumentUrl = sourceManual.DocumentUrl,
                Version = sourceManual.Version,
                FileName = sourceManual.FileName,
                FileSize = sourceManual.FileSize,
                ContentType = sourceManual.ContentType,
                UploadedDate = DateTime.UtcNow,
                UploadedBy = "System (Copied)"
            };

            _context.EquipmentManuals.Add(newManual);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Copied {Count} manuals from equipment {SourceId} to {TargetId}", sourceManuals.Count(), sourceEquipmentId, targetEquipmentId);
        return true;
    }

    public async Task<bool> DeleteAllManualsForEquipmentAsync(int equipmentId)
    {
        var manuals = await _context.EquipmentManuals
            .Where(m => m.EquipmentId == equipmentId)
            .ToListAsync();

        if (!manuals.Any())
            return false;

        _context.EquipmentManuals.RemoveRange(manuals);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted {Count} manuals for equipment {EquipmentId}", manuals.Count, equipmentId);
        return true;
    }

    #endregion

    #region Document Operations

    public async Task<bool> UpdateManualDocumentAsync(int id, string documentUrl, string? fileName = null, long? fileSize = null, string? contentType = null)
    {
        var manual = await _context.EquipmentManuals.FindAsync(id);
        if (manual == null)
            return false;

        manual.DocumentUrl = documentUrl;
        manual.LastModified = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(fileName))
            manual.FileName = fileName;

        if (fileSize.HasValue)
            manual.FileSize = fileSize.Value;

        if (!string.IsNullOrWhiteSpace(contentType))
            manual.ContentType = contentType;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<string?> GetManualDocumentUrlAsync(int id)
    {
        var manual = await _context.EquipmentManuals.FindAsync(id);
        return manual?.DocumentUrl;
    }

    public async Task<bool> UpdateManualVersionAsync(int id, string version)
    {
        var manual = await _context.EquipmentManuals.FindAsync(id);
        if (manual == null)
            return false;

        manual.Version = version;
        manual.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Analytics

    public async Task<Dictionary<int, int>> GetManualCountsByEquipmentAsync(int companyId)
    {
        return await _context.EquipmentManuals
            .Where(m => m.Equipment!.CompanyId == companyId)
            .GroupBy(m => m.EquipmentId)
            .Select(g => new { EquipmentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EquipmentId, x => x.Count);
    }

    public async Task<Dictionary<string, int>> GetManualCountsByTypeAsync(int companyId)
    {
        return await _context.EquipmentManuals
            .Where(m => m.Equipment!.CompanyId == companyId)
            .GroupBy(m => m.ManualType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count);
    }

    public async Task<IEnumerable<EquipmentManual>> GetRecentManualsAsync(int companyId, int count = 10)
    {
        return await _context.EquipmentManuals
            .Include(m => m.Equipment)
            .Where(m => m.Equipment!.CompanyId == companyId)
            .OrderByDescending(m => m.UploadedDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<Equipment>> GetEquipmentWithoutManualsAsync(int companyId)
    {
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Where(e => e.CompanyId == companyId &&
                       !e.IsDeleted &&
                       !e.Manuals.Any())
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<long> GetTotalManualStorageSizeAsync(int companyId)
    {
        return await _context.EquipmentManuals
            .Where(m => m.Equipment!.CompanyId == companyId && m.FileSize != null)
            .SumAsync(m => m.FileSize ?? 0);
    }

    #endregion
}
