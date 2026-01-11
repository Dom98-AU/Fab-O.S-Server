using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.DTOs.Forms;
using FabOS.WebServer.Models.Entities.Forms;
using FabOS.WebServer.Services.Interfaces.Forms;

namespace FabOS.WebServer.Services.Implementations.Forms;

/// <summary>
/// Service implementation for managing form templates
/// </summary>
public class FormTemplateService : IFormTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FormTemplateService> _logger;

    // Page size presets (standard paper sizes)
    private static readonly List<FormPageSizePreset> _pageSizePresets = new()
    {
        new() { Name = "A4", Description = "A4 (210mm x 297mm)", WidthMm = 210, HeightMm = 297 },
        new() { Name = "A5", Description = "A5 (148mm x 210mm)", WidthMm = 148, HeightMm = 210 },
        new() { Name = "Letter", Description = "US Letter (216mm x 279mm)", WidthMm = 216, HeightMm = 279 },
        new() { Name = "Legal", Description = "US Legal (216mm x 356mm)", WidthMm = 216, HeightMm = 356 },
        new() { Name = "A3", Description = "A3 (297mm x 420mm)", WidthMm = 297, HeightMm = 420 }
    };

    // Section layout options
    private static readonly List<SectionLayoutOption> _sectionLayoutOptions = new()
    {
        new()
        {
            LayoutType = "1-col",
            DisplayName = "Single Column",
            Description = "Full width single column layout",
            ColumnCount = 1,
            GridTemplateColumns = "1fr",
            MinPageWidthMm = 100,
            Icon = "fas fa-square"
        },
        new()
        {
            LayoutType = "2-col-equal",
            DisplayName = "Two Equal Columns",
            Description = "Two columns of equal width (50% / 50%)",
            ColumnCount = 2,
            GridTemplateColumns = "1fr 1fr",
            MinPageWidthMm = 140,
            Icon = "fas fa-columns"
        },
        new()
        {
            LayoutType = "2-col-left",
            DisplayName = "Two Columns (Wide Left)",
            Description = "Two columns with wider left side (66% / 33%)",
            ColumnCount = 2,
            GridTemplateColumns = "2fr 1fr",
            MinPageWidthMm = 140,
            Icon = "fas fa-align-left"
        },
        new()
        {
            LayoutType = "2-col-right",
            DisplayName = "Two Columns (Wide Right)",
            Description = "Two columns with wider right side (33% / 66%)",
            ColumnCount = 2,
            GridTemplateColumns = "1fr 2fr",
            MinPageWidthMm = 140,
            Icon = "fas fa-align-right"
        },
        new()
        {
            LayoutType = "3-col-equal",
            DisplayName = "Three Equal Columns",
            Description = "Three columns of equal width (33% / 33% / 33%)",
            ColumnCount = 3,
            GridTemplateColumns = "1fr 1fr 1fr",
            MinPageWidthMm = 180,
            Icon = "fas fa-th"
        }
    };

    public FormTemplateService(
        ApplicationDbContext context,
        ILogger<FormTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<FormTemplateListResponse> GetTemplatesAsync(
        int companyId,
        FormModuleContext? moduleContext = null,
        bool includeSystemTemplates = true,
        int page = 1,
        int pageSize = 20,
        string? search = null)
    {
        var query = _context.FormTemplates
            .Where(t => t.CompanyId == companyId && !t.IsDeleted);

        if (moduleContext.HasValue)
        {
            query = query.Where(t => t.ModuleContext == moduleContext.Value);
        }

        if (!includeSystemTemplates)
        {
            query = query.Where(t => !t.IsSystemTemplate);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(t =>
                t.Name.ToLower().Contains(searchLower) ||
                (t.Description != null && t.Description.ToLower().Contains(searchLower)) ||
                (t.FormType != null && t.FormType.ToLower().Contains(searchLower)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.ModifiedDate ?? t.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new FormTemplateSummaryDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                ModuleContext = t.ModuleContext,
                FormType = t.FormType,
                IsSystemTemplate = t.IsSystemTemplate,
                IsCompanyDefault = t.IsCompanyDefault,
                IsPublished = t.IsPublished,
                Version = t.Version,
                FieldCount = t.Fields.Count(f => !f.IsDeleted),
                InstanceCount = t.Instances.Count(i => !i.IsDeleted),
                CreatedDate = t.CreatedDate,
                CreatedByName = t.CreatedByUser != null ? t.CreatedByUser.FirstName + " " + t.CreatedByUser.LastName : null,
                ModifiedDate = t.ModifiedDate,
                PageWidthMm = t.PageWidthMm,
                PageHeightMm = t.PageHeightMm,
                PageOrientation = t.PageOrientation
            })
            .ToListAsync();

        return new FormTemplateListResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<FormTemplateDto?> GetTemplateByIdAsync(int id, int companyId)
    {
        return await _context.FormTemplates
            .Where(t => t.Id == id && t.CompanyId == companyId && !t.IsDeleted)
            .Select(t => MapToDto(t))
            .FirstOrDefaultAsync();
    }

    public async Task<FormTemplateDto?> GetTemplateWithFieldsAsync(int id, int companyId)
    {
        var template = await _context.FormTemplates
            .Include(t => t.Fields.Where(f => !f.IsDeleted).OrderBy(f => f.SectionOrder).ThenBy(f => f.DisplayOrder))
            .ThenInclude(f => f.LinkedWorksheetTemplate)
            .Include(t => t.CreatedByUser)
            .Include(t => t.ModifiedByUser)
            .Where(t => t.Id == id && t.CompanyId == companyId && !t.IsDeleted)
            .FirstOrDefaultAsync();

        if (template == null) return null;

        return MapToDtoWithFields(template);
    }

    public async Task<FormTemplateDto> CreateTemplateAsync(
        CreateFormTemplateRequest request,
        int companyId,
        int userId)
    {
        var template = new FormTemplate
        {
            CompanyId = companyId,
            Name = request.Name,
            Description = request.Description,
            ModuleContext = request.ModuleContext,
            FormType = request.FormType,
            IsCompanyDefault = request.IsCompanyDefault,
            NumberPrefix = request.NumberPrefix,
            ShowSectionHeaders = request.ShowSectionHeaders,
            AllowNotes = request.AllowNotes,
            PageWidthMm = request.PageWidthMm,
            PageHeightMm = request.PageHeightMm,
            PageOrientation = request.PageOrientation,
            CreatedByUserId = userId,
            CreatedDate = DateTime.UtcNow
        };

        // If setting as company default, clear existing default
        if (request.IsCompanyDefault)
        {
            await ClearExistingDefaultAsync(companyId, request.ModuleContext, request.FormType);
        }

        _context.FormTemplates.Add(template);
        await _context.SaveChangesAsync();

        // Add fields if provided
        if (request.Fields?.Any() == true)
        {
            foreach (var fieldRequest in request.Fields)
            {
                var field = CreateField(template.Id, fieldRequest);
                _context.FormTemplateFields.Add(field);
            }
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Created form template {TemplateId} '{Name}' for company {CompanyId}",
            template.Id, template.Name, companyId);

        return await GetTemplateWithFieldsAsync(template.Id, companyId)
            ?? throw new InvalidOperationException("Failed to retrieve created template");
    }

    public async Task<FormTemplateDto> UpdateTemplateAsync(
        int id,
        UpdateFormTemplateRequest request,
        int companyId,
        int userId)
    {
        var template = await _context.FormTemplates
            .Include(t => t.Fields.Where(f => !f.IsDeleted))
            .Where(t => t.Id == id && t.CompanyId == companyId && !t.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Template {id} not found");

        if (template.IsSystemTemplate)
        {
            throw new InvalidOperationException("Cannot modify system templates");
        }

        // Update basic properties
        template.Name = request.Name;
        template.Description = request.Description;
        template.ModuleContext = request.ModuleContext;
        template.FormType = request.FormType;
        template.IsPublished = request.IsPublished;
        template.NumberPrefix = request.NumberPrefix;
        template.ShowSectionHeaders = request.ShowSectionHeaders;
        template.AllowNotes = request.AllowNotes;
        template.PageWidthMm = request.PageWidthMm;
        template.PageHeightMm = request.PageHeightMm;
        template.PageOrientation = request.PageOrientation;
        template.ModifiedByUserId = userId;
        template.ModifiedDate = DateTime.UtcNow;
        template.Version++;

        // Handle company default
        if (request.IsCompanyDefault && !template.IsCompanyDefault)
        {
            await ClearExistingDefaultAsync(companyId, request.ModuleContext, request.FormType);
            template.IsCompanyDefault = true;
        }
        else if (!request.IsCompanyDefault)
        {
            template.IsCompanyDefault = false;
        }

        // Update fields if provided
        if (request.Fields != null)
        {
            await UpdateFieldsAsync(template, request.Fields, userId);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated form template {TemplateId} for company {CompanyId}",
            id, companyId);

        return await GetTemplateWithFieldsAsync(id, companyId)
            ?? throw new InvalidOperationException("Failed to retrieve updated template");
    }

    public async Task<bool> DeleteTemplateAsync(int id, int companyId)
    {
        var template = await _context.FormTemplates
            .Where(t => t.Id == id && t.CompanyId == companyId && !t.IsDeleted)
            .FirstOrDefaultAsync();

        if (template == null) return false;

        if (template.IsSystemTemplate)
        {
            throw new InvalidOperationException("Cannot delete system templates");
        }

        template.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted form template {TemplateId} for company {CompanyId}",
            id, companyId);

        return true;
    }

    public async Task<FormTemplateDto> DuplicateTemplateAsync(
        int id,
        string newName,
        int companyId,
        int userId)
    {
        var source = await _context.FormTemplates
            .Include(t => t.Fields.Where(f => !f.IsDeleted))
            .Where(t => t.Id == id && t.CompanyId == companyId && !t.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Template {id} not found");

        var duplicate = new FormTemplate
        {
            CompanyId = companyId,
            Name = newName,
            Description = source.Description,
            ModuleContext = source.ModuleContext,
            FormType = source.FormType,
            IsSystemTemplate = false,
            IsCompanyDefault = false,
            IsPublished = false,
            NumberPrefix = source.NumberPrefix,
            ShowSectionHeaders = source.ShowSectionHeaders,
            AllowNotes = source.AllowNotes,
            CreatedByUserId = userId,
            CreatedDate = DateTime.UtcNow
        };

        _context.FormTemplates.Add(duplicate);
        await _context.SaveChangesAsync();

        // Duplicate fields
        foreach (var sourceField in source.Fields.OrderBy(f => f.SectionOrder).ThenBy(f => f.DisplayOrder))
        {
            var field = new FormTemplateField
            {
                FormTemplateId = duplicate.Id,
                FieldKey = sourceField.FieldKey,
                DisplayName = sourceField.DisplayName,
                DataType = sourceField.DataType,
                DisplayOrder = sourceField.DisplayOrder,
                SectionName = sourceField.SectionName,
                SectionOrder = sourceField.SectionOrder,
                Width = sourceField.Width,
                IsRequired = sourceField.IsRequired,
                IsVisible = sourceField.IsVisible,
                IsReadOnly = sourceField.IsReadOnly,
                DefaultValue = sourceField.DefaultValue,
                Placeholder = sourceField.Placeholder,
                HelpText = sourceField.HelpText,
                ValidationRegex = sourceField.ValidationRegex,
                ValidationMessage = sourceField.ValidationMessage,
                SelectOptions = sourceField.SelectOptions,
                Formula = sourceField.Formula,
                MinValue = sourceField.MinValue,
                MaxValue = sourceField.MaxValue,
                DecimalPlaces = sourceField.DecimalPlaces,
                CurrencySymbol = sourceField.CurrencySymbol,
                LinkedWorksheetTemplateId = sourceField.LinkedWorksheetTemplateId,
                MaxPhotos = sourceField.MaxPhotos,
                RequirePhotoLocation = sourceField.RequirePhotoLocation
            };
            _context.FormTemplateFields.Add(field);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Duplicated form template {SourceId} to {NewId} for company {CompanyId}",
            id, duplicate.Id, companyId);

        return await GetTemplateWithFieldsAsync(duplicate.Id, companyId)
            ?? throw new InvalidOperationException("Failed to retrieve duplicated template");
    }

    public async Task<FormTemplateDto> PublishTemplateAsync(int id, int companyId, int userId)
    {
        var template = await _context.FormTemplates
            .Where(t => t.Id == id && t.CompanyId == companyId && !t.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Template {id} not found");

        template.IsPublished = true;
        template.ModifiedByUserId = userId;
        template.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetTemplateByIdAsync(id, companyId)
            ?? throw new InvalidOperationException("Failed to retrieve template");
    }

    public async Task<FormTemplateDto> UnpublishTemplateAsync(int id, int companyId, int userId)
    {
        var template = await _context.FormTemplates
            .Where(t => t.Id == id && t.CompanyId == companyId && !t.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Template {id} not found");

        template.IsPublished = false;
        template.ModifiedByUserId = userId;
        template.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetTemplateByIdAsync(id, companyId)
            ?? throw new InvalidOperationException("Failed to retrieve template");
    }

    public async Task<FormTemplateDto> SetAsDefaultAsync(int id, int companyId, int userId)
    {
        var template = await _context.FormTemplates
            .Where(t => t.Id == id && t.CompanyId == companyId && !t.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Template {id} not found");

        await ClearExistingDefaultAsync(companyId, template.ModuleContext, template.FormType);

        template.IsCompanyDefault = true;
        template.ModifiedByUserId = userId;
        template.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetTemplateByIdAsync(id, companyId)
            ?? throw new InvalidOperationException("Failed to retrieve template");
    }

    public async Task<FormTemplateDto?> GetDefaultTemplateAsync(
        int companyId,
        FormModuleContext moduleContext,
        string? formType = null)
    {
        var query = _context.FormTemplates
            .Where(t => t.CompanyId == companyId &&
                       t.ModuleContext == moduleContext &&
                       t.IsCompanyDefault &&
                       !t.IsDeleted);

        if (!string.IsNullOrEmpty(formType))
        {
            query = query.Where(t => t.FormType == formType);
        }

        return await query
            .Select(t => MapToDto(t))
            .FirstOrDefaultAsync();
    }

    public async Task<FormTemplateFieldDto> AddFieldAsync(
        int templateId,
        CreateFormTemplateFieldRequest request,
        int companyId,
        int userId)
    {
        var template = await _context.FormTemplates
            .Where(t => t.Id == templateId && t.CompanyId == companyId && !t.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Template {templateId} not found");

        if (template.IsSystemTemplate)
        {
            throw new InvalidOperationException("Cannot modify system templates");
        }

        var field = CreateField(templateId, request);
        _context.FormTemplateFields.Add(field);

        template.ModifiedByUserId = userId;
        template.ModifiedDate = DateTime.UtcNow;
        template.Version++;

        await _context.SaveChangesAsync();

        return MapFieldToDto(field);
    }

    public async Task<FormTemplateFieldDto> UpdateFieldAsync(
        int templateId,
        int fieldId,
        UpdateFormTemplateFieldRequest request,
        int companyId,
        int userId)
    {
        var template = await _context.FormTemplates
            .Where(t => t.Id == templateId && t.CompanyId == companyId && !t.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Template {templateId} not found");

        if (template.IsSystemTemplate)
        {
            throw new InvalidOperationException("Cannot modify system templates");
        }

        var field = await _context.FormTemplateFields
            .Where(f => f.Id == fieldId && f.FormTemplateId == templateId && !f.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Field {fieldId} not found");

        UpdateField(field, request);

        template.ModifiedByUserId = userId;
        template.ModifiedDate = DateTime.UtcNow;
        template.Version++;

        await _context.SaveChangesAsync();

        return MapFieldToDto(field);
    }

    public async Task<bool> DeleteFieldAsync(int templateId, int fieldId, int companyId)
    {
        var template = await _context.FormTemplates
            .Where(t => t.Id == templateId && t.CompanyId == companyId && !t.IsDeleted)
            .FirstOrDefaultAsync();

        if (template == null) return false;

        if (template.IsSystemTemplate)
        {
            throw new InvalidOperationException("Cannot modify system templates");
        }

        var field = await _context.FormTemplateFields
            .Where(f => f.Id == fieldId && f.FormTemplateId == templateId && !f.IsDeleted)
            .FirstOrDefaultAsync();

        if (field == null) return false;

        field.IsDeleted = true;
        template.Version++;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ReorderFieldsAsync(
        int templateId,
        List<int> fieldIds,
        int companyId,
        int userId)
    {
        var template = await _context.FormTemplates
            .Include(t => t.Fields.Where(f => !f.IsDeleted))
            .Where(t => t.Id == templateId && t.CompanyId == companyId && !t.IsDeleted)
            .FirstOrDefaultAsync();

        if (template == null) return false;

        if (template.IsSystemTemplate)
        {
            throw new InvalidOperationException("Cannot modify system templates");
        }

        for (int i = 0; i < fieldIds.Count; i++)
        {
            var field = template.Fields.FirstOrDefault(f => f.Id == fieldIds[i]);
            if (field != null)
            {
                field.DisplayOrder = i;
            }
        }

        template.ModifiedByUserId = userId;
        template.ModifiedDate = DateTime.UtcNow;
        template.Version++;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<string>> GetFormTypesAsync(int companyId, FormModuleContext? moduleContext = null)
    {
        var query = _context.FormTemplates
            .Where(t => t.CompanyId == companyId && !t.IsDeleted && t.FormType != null);

        if (moduleContext.HasValue)
        {
            query = query.Where(t => t.ModuleContext == moduleContext.Value);
        }

        return await query
            .Select(t => t.FormType!)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }

    public IEnumerable<FormPageSizePreset> GetPageSizePresets() => _pageSizePresets;

    #region Private Methods

    private async Task ClearExistingDefaultAsync(int companyId, FormModuleContext moduleContext, string? formType)
    {
        var existingDefaults = await _context.FormTemplates
            .Where(t => t.CompanyId == companyId &&
                       t.ModuleContext == moduleContext &&
                       t.IsCompanyDefault &&
                       !t.IsDeleted)
            .ToListAsync();

        if (!string.IsNullOrEmpty(formType))
        {
            existingDefaults = existingDefaults.Where(t => t.FormType == formType).ToList();
        }

        foreach (var existing in existingDefaults)
        {
            existing.IsCompanyDefault = false;
        }
    }

    private async Task UpdateFieldsAsync(
        FormTemplate template,
        List<UpdateFormTemplateFieldRequest> fieldRequests,
        int userId)
    {
        var existingFieldIds = template.Fields.Select(f => f.Id).ToHashSet();
        var requestFieldIds = fieldRequests.Where(r => r.Id.HasValue).Select(r => r.Id!.Value).ToHashSet();

        // Delete fields not in request
        foreach (var field in template.Fields.Where(f => !requestFieldIds.Contains(f.Id)))
        {
            field.IsDeleted = true;
        }

        // Update or add fields
        foreach (var request in fieldRequests)
        {
            if (request.Id.HasValue && existingFieldIds.Contains(request.Id.Value))
            {
                // Update existing
                var field = template.Fields.First(f => f.Id == request.Id.Value);
                UpdateField(field, request);
            }
            else
            {
                // Add new
                var field = CreateField(template.Id, request);
                _context.FormTemplateFields.Add(field);
            }
        }
    }

    private static FormTemplateField CreateField(int templateId, CreateFormTemplateFieldRequest request)
    {
        return new FormTemplateField
        {
            FormTemplateId = templateId,
            FieldKey = request.FieldKey,
            DisplayName = request.DisplayName,
            DataType = request.DataType,
            DisplayOrder = request.DisplayOrder,
            SectionName = request.SectionName,
            SectionOrder = request.SectionOrder,
            Width = request.Width,
            IsRequired = request.IsRequired,
            IsVisible = request.IsVisible,
            IsReadOnly = request.IsReadOnly,
            DefaultValue = request.DefaultValue,
            Placeholder = request.Placeholder,
            HelpText = request.HelpText,
            ValidationRegex = request.ValidationRegex,
            ValidationMessage = request.ValidationMessage,
            SelectOptions = request.SelectOptions != null ? JsonSerializer.Serialize(request.SelectOptions) : null,
            Formula = request.Formula,
            MinValue = request.MinValue,
            MaxValue = request.MaxValue,
            DecimalPlaces = request.DecimalPlaces,
            CurrencySymbol = request.CurrencySymbol,
            LinkedWorksheetTemplateId = request.LinkedWorksheetTemplateId,
            MaxPhotos = request.MaxPhotos,
            RequirePhotoLocation = request.RequirePhotoLocation
        };
    }

    private static void UpdateField(FormTemplateField field, UpdateFormTemplateFieldRequest request)
    {
        field.FieldKey = request.FieldKey;
        field.DisplayName = request.DisplayName;
        field.DataType = request.DataType;
        field.DisplayOrder = request.DisplayOrder;
        field.SectionName = request.SectionName;
        field.SectionOrder = request.SectionOrder;
        field.Width = request.Width;
        field.IsRequired = request.IsRequired;
        field.IsVisible = request.IsVisible;
        field.IsReadOnly = request.IsReadOnly;
        field.DefaultValue = request.DefaultValue;
        field.Placeholder = request.Placeholder;
        field.HelpText = request.HelpText;
        field.ValidationRegex = request.ValidationRegex;
        field.ValidationMessage = request.ValidationMessage;
        field.SelectOptions = request.SelectOptions != null ? JsonSerializer.Serialize(request.SelectOptions) : null;
        field.Formula = request.Formula;
        field.MinValue = request.MinValue;
        field.MaxValue = request.MaxValue;
        field.DecimalPlaces = request.DecimalPlaces;
        field.CurrencySymbol = request.CurrencySymbol;
        field.LinkedWorksheetTemplateId = request.LinkedWorksheetTemplateId;
        field.MaxPhotos = request.MaxPhotos;
        field.RequirePhotoLocation = request.RequirePhotoLocation;
    }

    private static FormTemplateDto MapToDto(FormTemplate t)
    {
        return new FormTemplateDto
        {
            Id = t.Id,
            CompanyId = t.CompanyId,
            Name = t.Name,
            Description = t.Description,
            ModuleContext = t.ModuleContext,
            FormType = t.FormType,
            IsSystemTemplate = t.IsSystemTemplate,
            IsCompanyDefault = t.IsCompanyDefault,
            IsPublished = t.IsPublished,
            Version = t.Version,
            NumberPrefix = t.NumberPrefix,
            ShowSectionHeaders = t.ShowSectionHeaders,
            AllowNotes = t.AllowNotes,
            PageWidthMm = t.PageWidthMm,
            PageHeightMm = t.PageHeightMm,
            PageOrientation = t.PageOrientation,
            CreatedDate = t.CreatedDate,
            CreatedByUserId = t.CreatedByUserId,
            CreatedByName = t.CreatedByUser != null ? t.CreatedByUser.FirstName + " " + t.CreatedByUser.LastName : null,
            ModifiedDate = t.ModifiedDate,
            ModifiedByUserId = t.ModifiedByUserId,
            ModifiedByName = t.ModifiedByUser != null ? t.ModifiedByUser.FirstName + " " + t.ModifiedByUser.LastName : null
        };
    }

    private static FormTemplateDto MapToDtoWithFields(FormTemplate t)
    {
        var dto = MapToDto(t);
        dto.Fields = t.Fields
            .Where(f => !f.IsDeleted)
            .OrderBy(f => f.SectionOrder)
            .ThenBy(f => f.DisplayOrder)
            .Select(MapFieldToDto)
            .ToList();
        return dto;
    }

    private static FormTemplateFieldDto MapFieldToDto(FormTemplateField f)
    {
        List<string>? selectOptions = null;
        if (!string.IsNullOrEmpty(f.SelectOptions))
        {
            try
            {
                selectOptions = JsonSerializer.Deserialize<List<string>>(f.SelectOptions);
            }
            catch { }
        }

        return new FormTemplateFieldDto
        {
            Id = f.Id,
            FormTemplateId = f.FormTemplateId,
            FieldKey = f.FieldKey,
            DisplayName = f.DisplayName,
            DataType = f.DataType,
            DisplayOrder = f.DisplayOrder,
            // New section-based positioning
            FormTemplateSectionId = f.FormTemplateSectionId,
            ColumnIndex = f.ColumnIndex,
            RowIndex = f.RowIndex,
            // Deprecated fields (kept for backward compatibility)
            SectionName = f.SectionName,
            SectionOrder = f.SectionOrder,
            Width = f.Width,
            IsRequired = f.IsRequired,
            IsVisible = f.IsVisible,
            IsReadOnly = f.IsReadOnly,
            DefaultValue = f.DefaultValue,
            Placeholder = f.Placeholder,
            HelpText = f.HelpText,
            ValidationRegex = f.ValidationRegex,
            ValidationMessage = f.ValidationMessage,
            SelectOptions = selectOptions,
            Formula = f.Formula,
            MinValue = f.MinValue,
            MaxValue = f.MaxValue,
            DecimalPlaces = f.DecimalPlaces,
            CurrencySymbol = f.CurrencySymbol,
            LinkedWorksheetTemplateId = f.LinkedWorksheetTemplateId,
            LinkedWorksheetTemplateName = f.LinkedWorksheetTemplate?.Name,
            MaxPhotos = f.MaxPhotos,
            RequirePhotoLocation = f.RequirePhotoLocation
        };
    }

    private static FormTemplateSectionDto MapSectionToDto(FormTemplateSection s)
    {
        return new FormTemplateSectionDto
        {
            Id = s.Id,
            FormTemplateId = s.FormTemplateId,
            Name = s.Name,
            LayoutType = s.LayoutType,
            DisplayOrder = s.DisplayOrder,
            PageBreakBefore = s.PageBreakBefore,
            KeepTogether = s.KeepTogether,
            BackgroundColor = s.BackgroundColor,
            BorderColor = s.BorderColor,
            HeaderBackgroundColor = s.HeaderBackgroundColor,
            HeaderTextColor = s.HeaderTextColor,
            BorderWidth = s.BorderWidth,
            BorderRadius = s.BorderRadius,
            Padding = s.Padding,
            IsCollapsible = s.IsCollapsible,
            IsCollapsedByDefault = s.IsCollapsedByDefault,
            Fields = s.Fields
                .Where(f => !f.IsDeleted)
                .OrderBy(f => f.ColumnIndex)
                .ThenBy(f => f.RowIndex)
                .Select(MapFieldToDto)
                .ToList()
        };
    }

    #endregion

    #region Section Methods

    public async Task<FormTemplateDto?> GetTemplateWithSectionsAsync(int id, int companyId)
    {
        var template = await _context.FormTemplates
            .Include(t => t.Sections.Where(s => !s.IsDeleted).OrderBy(s => s.DisplayOrder))
                .ThenInclude(s => s.Fields.Where(f => !f.IsDeleted).OrderBy(f => f.ColumnIndex).ThenBy(f => f.RowIndex))
            .Include(t => t.Fields.Where(f => !f.IsDeleted && f.FormTemplateSectionId == null))
            .Include(t => t.CreatedByUser)
            .Include(t => t.ModifiedByUser)
            .Where(t => t.Id == id && t.CompanyId == companyId && !t.IsDeleted)
            .FirstOrDefaultAsync();

        if (template == null) return null;

        var dto = MapToDto(template);
        dto.Sections = template.Sections
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.DisplayOrder)
            .Select(MapSectionToDto)
            .ToList();

        // Include unassigned fields (fields not in any section)
        dto.Fields = template.Fields
            .Where(f => !f.IsDeleted && f.FormTemplateSectionId == null)
            .OrderBy(f => f.DisplayOrder)
            .Select(MapFieldToDto)
            .ToList();

        return dto;
    }

    public async Task<FormTemplateSectionDto> AddSectionAsync(
        int templateId,
        CreateFormTemplateSectionRequest request,
        int companyId,
        int userId)
    {
        var template = await _context.FormTemplates
            .Include(t => t.Sections.Where(s => !s.IsDeleted))
            .Where(t => t.Id == templateId && t.CompanyId == companyId && !t.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Template {templateId} not found");

        if (template.IsSystemTemplate)
        {
            throw new InvalidOperationException("Cannot modify system templates");
        }

        // Check page width constraint for the layout type
        var layoutOption = _sectionLayoutOptions.FirstOrDefault(l => l.LayoutType == request.LayoutType);
        if (layoutOption != null && template.PageWidthMm < layoutOption.MinPageWidthMm)
        {
            throw new InvalidOperationException(
                $"Layout '{request.LayoutType}' requires minimum page width of {layoutOption.MinPageWidthMm}mm. " +
                $"Current page width is {template.PageWidthMm}mm.");
        }

        // Set display order if not specified
        var displayOrder = request.DisplayOrder > 0
            ? request.DisplayOrder
            : (template.Sections.Any() ? template.Sections.Max(s => s.DisplayOrder) + 1 : 0);

        var section = new FormTemplateSection
        {
            FormTemplateId = templateId,
            Name = request.Name,
            LayoutType = request.LayoutType,
            DisplayOrder = displayOrder,
            PageBreakBefore = request.PageBreakBefore,
            KeepTogether = request.KeepTogether,
            BackgroundColor = request.BackgroundColor,
            BorderColor = request.BorderColor,
            HeaderBackgroundColor = request.HeaderBackgroundColor,
            HeaderTextColor = request.HeaderTextColor,
            BorderWidth = request.BorderWidth,
            BorderRadius = request.BorderRadius,
            Padding = request.Padding,
            IsCollapsible = request.IsCollapsible,
            IsCollapsedByDefault = request.IsCollapsedByDefault
        };

        _context.FormTemplateSections.Add(section);

        template.ModifiedByUserId = userId;
        template.ModifiedDate = DateTime.UtcNow;
        template.Version++;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Added section {SectionId} '{Name}' to template {TemplateId}",
            section.Id, section.Name, templateId);

        return MapSectionToDto(section);
    }

    public async Task<FormTemplateSectionDto> UpdateSectionAsync(
        int templateId,
        int sectionId,
        UpdateFormTemplateSectionRequest request,
        int companyId,
        int userId)
    {
        var template = await _context.FormTemplates
            .Where(t => t.Id == templateId && t.CompanyId == companyId && !t.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Template {templateId} not found");

        if (template.IsSystemTemplate)
        {
            throw new InvalidOperationException("Cannot modify system templates");
        }

        var section = await _context.FormTemplateSections
            .Include(s => s.Fields.Where(f => !f.IsDeleted))
            .Where(s => s.Id == sectionId && s.FormTemplateId == templateId && !s.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Section {sectionId} not found");

        // Check page width constraint for the new layout type
        var layoutOption = _sectionLayoutOptions.FirstOrDefault(l => l.LayoutType == request.LayoutType);
        if (layoutOption != null && template.PageWidthMm < layoutOption.MinPageWidthMm)
        {
            throw new InvalidOperationException(
                $"Layout '{request.LayoutType}' requires minimum page width of {layoutOption.MinPageWidthMm}mm. " +
                $"Current page width is {template.PageWidthMm}mm.");
        }

        // Update section properties
        section.Name = request.Name;
        section.LayoutType = request.LayoutType;
        section.DisplayOrder = request.DisplayOrder;
        section.PageBreakBefore = request.PageBreakBefore;
        section.KeepTogether = request.KeepTogether;
        section.BackgroundColor = request.BackgroundColor;
        section.BorderColor = request.BorderColor;
        section.HeaderBackgroundColor = request.HeaderBackgroundColor;
        section.HeaderTextColor = request.HeaderTextColor;
        section.BorderWidth = request.BorderWidth;
        section.BorderRadius = request.BorderRadius;
        section.Padding = request.Padding;
        section.IsCollapsible = request.IsCollapsible;
        section.IsCollapsedByDefault = request.IsCollapsedByDefault;

        template.ModifiedByUserId = userId;
        template.ModifiedDate = DateTime.UtcNow;
        template.Version++;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated section {SectionId} in template {TemplateId}",
            sectionId, templateId);

        return MapSectionToDto(section);
    }

    public async Task<bool> DeleteSectionAsync(int templateId, int sectionId, int companyId, bool deleteFields = false)
    {
        var template = await _context.FormTemplates
            .Where(t => t.Id == templateId && t.CompanyId == companyId && !t.IsDeleted)
            .FirstOrDefaultAsync();

        if (template == null) return false;

        if (template.IsSystemTemplate)
        {
            throw new InvalidOperationException("Cannot modify system templates");
        }

        var section = await _context.FormTemplateSections
            .Include(s => s.Fields.Where(f => !f.IsDeleted))
            .Where(s => s.Id == sectionId && s.FormTemplateId == templateId && !s.IsDeleted)
            .FirstOrDefaultAsync();

        if (section == null) return false;

        // Handle fields in the section
        foreach (var field in section.Fields)
        {
            if (deleteFields)
            {
                field.IsDeleted = true;
            }
            else
            {
                // Move to unassigned
                field.FormTemplateSectionId = null;
                field.ColumnIndex = 0;
                field.RowIndex = 0;
            }
        }

        section.IsDeleted = true;
        template.Version++;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted section {SectionId} from template {TemplateId} (deleteFields={DeleteFields})",
            sectionId, templateId, deleteFields);

        return true;
    }

    public async Task<bool> ReorderSectionsAsync(
        int templateId,
        List<int> sectionIds,
        int companyId,
        int userId)
    {
        var template = await _context.FormTemplates
            .Include(t => t.Sections.Where(s => !s.IsDeleted))
            .Where(t => t.Id == templateId && t.CompanyId == companyId && !t.IsDeleted)
            .FirstOrDefaultAsync();

        if (template == null) return false;

        if (template.IsSystemTemplate)
        {
            throw new InvalidOperationException("Cannot modify system templates");
        }

        for (int i = 0; i < sectionIds.Count; i++)
        {
            var section = template.Sections.FirstOrDefault(s => s.Id == sectionIds[i]);
            if (section != null)
            {
                section.DisplayOrder = i;
            }
        }

        template.ModifiedByUserId = userId;
        template.ModifiedDate = DateTime.UtcNow;
        template.Version++;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Reordered sections in template {TemplateId}", templateId);

        return true;
    }

    public async Task<bool> MoveFieldToSectionAsync(
        int templateId,
        int fieldId,
        int targetSectionId,
        int columnIndex,
        int rowIndex,
        int companyId,
        int userId)
    {
        var template = await _context.FormTemplates
            .Where(t => t.Id == templateId && t.CompanyId == companyId && !t.IsDeleted)
            .FirstOrDefaultAsync();

        if (template == null) return false;

        if (template.IsSystemTemplate)
        {
            throw new InvalidOperationException("Cannot modify system templates");
        }

        var field = await _context.FormTemplateFields
            .Where(f => f.Id == fieldId && f.FormTemplateId == templateId && !f.IsDeleted)
            .FirstOrDefaultAsync();

        if (field == null) return false;

        // Verify target section exists and belongs to this template
        var targetSection = await _context.FormTemplateSections
            .Where(s => s.Id == targetSectionId && s.FormTemplateId == templateId && !s.IsDeleted)
            .FirstOrDefaultAsync();

        if (targetSection == null) return false;

        // Validate column index against section layout
        var layoutOption = _sectionLayoutOptions.FirstOrDefault(l => l.LayoutType == targetSection.LayoutType);
        if (layoutOption != null && columnIndex >= layoutOption.ColumnCount)
        {
            throw new InvalidOperationException(
                $"Column index {columnIndex} is invalid for layout '{targetSection.LayoutType}' " +
                $"which has {layoutOption.ColumnCount} columns.");
        }

        // Update field position
        field.FormTemplateSectionId = targetSectionId;
        field.ColumnIndex = columnIndex;
        field.RowIndex = rowIndex;

        template.ModifiedByUserId = userId;
        template.ModifiedDate = DateTime.UtcNow;
        template.Version++;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Moved field {FieldId} to section {SectionId} column {Column} row {Row}",
            fieldId, targetSectionId, columnIndex, rowIndex);

        return true;
    }

    public IEnumerable<SectionLayoutOption> GetSectionLayoutOptions() => _sectionLayoutOptions;

    #endregion
}
