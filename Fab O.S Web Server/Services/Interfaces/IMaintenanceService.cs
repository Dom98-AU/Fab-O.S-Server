using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service interface for Maintenance Schedule and Record management
/// </summary>
public interface IMaintenanceService
{
    #region Schedule CRUD Operations

    Task<MaintenanceSchedule?> GetScheduleByIdAsync(int id);
    Task<IEnumerable<MaintenanceSchedule>> GetSchedulesByEquipmentAsync(int equipmentId);
    Task<IEnumerable<MaintenanceSchedule>> GetSchedulesPagedAsync(int companyId, int page, int pageSize, int? equipmentId = null, MaintenanceScheduleStatus? status = null);
    Task<int> GetSchedulesCountAsync(int companyId, int? equipmentId = null, MaintenanceScheduleStatus? status = null);
    Task<MaintenanceSchedule> CreateScheduleAsync(MaintenanceSchedule schedule, string? createdBy = null);
    Task<MaintenanceSchedule> UpdateScheduleAsync(MaintenanceSchedule schedule);
    Task<bool> DeleteScheduleAsync(int id);

    #endregion

    #region Schedule Status Operations

    Task<MaintenanceSchedule> PauseScheduleAsync(int id);
    Task<MaintenanceSchedule> ResumeScheduleAsync(int id);
    Task<MaintenanceSchedule> CompleteScheduleAsync(int id);
    Task<MaintenanceSchedule> CancelScheduleAsync(int id);
    Task<IEnumerable<MaintenanceSchedule>> GetActiveSchedulesAsync(int companyId);
    Task<IEnumerable<MaintenanceSchedule>> GetSchedulesByStatusAsync(int companyId, MaintenanceScheduleStatus status);

    #endregion

    #region Schedule Due Date Operations

    Task<IEnumerable<MaintenanceSchedule>> GetDueSchedulesAsync(int companyId, int daysAhead = 7);
    Task<IEnumerable<MaintenanceSchedule>> GetOverdueSchedulesAsync(int companyId);
    Task<MaintenanceSchedule> UpdateNextDueDateAsync(int id, DateTime nextDue);
    Task CalculateAllNextDueDatesAsync(int companyId);

    #endregion

    #region Record CRUD Operations

    Task<MaintenanceRecord?> GetRecordByIdAsync(int id);
    Task<IEnumerable<MaintenanceRecord>> GetRecordsByEquipmentAsync(int equipmentId);
    Task<IEnumerable<MaintenanceRecord>> GetRecordsByScheduleAsync(int scheduleId);
    Task<IEnumerable<MaintenanceRecord>> GetRecordsPagedAsync(int companyId, int page, int pageSize, int? equipmentId = null, MaintenanceRecordStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<int> GetRecordsCountAsync(int companyId, int? equipmentId = null, MaintenanceRecordStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<MaintenanceRecord> CreateRecordAsync(MaintenanceRecord record, string? createdBy = null);
    Task<MaintenanceRecord> UpdateRecordAsync(MaintenanceRecord record);
    Task<bool> DeleteRecordAsync(int id);

    #endregion

    #region Record Status Operations

    Task<MaintenanceRecord> StartMaintenanceAsync(int recordId, string? performedBy = null);
    Task<MaintenanceRecord> CompleteMaintenanceAsync(int recordId, string? performedBy, decimal? actualHours, decimal? laborCost, decimal? partsCost, string? notes = null, string? partsUsed = null, string? completedChecklist = null);
    Task<MaintenanceRecord> CancelMaintenanceAsync(int recordId, string? reason = null);
    Task<IEnumerable<MaintenanceRecord>> GetRecordsByStatusAsync(int companyId, MaintenanceRecordStatus status);
    Task<IEnumerable<MaintenanceRecord>> GetScheduledRecordsAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<MaintenanceRecord>> GetInProgressRecordsAsync(int companyId);
    Task<IEnumerable<MaintenanceRecord>> GetCompletedRecordsAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<MaintenanceRecord>> GetOverdueRecordsAsync(int companyId);

    #endregion

    #region Record Generation

    Task<MaintenanceRecord> GenerateRecordFromScheduleAsync(int scheduleId, DateTime scheduledDate, string? createdBy = null);
    Task<IEnumerable<MaintenanceRecord>> GenerateUpcomingRecordsAsync(int companyId, int daysAhead = 30, string? createdBy = null);
    Task<IEnumerable<MaintenanceRecord>> GenerateRecordsForEquipmentAsync(int equipmentId, int daysAhead = 30, string? createdBy = null);

    #endregion

    #region Cost Tracking

    Task<decimal> GetTotalMaintenanceCostAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<decimal> GetMaintenanceCostByEquipmentAsync(int equipmentId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<Dictionary<int, decimal>> GetMaintenanceCostsByEquipmentAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<decimal> GetAverageMaintenanceCostAsync(int companyId);

    #endregion

    #region Reminder Operations

    Task<IEnumerable<MaintenanceSchedule>> GetSchedulesNeedingRemindersAsync(int companyId);
    Task<Dictionary<int, int>> GetMaintenanceCountsByMonthAsync(int companyId, int year);
    Task<Dictionary<string, int>> GetMaintenanceCountsByTypeAsync(int companyId);

    #endregion

    #region Analytics

    Task<IEnumerable<MaintenanceRecord>> GetRecentRecordsAsync(int companyId, int count = 10);
    Task<int> GetCompletedCountAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<decimal> GetAverageCompletionTimeAsync(int companyId);
    Task<decimal> GetComplianceRateAsync(int companyId);

    #endregion
}
