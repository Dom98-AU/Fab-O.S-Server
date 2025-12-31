using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

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
