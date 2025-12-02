using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FabOS.WebServer.Models.DTOs.Assets;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Controllers;

/// <summary>
/// API controller for Maintenance Schedules and Records in the Asset module
/// </summary>
[ApiController]
[Route("api/assets/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceService _maintenanceService;
    private readonly ILogger<MaintenanceController> _logger;

    public MaintenanceController(
        IMaintenanceService maintenanceService,
        ILogger<MaintenanceController> logger)
    {
        _maintenanceService = maintenanceService;
        _logger = logger;
    }

    #region Maintenance Schedules

    /// <summary>
    /// Get all maintenance schedules (paged)
    /// </summary>
    [HttpGet("schedules")]
    public async Task<ActionResult<ScheduleListResponse>> GetSchedules(
        [FromQuery] int companyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? equipmentId = null,
        [FromQuery] MaintenanceScheduleStatus? status = null)
    {
        try
        {
            var schedules = await _maintenanceService.GetSchedulesPagedAsync(companyId, page, pageSize, equipmentId, status);
            var totalCount = await _maintenanceService.GetSchedulesCountAsync(companyId, equipmentId, status);

            var items = schedules.Select(s => MapScheduleToDto(s)).ToList();

            return Ok(new ScheduleListResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting maintenance schedules");
            return StatusCode(500, new { message = "Error retrieving schedules" });
        }
    }

    /// <summary>
    /// Get schedules for specific equipment
    /// </summary>
    [HttpGet("schedules/equipment/{equipmentId:int}")]
    public async Task<ActionResult<List<MaintenanceScheduleDto>>> GetSchedulesByEquipment(int equipmentId)
    {
        try
        {
            var schedules = await _maintenanceService.GetSchedulesByEquipmentAsync(equipmentId);
            return Ok(schedules.Select(s => MapScheduleToDto(s)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting schedules for equipment {EquipmentId}", equipmentId);
            return StatusCode(500, new { message = "Error retrieving schedules" });
        }
    }

    /// <summary>
    /// Get schedule by ID
    /// </summary>
    [HttpGet("schedules/{id:int}")]
    public async Task<ActionResult<MaintenanceScheduleDto>> GetSchedule(int id)
    {
        try
        {
            var schedule = await _maintenanceService.GetScheduleByIdAsync(id);
            if (schedule == null)
                return NotFound(new { message = $"Schedule with ID {id} not found" });

            return Ok(MapScheduleToDto(schedule));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting schedule {Id}", id);
            return StatusCode(500, new { message = "Error retrieving schedule" });
        }
    }

    /// <summary>
    /// Create maintenance schedule
    /// </summary>
    [HttpPost("schedules")]
    public async Task<ActionResult<MaintenanceScheduleDto>> CreateSchedule([FromBody] CreateScheduleRequest request)
    {
        try
        {
            var schedule = new MaintenanceSchedule
            {
                EquipmentId = request.EquipmentId,
                Title = request.Title,
                Description = request.Description,
                Frequency = request.Frequency,
                CustomIntervalDays = request.CustomIntervalDays,
                NextDue = request.NextDue,
                ReminderDaysBefore = request.ReminderDaysBefore,
                EstimatedHours = request.EstimatedHours,
                EstimatedCost = request.EstimatedCost,
                AssignedTo = request.AssignedTo,
                ChecklistItems = request.ChecklistItems,
                RequiredParts = request.RequiredParts
            };

            var created = await _maintenanceService.CreateScheduleAsync(schedule);

            return CreatedAtAction(nameof(GetSchedule), new { id = created.Id }, MapScheduleToDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating schedule");
            return StatusCode(500, new { message = "Error creating schedule" });
        }
    }

    /// <summary>
    /// Update maintenance schedule
    /// </summary>
    [HttpPut("schedules/{id:int}")]
    public async Task<ActionResult<MaintenanceScheduleDto>> UpdateSchedule(int id, [FromBody] UpdateScheduleRequest request)
    {
        try
        {
            var existing = await _maintenanceService.GetScheduleByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Schedule with ID {id} not found" });

            existing.Title = request.Title;
            existing.Description = request.Description;
            existing.Frequency = request.Frequency;
            existing.CustomIntervalDays = request.CustomIntervalDays;
            existing.NextDue = request.NextDue;
            existing.ReminderDaysBefore = request.ReminderDaysBefore;
            existing.EstimatedHours = request.EstimatedHours;
            existing.EstimatedCost = request.EstimatedCost;
            existing.AssignedTo = request.AssignedTo;
            existing.ChecklistItems = request.ChecklistItems;
            existing.RequiredParts = request.RequiredParts;

            var updated = await _maintenanceService.UpdateScheduleAsync(existing);

            return Ok(MapScheduleToDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating schedule {Id}", id);
            return StatusCode(500, new { message = "Error updating schedule" });
        }
    }

    /// <summary>
    /// Delete maintenance schedule
    /// </summary>
    [HttpDelete("schedules/{id:int}")]
    public async Task<ActionResult> DeleteSchedule(int id)
    {
        try
        {
            var result = await _maintenanceService.DeleteScheduleAsync(id);
            if (!result)
                return NotFound(new { message = $"Schedule with ID {id} not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting schedule {Id}", id);
            return StatusCode(500, new { message = "Error deleting schedule" });
        }
    }

    /// <summary>
    /// Pause a schedule
    /// </summary>
    [HttpPost("schedules/{id:int}/pause")]
    public async Task<ActionResult<MaintenanceScheduleDto>> PauseSchedule(int id)
    {
        try
        {
            var schedule = await _maintenanceService.PauseScheduleAsync(id);
            return Ok(MapScheduleToDto(schedule));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing schedule {Id}", id);
            return StatusCode(500, new { message = "Error pausing schedule" });
        }
    }

    /// <summary>
    /// Resume a paused schedule
    /// </summary>
    [HttpPost("schedules/{id:int}/resume")]
    public async Task<ActionResult<MaintenanceScheduleDto>> ResumeSchedule(int id)
    {
        try
        {
            var schedule = await _maintenanceService.ResumeScheduleAsync(id);
            return Ok(MapScheduleToDto(schedule));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming schedule {Id}", id);
            return StatusCode(500, new { message = "Error resuming schedule" });
        }
    }

    /// <summary>
    /// Get due schedules
    /// </summary>
    [HttpGet("schedules/due")]
    public async Task<ActionResult<List<MaintenanceScheduleDto>>> GetDueSchedules(
        [FromQuery] int companyId,
        [FromQuery] int daysAhead = 7)
    {
        try
        {
            var schedules = await _maintenanceService.GetDueSchedulesAsync(companyId, daysAhead);
            return Ok(schedules.Select(s => MapScheduleToDto(s)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting due schedules");
            return StatusCode(500, new { message = "Error retrieving schedules" });
        }
    }

    /// <summary>
    /// Get overdue schedules
    /// </summary>
    [HttpGet("schedules/overdue")]
    public async Task<ActionResult<List<MaintenanceScheduleDto>>> GetOverdueSchedules([FromQuery] int companyId)
    {
        try
        {
            var schedules = await _maintenanceService.GetOverdueSchedulesAsync(companyId);
            return Ok(schedules.Select(s => MapScheduleToDto(s)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overdue schedules");
            return StatusCode(500, new { message = "Error retrieving schedules" });
        }
    }

    #endregion

    #region Maintenance Records

    /// <summary>
    /// Get all maintenance records (paged)
    /// </summary>
    [HttpGet("records")]
    public async Task<ActionResult<RecordListResponse>> GetRecords(
        [FromQuery] int companyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? equipmentId = null,
        [FromQuery] MaintenanceRecordStatus? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var records = await _maintenanceService.GetRecordsPagedAsync(
                companyId, page, pageSize, equipmentId, status, fromDate, toDate);
            var totalCount = await _maintenanceService.GetRecordsCountAsync(
                companyId, equipmentId, status, fromDate, toDate);

            var items = records.Select(r => MapRecordToDto(r)).ToList();

            return Ok(new RecordListResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting maintenance records");
            return StatusCode(500, new { message = "Error retrieving records" });
        }
    }

    /// <summary>
    /// Get maintenance records for equipment
    /// </summary>
    [HttpGet("records/equipment/{equipmentId:int}")]
    public async Task<ActionResult<List<MaintenanceRecordDto>>> GetRecordsByEquipment(int equipmentId)
    {
        try
        {
            var records = await _maintenanceService.GetRecordsByEquipmentAsync(equipmentId);
            return Ok(records.Select(r => MapRecordToDto(r)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting records for equipment {EquipmentId}", equipmentId);
            return StatusCode(500, new { message = "Error retrieving records" });
        }
    }

    /// <summary>
    /// Get record by ID
    /// </summary>
    [HttpGet("records/{id:int}")]
    public async Task<ActionResult<MaintenanceRecordDto>> GetRecord(int id)
    {
        try
        {
            var record = await _maintenanceService.GetRecordByIdAsync(id);
            if (record == null)
                return NotFound(new { message = $"Record with ID {id} not found" });

            return Ok(MapRecordToDto(record));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting record {Id}", id);
            return StatusCode(500, new { message = "Error retrieving record" });
        }
    }

    /// <summary>
    /// Create maintenance record
    /// </summary>
    [HttpPost("records")]
    public async Task<ActionResult<MaintenanceRecordDto>> CreateRecord([FromBody] CreateRecordRequest request)
    {
        try
        {
            var record = new MaintenanceRecord
            {
                EquipmentId = request.EquipmentId,
                ScheduleId = request.ScheduleId,
                Title = request.Title,
                Description = request.Description,
                MaintenanceType = request.MaintenanceType,
                ScheduledDate = request.ScheduledDate
            };

            var created = await _maintenanceService.CreateRecordAsync(record);

            return CreatedAtAction(nameof(GetRecord), new { id = created.Id }, MapRecordToDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating record");
            return StatusCode(500, new { message = "Error creating record" });
        }
    }

    /// <summary>
    /// Start maintenance
    /// </summary>
    [HttpPost("records/{id:int}/start")]
    public async Task<ActionResult<MaintenanceRecordDto>> StartMaintenance(int id, [FromQuery] string? performedBy = null)
    {
        try
        {
            var record = await _maintenanceService.StartMaintenanceAsync(id, performedBy);
            return Ok(MapRecordToDto(record));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting maintenance {Id}", id);
            return StatusCode(500, new { message = "Error starting maintenance" });
        }
    }

    /// <summary>
    /// Complete maintenance
    /// </summary>
    [HttpPost("records/{id:int}/complete")]
    public async Task<ActionResult<MaintenanceRecordDto>> CompleteMaintenance(int id, [FromBody] CompleteMaintenanceRequest request)
    {
        try
        {
            var record = await _maintenanceService.CompleteMaintenanceAsync(
                id,
                request.PerformedBy,
                request.ActualHours,
                request.LaborCost,
                request.PartsCost,
                request.Notes,
                request.PartsUsed,
                request.CompletedChecklist);

            return Ok(MapRecordToDto(record));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing maintenance {Id}", id);
            return StatusCode(500, new { message = "Error completing maintenance" });
        }
    }

    /// <summary>
    /// Cancel maintenance
    /// </summary>
    [HttpPost("records/{id:int}/cancel")]
    public async Task<ActionResult<MaintenanceRecordDto>> CancelMaintenance(int id, [FromBody] string? reason = null)
    {
        try
        {
            var record = await _maintenanceService.CancelMaintenanceAsync(id, reason);
            return Ok(MapRecordToDto(record));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling maintenance {Id}", id);
            return StatusCode(500, new { message = "Error cancelling maintenance" });
        }
    }

    /// <summary>
    /// Delete maintenance record
    /// </summary>
    [HttpDelete("records/{id:int}")]
    public async Task<ActionResult> DeleteRecord(int id)
    {
        try
        {
            var result = await _maintenanceService.DeleteRecordAsync(id);
            if (!result)
                return NotFound(new { message = $"Record with ID {id} not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting record {Id}", id);
            return StatusCode(500, new { message = "Error deleting record" });
        }
    }

    /// <summary>
    /// Get overdue maintenance records
    /// </summary>
    [HttpGet("records/overdue")]
    public async Task<ActionResult<List<MaintenanceRecordDto>>> GetOverdueRecords([FromQuery] int companyId)
    {
        try
        {
            var records = await _maintenanceService.GetOverdueRecordsAsync(companyId);
            return Ok(records.Select(r => MapRecordToDto(r)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overdue records");
            return StatusCode(500, new { message = "Error retrieving records" });
        }
    }

    /// <summary>
    /// Get in-progress maintenance records
    /// </summary>
    [HttpGet("records/in-progress")]
    public async Task<ActionResult<List<MaintenanceRecordDto>>> GetInProgressRecords([FromQuery] int companyId)
    {
        try
        {
            var records = await _maintenanceService.GetInProgressRecordsAsync(companyId);
            return Ok(records.Select(r => MapRecordToDto(r)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting in-progress records");
            return StatusCode(500, new { message = "Error retrieving records" });
        }
    }

    /// <summary>
    /// Generate upcoming maintenance records from schedules
    /// </summary>
    [HttpPost("records/generate")]
    public async Task<ActionResult<List<MaintenanceRecordDto>>> GenerateUpcomingRecords(
        [FromQuery] int companyId,
        [FromQuery] int daysAhead = 30,
        [FromQuery] string? createdBy = null)
    {
        try
        {
            var records = await _maintenanceService.GenerateUpcomingRecordsAsync(companyId, daysAhead, createdBy);
            return Ok(records.Select(r => MapRecordToDto(r)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating upcoming records");
            return StatusCode(500, new { message = "Error generating records" });
        }
    }

    #endregion

    #region Dashboard / Analytics

    /// <summary>
    /// Get maintenance dashboard data
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<MaintenanceDashboardDto>> GetDashboard([FromQuery] int companyId)
    {
        try
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var activeSchedules = await _maintenanceService.GetActiveSchedulesAsync(companyId);
            var overdueSchedules = await _maintenanceService.GetOverdueSchedulesAsync(companyId);
            var upcomingSchedules = await _maintenanceService.GetDueSchedulesAsync(companyId, 7);
            var inProgressRecords = await _maintenanceService.GetInProgressRecordsAsync(companyId);
            var completedThisMonth = await _maintenanceService.GetCompletedCountAsync(companyId, monthStart, now);
            var totalCostThisMonth = await _maintenanceService.GetTotalMaintenanceCostAsync(companyId, monthStart, now);
            var avgCost = await _maintenanceService.GetAverageMaintenanceCostAsync(companyId);
            var countsByType = await _maintenanceService.GetMaintenanceCountsByTypeAsync(companyId);
            var recentRecords = await _maintenanceService.GetRecentRecordsAsync(companyId, 5);
            var overdueRecords = await _maintenanceService.GetOverdueRecordsAsync(companyId);

            return Ok(new MaintenanceDashboardDto
            {
                TotalSchedules = activeSchedules.Count(),
                ActiveSchedules = activeSchedules.Count(),
                UpcomingMaintenanceCount = upcomingSchedules.Count(),
                OverdueMaintenanceCount = overdueSchedules.Count() + overdueRecords.Count(),
                InProgressCount = inProgressRecords.Count(),
                CompletedThisMonth = completedThisMonth,
                TotalCostThisMonth = totalCostThisMonth,
                AverageCostPerMaintenance = avgCost,
                MaintenanceByType = countsByType,
                UpcomingMaintenance = recentRecords.Take(5).Select(r => MapRecordToDto(r)).ToList(),
                OverdueMaintenance = overdueRecords.Select(r => MapRecordToDto(r)).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting maintenance dashboard");
            return StatusCode(500, new { message = "Error retrieving dashboard" });
        }
    }

    /// <summary>
    /// Get maintenance cost statistics
    /// </summary>
    [HttpGet("analytics/costs")]
    public async Task<ActionResult<object>> GetCostAnalytics(
        [FromQuery] int companyId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var totalCost = await _maintenanceService.GetTotalMaintenanceCostAsync(companyId, fromDate, toDate);
            var avgCost = await _maintenanceService.GetAverageMaintenanceCostAsync(companyId);
            var costsByEquipment = await _maintenanceService.GetMaintenanceCostsByEquipmentAsync(companyId, fromDate, toDate);

            return Ok(new
            {
                totalCost,
                averageCost = avgCost,
                costsByEquipment
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cost analytics");
            return StatusCode(500, new { message = "Error retrieving analytics" });
        }
    }

    /// <summary>
    /// Get recent maintenance records
    /// </summary>
    [HttpGet("records/recent")]
    public async Task<ActionResult<List<MaintenanceRecordDto>>> GetRecentRecords(
        [FromQuery] int companyId,
        [FromQuery] int count = 10)
    {
        try
        {
            var records = await _maintenanceService.GetRecentRecordsAsync(companyId, count);
            return Ok(records.Select(r => MapRecordToDto(r)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent records");
            return StatusCode(500, new { message = "Error retrieving records" });
        }
    }

    #endregion

    #region Helper Methods

    private static MaintenanceScheduleDto MapScheduleToDto(MaintenanceSchedule s)
    {
        return new MaintenanceScheduleDto
        {
            Id = s.Id,
            EquipmentId = s.EquipmentId,
            EquipmentCode = s.Equipment?.EquipmentCode,
            EquipmentName = s.Equipment?.Name,
            Title = s.Title,
            Description = s.Description,
            Frequency = s.Frequency,
            CustomIntervalDays = s.CustomIntervalDays,
            LastPerformed = s.LastPerformed,
            NextDue = s.NextDue,
            ReminderDaysBefore = s.ReminderDaysBefore,
            Status = s.Status,
            EstimatedHours = s.EstimatedHours,
            EstimatedCost = s.EstimatedCost,
            AssignedTo = s.AssignedTo,
            ChecklistItems = s.ChecklistItems,
            RequiredParts = s.RequiredParts,
            CreatedDate = s.CreatedDate,
            CreatedBy = s.CreatedBy,
            LastModified = s.LastModified
        };
    }

    private static MaintenanceRecordDto MapRecordToDto(MaintenanceRecord r)
    {
        return new MaintenanceRecordDto
        {
            Id = r.Id,
            EquipmentId = r.EquipmentId,
            EquipmentCode = r.Equipment?.EquipmentCode,
            EquipmentName = r.Equipment?.Name,
            ScheduleId = r.ScheduleId,
            ScheduleTitle = r.Schedule?.Title,
            Title = r.Title,
            Description = r.Description,
            MaintenanceType = r.MaintenanceType,
            ScheduledDate = r.ScheduledDate,
            StartedDate = r.StartedDate,
            CompletedDate = r.CompletedDate,
            Status = r.Status,
            PerformedBy = r.PerformedBy,
            ActualHours = r.ActualHours,
            LaborCost = r.LaborCost,
            PartsCost = r.PartsCost,
            TotalCost = r.TotalCost,
            CompletedChecklist = r.CompletedChecklist,
            PartsUsed = r.PartsUsed,
            Notes = r.Notes,
            Attachments = r.Attachments,
            CreatedDate = r.CreatedDate,
            CreatedBy = r.CreatedBy,
            LastModified = r.LastModified
        };
    }

    #endregion
}
