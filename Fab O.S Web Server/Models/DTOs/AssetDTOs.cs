using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

#region Equipment DTOs

public class EquipmentDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int TypeId { get; set; }
    public string? TypeName { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseCost { get; set; }
    public decimal? CurrentValue { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public string? Location { get; set; }
    public string? Department { get; set; }
    public string? AssignedTo { get; set; }
    public int? AssignedToUserId { get; set; }
    public EquipmentStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? Notes { get; set; }
    public string? QRCodeData { get; set; }
    public string? QRCodeIdentifier { get; set; }
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public int? MaintenanceIntervalDays { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }

    // Related counts
    public int MaintenanceScheduleCount { get; set; }
    public int MaintenanceRecordCount { get; set; }
    public int CertificationCount { get; set; }
    public int ManualCount { get; set; }
}

public class CreateEquipmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public int TypeId { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseCost { get; set; }
    public decimal? CurrentValue { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public string? Location { get; set; }
    public string? Department { get; set; }
    public string? AssignedTo { get; set; }
    public int? AssignedToUserId { get; set; }
    public string? Notes { get; set; }
    public int? MaintenanceIntervalDays { get; set; }
}

public class UpdateEquipmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public int TypeId { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseCost { get; set; }
    public decimal? CurrentValue { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public string? Location { get; set; }
    public string? Department { get; set; }
    public string? AssignedTo { get; set; }
    public int? AssignedToUserId { get; set; }
    public string? Notes { get; set; }
    public int? MaintenanceIntervalDays { get; set; }
}

public class EquipmentListResponse
{
    public List<EquipmentDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class EquipmentQRCodeResponse
{
    public int EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string QRCodeIdentifier { get; set; } = string.Empty;
    public string QRCodeDataUrl { get; set; } = string.Empty;
}

public class EquipmentDashboardDto
{
    public int TotalEquipment { get; set; }
    public int ActiveEquipment { get; set; }
    public int InMaintenanceEquipment { get; set; }
    public int OutOfServiceEquipment { get; set; }
    public int RetiredEquipment { get; set; }
    public int UpcomingMaintenanceCount { get; set; }
    public int OverdueMaintenanceCount { get; set; }
    public int ExpiringCertificationsCount { get; set; }
    public Dictionary<string, int> EquipmentByCategory { get; set; } = new();
    public Dictionary<string, int> EquipmentByLocation { get; set; } = new();
    public List<EquipmentDto> RecentEquipment { get; set; } = new();
    public List<MaintenanceRecordDto> RecentMaintenance { get; set; } = new();
}

#endregion

#region Category and Type DTOs

public class EquipmentCategoryDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystemCategory { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastModified { get; set; }
    public int EquipmentCount { get; set; }
    public int TypeCount { get; set; }
}

public class EquipmentCategoryWithTypesDto : EquipmentCategoryDto
{
    public List<EquipmentTypeDto> Types { get; set; } = new();
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
    public int DisplayOrder { get; set; }
}

public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class EquipmentTypeDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DefaultMaintenanceIntervalDays { get; set; }
    public string? RequiredCertifications { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystemType { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastModified { get; set; }
    public int EquipmentCount { get; set; }
}

public class CreateTypeRequest
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DefaultMaintenanceIntervalDays { get; set; }
    public string? RequiredCertifications { get; set; }
    public int DisplayOrder { get; set; }
}

public class UpdateTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DefaultMaintenanceIntervalDays { get; set; }
    public string? RequiredCertifications { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

#endregion

#region Maintenance Schedule DTOs

public class MaintenanceScheduleDto
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public string? EquipmentCode { get; set; }
    public string? EquipmentName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MaintenanceFrequency Frequency { get; set; }
    public string FrequencyName => Frequency.ToString();
    public int? CustomIntervalDays { get; set; }
    public DateTime? LastPerformed { get; set; }
    public DateTime? NextDue { get; set; }
    public int ReminderDaysBefore { get; set; }
    public MaintenanceScheduleStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public decimal? EstimatedHours { get; set; }
    public decimal? EstimatedCost { get; set; }
    public string? AssignedTo { get; set; }
    public string? ChecklistItems { get; set; }
    public string? RequiredParts { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModified { get; set; }
    public int CompletedCount { get; set; }
}

public class CreateScheduleRequest
{
    public int EquipmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MaintenanceFrequency Frequency { get; set; }
    public int? CustomIntervalDays { get; set; }
    public DateTime? NextDue { get; set; }
    public int ReminderDaysBefore { get; set; } = 7;
    public decimal? EstimatedHours { get; set; }
    public decimal? EstimatedCost { get; set; }
    public string? AssignedTo { get; set; }
    public string? ChecklistItems { get; set; }
    public string? RequiredParts { get; set; }
}

public class UpdateScheduleRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MaintenanceFrequency Frequency { get; set; }
    public int? CustomIntervalDays { get; set; }
    public DateTime? NextDue { get; set; }
    public int ReminderDaysBefore { get; set; }
    public decimal? EstimatedHours { get; set; }
    public decimal? EstimatedCost { get; set; }
    public string? AssignedTo { get; set; }
    public string? ChecklistItems { get; set; }
    public string? RequiredParts { get; set; }
}

public class ScheduleListResponse
{
    public List<MaintenanceScheduleDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

#endregion

#region Maintenance Record DTOs

public class MaintenanceRecordDto
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public string? EquipmentCode { get; set; }
    public string? EquipmentName { get; set; }
    public int? ScheduleId { get; set; }
    public string? ScheduleTitle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MaintenanceType { get; set; }
    public DateTime ScheduledDate { get; set; }
    public DateTime? StartedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public MaintenanceRecordStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? PerformedBy { get; set; }
    public decimal? ActualHours { get; set; }
    public decimal? LaborCost { get; set; }
    public decimal? PartsCost { get; set; }
    public decimal? TotalCost { get; set; }
    public string? CompletedChecklist { get; set; }
    public string? PartsUsed { get; set; }
    public string? Notes { get; set; }
    public string? Attachments { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModified { get; set; }
}

public class CreateRecordRequest
{
    public int EquipmentId { get; set; }
    public int? ScheduleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MaintenanceType { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string? AssignedTo { get; set; }
    public decimal? EstimatedHours { get; set; }
    public decimal? EstimatedCost { get; set; }
}

public class CompleteMaintenanceRequest
{
    public string? PerformedBy { get; set; }
    public decimal? ActualHours { get; set; }
    public decimal? LaborCost { get; set; }
    public decimal? PartsCost { get; set; }
    public string? CompletedChecklist { get; set; }
    public string? PartsUsed { get; set; }
    public string? Notes { get; set; }
    public string? Attachments { get; set; }
}

public class RecordListResponse
{
    public List<MaintenanceRecordDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class MaintenanceDashboardDto
{
    public int TotalSchedules { get; set; }
    public int ActiveSchedules { get; set; }
    public int UpcomingMaintenanceCount { get; set; }
    public int OverdueMaintenanceCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedThisMonth { get; set; }
    public decimal TotalCostThisMonth { get; set; }
    public decimal AverageCostPerMaintenance { get; set; }
    public Dictionary<string, int> MaintenanceByType { get; set; } = new();
    public Dictionary<string, decimal> CostByEquipment { get; set; } = new();
    public List<MaintenanceRecordDto> UpcomingMaintenance { get; set; } = new();
    public List<MaintenanceRecordDto> OverdueMaintenance { get; set; } = new();
}

#endregion

#region Certification DTOs

public class CertificationDto
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public string? EquipmentCode { get; set; }
    public string? EquipmentName { get; set; }
    public string CertificationType { get; set; } = string.Empty;
    public string? CertificateNumber { get; set; }
    public string? IssuingAuthority { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? DocumentUrl { get; set; }
    public string Status { get; set; } = "Valid";
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastModified { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;
    public bool IsExpiringSoon => ExpiryDate.HasValue &&
        ExpiryDate.Value >= DateTime.UtcNow &&
        ExpiryDate.Value <= DateTime.UtcNow.AddDays(30);
}

public class CreateCertificationRequest
{
    public int EquipmentId { get; set; }
    public string CertificationType { get; set; } = string.Empty;
    public string? CertificateNumber { get; set; }
    public string? IssuingAuthority { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? DocumentUrl { get; set; }
    public string? Notes { get; set; }
}

public class UpdateCertificationRequest
{
    public string CertificationType { get; set; } = string.Empty;
    public string? CertificateNumber { get; set; }
    public string? IssuingAuthority { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? DocumentUrl { get; set; }
    public string? Notes { get; set; }
}

public class RenewCertificationRequest
{
    public DateTime NewIssueDate { get; set; }
    public DateTime? NewExpiryDate { get; set; }
    public string? NewCertificateNumber { get; set; }
    public string? DocumentUrl { get; set; }
}

public class CertificationListResponse
{
    public List<CertificationDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

#endregion

#region Manual DTOs

public class ManualDto
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public string? EquipmentCode { get; set; }
    public string? EquipmentName { get; set; }
    public string ManualType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DocumentUrl { get; set; }
    public string? Version { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? ContentType { get; set; }
    public DateTime UploadedDate { get; set; }
    public string? UploadedBy { get; set; }
    public DateTime? LastModified { get; set; }
    public string FormattedFileSize => FormatFileSize(FileSize);

    private static string FormatFileSize(long? bytes)
    {
        if (!bytes.HasValue) return "Unknown";
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes.Value;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}

public class CreateManualRequest
{
    public int EquipmentId { get; set; }
    public string ManualType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DocumentUrl { get; set; }
    public string? Version { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? ContentType { get; set; }
}

public class UpdateManualRequest
{
    public string ManualType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DocumentUrl { get; set; }
    public string? Version { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? ContentType { get; set; }
}

public class ManualListResponse
{
    public List<ManualDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

#endregion

#region Label Printing DTOs

public class GenerateLabelRequest
{
    public int EquipmentId { get; set; }
    public string Template { get; set; } = "Standard";
    public LabelOptionsDto? Options { get; set; }
}

public class GenerateBatchLabelsRequest
{
    public List<int> EquipmentIds { get; set; } = new();
    public string Template { get; set; } = "Standard";
    public LabelOptionsDto? Options { get; set; }
}

public class LabelOptionsDto
{
    public bool IncludeQRCode { get; set; } = true;
    public bool IncludeCategory { get; set; } = true;
    public bool IncludeLocation { get; set; } = true;
    public bool IncludeNextServiceDate { get; set; } = true;
    public bool IncludeSerialNumber { get; set; } = true;
    public bool IncludeBarcode { get; set; }
    public string? CustomField1Label { get; set; }
    public string? CustomField1Value { get; set; }
    public string? CustomField2Label { get; set; }
    public string? CustomField2Value { get; set; }
}

public class LabelTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double WidthMm { get; set; }
    public double HeightMm { get; set; }
    public bool SupportsQRCode { get; set; }
    public bool SupportsBarcode { get; set; }
    public int MaxCustomFields { get; set; }
}

public class LabelGenerationResult
{
    public bool Success { get; set; }
    public string? PdfBase64 { get; set; }
    public string? FileName { get; set; }
    public string? ErrorMessage { get; set; }
    public int LabelCount { get; set; }
}

#endregion

#region Controller Request/Response DTOs

// Additional DTOs required by controllers

public class EquipmentStatusUpdateRequest
{
    public string Status { get; set; } = string.Empty;
}

public class EquipmentAssignmentRequest
{
    public int? UserId { get; set; }
}

public class LabelOptions
{
    public double? WidthMm { get; set; }
    public double? HeightMm { get; set; }
    public string? Template { get; set; }
    public bool IncludeLogo { get; set; }
    public bool IncludeQRCode { get; set; } = true;
    public bool IncludeServiceDate { get; set; } = true;
    public bool IncludeLocation { get; set; } = true;
    public bool IncludeCategory { get; set; } = true;
    public int LabelsPerRow { get; set; } = 2;
    public int RowsPerPage { get; set; } = 5;
    public double MarginMm { get; set; } = 5;
}

public class CreateMaintenanceScheduleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FrequencyType { get; set; } = "Monthly";
    public int FrequencyValue { get; set; } = 1;
    public int? HoursInterval { get; set; }
    public string? ChecklistItems { get; set; }
    public decimal? EstimatedDurationHours { get; set; }
    public decimal? EstimatedCost { get; set; }
    public int ReminderDaysBefore { get; set; } = 7;
    public bool SendEmailReminder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateMaintenanceScheduleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FrequencyType { get; set; } = "Monthly";
    public int FrequencyValue { get; set; } = 1;
    public int? HoursInterval { get; set; }
    public string? ChecklistItems { get; set; }
    public decimal? EstimatedDurationHours { get; set; }
    public decimal? EstimatedCost { get; set; }
    public int ReminderDaysBefore { get; set; } = 7;
    public bool SendEmailReminder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MaintenanceListResponse
{
    public List<MaintenanceRecordDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class ScheduleMaintenanceRequest
{
    public int EquipmentId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string MaintenanceType { get; set; } = string.Empty;
    public string? Description { get; set; }
}

#endregion

#region QR Code DTOs

public class EquipmentQRData
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Type { get; set; }
    public string? Location { get; set; }
    public DateTime? NextServiceDate { get; set; }
    public string? SerialNumber { get; set; }
    public string GeneratedAt { get; set; } = DateTime.UtcNow.ToString("O");
}

public class QRCodeGenerationResult
{
    public bool Success { get; set; }
    public string? QRCodeDataUrl { get; set; }
    public string? QRCodeIdentifier { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion

#region Kit Template DTOs

public class KitTemplateDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? IconClass { get; set; }
    public int DefaultCheckoutDays { get; set; }
    public bool RequiresSignature { get; set; }
    public bool RequiresConditionCheck { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? LastModified { get; set; }
    public int? LastModifiedByUserId { get; set; }
    public int TemplateItemCount { get; set; }
    public int KitCount { get; set; }
    public List<KitTemplateItemDto> TemplateItems { get; set; } = new();
}

public class KitTemplateItemDto
{
    public int Id { get; set; }
    public int KitTemplateId { get; set; }
    public int EquipmentTypeId { get; set; }
    public string? EquipmentTypeName { get; set; }
    public string? EquipmentCategoryName { get; set; }
    public int Quantity { get; set; }
    public bool IsMandatory { get; set; }
    public int DisplayOrder { get; set; }
    public string? Notes { get; set; }
}

public class CreateKitTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? IconClass { get; set; }
    public int DefaultCheckoutDays { get; set; } = 7;
    public bool RequiresSignature { get; set; } = true;
    public bool RequiresConditionCheck { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class UpdateKitTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? IconClass { get; set; }
    public int DefaultCheckoutDays { get; set; }
    public bool RequiresSignature { get; set; }
    public bool RequiresConditionCheck { get; set; }
    public bool IsActive { get; set; }
}

public class AddTemplateItemRequest
{
    public int EquipmentTypeId { get; set; }
    public int Quantity { get; set; } = 1;
    public bool IsMandatory { get; set; } = true;
    public int DisplayOrder { get; set; }
    public string? Notes { get; set; }
}

public class UpdateTemplateItemRequest
{
    public int Quantity { get; set; } = 1;
    public bool IsMandatory { get; set; } = true;
    public int DisplayOrder { get; set; }
    public string? Notes { get; set; }
}

public class KitTemplateListResponse
{
    public List<KitTemplateDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

#endregion

#region Equipment Kit DTOs

public class EquipmentKitDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int? KitTemplateId { get; set; }
    public string? KitTemplateName { get; set; }
    public string KitCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public KitStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? Location { get; set; }
    public int? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
    public string? QRCodeData { get; set; }
    public string? QRCodeIdentifier { get; set; }
    public bool HasMaintenanceFlag { get; set; }
    public string? MaintenanceFlagNotes { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? LastModified { get; set; }
    public int? LastModifiedByUserId { get; set; }
    public int KitItemCount { get; set; }
    public List<EquipmentKitItemDto> KitItems { get; set; } = new();
}

public class EquipmentKitItemDto
{
    public int Id { get; set; }
    public int KitId { get; set; }
    public int EquipmentId { get; set; }
    public string? EquipmentCode { get; set; }
    public string? EquipmentName { get; set; }
    public string? EquipmentTypeName { get; set; }
    public string? EquipmentCategoryName { get; set; }
    public int? TemplateItemId { get; set; }
    public int DisplayOrder { get; set; }
    public bool NeedsMaintenance { get; set; }
    public string? Notes { get; set; }
    public DateTime AddedDate { get; set; }
    public int? AddedByUserId { get; set; }
}

public class CreateKitFromTemplateRequest
{
    public int TemplateId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public List<int> EquipmentIds { get; set; } = new();
}

public class CreateAdHocKitRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public List<int> EquipmentIds { get; set; } = new();
}

public class UpdateKitRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
}

public class AddKitItemRequest
{
    public int EquipmentId { get; set; }
    public int? TemplateItemId { get; set; }
    public int DisplayOrder { get; set; }
    public string? Notes { get; set; }
}

public class SwapKitItemRequest
{
    public int OldEquipmentId { get; set; }
    public int NewEquipmentId { get; set; }
}

public class FlagMaintenanceRequest
{
    public int EquipmentId { get; set; }
    public string? Notes { get; set; }
}

public class UpdateKitStatusRequest
{
    public KitStatus Status { get; set; }
}

public class EquipmentKitListResponse
{
    public List<EquipmentKitDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class KitDashboardDto
{
    public int TotalKits { get; set; }
    public int AvailableKits { get; set; }
    public int CheckedOutKits { get; set; }
    public int MaintenanceFlaggedKits { get; set; }
    public int OverdueCheckouts { get; set; }
    public Dictionary<string, int> KitsByTemplate { get; set; } = new();
    public Dictionary<KitStatus, int> KitsByStatus { get; set; } = new();
}

#endregion

#region Kit Checkout DTOs

public class KitCheckoutDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int KitId { get; set; }
    public string? KitCode { get; set; }
    public string? KitName { get; set; }
    public CheckoutStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public int CheckedOutToUserId { get; set; }
    public string? CheckedOutToUserName { get; set; }
    public DateTime CheckoutDate { get; set; }
    public DateTime ExpectedReturnDate { get; set; }
    public string? CheckoutPurpose { get; set; }
    public string? ProjectReference { get; set; }
    public EquipmentCondition CheckoutOverallCondition { get; set; }
    public string CheckoutConditionName => CheckoutOverallCondition.ToString();
    public string? CheckoutNotes { get; set; }
    public bool HasCheckoutSignature { get; set; }
    public DateTime? CheckoutSignedDate { get; set; }
    public int? CheckoutProcessedByUserId { get; set; }
    public DateTime? ActualReturnDate { get; set; }
    public int? ReturnedByUserId { get; set; }
    public string? ReturnedByUserName { get; set; }
    public EquipmentCondition? ReturnOverallCondition { get; set; }
    public string? ReturnConditionName => ReturnOverallCondition?.ToString();
    public string? ReturnNotes { get; set; }
    public bool HasReturnSignature { get; set; }
    public DateTime? ReturnSignedDate { get; set; }
    public int? ReturnProcessedByUserId { get; set; }
    public DateTime CreatedDate { get; set; }
    public int DaysUntilDue => (ExpectedReturnDate - DateTime.UtcNow).Days;
    public bool IsOverdue => ExpectedReturnDate < DateTime.UtcNow && Status == CheckoutStatus.CheckedOut;
    public List<KitCheckoutItemDto> CheckoutItems { get; set; } = new();
}

public class KitCheckoutItemDto
{
    public int Id { get; set; }
    public int KitCheckoutId { get; set; }
    public int KitItemId { get; set; }
    public int EquipmentId { get; set; }
    public string? EquipmentCode { get; set; }
    public string? EquipmentName { get; set; }
    public bool WasPresentAtCheckout { get; set; }
    public EquipmentCondition? CheckoutCondition { get; set; }
    public string? CheckoutConditionName => CheckoutCondition?.ToString();
    public string? CheckoutNotes { get; set; }
    public bool? WasPresentAtReturn { get; set; }
    public EquipmentCondition? ReturnCondition { get; set; }
    public string? ReturnConditionName => ReturnCondition?.ToString();
    public string? ReturnNotes { get; set; }
    public bool DamageReported { get; set; }
    public string? DamageDescription { get; set; }
}

public class InitiateCheckoutRequest
{
    public int KitId { get; set; }
    public int CheckedOutToUserId { get; set; }
    public DateTime ExpectedReturnDate { get; set; }
    public string? CheckoutPurpose { get; set; }
    public string? ProjectReference { get; set; }
    public EquipmentCondition OverallCondition { get; set; } = EquipmentCondition.Good;
    public string? Notes { get; set; }
    public List<CheckoutItemConditionRequest> ItemConditions { get; set; } = new();
}

public class CheckoutItemConditionRequest
{
    public int KitItemId { get; set; }
    public int EquipmentId { get; set; }
    public bool WasPresent { get; set; } = true;
    public EquipmentCondition Condition { get; set; } = EquipmentCondition.Good;
    public string? Notes { get; set; }
}

public class ConfirmCheckoutRequest
{
    public string Signature { get; set; } = string.Empty; // Base64 encoded
}

public class InitiateReturnRequest
{
    public EquipmentCondition OverallCondition { get; set; }
    public string? Notes { get; set; }
    public List<ReturnItemConditionRequest> ItemConditions { get; set; } = new();
}

public class ReturnItemConditionRequest
{
    public int KitItemId { get; set; }
    public int EquipmentId { get; set; }
    public bool WasPresent { get; set; } = true;
    public EquipmentCondition Condition { get; set; }
    public string? Notes { get; set; }
    public bool DamageReported { get; set; }
    public string? DamageDescription { get; set; }
}

public class ConfirmReturnRequest
{
    public string Signature { get; set; } = string.Empty; // Base64 encoded
}

public class PartialReturnRequest
{
    public List<ReturnItemConditionRequest> ReturnedItems { get; set; } = new();
    public string Signature { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class ExtendCheckoutRequest
{
    public DateTime NewExpectedReturnDate { get; set; }
    public string? Reason { get; set; }
}

public class CancelCheckoutRequest
{
    public string? Reason { get; set; }
}

public class UpdateItemConditionRequest
{
    public EquipmentCondition Condition { get; set; }
    public string? Notes { get; set; }
}

public class ReportDamageRequest
{
    public string DamageDescription { get; set; } = string.Empty;
}

public class KitCheckoutListResponse
{
    public List<KitCheckoutDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class CheckoutStatisticsDto
{
    public int TotalCheckouts { get; set; }
    public int ActiveCheckouts { get; set; }
    public int CompletedCheckouts { get; set; }
    public int OverdueCheckouts { get; set; }
    public int CancelledCheckouts { get; set; }
    public double AverageCheckoutDays { get; set; }
    public int TotalDamageReports { get; set; }
    public Dictionary<int, int> CheckoutsByUser { get; set; } = new();
    public Dictionary<int, int> CheckoutsByKit { get; set; } = new();
}

#endregion

#region Location DTOs

/// <summary>
/// Location response DTO
/// </summary>
public class LocationDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LocationType Type { get; set; }
    public string TypeName => Type.ToString();
    public string? Address { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; }
    public int EquipmentCount { get; set; }
    public int KitCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? LastModified { get; set; }
    public int? LastModifiedByUserId { get; set; }
}

/// <summary>
/// Location list response with pagination
/// </summary>
public class LocationListResponse
{
    public List<LocationDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Request DTO for creating a location
/// </summary>
public class CreateLocationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LocationType Type { get; set; } = LocationType.PhysicalSite;
    public string? Address { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request DTO for updating a location
/// </summary>
public class UpdateLocationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LocationType Type { get; set; }
    public string? Address { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Request DTO for assigning equipment to a location
/// </summary>
public class AssignEquipmentRequest
{
    public List<int> EquipmentIds { get; set; } = new();
}

/// <summary>
/// Request DTO for assigning kits to a location
/// </summary>
public class AssignKitsRequest
{
    public List<int> KitIds { get; set; } = new();
}

/// <summary>
/// Dashboard statistics for locations
/// </summary>
public class LocationDashboardDto
{
    public int TotalLocations { get; set; }
    public int ActiveLocations { get; set; }
    public int PhysicalSites { get; set; }
    public int JobSites { get; set; }
    public int Vehicles { get; set; }
    public int TotalEquipmentAllocated { get; set; }
    public int TotalKitsAllocated { get; set; }
}

/// <summary>
/// Location type enumeration info
/// </summary>
public class LocationTypeDto
{
    public int Value { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

#endregion
