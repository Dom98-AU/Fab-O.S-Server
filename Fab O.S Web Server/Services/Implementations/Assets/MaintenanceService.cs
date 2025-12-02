using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations.Assets;

/// <summary>
/// Service implementation for Maintenance Schedule and Record management
/// </summary>
public class MaintenanceService : IMaintenanceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MaintenanceService> _logger;

    public MaintenanceService(ApplicationDbContext context, ILogger<MaintenanceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Schedule CRUD Operations

    public async Task<MaintenanceSchedule?> GetScheduleByIdAsync(int id)
    {
        return await _context.MaintenanceSchedules
            .Include(s => s.Equipment)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<MaintenanceSchedule>> GetSchedulesByEquipmentAsync(int equipmentId)
    {
        return await _context.MaintenanceSchedules
            .Include(s => s.Equipment)
            .Where(s => s.EquipmentId == equipmentId)
            .OrderBy(s => s.NextDue)
            .ToListAsync();
    }

    public async Task<IEnumerable<MaintenanceSchedule>> GetSchedulesPagedAsync(int companyId, int page, int pageSize, int? equipmentId = null, MaintenanceScheduleStatus? status = null)
    {
        var query = _context.MaintenanceSchedules
            .Include(s => s.Equipment)
            .Where(s => s.Equipment!.CompanyId == companyId);

        if (equipmentId.HasValue)
            query = query.Where(s => s.EquipmentId == equipmentId.Value);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        return await query
            .OrderBy(s => s.NextDue)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetSchedulesCountAsync(int companyId, int? equipmentId = null, MaintenanceScheduleStatus? status = null)
    {
        var query = _context.MaintenanceSchedules
            .Where(s => s.Equipment!.CompanyId == companyId);

        if (equipmentId.HasValue)
            query = query.Where(s => s.EquipmentId == equipmentId.Value);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        return await query.CountAsync();
    }

    public async Task<MaintenanceSchedule> CreateScheduleAsync(MaintenanceSchedule schedule, string? createdBy = null)
    {
        schedule.CreatedDate = DateTime.UtcNow;
        schedule.CreatedBy = createdBy;

        if (!schedule.NextDue.HasValue)
            schedule.NextDue = CalculateNextDueDate(schedule.Frequency, schedule.CustomIntervalDays);

        _context.MaintenanceSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created maintenance schedule {Id} for equipment {EquipmentId}", schedule.Id, schedule.EquipmentId);
        return schedule;
    }

    public async Task<MaintenanceSchedule> UpdateScheduleAsync(MaintenanceSchedule schedule)
    {
        schedule.LastModified = DateTime.UtcNow;
        _context.MaintenanceSchedules.Update(schedule);
        await _context.SaveChangesAsync();
        return schedule;
    }

    public async Task<bool> DeleteScheduleAsync(int id)
    {
        var schedule = await _context.MaintenanceSchedules.FindAsync(id);
        if (schedule == null)
            return false;

        _context.MaintenanceSchedules.Remove(schedule);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Schedule Status Operations

    public async Task<MaintenanceSchedule> PauseScheduleAsync(int id)
    {
        var schedule = await GetScheduleByIdAsync(id);
        if (schedule == null)
            throw new InvalidOperationException($"Schedule with ID {id} not found");

        schedule.Status = MaintenanceScheduleStatus.Paused;
        schedule.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return schedule;
    }

    public async Task<MaintenanceSchedule> ResumeScheduleAsync(int id)
    {
        var schedule = await GetScheduleByIdAsync(id);
        if (schedule == null)
            throw new InvalidOperationException($"Schedule with ID {id} not found");

        schedule.Status = MaintenanceScheduleStatus.Active;
        schedule.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return schedule;
    }

    public async Task<MaintenanceSchedule> CompleteScheduleAsync(int id)
    {
        var schedule = await GetScheduleByIdAsync(id);
        if (schedule == null)
            throw new InvalidOperationException($"Schedule with ID {id} not found");

        schedule.Status = MaintenanceScheduleStatus.Completed;
        schedule.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return schedule;
    }

    public async Task<MaintenanceSchedule> CancelScheduleAsync(int id)
    {
        var schedule = await GetScheduleByIdAsync(id);
        if (schedule == null)
            throw new InvalidOperationException($"Schedule with ID {id} not found");

        schedule.Status = MaintenanceScheduleStatus.Cancelled;
        schedule.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return schedule;
    }

    public async Task<IEnumerable<MaintenanceSchedule>> GetActiveSchedulesAsync(int companyId)
    {
        return await _context.MaintenanceSchedules
            .Include(s => s.Equipment)
            .Where(s => s.Equipment!.CompanyId == companyId && s.Status == MaintenanceScheduleStatus.Active)
            .OrderBy(s => s.NextDue)
            .ToListAsync();
    }

    public async Task<IEnumerable<MaintenanceSchedule>> GetSchedulesByStatusAsync(int companyId, MaintenanceScheduleStatus status)
    {
        return await _context.MaintenanceSchedules
            .Include(s => s.Equipment)
            .Where(s => s.Equipment!.CompanyId == companyId && s.Status == status)
            .OrderBy(s => s.NextDue)
            .ToListAsync();
    }

    #endregion

    #region Schedule Due Date Operations

    public async Task<IEnumerable<MaintenanceSchedule>> GetDueSchedulesAsync(int companyId, int daysAhead = 7)
    {
        var futureDate = DateTime.UtcNow.AddDays(daysAhead);
        return await _context.MaintenanceSchedules
            .Include(s => s.Equipment)
            .Where(s => s.Equipment!.CompanyId == companyId &&
                        s.Status == MaintenanceScheduleStatus.Active &&
                        s.NextDue != null &&
                        s.NextDue <= futureDate &&
                        s.NextDue >= DateTime.UtcNow)
            .OrderBy(s => s.NextDue)
            .ToListAsync();
    }

    public async Task<IEnumerable<MaintenanceSchedule>> GetOverdueSchedulesAsync(int companyId)
    {
        return await _context.MaintenanceSchedules
            .Include(s => s.Equipment)
            .Where(s => s.Equipment!.CompanyId == companyId &&
                        s.Status == MaintenanceScheduleStatus.Active &&
                        s.NextDue != null &&
                        s.NextDue < DateTime.UtcNow)
            .OrderBy(s => s.NextDue)
            .ToListAsync();
    }

    public async Task<MaintenanceSchedule> UpdateNextDueDateAsync(int id, DateTime nextDue)
    {
        var schedule = await GetScheduleByIdAsync(id);
        if (schedule == null)
            throw new InvalidOperationException($"Schedule with ID {id} not found");

        schedule.NextDue = nextDue;
        schedule.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return schedule;
    }

    public async Task CalculateAllNextDueDatesAsync(int companyId)
    {
        var schedules = await _context.MaintenanceSchedules
            .Where(s => s.Equipment!.CompanyId == companyId && s.Status == MaintenanceScheduleStatus.Active)
            .ToListAsync();

        foreach (var schedule in schedules)
        {
            if (schedule.LastPerformed.HasValue)
            {
                schedule.NextDue = CalculateNextDueDate(schedule.Frequency, schedule.CustomIntervalDays, schedule.LastPerformed.Value);
            }
        }

        await _context.SaveChangesAsync();
    }

    private DateTime CalculateNextDueDate(MaintenanceFrequency frequency, int? customIntervalDays, DateTime? fromDate = null)
    {
        var baseDate = fromDate ?? DateTime.UtcNow;

        return frequency switch
        {
            MaintenanceFrequency.Daily => baseDate.AddDays(1),
            MaintenanceFrequency.Weekly => baseDate.AddDays(7),
            MaintenanceFrequency.Monthly => baseDate.AddMonths(1),
            MaintenanceFrequency.Quarterly => baseDate.AddMonths(3),
            MaintenanceFrequency.Yearly => baseDate.AddYears(1),
            MaintenanceFrequency.Custom => baseDate.AddDays(customIntervalDays ?? 30),
            _ => baseDate.AddMonths(1)
        };
    }

    #endregion

    #region Record CRUD Operations

    public async Task<MaintenanceRecord?> GetRecordByIdAsync(int id)
    {
        return await _context.MaintenanceRecords
            .Include(r => r.Equipment)
            .Include(r => r.Schedule)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<MaintenanceRecord>> GetRecordsByEquipmentAsync(int equipmentId)
    {
        return await _context.MaintenanceRecords
            .Include(r => r.Equipment)
            .Include(r => r.Schedule)
            .Where(r => r.EquipmentId == equipmentId)
            .OrderByDescending(r => r.ScheduledDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<MaintenanceRecord>> GetRecordsByScheduleAsync(int scheduleId)
    {
        return await _context.MaintenanceRecords
            .Include(r => r.Equipment)
            .Where(r => r.ScheduleId == scheduleId)
            .OrderByDescending(r => r.ScheduledDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<MaintenanceRecord>> GetRecordsPagedAsync(int companyId, int page, int pageSize, int? equipmentId = null, MaintenanceRecordStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.MaintenanceRecords
            .Include(r => r.Equipment)
            .Include(r => r.Schedule)
            .Where(r => r.Equipment!.CompanyId == companyId);

        if (equipmentId.HasValue)
            query = query.Where(r => r.EquipmentId == equipmentId.Value);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (fromDate.HasValue)
            query = query.Where(r => r.ScheduledDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.ScheduledDate <= toDate.Value);

        return await query
            .OrderByDescending(r => r.ScheduledDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetRecordsCountAsync(int companyId, int? equipmentId = null, MaintenanceRecordStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.MaintenanceRecords
            .Where(r => r.Equipment!.CompanyId == companyId);

        if (equipmentId.HasValue)
            query = query.Where(r => r.EquipmentId == equipmentId.Value);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (fromDate.HasValue)
            query = query.Where(r => r.ScheduledDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.ScheduledDate <= toDate.Value);

        return await query.CountAsync();
    }

    public async Task<MaintenanceRecord> CreateRecordAsync(MaintenanceRecord record, string? createdBy = null)
    {
        record.CreatedDate = DateTime.UtcNow;
        record.CreatedBy = createdBy;
        record.Status = MaintenanceRecordStatus.Scheduled;

        _context.MaintenanceRecords.Add(record);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created maintenance record {Id} for equipment {EquipmentId}", record.Id, record.EquipmentId);
        return record;
    }

    public async Task<MaintenanceRecord> UpdateRecordAsync(MaintenanceRecord record)
    {
        record.LastModified = DateTime.UtcNow;
        _context.MaintenanceRecords.Update(record);
        await _context.SaveChangesAsync();
        return record;
    }

    public async Task<bool> DeleteRecordAsync(int id)
    {
        var record = await _context.MaintenanceRecords.FindAsync(id);
        if (record == null)
            return false;

        _context.MaintenanceRecords.Remove(record);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Record Status Operations

    public async Task<MaintenanceRecord> StartMaintenanceAsync(int recordId, string? performedBy = null)
    {
        var record = await GetRecordByIdAsync(recordId);
        if (record == null)
            throw new InvalidOperationException($"Record with ID {recordId} not found");

        record.Status = MaintenanceRecordStatus.InProgress;
        record.StartedDate = DateTime.UtcNow;
        record.PerformedBy = performedBy;
        record.LastModified = DateTime.UtcNow;

        // Update equipment status
        var equipment = await _context.Equipment.FindAsync(record.EquipmentId);
        if (equipment != null)
        {
            equipment.Status = EquipmentStatus.InMaintenance;
            equipment.LastModified = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return record;
    }

    public async Task<MaintenanceRecord> CompleteMaintenanceAsync(int recordId, string? performedBy, decimal? actualHours, decimal? laborCost, decimal? partsCost, string? notes = null, string? partsUsed = null, string? completedChecklist = null)
    {
        var record = await GetRecordByIdAsync(recordId);
        if (record == null)
            throw new InvalidOperationException($"Record with ID {recordId} not found");

        record.Status = MaintenanceRecordStatus.Completed;
        record.CompletedDate = DateTime.UtcNow;
        record.PerformedBy = performedBy ?? record.PerformedBy;
        record.ActualHours = actualHours;
        record.LaborCost = laborCost;
        record.PartsCost = partsCost;
        record.TotalCost = (laborCost ?? 0) + (partsCost ?? 0);
        record.Notes = notes;
        record.PartsUsed = partsUsed;
        record.CompletedChecklist = completedChecklist;
        record.LastModified = DateTime.UtcNow;

        // Update equipment
        var equipment = await _context.Equipment.FindAsync(record.EquipmentId);
        if (equipment != null)
        {
            equipment.Status = EquipmentStatus.Active;
            equipment.LastMaintenanceDate = DateTime.UtcNow;
            equipment.LastModified = DateTime.UtcNow;

            // Calculate next maintenance date if interval is set
            if (equipment.MaintenanceIntervalDays.HasValue)
            {
                equipment.NextMaintenanceDate = DateTime.UtcNow.AddDays(equipment.MaintenanceIntervalDays.Value);
            }
        }

        // Update schedule if linked
        if (record.ScheduleId.HasValue)
        {
            var schedule = await _context.MaintenanceSchedules.FindAsync(record.ScheduleId.Value);
            if (schedule != null)
            {
                schedule.LastPerformed = DateTime.UtcNow;
                schedule.NextDue = CalculateNextDueDate(schedule.Frequency, schedule.CustomIntervalDays, DateTime.UtcNow);
                schedule.LastModified = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        return record;
    }

    public async Task<MaintenanceRecord> CancelMaintenanceAsync(int recordId, string? reason = null)
    {
        var record = await GetRecordByIdAsync(recordId);
        if (record == null)
            throw new InvalidOperationException($"Record with ID {recordId} not found");

        record.Status = MaintenanceRecordStatus.Cancelled;
        record.Notes = reason ?? record.Notes;
        record.LastModified = DateTime.UtcNow;

        // Restore equipment status if it was in maintenance
        var equipment = await _context.Equipment.FindAsync(record.EquipmentId);
        if (equipment != null && equipment.Status == EquipmentStatus.InMaintenance)
        {
            equipment.Status = EquipmentStatus.Active;
            equipment.LastModified = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return record;
    }

    public async Task<IEnumerable<MaintenanceRecord>> GetRecordsByStatusAsync(int companyId, MaintenanceRecordStatus status)
    {
        return await _context.MaintenanceRecords
            .Include(r => r.Equipment)
            .Where(r => r.Equipment!.CompanyId == companyId && r.Status == status)
            .OrderByDescending(r => r.ScheduledDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<MaintenanceRecord>> GetScheduledRecordsAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.MaintenanceRecords
            .Include(r => r.Equipment)
            .Where(r => r.Equipment!.CompanyId == companyId && r.Status == MaintenanceRecordStatus.Scheduled);

        if (fromDate.HasValue)
            query = query.Where(r => r.ScheduledDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.ScheduledDate <= toDate.Value);

        return await query.OrderBy(r => r.ScheduledDate).ToListAsync();
    }

    public async Task<IEnumerable<MaintenanceRecord>> GetInProgressRecordsAsync(int companyId)
    {
        return await _context.MaintenanceRecords
            .Include(r => r.Equipment)
            .Where(r => r.Equipment!.CompanyId == companyId && r.Status == MaintenanceRecordStatus.InProgress)
            .OrderBy(r => r.StartedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<MaintenanceRecord>> GetCompletedRecordsAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.MaintenanceRecords
            .Include(r => r.Equipment)
            .Where(r => r.Equipment!.CompanyId == companyId && r.Status == MaintenanceRecordStatus.Completed);

        if (fromDate.HasValue)
            query = query.Where(r => r.CompletedDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.CompletedDate <= toDate.Value);

        return await query.OrderByDescending(r => r.CompletedDate).ToListAsync();
    }

    public async Task<IEnumerable<MaintenanceRecord>> GetOverdueRecordsAsync(int companyId)
    {
        return await _context.MaintenanceRecords
            .Include(r => r.Equipment)
            .Where(r => r.Equipment!.CompanyId == companyId &&
                        r.Status == MaintenanceRecordStatus.Scheduled &&
                        r.ScheduledDate < DateTime.UtcNow)
            .OrderBy(r => r.ScheduledDate)
            .ToListAsync();
    }

    #endregion

    #region Record Generation

    public async Task<MaintenanceRecord> GenerateRecordFromScheduleAsync(int scheduleId, DateTime scheduledDate, string? createdBy = null)
    {
        var schedule = await GetScheduleByIdAsync(scheduleId);
        if (schedule == null)
            throw new InvalidOperationException($"Schedule with ID {scheduleId} not found");

        var record = new MaintenanceRecord
        {
            EquipmentId = schedule.EquipmentId,
            ScheduleId = scheduleId,
            Title = schedule.Title,
            Description = schedule.Description,
            ScheduledDate = scheduledDate,
            Status = MaintenanceRecordStatus.Scheduled,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _context.MaintenanceRecords.Add(record);
        await _context.SaveChangesAsync();

        return record;
    }

    public async Task<IEnumerable<MaintenanceRecord>> GenerateUpcomingRecordsAsync(int companyId, int daysAhead = 30, string? createdBy = null)
    {
        var futureDate = DateTime.UtcNow.AddDays(daysAhead);
        var schedules = await GetActiveSchedulesAsync(companyId);
        var generatedRecords = new List<MaintenanceRecord>();

        foreach (var schedule in schedules.Where(s => s.NextDue.HasValue && s.NextDue <= futureDate))
        {
            // Check if a record already exists for this date
            var existingRecord = await _context.MaintenanceRecords
                .AnyAsync(r => r.ScheduleId == schedule.Id &&
                              r.ScheduledDate.Date == schedule.NextDue!.Value.Date);

            if (!existingRecord)
            {
                var record = await GenerateRecordFromScheduleAsync(schedule.Id, schedule.NextDue!.Value, createdBy);
                generatedRecords.Add(record);
            }
        }

        return generatedRecords;
    }

    public async Task<IEnumerable<MaintenanceRecord>> GenerateRecordsForEquipmentAsync(int equipmentId, int daysAhead = 30, string? createdBy = null)
    {
        var futureDate = DateTime.UtcNow.AddDays(daysAhead);
        var schedules = await GetSchedulesByEquipmentAsync(equipmentId);
        var generatedRecords = new List<MaintenanceRecord>();

        foreach (var schedule in schedules.Where(s => s.Status == MaintenanceScheduleStatus.Active && s.NextDue.HasValue && s.NextDue <= futureDate))
        {
            var existingRecord = await _context.MaintenanceRecords
                .AnyAsync(r => r.ScheduleId == schedule.Id &&
                              r.ScheduledDate.Date == schedule.NextDue!.Value.Date);

            if (!existingRecord)
            {
                var record = await GenerateRecordFromScheduleAsync(schedule.Id, schedule.NextDue!.Value, createdBy);
                generatedRecords.Add(record);
            }
        }

        return generatedRecords;
    }

    #endregion

    #region Cost Tracking

    public async Task<decimal> GetTotalMaintenanceCostAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.MaintenanceRecords
            .Where(r => r.Equipment!.CompanyId == companyId &&
                        r.Status == MaintenanceRecordStatus.Completed &&
                        r.TotalCost != null);

        if (fromDate.HasValue)
            query = query.Where(r => r.CompletedDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.CompletedDate <= toDate.Value);

        return await query.SumAsync(r => r.TotalCost ?? 0);
    }

    public async Task<decimal> GetMaintenanceCostByEquipmentAsync(int equipmentId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.MaintenanceRecords
            .Where(r => r.EquipmentId == equipmentId &&
                        r.Status == MaintenanceRecordStatus.Completed &&
                        r.TotalCost != null);

        if (fromDate.HasValue)
            query = query.Where(r => r.CompletedDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.CompletedDate <= toDate.Value);

        return await query.SumAsync(r => r.TotalCost ?? 0);
    }

    public async Task<Dictionary<int, decimal>> GetMaintenanceCostsByEquipmentAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.MaintenanceRecords
            .Where(r => r.Equipment!.CompanyId == companyId &&
                        r.Status == MaintenanceRecordStatus.Completed &&
                        r.TotalCost != null);

        if (fromDate.HasValue)
            query = query.Where(r => r.CompletedDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.CompletedDate <= toDate.Value);

        return await query
            .GroupBy(r => r.EquipmentId)
            .Select(g => new { EquipmentId = g.Key, TotalCost = g.Sum(r => r.TotalCost ?? 0) })
            .ToDictionaryAsync(x => x.EquipmentId, x => x.TotalCost);
    }

    public async Task<decimal> GetAverageMaintenanceCostAsync(int companyId)
    {
        var costs = await _context.MaintenanceRecords
            .Where(r => r.Equipment!.CompanyId == companyId &&
                        r.Status == MaintenanceRecordStatus.Completed &&
                        r.TotalCost != null)
            .Select(r => r.TotalCost ?? 0)
            .ToListAsync();

        return costs.Any() ? costs.Average() : 0;
    }

    #endregion

    #region Reminder Operations

    public async Task<IEnumerable<MaintenanceSchedule>> GetSchedulesNeedingRemindersAsync(int companyId)
    {
        return await _context.MaintenanceSchedules
            .Include(s => s.Equipment)
            .Where(s => s.Equipment!.CompanyId == companyId &&
                        s.Status == MaintenanceScheduleStatus.Active &&
                        s.NextDue != null &&
                        s.NextDue <= DateTime.UtcNow.AddDays(s.ReminderDaysBefore) &&
                        s.NextDue >= DateTime.UtcNow)
            .OrderBy(s => s.NextDue)
            .ToListAsync();
    }

    public async Task<Dictionary<int, int>> GetMaintenanceCountsByMonthAsync(int companyId, int year)
    {
        var records = await _context.MaintenanceRecords
            .Where(r => r.Equipment!.CompanyId == companyId &&
                        r.CompletedDate != null &&
                        r.CompletedDate.Value.Year == year)
            .ToListAsync();

        return records
            .GroupBy(r => r.CompletedDate!.Value.Month)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<Dictionary<string, int>> GetMaintenanceCountsByTypeAsync(int companyId)
    {
        return await _context.MaintenanceRecords
            .Where(r => r.Equipment!.CompanyId == companyId && r.MaintenanceType != null)
            .GroupBy(r => r.MaintenanceType!)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count);
    }

    #endregion

    #region Analytics

    public async Task<IEnumerable<MaintenanceRecord>> GetRecentRecordsAsync(int companyId, int count = 10)
    {
        return await _context.MaintenanceRecords
            .Include(r => r.Equipment)
            .Where(r => r.Equipment!.CompanyId == companyId)
            .OrderByDescending(r => r.CreatedDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> GetCompletedCountAsync(int companyId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.MaintenanceRecords
            .Where(r => r.Equipment!.CompanyId == companyId && r.Status == MaintenanceRecordStatus.Completed);

        if (fromDate.HasValue)
            query = query.Where(r => r.CompletedDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.CompletedDate <= toDate.Value);

        return await query.CountAsync();
    }

    public async Task<decimal> GetAverageCompletionTimeAsync(int companyId)
    {
        var completedRecords = await _context.MaintenanceRecords
            .Where(r => r.Equipment!.CompanyId == companyId &&
                        r.Status == MaintenanceRecordStatus.Completed &&
                        r.StartedDate != null &&
                        r.CompletedDate != null)
            .Select(r => new { r.StartedDate, r.CompletedDate })
            .ToListAsync();

        if (!completedRecords.Any())
            return 0;

        var avgHours = completedRecords
            .Select(r => (r.CompletedDate!.Value - r.StartedDate!.Value).TotalHours)
            .Average();

        return (decimal)avgHours;
    }

    public async Task<decimal> GetComplianceRateAsync(int companyId)
    {
        var totalDue = await _context.MaintenanceRecords
            .CountAsync(r => r.Equipment!.CompanyId == companyId &&
                            r.ScheduledDate < DateTime.UtcNow);

        if (totalDue == 0)
            return 100;

        var completedOnTime = await _context.MaintenanceRecords
            .CountAsync(r => r.Equipment!.CompanyId == companyId &&
                            r.Status == MaintenanceRecordStatus.Completed &&
                            r.CompletedDate != null &&
                            r.CompletedDate <= r.ScheduledDate.AddDays(1));

        return (decimal)completedOnTime / totalDue * 100;
    }

    #endregion
}
