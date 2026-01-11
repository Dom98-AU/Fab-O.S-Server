using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.CloudStorage;
using FabOS.WebServer.Models.DTOs.Forms;
using FabOS.WebServer.Models.Entities.Forms;
using FabOS.WebServer.Services.Interfaces.Forms;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations.Forms;

/// <summary>
/// Service implementation for managing form instances
/// </summary>
public class FormInstanceService : IFormInstanceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FormInstanceService> _logger;
    private readonly ICloudStorageProvider? _storageProvider;

    public FormInstanceService(
        ApplicationDbContext context,
        ILogger<FormInstanceService> logger,
        ICloudStorageProvider? storageProvider = null)
    {
        _context = context;
        _logger = logger;
        _storageProvider = storageProvider;
    }

    public async Task<FormInstanceListResponse> GetInstancesAsync(
        int companyId,
        FormInstanceFilterRequest filter)
    {
        var query = _context.FormInstances
            .Include(i => i.FormTemplate)
            .Include(i => i.Values)
            .Where(i => i.CompanyId == companyId && !i.IsDeleted);

        // Apply filters
        if (filter.ModuleContext.HasValue)
        {
            query = query.Where(i => i.FormTemplate!.ModuleContext == filter.ModuleContext.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(i => i.Status == filter.Status.Value);
        }

        if (filter.FormTemplateId.HasValue)
        {
            query = query.Where(i => i.FormTemplateId == filter.FormTemplateId.Value);
        }

        if (!string.IsNullOrEmpty(filter.LinkedEntityType))
        {
            query = query.Where(i => i.LinkedEntityType == filter.LinkedEntityType);
        }

        if (filter.LinkedEntityId.HasValue)
        {
            query = query.Where(i => i.LinkedEntityId == filter.LinkedEntityId.Value);
        }

        if (filter.CreatedByUserId.HasValue)
        {
            query = query.Where(i => i.CreatedByUserId == filter.CreatedByUserId.Value);
        }

        if (filter.DateFrom.HasValue)
        {
            query = query.Where(i => i.CreatedDate >= filter.DateFrom.Value);
        }

        if (filter.DateTo.HasValue)
        {
            query = query.Where(i => i.CreatedDate <= filter.DateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchLower = filter.Search.ToLower();
            query = query.Where(i =>
                i.FormNumber.ToLower().Contains(searchLower) ||
                i.FormTemplate!.Name.ToLower().Contains(searchLower) ||
                (i.LinkedEntityDisplay != null && i.LinkedEntityDisplay.ToLower().Contains(searchLower)));
        }

        var totalCount = await query.CountAsync();

        // Apply sorting
        query = filter.SortBy?.ToLower() switch
        {
            "formnumber" => filter.SortDescending
                ? query.OrderByDescending(i => i.FormNumber)
                : query.OrderBy(i => i.FormNumber),
            "status" => filter.SortDescending
                ? query.OrderByDescending(i => i.Status)
                : query.OrderBy(i => i.Status),
            "template" => filter.SortDescending
                ? query.OrderByDescending(i => i.FormTemplate!.Name)
                : query.OrderBy(i => i.FormTemplate!.Name),
            _ => filter.SortDescending
                ? query.OrderByDescending(i => i.CreatedDate)
                : query.OrderBy(i => i.CreatedDate)
        };

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(i => new FormInstanceSummaryDto
            {
                Id = i.Id,
                FormNumber = i.FormNumber,
                FormTemplateId = i.FormTemplateId,
                TemplateName = i.FormTemplate!.Name,
                ModuleContext = i.FormTemplate.ModuleContext,
                Status = i.Status,
                LinkedEntityType = i.LinkedEntityType,
                LinkedEntityId = i.LinkedEntityId,
                LinkedEntityDisplay = i.LinkedEntityDisplay,
                CompletedFieldCount = i.Values.Count(v => v.TextValue != null || v.NumberValue != null ||
                    v.DateValue != null || v.BoolValue != null || v.JsonValue != null ||
                    v.SignatureDataUrl != null || v.PassFailValue != null),
                TotalFieldCount = i.FormTemplate.Fields.Count(f => !f.IsDeleted),
                CreatedDate = i.CreatedDate,
                CreatedByName = i.CreatedByUser != null ? i.CreatedByUser.FirstName + " " + i.CreatedByUser.LastName : null,
                SubmittedDate = i.SubmittedDate,
                SubmittedByName = i.SubmittedByUser != null ? i.SubmittedByUser.FirstName + " " + i.SubmittedByUser.LastName : null,
                ApprovedDate = i.ApprovedDate,
                ApprovedByName = i.ApprovedByUser != null ? i.ApprovedByUser.FirstName + " " + i.ApprovedByUser.LastName : null
            })
            .ToListAsync();

        return new FormInstanceListResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<FormInstanceDto?> GetInstanceByIdAsync(int id, int companyId)
    {
        var instance = await _context.FormInstances
            .Include(i => i.FormTemplate)
            .Include(i => i.CreatedByUser)
            .Where(i => i.Id == id && i.CompanyId == companyId && !i.IsDeleted)
            .FirstOrDefaultAsync();

        if (instance == null) return null;

        return MapToDto(instance);
    }

    public async Task<FormInstanceDto?> GetInstanceWithValuesAsync(int id, int companyId)
    {
        var instance = await _context.FormInstances
            .Include(i => i.FormTemplate)
                .ThenInclude(t => t!.Fields.Where(f => !f.IsDeleted))
            .Include(i => i.Values)
                .ThenInclude(v => v.Attachments.Where(a => !a.IsDeleted))
            .Include(i => i.Attachments.Where(a => !a.IsDeleted))
            .Include(i => i.CreatedByUser)
            .Include(i => i.ModifiedByUser)
            .Include(i => i.SubmittedByUser)
            .Include(i => i.ReviewedByUser)
            .Include(i => i.ApprovedByUser)
            .Where(i => i.Id == id && i.CompanyId == companyId && !i.IsDeleted)
            .FirstOrDefaultAsync();

        if (instance == null) return null;

        return MapToDtoWithValues(instance);
    }

    public async Task<List<FormInstanceSummaryDto>> GetInstancesByEntityAsync(
        string entityType,
        int entityId,
        int companyId)
    {
        return await _context.FormInstances
            .Include(i => i.FormTemplate)
            .Include(i => i.Values)
            .Where(i => i.CompanyId == companyId &&
                       i.LinkedEntityType == entityType &&
                       i.LinkedEntityId == entityId &&
                       !i.IsDeleted)
            .OrderByDescending(i => i.CreatedDate)
            .Select(i => new FormInstanceSummaryDto
            {
                Id = i.Id,
                FormNumber = i.FormNumber,
                FormTemplateId = i.FormTemplateId,
                TemplateName = i.FormTemplate!.Name,
                ModuleContext = i.FormTemplate.ModuleContext,
                Status = i.Status,
                LinkedEntityType = i.LinkedEntityType,
                LinkedEntityId = i.LinkedEntityId,
                LinkedEntityDisplay = i.LinkedEntityDisplay,
                CompletedFieldCount = i.Values.Count(v => v.TextValue != null || v.NumberValue != null ||
                    v.DateValue != null || v.BoolValue != null || v.JsonValue != null ||
                    v.SignatureDataUrl != null || v.PassFailValue != null),
                TotalFieldCount = i.FormTemplate.Fields.Count(f => !f.IsDeleted),
                CreatedDate = i.CreatedDate,
                CreatedByName = i.CreatedByUser != null ? i.CreatedByUser.FirstName + " " + i.CreatedByUser.LastName : null,
                SubmittedDate = i.SubmittedDate,
                SubmittedByName = i.SubmittedByUser != null ? i.SubmittedByUser.FirstName + " " + i.SubmittedByUser.LastName : null,
                ApprovedDate = i.ApprovedDate,
                ApprovedByName = i.ApprovedByUser != null ? i.ApprovedByUser.FirstName + " " + i.ApprovedByUser.LastName : null
            })
            .ToListAsync();
    }

    public async Task<FormInstanceDto> CreateInstanceAsync(
        CreateFormInstanceRequest request,
        int companyId,
        int userId)
    {
        var template = await _context.FormTemplates
            .Include(t => t.Fields.Where(f => !f.IsDeleted))
            .Where(t => t.Id == request.FormTemplateId && t.CompanyId == companyId && !t.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Template {request.FormTemplateId} not found");

        if (!template.IsPublished)
        {
            throw new InvalidOperationException("Cannot create instances from unpublished templates");
        }

        var formNumber = await GetNextFormNumberAsync(request.FormTemplateId, companyId);

        var instance = new FormInstance
        {
            CompanyId = companyId,
            FormTemplateId = request.FormTemplateId,
            FormNumber = formNumber,
            Status = FormInstanceStatus.Draft,
            LinkedEntityType = request.LinkedEntityType,
            LinkedEntityId = request.LinkedEntityId,
            LinkedEntityDisplay = request.LinkedEntityDisplay,
            Notes = request.Notes,
            CreatedByUserId = userId,
            CreatedDate = DateTime.UtcNow
        };

        _context.FormInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Create values with defaults if provided
        foreach (var field in template.Fields)
        {
            var value = new FormInstanceValue
            {
                FormInstanceId = instance.Id,
                FormTemplateFieldId = field.Id,
                FieldKey = field.FieldKey
            };

            // Apply default value
            if (!string.IsNullOrEmpty(field.DefaultValue))
            {
                ApplyDefaultValue(value, field);
            }

            // Apply request values if provided
            var requestValue = request.Values?.FirstOrDefault(v => v.FieldKey == field.FieldKey);
            if (requestValue != null)
            {
                ApplyRequestValue(value, requestValue, userId);
            }

            _context.FormInstanceValues.Add(value);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Created form instance {InstanceId} '{FormNumber}' for company {CompanyId}",
            instance.Id, formNumber, companyId);

        return await GetInstanceWithValuesAsync(instance.Id, companyId)
            ?? throw new InvalidOperationException("Failed to retrieve created instance");
    }

    public async Task<FormInstanceDto> UpdateInstanceAsync(
        int id,
        UpdateFormInstanceRequest request,
        int companyId,
        int userId)
    {
        var instance = await _context.FormInstances
            .Include(i => i.Values)
            .Where(i => i.Id == id && i.CompanyId == companyId && !i.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Instance {id} not found");

        if (instance.Status != FormInstanceStatus.Draft && instance.Status != FormInstanceStatus.Rejected)
        {
            throw new InvalidOperationException("Can only update draft or rejected forms");
        }

        instance.Notes = request.Notes;
        instance.ModifiedByUserId = userId;
        instance.ModifiedDate = DateTime.UtcNow;

        // Update values if provided
        if (request.Values != null)
        {
            foreach (var requestValue in request.Values)
            {
                var value = instance.Values.FirstOrDefault(v => v.FieldKey == requestValue.FieldKey);
                if (value != null)
                {
                    ApplyRequestValue(value, requestValue, userId);
                }
            }
        }

        await _context.SaveChangesAsync();

        return await GetInstanceWithValuesAsync(id, companyId)
            ?? throw new InvalidOperationException("Failed to retrieve updated instance");
    }

    public async Task<bool> DeleteInstanceAsync(int id, int companyId)
    {
        var instance = await _context.FormInstances
            .Where(i => i.Id == id && i.CompanyId == companyId && !i.IsDeleted)
            .FirstOrDefaultAsync();

        if (instance == null) return false;

        instance.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted form instance {InstanceId} for company {CompanyId}",
            id, companyId);

        return true;
    }

    public async Task<FormInstanceDto> SubmitForReviewAsync(int id, int companyId, int userId)
    {
        var instance = await _context.FormInstances
            .Where(i => i.Id == id && i.CompanyId == companyId && !i.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Instance {id} not found");

        if (instance.Status != FormInstanceStatus.Draft && instance.Status != FormInstanceStatus.Rejected)
        {
            throw new InvalidOperationException("Can only submit draft or rejected forms");
        }

        // Validate required fields
        var validation = await ValidateInstanceAsync(id, companyId);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Form validation failed: {string.Join(", ", validation.Errors.Select(e => e.Message))}");
        }

        instance.Status = FormInstanceStatus.Submitted;
        instance.SubmittedByUserId = userId;
        instance.SubmittedDate = DateTime.UtcNow;
        instance.ModifiedByUserId = userId;
        instance.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetInstanceByIdAsync(id, companyId)
            ?? throw new InvalidOperationException("Failed to retrieve instance");
    }

    public async Task<FormInstanceDto> StartReviewAsync(int id, int companyId, int userId)
    {
        var instance = await _context.FormInstances
            .Where(i => i.Id == id && i.CompanyId == companyId && !i.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Instance {id} not found");

        if (instance.Status != FormInstanceStatus.Submitted)
        {
            throw new InvalidOperationException("Can only start review on submitted forms");
        }

        instance.Status = FormInstanceStatus.UnderReview;
        instance.ReviewedByUserId = userId;
        instance.ReviewedDate = DateTime.UtcNow;
        instance.ModifiedByUserId = userId;
        instance.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetInstanceByIdAsync(id, companyId)
            ?? throw new InvalidOperationException("Failed to retrieve instance");
    }

    public async Task<FormInstanceDto> ApproveAsync(int id, int companyId, int userId)
    {
        var instance = await _context.FormInstances
            .Where(i => i.Id == id && i.CompanyId == companyId && !i.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Instance {id} not found");

        if (instance.Status != FormInstanceStatus.Submitted && instance.Status != FormInstanceStatus.UnderReview)
        {
            throw new InvalidOperationException("Can only approve submitted or under review forms");
        }

        instance.Status = FormInstanceStatus.Approved;
        instance.ApprovedByUserId = userId;
        instance.ApprovedDate = DateTime.UtcNow;
        instance.ModifiedByUserId = userId;
        instance.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Approved form instance {InstanceId} for company {CompanyId}",
            id, companyId);

        return await GetInstanceByIdAsync(id, companyId)
            ?? throw new InvalidOperationException("Failed to retrieve instance");
    }

    public async Task<FormInstanceDto> RejectAsync(int id, string reason, int companyId, int userId)
    {
        var instance = await _context.FormInstances
            .Where(i => i.Id == id && i.CompanyId == companyId && !i.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Instance {id} not found");

        if (instance.Status != FormInstanceStatus.Submitted && instance.Status != FormInstanceStatus.UnderReview)
        {
            throw new InvalidOperationException("Can only reject submitted or under review forms");
        }

        instance.Status = FormInstanceStatus.Rejected;
        instance.RejectionReason = reason;
        instance.ModifiedByUserId = userId;
        instance.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Rejected form instance {InstanceId} for company {CompanyId}: {Reason}",
            id, companyId, reason);

        return await GetInstanceByIdAsync(id, companyId)
            ?? throw new InvalidOperationException("Failed to retrieve instance");
    }

    public async Task<FormInstanceDto> RevertToDraftAsync(int id, int companyId, int userId)
    {
        var instance = await _context.FormInstances
            .Where(i => i.Id == id && i.CompanyId == companyId && !i.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Instance {id} not found");

        if (instance.Status == FormInstanceStatus.Approved)
        {
            throw new InvalidOperationException("Cannot revert approved forms");
        }

        instance.Status = FormInstanceStatus.Draft;
        instance.SubmittedByUserId = null;
        instance.SubmittedDate = null;
        instance.ReviewedByUserId = null;
        instance.ReviewedDate = null;
        instance.RejectionReason = null;
        instance.ModifiedByUserId = userId;
        instance.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetInstanceByIdAsync(id, companyId)
            ?? throw new InvalidOperationException("Failed to retrieve instance");
    }

    public async Task<FormInstanceValueDto> SetFieldValueAsync(
        int instanceId,
        string fieldKey,
        FormInstanceValueRequest request,
        int companyId,
        int userId)
    {
        var instance = await _context.FormInstances
            .Include(i => i.Values)
            .Where(i => i.Id == instanceId && i.CompanyId == companyId && !i.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Instance {instanceId} not found");

        if (instance.Status != FormInstanceStatus.Draft && instance.Status != FormInstanceStatus.Rejected)
        {
            throw new InvalidOperationException("Can only update draft or rejected forms");
        }

        var value = instance.Values.FirstOrDefault(v => v.FieldKey == fieldKey);
        if (value == null)
        {
            throw new KeyNotFoundException($"Field {fieldKey} not found");
        }

        ApplyRequestValue(value, request, userId);
        instance.ModifiedByUserId = userId;
        instance.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapValueToDto(value);
    }

    public async Task<FormInstanceValueDto?> GetFieldValueAsync(
        int instanceId,
        string fieldKey,
        int companyId)
    {
        var value = await _context.FormInstanceValues
            .Include(v => v.FormTemplateField)
            .Include(v => v.Attachments.Where(a => !a.IsDeleted))
            .Where(v => v.FormInstanceId == instanceId &&
                       v.FieldKey == fieldKey &&
                       v.FormInstance!.CompanyId == companyId &&
                       !v.FormInstance.IsDeleted)
            .FirstOrDefaultAsync();

        return value != null ? MapValueToDto(value) : null;
    }

    public async Task<FormInstanceAttachmentDto> UploadAttachmentAsync(
        int instanceId,
        string? fieldKey,
        Stream fileStream,
        string fileName,
        string contentType,
        decimal? latitude,
        decimal? longitude,
        string? caption,
        int companyId,
        int userId)
    {
        var instance = await _context.FormInstances
            .Include(i => i.Values)
            .Where(i => i.Id == instanceId && i.CompanyId == companyId && !i.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Instance {instanceId} not found");

        if (instance.Status != FormInstanceStatus.Draft && instance.Status != FormInstanceStatus.Rejected)
        {
            throw new InvalidOperationException("Can only add attachments to draft or rejected forms");
        }

        int? valueId = null;
        if (!string.IsNullOrEmpty(fieldKey))
        {
            var value = instance.Values.FirstOrDefault(v => v.FieldKey == fieldKey);
            if (value != null)
            {
                valueId = value.Id;
            }
        }

        // Upload to storage
        string? storagePath = null;
        string? thumbnailPath = null;
        long fileSize = 0;

        using (var memoryStream = new MemoryStream())
        {
            await fileStream.CopyToAsync(memoryStream);
            fileSize = memoryStream.Length;

            if (_storageProvider != null)
            {
                memoryStream.Position = 0;
                var uploadRequest = new CloudFileUploadRequest
                {
                    FolderPath = $"forms/{companyId}/{instanceId}",
                    FileName = fileName,
                    Content = memoryStream,
                    ContentType = contentType
                };
                var result = await _storageProvider.UploadFileAsync(uploadRequest);
                storagePath = result.FileId;
            }
        }

        var attachment = new FormInstanceAttachment
        {
            FormInstanceId = instanceId,
            FormInstanceValueId = valueId,
            FileName = fileName,
            FileType = contentType,
            FileSize = fileSize,
            StorageProvider = _storageProvider?.ProviderName,
            StoragePath = storagePath,
            ThumbnailPath = thumbnailPath,
            Latitude = latitude,
            Longitude = longitude,
            Caption = caption,
            UploadedByUserId = userId,
            UploadedDate = DateTime.UtcNow
        };

        _context.FormInstanceAttachments.Add(attachment);
        instance.ModifiedByUserId = userId;
        instance.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapAttachmentToDto(attachment);
    }

    public async Task<bool> DeleteAttachmentAsync(int attachmentId, int companyId)
    {
        var attachment = await _context.FormInstanceAttachments
            .Include(a => a.FormInstance)
            .Where(a => a.Id == attachmentId && a.FormInstance!.CompanyId == companyId && !a.IsDeleted)
            .FirstOrDefaultAsync();

        if (attachment == null) return false;

        attachment.IsDeleted = true;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<string> GetNextFormNumberAsync(int templateId, int companyId)
    {
        var template = await _context.FormTemplates
            .Where(t => t.Id == templateId && t.CompanyId == companyId)
            .FirstOrDefaultAsync();

        var prefix = template?.NumberPrefix ?? "FORM";

        var lastNumber = await _context.FormInstances
            .Where(i => i.FormTemplateId == templateId && i.CompanyId == companyId)
            .OrderByDescending(i => i.Id)
            .Select(i => i.FormNumber)
            .FirstOrDefaultAsync();

        int nextNum = 1;
        if (!string.IsNullOrEmpty(lastNumber))
        {
            var numPart = lastNumber.Split('-').LastOrDefault();
            if (int.TryParse(numPart, out var parsed))
            {
                nextNum = parsed + 1;
            }
        }

        return $"{prefix}-{nextNum:D3}";
    }

    public async Task<FormValidationResult> ValidateInstanceAsync(int id, int companyId)
    {
        var instance = await _context.FormInstances
            .Include(i => i.FormTemplate)
                .ThenInclude(t => t!.Fields.Where(f => !f.IsDeleted))
            .Include(i => i.Values)
            .Where(i => i.Id == id && i.CompanyId == companyId && !i.IsDeleted)
            .FirstOrDefaultAsync();

        var result = new FormValidationResult { IsValid = true };

        if (instance == null)
        {
            result.IsValid = false;
            result.Errors.Add(new FormValidationError
            {
                FieldKey = "",
                FieldName = "",
                Message = "Form instance not found"
            });
            return result;
        }

        foreach (var field in instance.FormTemplate!.Fields.Where(f => f.IsRequired))
        {
            var value = instance.Values.FirstOrDefault(v => v.FieldKey == field.FieldKey);

            bool hasValue = value != null && (
                !string.IsNullOrEmpty(value.TextValue) ||
                value.NumberValue.HasValue ||
                value.DateValue.HasValue ||
                value.BoolValue.HasValue ||
                !string.IsNullOrEmpty(value.JsonValue) ||
                !string.IsNullOrEmpty(value.SignatureDataUrl) ||
                value.PassFailValue.HasValue);

            if (!hasValue)
            {
                result.IsValid = false;
                result.Errors.Add(new FormValidationError
                {
                    FieldKey = field.FieldKey,
                    FieldName = field.DisplayName,
                    Message = $"{field.DisplayName} is required"
                });
            }
        }

        return result;
    }

    public async Task<string> GenerateExportHtmlAsync(int id, int companyId)
    {
        var instance = await GetInstanceWithValuesAsync(id, companyId)
            ?? throw new KeyNotFoundException($"Instance {id} not found");

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html><html><head>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        html.AppendLine(".form-header { background: #f8f9fa; padding: 20px; border-bottom: 2px solid #0D1A80; margin-bottom: 20px; }");
        html.AppendLine(".form-section { margin-bottom: 20px; }");
        html.AppendLine(".section-header { font-size: 16px; font-weight: bold; border-bottom: 1px solid #ddd; padding-bottom: 8px; margin-bottom: 12px; }");
        html.AppendLine(".field-row { display: flex; margin-bottom: 8px; }");
        html.AppendLine(".field-label { width: 200px; font-weight: bold; color: #666; }");
        html.AppendLine(".field-value { flex: 1; }");
        html.AppendLine(".signature-box { border: 1px solid #ccc; padding: 10px; min-height: 60px; }");
        html.AppendLine(".status-badge { display: inline-block; padding: 4px 8px; border-radius: 4px; font-size: 12px; }");
        html.AppendLine(".status-approved { background: #28a745; color: white; }");
        html.AppendLine(".status-rejected { background: #dc3545; color: white; }");
        html.AppendLine(".status-draft { background: #6c757d; color: white; }");
        html.AppendLine("</style>");
        html.AppendLine("</head><body>");

        // Header
        html.AppendLine("<div class='form-header'>");
        html.AppendLine($"<h1>{instance.Template?.Name ?? "Form"}</h1>");
        html.AppendLine($"<p><strong>Form Number:</strong> {instance.FormNumber}</p>");
        html.AppendLine($"<p><strong>Status:</strong> <span class='status-badge status-{instance.StatusName.ToLower()}'>{instance.StatusName}</span></p>");
        if (!string.IsNullOrEmpty(instance.LinkedEntityDisplay))
        {
            html.AppendLine($"<p><strong>Linked To:</strong> {instance.LinkedEntityDisplay}</p>");
        }
        html.AppendLine("</div>");

        // Group fields by section
        var fieldsBySection = instance.Template?.Fields
            .OrderBy(f => f.SectionOrder)
            .ThenBy(f => f.DisplayOrder)
            .GroupBy(f => f.SectionName ?? "General")
            ?? Enumerable.Empty<IGrouping<string, FormTemplateFieldDto>>();

        foreach (var section in fieldsBySection)
        {
            html.AppendLine("<div class='form-section'>");
            html.AppendLine($"<div class='section-header'>{section.Key}</div>");

            foreach (var field in section)
            {
                var value = instance.Values.FirstOrDefault(v => v.FieldKey == field.FieldKey);
                html.AppendLine("<div class='field-row'>");
                html.AppendLine($"<div class='field-label'>{field.DisplayName}</div>");
                html.AppendLine($"<div class='field-value'>{FormatValueForHtml(value, field)}</div>");
                html.AppendLine("</div>");
            }

            html.AppendLine("</div>");
        }

        // Footer
        if (instance.ApprovedDate.HasValue)
        {
            html.AppendLine("<div class='form-section'>");
            html.AppendLine($"<p><strong>Approved By:</strong> {instance.ApprovedByName} on {instance.ApprovedDate:yyyy-MM-dd HH:mm}</p>");
            html.AppendLine("</div>");
        }

        html.AppendLine("</body></html>");

        return html.ToString();
    }

    public async Task<FormStatisticsDto> GetStatisticsAsync(
        int companyId,
        FormModuleContext? moduleContext = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null)
    {
        var query = _context.FormInstances
            .Include(i => i.FormTemplate)
            .Where(i => i.CompanyId == companyId && !i.IsDeleted);

        if (moduleContext.HasValue)
        {
            query = query.Where(i => i.FormTemplate!.ModuleContext == moduleContext.Value);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(i => i.CreatedDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(i => i.CreatedDate <= dateTo.Value);
        }

        var instances = await query.ToListAsync();

        return new FormStatisticsDto
        {
            TotalForms = instances.Count,
            DraftCount = instances.Count(i => i.Status == FormInstanceStatus.Draft),
            SubmittedCount = instances.Count(i => i.Status == FormInstanceStatus.Submitted),
            UnderReviewCount = instances.Count(i => i.Status == FormInstanceStatus.UnderReview),
            ApprovedCount = instances.Count(i => i.Status == FormInstanceStatus.Approved),
            RejectedCount = instances.Count(i => i.Status == FormInstanceStatus.Rejected),
            ByTemplate = instances
                .GroupBy(i => i.FormTemplate?.Name ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count()),
            ByModule = instances
                .GroupBy(i => i.FormTemplate?.ModuleContext.ToString() ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    #region Private Methods

    private static void ApplyDefaultValue(FormInstanceValue value, FormTemplateField field)
    {
        switch (field.DataType)
        {
            case FormFieldDataType.Text:
            case FormFieldDataType.MultilineText:
                value.TextValue = field.DefaultValue;
                break;
            case FormFieldDataType.Number:
            case FormFieldDataType.Currency:
            case FormFieldDataType.Percentage:
                if (decimal.TryParse(field.DefaultValue, out var numVal))
                    value.NumberValue = numVal;
                break;
            case FormFieldDataType.Date:
            case FormFieldDataType.DateTime:
                if (DateTime.TryParse(field.DefaultValue, out var dateVal))
                    value.DateValue = dateVal;
                break;
            case FormFieldDataType.Checkbox:
                if (bool.TryParse(field.DefaultValue, out var boolVal))
                    value.BoolValue = boolVal;
                break;
        }
    }

    private static void ApplyRequestValue(FormInstanceValue value, FormInstanceValueRequest request, int userId)
    {
        value.TextValue = request.TextValue;
        value.NumberValue = request.NumberValue;
        value.DateValue = request.DateValue;
        value.BoolValue = request.BoolValue;
        value.JsonValue = request.JsonValue != null ? JsonSerializer.Serialize(request.JsonValue) : null;
        value.SignatureDataUrl = request.SignatureDataUrl;
        value.PassFailValue = request.PassFailValue;
        value.PassFailComment = request.PassFailComment;
        value.ModifiedByUserId = userId;
        value.ModifiedDate = DateTime.UtcNow;
    }

    private static FormInstanceDto MapToDto(FormInstance i)
    {
        return new FormInstanceDto
        {
            Id = i.Id,
            CompanyId = i.CompanyId,
            FormNumber = i.FormNumber,
            FormTemplateId = i.FormTemplateId,
            Status = i.Status,
            LinkedEntityType = i.LinkedEntityType,
            LinkedEntityId = i.LinkedEntityId,
            LinkedEntityDisplay = i.LinkedEntityDisplay,
            Notes = i.Notes,
            CreatedDate = i.CreatedDate,
            CreatedByUserId = i.CreatedByUserId,
            CreatedByName = i.CreatedByUser != null ? i.CreatedByUser.FirstName + " " + i.CreatedByUser.LastName : null,
            ModifiedDate = i.ModifiedDate,
            ModifiedByUserId = i.ModifiedByUserId,
            ModifiedByName = i.ModifiedByUser != null ? i.ModifiedByUser.FirstName + " " + i.ModifiedByUser.LastName : null,
            SubmittedByUserId = i.SubmittedByUserId,
            SubmittedByName = i.SubmittedByUser != null ? i.SubmittedByUser.FirstName + " " + i.SubmittedByUser.LastName : null,
            SubmittedDate = i.SubmittedDate,
            ReviewedByUserId = i.ReviewedByUserId,
            ReviewedByName = i.ReviewedByUser != null ? i.ReviewedByUser.FirstName + " " + i.ReviewedByUser.LastName : null,
            ReviewedDate = i.ReviewedDate,
            ApprovedByUserId = i.ApprovedByUserId,
            ApprovedByName = i.ApprovedByUser != null ? i.ApprovedByUser.FirstName + " " + i.ApprovedByUser.LastName : null,
            ApprovedDate = i.ApprovedDate,
            RejectionReason = i.RejectionReason
        };
    }

    private static FormInstanceDto MapToDtoWithValues(FormInstance i)
    {
        var dto = MapToDto(i);

        if (i.FormTemplate != null)
        {
            dto.Template = new FormTemplateDto
            {
                Id = i.FormTemplate.Id,
                Name = i.FormTemplate.Name,
                Description = i.FormTemplate.Description,
                ModuleContext = i.FormTemplate.ModuleContext,
                FormType = i.FormTemplate.FormType,
                ShowSectionHeaders = i.FormTemplate.ShowSectionHeaders,
                AllowNotes = i.FormTemplate.AllowNotes,
                Fields = i.FormTemplate.Fields
                    .Where(f => !f.IsDeleted)
                    .OrderBy(f => f.SectionOrder)
                    .ThenBy(f => f.DisplayOrder)
                    .Select(f => new FormTemplateFieldDto
                    {
                        Id = f.Id,
                        FieldKey = f.FieldKey,
                        DisplayName = f.DisplayName,
                        DataType = f.DataType,
                        SectionName = f.SectionName,
                        SectionOrder = f.SectionOrder,
                        DisplayOrder = f.DisplayOrder,
                        Width = f.Width,
                        IsRequired = f.IsRequired,
                        IsVisible = f.IsVisible,
                        IsReadOnly = f.IsReadOnly,
                        HelpText = f.HelpText,
                        Placeholder = f.Placeholder
                    })
                    .ToList()
            };
        }

        dto.Values = i.Values.Select(MapValueToDto).ToList();
        dto.Attachments = i.Attachments.Where(a => !a.IsDeleted).Select(MapAttachmentToDto).ToList();

        return dto;
    }

    private static FormInstanceValueDto MapValueToDto(FormInstanceValue v)
    {
        object? jsonValue = null;
        if (!string.IsNullOrEmpty(v.JsonValue))
        {
            try
            {
                jsonValue = JsonSerializer.Deserialize<object>(v.JsonValue);
            }
            catch { }
        }

        return new FormInstanceValueDto
        {
            Id = v.Id,
            FormInstanceId = v.FormInstanceId,
            FormTemplateFieldId = v.FormTemplateFieldId,
            FieldKey = v.FieldKey,
            DataType = v.FormTemplateField?.DataType ?? FormFieldDataType.Text,
            DisplayName = v.FormTemplateField?.DisplayName,
            TextValue = v.TextValue,
            NumberValue = v.NumberValue,
            DateValue = v.DateValue,
            BoolValue = v.BoolValue,
            JsonValue = jsonValue,
            SignatureDataUrl = v.SignatureDataUrl,
            PassFailValue = v.PassFailValue,
            PassFailComment = v.PassFailComment,
            ModifiedDate = v.ModifiedDate,
            ModifiedByName = v.ModifiedByUser != null ? v.ModifiedByUser.FirstName + " " + v.ModifiedByUser.LastName : null,
            Attachments = v.Attachments?.Where(a => !a.IsDeleted).Select(MapAttachmentToDto).ToList()
        };
    }

    private static FormInstanceAttachmentDto MapAttachmentToDto(FormInstanceAttachment a)
    {
        return new FormInstanceAttachmentDto
        {
            Id = a.Id,
            FormInstanceId = a.FormInstanceId,
            FormInstanceValueId = a.FormInstanceValueId,
            FileName = a.FileName,
            FileType = a.FileType,
            FileSize = a.FileSize,
            StorageProvider = a.StorageProvider,
            StoragePath = a.StoragePath,
            ThumbnailPath = a.ThumbnailPath,
            Latitude = a.Latitude,
            Longitude = a.Longitude,
            Caption = a.Caption,
            UploadedDate = a.UploadedDate,
            UploadedByUserId = a.UploadedByUserId,
            UploadedByName = a.UploadedByUser != null ? a.UploadedByUser.FirstName + " " + a.UploadedByUser.LastName : null
        };
    }

    private static string FormatValueForHtml(FormInstanceValueDto? value, FormTemplateFieldDto field)
    {
        if (value == null) return "-";

        return field.DataType switch
        {
            FormFieldDataType.Text or FormFieldDataType.MultilineText => value.TextValue ?? "-",
            FormFieldDataType.Number => value.NumberValue?.ToString("N2") ?? "-",
            FormFieldDataType.Currency => value.NumberValue != null ? $"${value.NumberValue:N2}" : "-",
            FormFieldDataType.Percentage => value.NumberValue != null ? $"{value.NumberValue}%" : "-",
            FormFieldDataType.Date => value.DateValue?.ToString("yyyy-MM-dd") ?? "-",
            FormFieldDataType.DateTime => value.DateValue?.ToString("yyyy-MM-dd HH:mm") ?? "-",
            FormFieldDataType.Checkbox => value.BoolValue == true ? "☑ Yes" : "☐ No",
            FormFieldDataType.PassFail => value.PassFailValue switch
            {
                true => "✓ Pass",
                false => "✗ Fail",
                _ => "-"
            } + (string.IsNullOrEmpty(value.PassFailComment) ? "" : $" ({value.PassFailComment})"),
            FormFieldDataType.Signature => !string.IsNullOrEmpty(value.SignatureDataUrl)
                ? "<img src='" + value.SignatureDataUrl + "' style='max-height: 60px;' />"
                : "-",
            _ => value.TextValue ?? value.JsonValue?.ToString() ?? "-"
        };
    }

    #endregion
}
