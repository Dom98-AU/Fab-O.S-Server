using FabOS.WebServer.Models.DTOs.Forms;
using FabOS.WebServer.Models.Entities.Forms;

namespace FabOS.WebServer.Services.Interfaces.Forms;

/// <summary>
/// Service for managing form instances (filled forms)
/// </summary>
public interface IFormInstanceService
{
    /// <summary>
    /// Get form instances with filtering and pagination
    /// </summary>
    Task<FormInstanceListResponse> GetInstancesAsync(
        int companyId,
        FormInstanceFilterRequest filter);

    /// <summary>
    /// Get a form instance by ID
    /// </summary>
    Task<FormInstanceDto?> GetInstanceByIdAsync(int id, int companyId);

    /// <summary>
    /// Get a form instance with all values and attachments
    /// </summary>
    Task<FormInstanceDto?> GetInstanceWithValuesAsync(int id, int companyId);

    /// <summary>
    /// Get form instances linked to a specific entity
    /// </summary>
    Task<List<FormInstanceSummaryDto>> GetInstancesByEntityAsync(
        string entityType,
        int entityId,
        int companyId);

    /// <summary>
    /// Create a new form instance from a template
    /// </summary>
    Task<FormInstanceDto> CreateInstanceAsync(
        CreateFormInstanceRequest request,
        int companyId,
        int userId);

    /// <summary>
    /// Update form instance values
    /// </summary>
    Task<FormInstanceDto> UpdateInstanceAsync(
        int id,
        UpdateFormInstanceRequest request,
        int companyId,
        int userId);

    /// <summary>
    /// Delete a form instance (soft delete)
    /// </summary>
    Task<bool> DeleteInstanceAsync(int id, int companyId);

    /// <summary>
    /// Submit a form instance for review
    /// </summary>
    Task<FormInstanceDto> SubmitForReviewAsync(int id, int companyId, int userId);

    /// <summary>
    /// Mark a form instance as under review
    /// </summary>
    Task<FormInstanceDto> StartReviewAsync(int id, int companyId, int userId);

    /// <summary>
    /// Approve a form instance
    /// </summary>
    Task<FormInstanceDto> ApproveAsync(int id, int companyId, int userId);

    /// <summary>
    /// Reject a form instance
    /// </summary>
    Task<FormInstanceDto> RejectAsync(
        int id,
        string reason,
        int companyId,
        int userId);

    /// <summary>
    /// Revert a form instance back to draft status
    /// </summary>
    Task<FormInstanceDto> RevertToDraftAsync(int id, int companyId, int userId);

    /// <summary>
    /// Set a single field value
    /// </summary>
    Task<FormInstanceValueDto> SetFieldValueAsync(
        int instanceId,
        string fieldKey,
        FormInstanceValueRequest request,
        int companyId,
        int userId);

    /// <summary>
    /// Get a single field value
    /// </summary>
    Task<FormInstanceValueDto?> GetFieldValueAsync(
        int instanceId,
        string fieldKey,
        int companyId);

    /// <summary>
    /// Upload an attachment to a form instance
    /// </summary>
    Task<FormInstanceAttachmentDto> UploadAttachmentAsync(
        int instanceId,
        string? fieldKey,
        Stream fileStream,
        string fileName,
        string contentType,
        decimal? latitude,
        decimal? longitude,
        string? caption,
        int companyId,
        int userId);

    /// <summary>
    /// Delete an attachment
    /// </summary>
    Task<bool> DeleteAttachmentAsync(int attachmentId, int companyId);

    /// <summary>
    /// Get the next form number for a template
    /// </summary>
    Task<string> GetNextFormNumberAsync(int templateId, int companyId);

    /// <summary>
    /// Validate form instance values against template requirements
    /// </summary>
    Task<FormValidationResult> ValidateInstanceAsync(int id, int companyId);

    /// <summary>
    /// Generate HTML for PDF export
    /// </summary>
    Task<string> GenerateExportHtmlAsync(int id, int companyId);

    /// <summary>
    /// Get form statistics for a company
    /// </summary>
    Task<FormStatisticsDto> GetStatisticsAsync(
        int companyId,
        FormModuleContext? moduleContext = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null);
}

/// <summary>
/// Result of form validation
/// </summary>
public class FormValidationResult
{
    public bool IsValid { get; set; }
    public List<FormValidationError> Errors { get; set; } = new();
}

/// <summary>
/// Validation error details
/// </summary>
public class FormValidationError
{
    public string FieldKey { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Form statistics DTO
/// </summary>
public class FormStatisticsDto
{
    public int TotalForms { get; set; }
    public int DraftCount { get; set; }
    public int SubmittedCount { get; set; }
    public int UnderReviewCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public Dictionary<string, int> ByTemplate { get; set; } = new();
    public Dictionary<string, int> ByModule { get; set; } = new();
}
