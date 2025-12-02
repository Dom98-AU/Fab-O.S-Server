using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations.Assets;

/// <summary>
/// Service implementation for Equipment Certification management
/// </summary>
public class CertificationService : ICertificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CertificationService> _logger;

    public CertificationService(ApplicationDbContext context, ILogger<CertificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region CRUD Operations

    public async Task<EquipmentCertification?> GetCertificationByIdAsync(int id)
    {
        return await _context.EquipmentCertifications
            .Include(c => c.Equipment)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<EquipmentCertification>> GetCertificationsByEquipmentAsync(int equipmentId)
    {
        return await _context.EquipmentCertifications
            .Include(c => c.Equipment)
            .Where(c => c.EquipmentId == equipmentId)
            .OrderBy(c => c.ExpiryDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<EquipmentCertification>> GetCertificationsPagedAsync(int companyId, int page, int pageSize, int? equipmentId = null, string? status = null, string? type = null)
    {
        var query = _context.EquipmentCertifications
            .Include(c => c.Equipment)
            .Where(c => c.Equipment!.CompanyId == companyId);

        if (equipmentId.HasValue)
            query = query.Where(c => c.EquipmentId == equipmentId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(c => c.CertificationType == type);

        return await query
            .OrderBy(c => c.ExpiryDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCertificationsCountAsync(int companyId, int? equipmentId = null, string? status = null, string? type = null)
    {
        var query = _context.EquipmentCertifications
            .Where(c => c.Equipment!.CompanyId == companyId);

        if (equipmentId.HasValue)
            query = query.Where(c => c.EquipmentId == equipmentId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(c => c.CertificationType == type);

        return await query.CountAsync();
    }

    public async Task<EquipmentCertification> CreateCertificationAsync(EquipmentCertification certification)
    {
        certification.CreatedDate = DateTime.UtcNow;
        certification.Status = DetermineStatus(certification.ExpiryDate);

        _context.EquipmentCertifications.Add(certification);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created certification {CertificationType} for equipment {EquipmentId}", certification.CertificationType, certification.EquipmentId);
        return certification;
    }

    public async Task<EquipmentCertification> UpdateCertificationAsync(EquipmentCertification certification)
    {
        certification.LastModified = DateTime.UtcNow;
        certification.Status = DetermineStatus(certification.ExpiryDate);

        _context.EquipmentCertifications.Update(certification);
        await _context.SaveChangesAsync();
        return certification;
    }

    public async Task<bool> DeleteCertificationAsync(int id)
    {
        var certification = await _context.EquipmentCertifications.FindAsync(id);
        if (certification == null)
            return false;

        _context.EquipmentCertifications.Remove(certification);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Status Operations

    public async Task<IEnumerable<EquipmentCertification>> GetValidCertificationsAsync(int companyId)
    {
        return await _context.EquipmentCertifications
            .Include(c => c.Equipment)
            .Where(c => c.Equipment!.CompanyId == companyId && c.Status == "Valid")
            .OrderBy(c => c.ExpiryDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<EquipmentCertification>> GetExpiredCertificationsAsync(int companyId)
    {
        return await _context.EquipmentCertifications
            .Include(c => c.Equipment)
            .Where(c => c.Equipment!.CompanyId == companyId &&
                       (c.Status == "Expired" || (c.ExpiryDate != null && c.ExpiryDate < DateTime.UtcNow)))
            .OrderBy(c => c.ExpiryDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<EquipmentCertification>> GetExpiringCertificationsAsync(int companyId, int daysAhead = 30)
    {
        var futureDate = DateTime.UtcNow.AddDays(daysAhead);
        return await _context.EquipmentCertifications
            .Include(c => c.Equipment)
            .Where(c => c.Equipment!.CompanyId == companyId &&
                       c.ExpiryDate != null &&
                       c.ExpiryDate <= futureDate &&
                       c.ExpiryDate >= DateTime.UtcNow &&
                       c.Status != "Revoked")
            .OrderBy(c => c.ExpiryDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<EquipmentCertification>> GetCertificationsByStatusAsync(int companyId, string status)
    {
        return await _context.EquipmentCertifications
            .Include(c => c.Equipment)
            .Where(c => c.Equipment!.CompanyId == companyId && c.Status == status)
            .OrderBy(c => c.ExpiryDate)
            .ToListAsync();
    }

    public async Task UpdateCertificationStatusesAsync(int companyId)
    {
        var certifications = await _context.EquipmentCertifications
            .Where(c => c.Equipment!.CompanyId == companyId && c.Status != "Revoked")
            .ToListAsync();

        foreach (var cert in certifications)
        {
            var newStatus = DetermineStatus(cert.ExpiryDate);
            if (cert.Status != newStatus)
            {
                cert.Status = newStatus;
                cert.LastModified = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated certification statuses for company {CompanyId}", companyId);
    }

    private string DetermineStatus(DateTime? expiryDate)
    {
        if (!expiryDate.HasValue)
            return "Valid";

        if (expiryDate.Value < DateTime.UtcNow)
            return "Expired";

        if (expiryDate.Value <= DateTime.UtcNow.AddDays(30))
            return "Expiring";

        return "Valid";
    }

    #endregion

    #region Renewal and Revocation

    public async Task<EquipmentCertification> RenewCertificationAsync(int id, DateTime newIssueDate, DateTime? newExpiryDate, string? newCertificateNumber = null, string? documentUrl = null)
    {
        var certification = await GetCertificationByIdAsync(id);
        if (certification == null)
            throw new InvalidOperationException($"Certification with ID {id} not found");

        certification.IssueDate = newIssueDate;
        certification.ExpiryDate = newExpiryDate;
        certification.Status = DetermineStatus(newExpiryDate);
        certification.LastModified = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(newCertificateNumber))
            certification.CertificateNumber = newCertificateNumber;

        if (!string.IsNullOrWhiteSpace(documentUrl))
            certification.DocumentUrl = documentUrl;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Renewed certification {Id} for equipment {EquipmentId}", id, certification.EquipmentId);
        return certification;
    }

    public async Task<bool> RevokeCertificationAsync(int id, string reason)
    {
        var certification = await GetCertificationByIdAsync(id);
        if (certification == null)
            return false;

        certification.Status = "Revoked";
        certification.Notes = $"Revoked: {reason}. {certification.Notes}";
        certification.LastModified = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Revoked certification {Id} for equipment {EquipmentId}: {Reason}", id, certification.EquipmentId, reason);
        return true;
    }

    #endregion

    #region Type Operations

    public async Task<IEnumerable<string>> GetCertificationTypesAsync(int companyId)
    {
        return await _context.EquipmentCertifications
            .Where(c => c.Equipment!.CompanyId == companyId)
            .Select(c => c.CertificationType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }

    public async Task<IEnumerable<EquipmentCertification>> GetCertificationsByTypeAsync(int companyId, string certificationType)
    {
        return await _context.EquipmentCertifications
            .Include(c => c.Equipment)
            .Where(c => c.Equipment!.CompanyId == companyId && c.CertificationType == certificationType)
            .OrderBy(c => c.ExpiryDate)
            .ToListAsync();
    }

    #endregion

    #region Compliance

    public async Task<bool> IsEquipmentFullyCompliantAsync(int equipmentId)
    {
        var equipment = await _context.Equipment
            .Include(e => e.EquipmentType)
            .Include(e => e.Certifications)
            .FirstOrDefaultAsync(e => e.Id == equipmentId);

        if (equipment == null)
            return false;

        // Check if all required certifications are valid
        var requiredCerts = equipment.EquipmentType?.RequiredCertifications?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

        foreach (var requiredCert in requiredCerts)
        {
            var cert = equipment.Certifications
                .FirstOrDefault(c => c.CertificationType.Equals(requiredCert.Trim(), StringComparison.OrdinalIgnoreCase) && c.Status == "Valid");

            if (cert == null)
                return false;
        }

        // Also check for any expired certifications
        var hasExpired = equipment.Certifications.Any(c => c.Status == "Expired");
        return !hasExpired;
    }

    public async Task<IEnumerable<Equipment>> GetNonCompliantEquipmentAsync(int companyId)
    {
        var equipment = await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Include(e => e.Certifications)
            .Where(e => e.CompanyId == companyId && !e.IsDeleted)
            .ToListAsync();

        var nonCompliant = new List<Equipment>();

        foreach (var eq in equipment)
        {
            if (!await IsEquipmentFullyCompliantAsync(eq.Id))
            {
                nonCompliant.Add(eq);
            }
        }

        return nonCompliant;
    }

    public async Task<IEnumerable<string>> GetMissingCertificationTypesAsync(int equipmentId)
    {
        var equipment = await _context.Equipment
            .Include(e => e.EquipmentType)
            .Include(e => e.Certifications)
            .FirstOrDefaultAsync(e => e.Id == equipmentId);

        if (equipment == null)
            return Enumerable.Empty<string>();

        var requiredCerts = equipment.EquipmentType?.RequiredCertifications?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim())
            .ToList() ?? new List<string>();

        var validCerts = equipment.Certifications
            .Where(c => c.Status == "Valid")
            .Select(c => c.CertificationType)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return requiredCerts.Where(r => !validCerts.Contains(r));
    }

    #endregion

    #region Analytics

    public async Task<Dictionary<string, int>> GetCertificationCountByStatusAsync(int companyId)
    {
        return await _context.EquipmentCertifications
            .Where(c => c.Equipment!.CompanyId == companyId)
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);
    }

    public async Task<Dictionary<string, int>> GetCertificationCountByTypeAsync(int companyId)
    {
        return await _context.EquipmentCertifications
            .Where(c => c.Equipment!.CompanyId == companyId)
            .GroupBy(c => c.CertificationType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count);
    }

    public async Task<IEnumerable<EquipmentCertification>> GetRecentCertificationsAsync(int companyId, int count = 10)
    {
        return await _context.EquipmentCertifications
            .Include(c => c.Equipment)
            .Where(c => c.Equipment!.CompanyId == companyId)
            .OrderByDescending(c => c.CreatedDate)
            .Take(count)
            .ToListAsync();
    }

    #endregion
}
