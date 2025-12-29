using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.DTOs.Assets;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations.Assets;

/// <summary>
/// Service implementation for managing customizable label templates
/// </summary>
public class LabelTemplateService : ILabelTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LabelTemplateService> _logger;

    // Standard preset sizes
    private static readonly List<LabelSizePreset> _presetSizes = new()
    {
        new LabelSizePreset { Name = "Small", Description = "Small label (30mm x 15mm)", WidthMm = 30, HeightMm = 15 },
        new LabelSizePreset { Name = "Standard", Description = "Standard label (50mm x 25mm)", WidthMm = 50, HeightMm = 25 },
        new LabelSizePreset { Name = "Large", Description = "Large label (100mm x 50mm)", WidthMm = 100, HeightMm = 50 },
        new LabelSizePreset { Name = "Inventory", Description = "Inventory tag (75mm x 35mm)", WidthMm = 75, HeightMm = 35 },
        new LabelSizePreset { Name = "Dymo Small", Description = "Dymo 30336 (25mm x 54mm)", WidthMm = 25, HeightMm = 54 },
        new LabelSizePreset { Name = "Dymo Large", Description = "Dymo 30323 (54mm x 101mm)", WidthMm = 54, HeightMm = 101 },
        new LabelSizePreset { Name = "Brother Small", Description = "Brother DK-1201 (29mm x 90mm)", WidthMm = 29, HeightMm = 90 },
        new LabelSizePreset { Name = "Avery 5160", Description = "Avery 5160 (25mm x 66mm)", WidthMm = 25, HeightMm = 66 }
    };

    public LabelTemplateService(
        ApplicationDbContext context,
        ILogger<LabelTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region CRUD Operations

    public async Task<LabelTemplate> CreateAsync(int companyId, CreateLabelTemplateRequest request, int userId)
    {
        // Check for duplicate name
        if (await TemplateNameExistsAsync(companyId, request.Name))
        {
            throw new InvalidOperationException($"A template named '{request.Name}' already exists");
        }

        var template = new LabelTemplate
        {
            CompanyId = companyId,
            Name = request.Name,
            Description = request.Description,
            EntityType = request.EntityType,
            WidthMm = request.WidthMm,
            HeightMm = request.HeightMm,
            IncludeQRCode = request.IncludeQRCode,
            QRCodePixelsPerModule = request.QRCodePixelsPerModule,
            IncludeCode = request.IncludeCode,
            IncludeName = request.IncludeName,
            IncludeCategory = request.IncludeCategory,
            IncludeLocation = request.IncludeLocation,
            IncludeSerialNumber = request.IncludeSerialNumber,
            IncludeServiceDate = request.IncludeServiceDate,
            IncludeContactInfo = request.IncludeContactInfo,
            PrimaryFontSize = request.PrimaryFontSize,
            SecondaryFontSize = request.SecondaryFontSize,
            MarginMm = request.MarginMm,
            IsSystemTemplate = false,
            IsDefault = false,
            CreatedDate = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        _context.LabelTemplates.Add(template);
        await _context.SaveChangesAsync();

        // Set as default if requested
        if (request.SetAsDefault)
        {
            await SetAsDefaultAsync(template.Id, companyId);
        }

        _logger.LogInformation("Created label template '{Name}' (ID: {Id}) for company {CompanyId}",
            template.Name, template.Id, companyId);

        return template;
    }

    public async Task<LabelTemplate?> GetByIdAsync(int id)
    {
        return await _context.LabelTemplates
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
    }

    public async Task<LabelTemplate?> GetByNameAsync(int companyId, string name)
    {
        // First try to find a company-specific template
        var template = await _context.LabelTemplates
            .FirstOrDefaultAsync(t => t.CompanyId == companyId &&
                                      t.Name == name &&
                                      !t.IsDeleted);

        // If not found, try system templates
        if (template == null)
        {
            template = await _context.LabelTemplates
                .FirstOrDefaultAsync(t => t.IsSystemTemplate &&
                                          t.Name == name &&
                                          !t.IsDeleted);
        }

        return template;
    }

    public async Task<IEnumerable<LabelTemplate>> GetAllForCompanyAsync(int companyId, string? entityType = null, bool includeSystem = true)
    {
        var query = _context.LabelTemplates
            .Where(t => !t.IsDeleted)
            .Where(t => t.CompanyId == companyId || (includeSystem && t.IsSystemTemplate));

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(t => t.EntityType == entityType || t.EntityType == "All");
        }

        return await query
            .OrderBy(t => t.IsSystemTemplate) // Company templates first
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<LabelTemplate>> GetSystemTemplatesAsync(string? entityType = null)
    {
        var query = _context.LabelTemplates
            .Where(t => t.IsSystemTemplate && !t.IsDeleted);

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(t => t.EntityType == entityType || t.EntityType == "All");
        }

        return await query.OrderBy(t => t.Name).ToListAsync();
    }

    public async Task<LabelTemplate> UpdateAsync(int id, UpdateLabelTemplateRequest request, int userId)
    {
        var template = await GetByIdAsync(id);
        if (template == null)
        {
            throw new KeyNotFoundException($"Template with ID {id} not found");
        }

        if (!CanModifyTemplate(template))
        {
            throw new InvalidOperationException("System templates cannot be modified");
        }

        // Check for duplicate name (excluding current template)
        if (await TemplateNameExistsAsync(template.CompanyId!.Value, request.Name, id))
        {
            throw new InvalidOperationException($"A template named '{request.Name}' already exists");
        }

        template.Name = request.Name;
        template.Description = request.Description;
        template.EntityType = request.EntityType;
        template.WidthMm = request.WidthMm;
        template.HeightMm = request.HeightMm;
        template.IncludeQRCode = request.IncludeQRCode;
        template.QRCodePixelsPerModule = request.QRCodePixelsPerModule;
        template.IncludeCode = request.IncludeCode;
        template.IncludeName = request.IncludeName;
        template.IncludeCategory = request.IncludeCategory;
        template.IncludeLocation = request.IncludeLocation;
        template.IncludeSerialNumber = request.IncludeSerialNumber;
        template.IncludeServiceDate = request.IncludeServiceDate;
        template.IncludeContactInfo = request.IncludeContactInfo;
        template.PrimaryFontSize = request.PrimaryFontSize;
        template.SecondaryFontSize = request.SecondaryFontSize;
        template.MarginMm = request.MarginMm;
        template.LastModified = DateTime.UtcNow;
        template.LastModifiedByUserId = userId;

        await _context.SaveChangesAsync();

        // Handle default status change
        if (request.SetAsDefault && !template.IsDefault)
        {
            await SetAsDefaultAsync(id, template.CompanyId!.Value);
        }

        _logger.LogInformation("Updated label template '{Name}' (ID: {Id})", template.Name, id);

        return template;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var template = await GetByIdAsync(id);
        if (template == null)
        {
            return false;
        }

        if (!CanModifyTemplate(template))
        {
            throw new InvalidOperationException("System templates cannot be deleted");
        }

        template.IsDeleted = true;
        template.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Soft-deleted label template '{Name}' (ID: {Id})", template.Name, id);

        return true;
    }

    #endregion

    #region Default Template Management

    public async Task<LabelTemplate?> GetDefaultTemplateAsync(int companyId, string entityType)
    {
        // First try to find a company default for the specific entity type
        var template = await _context.LabelTemplates
            .FirstOrDefaultAsync(t => t.CompanyId == companyId &&
                                      t.IsDefault &&
                                      !t.IsDeleted &&
                                      (t.EntityType == entityType || t.EntityType == "All"));

        // If not found, try system default
        if (template == null)
        {
            template = await _context.LabelTemplates
                .FirstOrDefaultAsync(t => t.IsSystemTemplate &&
                                          t.Name == "Standard" &&
                                          !t.IsDeleted);
        }

        return template;
    }

    public async Task SetAsDefaultAsync(int id, int companyId)
    {
        var template = await GetByIdAsync(id);
        if (template == null)
        {
            throw new KeyNotFoundException($"Template with ID {id} not found");
        }

        // Clear existing defaults for this entity type
        await ClearDefaultAsync(companyId, template.EntityType);

        // Set new default
        template.IsDefault = true;
        template.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Set template '{Name}' (ID: {Id}) as default for entity type '{EntityType}'",
            template.Name, id, template.EntityType);
    }

    public async Task ClearDefaultAsync(int companyId, string entityType)
    {
        var defaults = await _context.LabelTemplates
            .Where(t => t.CompanyId == companyId &&
                        t.IsDefault &&
                        !t.IsDeleted &&
                        (t.EntityType == entityType || t.EntityType == "All"))
            .ToListAsync();

        foreach (var template in defaults)
        {
            template.IsDefault = false;
            template.LastModified = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    #endregion

    #region Template Resolution

    public async Task<LabelTemplate> ResolveTemplateAsync(int companyId, int? templateId, string? templateName, string entityType)
    {
        LabelTemplate? template = null;

        // Priority 1: Template ID
        if (templateId.HasValue)
        {
            template = await GetByIdAsync(templateId.Value);
            if (template != null)
            {
                return template;
            }
            _logger.LogWarning("Template ID {TemplateId} not found, falling back to other resolution methods", templateId);
        }

        // Priority 2: Template Name
        if (!string.IsNullOrEmpty(templateName))
        {
            template = await GetByNameAsync(companyId, templateName);
            if (template != null)
            {
                return template;
            }
            _logger.LogWarning("Template name '{TemplateName}' not found, falling back to defaults", templateName);
        }

        // Priority 3: Company Default for entity type
        template = await GetDefaultTemplateAsync(companyId, entityType);
        if (template != null)
        {
            return template;
        }

        // Priority 4: System "Standard" template
        template = await _context.LabelTemplates
            .FirstOrDefaultAsync(t => t.IsSystemTemplate && t.Name == "Standard" && !t.IsDeleted);

        if (template == null)
        {
            throw new InvalidOperationException("No suitable template found. Please ensure system templates are seeded.");
        }

        return template;
    }

    #endregion

    #region Validation

    public async Task<bool> TemplateNameExistsAsync(int companyId, string name, int? excludeId = null)
    {
        var query = _context.LabelTemplates
            .Where(t => t.CompanyId == companyId &&
                        t.Name == name &&
                        !t.IsDeleted);

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public bool CanModifyTemplate(LabelTemplate template)
    {
        return !template.IsSystemTemplate;
    }

    #endregion

    #region Preset Sizes

    public IEnumerable<LabelSizePreset> GetPresetSizes()
    {
        return _presetSizes;
    }

    #endregion
}
