using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services;

namespace FabOS.WebServer.Controllers.Api;

/// <summary>
/// WorkPackages API - Manage work packages for FabMate module
/// WorkPackages sit between Orders and WorkOrders: Order → WorkPackage → WorkOrder
/// </summary>
[ApiController]
[Route("api/workpackages")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class WorkPackagesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly NumberSeriesService _numberSeriesService;
    private readonly ILogger<WorkPackagesController> _logger;

    public WorkPackagesController(
        ApplicationDbContext context,
        NumberSeriesService numberSeriesService,
        ILogger<WorkPackagesController> logger)
    {
        _context = context;
        _numberSeriesService = numberSeriesService;
        _logger = logger;
    }

    /// <summary>
    /// Get all work packages with filtering and pagination
    /// GET /api/workpackages?companyId=1&status=InProgress&pageNumber=1&pageSize=50
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> GetWorkPackages(
        [FromQuery] int companyId,
        [FromQuery] int? orderId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? packageType = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            if (companyId <= 0)
            {
                return BadRequest(new { success = false, message = "Valid CompanyId is required" });
            }

            // Base query with multi-tenant filtering
            var query = _context.WorkPackages
                .Include(wp => wp.Order)
                    .ThenInclude(o => o.Customer)
                .Include(wp => wp.WorkOrders)
                .Where(wp => wp.CompanyId == companyId && !wp.IsDeleted);

            // Apply filters
            if (orderId.HasValue)
            {
                query = query.Where(wp => wp.OrderId == orderId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(wp => wp.Status == status);
            }

            if (!string.IsNullOrEmpty(priority))
            {
                query = query.Where(wp => wp.Priority == priority);
            }

            if (!string.IsNullOrEmpty(packageType))
            {
                query = query.Where(wp => wp.PackageType == packageType);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(wp => wp.PlannedStartDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(wp => wp.PlannedEndDate <= toDate.Value);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(wp =>
                    wp.PackageNumber.Contains(searchTerm) ||
                    wp.PackageName.Contains(searchTerm) ||
                    (wp.Description != null && wp.Description.Contains(searchTerm)) ||
                    wp.Order.OrderNumber.Contains(searchTerm) ||
                    wp.Order.Customer.Name.Contains(searchTerm));
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var workPackages = await query
                .OrderByDescending(wp => wp.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(wp => new
                {
                    id = wp.Id,
                    packageNumber = wp.PackageNumber,
                    packageName = wp.PackageName,
                    orderId = wp.OrderId,
                    orderNumber = wp.Order.OrderNumber,
                    customerName = wp.Order.Customer.Name,
                    description = wp.Description,
                    priority = wp.Priority,
                    packageType = wp.PackageType,
                    status = wp.Status,
                    percentComplete = wp.PercentComplete,
                    plannedStartDate = wp.PlannedStartDate,
                    plannedEndDate = wp.PlannedEndDate,
                    actualStartDate = wp.ActualStartDate,
                    actualEndDate = wp.ActualEndDate,
                    estimatedHours = wp.EstimatedHours,
                    actualHours = wp.ActualHours,
                    estimatedCost = wp.EstimatedCost,
                    actualCost = wp.ActualCost,
                    billableValue = wp.BillableValue,
                    requiresITP = wp.RequiresITP,
                    itpNumber = wp.ITPNumber,
                    totalWorkOrders = wp.WorkOrders.Count,
                    completedWorkOrders = wp.WorkOrders.Count(wo => wo.Status == "Complete"),
                    createdDate = wp.CreatedDate,
                    lastModified = wp.LastModified
                })
                .ToListAsync();

            var response = new
            {
                items = workPackages,
                totalCount,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                hasPreviousPage = pageNumber > 1,
                hasNextPage = pageNumber < (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(new { success = true, data = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving work packages for company {CompanyId}", companyId);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving work packages" });
        }
    }

    /// <summary>
    /// Get work package by ID with full details
    /// GET /api/workpackages/123?companyId=1
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> GetWorkPackageById(int id, [FromQuery] int companyId)
    {
        try
        {
            var workPackage = await _context.WorkPackages
                .Include(wp => wp.Order)
                    .ThenInclude(o => o.Customer)
                .Include(wp => wp.WorkOrders)
                    .ThenInclude(wo => wo.Routing)
                        .ThenInclude(r => r!.RoutingLines)
                .Include(wp => wp.WorkOrders)
                    .ThenInclude(wo => wo.PrimaryResource)
                .Include(wp => wp.WorkOrders)
                    .ThenInclude(wo => wo.WorkCenter)
                .Where(wp => wp.Id == id && wp.CompanyId == companyId && !wp.IsDeleted)
                .FirstOrDefaultAsync();

            if (workPackage == null)
            {
                return NotFound(new { success = false, message = "Work package not found" });
            }

            var result = new
            {
                id = workPackage.Id,
                packageNumber = workPackage.PackageNumber,
                packageName = workPackage.PackageName,
                orderId = workPackage.OrderId,
                orderNumber = workPackage.Order.OrderNumber,
                customerName = workPackage.Order.Customer.Name,
                description = workPackage.Description,
                priority = workPackage.Priority,
                packageType = workPackage.PackageType,
                status = workPackage.Status,
                percentComplete = workPackage.PercentComplete,
                plannedStartDate = workPackage.PlannedStartDate,
                plannedEndDate = workPackage.PlannedEndDate,
                actualStartDate = workPackage.ActualStartDate,
                actualEndDate = workPackage.ActualEndDate,
                estimatedHours = workPackage.EstimatedHours,
                actualHours = workPackage.ActualHours,
                estimatedCost = workPackage.EstimatedCost,
                actualCost = workPackage.ActualCost,
                billableValue = workPackage.BillableValue,
                laborRatePerHour = workPackage.LaborRatePerHour,
                requiresITP = workPackage.RequiresITP,
                itpNumber = workPackage.ITPNumber,
                createdDate = workPackage.CreatedDate,
                lastModified = workPackage.LastModified,
                workOrders = workPackage.WorkOrders.Select(wo => new
                {
                    id = wo.Id,
                    workOrderNumber = wo.WorkOrderNumber,
                    status = wo.Status,
                    priority = wo.Priority,
                    workOrderType = wo.WorkOrderType,
                    description = wo.Description,
                    scheduledStartDate = wo.ScheduledStartDate,
                    scheduledEndDate = wo.ScheduledEndDate,
                    actualStartDate = wo.ActualStartDate,
                    actualEndDate = wo.ActualEndDate,
                    estimatedHours = wo.EstimatedHours,
                    actualHours = wo.ActualHours,
                    primaryResourceName = wo.PrimaryResource != null ? $"{wo.PrimaryResource.FirstName} {wo.PrimaryResource.LastName}" : null,
                    workCenterName = wo.WorkCenter?.WorkCenterName,
                    totalOperations = wo.Routing != null ? wo.Routing.RoutingLines.Count : 0,
                    completedOperations = wo.Routing != null ? wo.Routing.RoutingLines.Count(rl => rl.Status == "Finished") : 0,
                    percentComplete = wo.PercentComplete
                }).ToList(),
                summary = new
                {
                    totalWorkOrders = workPackage.WorkOrders.Count,
                    completedWorkOrders = workPackage.WorkOrders.Count(wo => wo.Status == "Complete"),
                    inProgressWorkOrders = workPackage.WorkOrders.Count(wo => wo.Status == "InProgress"),
                    totalOperations = workPackage.WorkOrders.Sum(wo => wo.Routing != null ? wo.Routing.RoutingLines.Count : 0),
                    completedOperations = workPackage.WorkOrders.Sum(wo => wo.Routing != null ? wo.Routing.RoutingLines.Count(rl => rl.Status == "Finished") : 0)
                }
            };

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving work package {WorkPackageId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving the work package" });
        }
    }

    /// <summary>
    /// Create a new work package
    /// POST /api/workpackages
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> CreateWorkPackage([FromBody] CreateWorkPackageRequest request)
    {
        try
        {
            // Validate order exists and belongs to company
            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.CompanyId == request.CompanyId);

            if (order == null)
            {
                return BadRequest(new { success = false, message = "Order not found or does not belong to this company" });
            }

            // Validate priority (Low, Normal, High, Urgent)
            if (!new[] { "Low", "Normal", "High", "Urgent" }.Contains(request.Priority))
            {
                return BadRequest(new { success = false, message = "Priority must be Low, Normal, High, or Urgent" });
            }

            // Validate package type if provided
            if (!string.IsNullOrEmpty(request.PackageType))
            {
                if (!new[] { "PartsProcessing", "AssemblyBuilding", "Mixed", "Finishing" }.Contains(request.PackageType))
                {
                    return BadRequest(new { success = false, message = "PackageType must be PartsProcessing, AssemblyBuilding, Mixed, or Finishing" });
                }
            }

            // Generate package number
            var packageNumber = await _numberSeriesService.GetNextNumberAsync("WorkPackage", request.CompanyId);

            // Create work package
            var workPackage = new WorkPackage
            {
                PackageNumber = packageNumber,
                PackageName = request.PackageName,
                OrderId = request.OrderId,
                CompanyId = request.CompanyId,
                Description = request.Description,
                Priority = request.Priority,
                PackageType = request.PackageType,
                PlannedStartDate = request.PlannedStartDate,
                PlannedEndDate = request.PlannedEndDate,
                EstimatedHours = request.EstimatedHours,
                EstimatedCost = request.EstimatedCost,
                BillableValue = request.BillableValue,
                LaborRatePerHour = request.LaborRatePerHour,
                RequiresITP = request.RequiresITP,
                ITPNumber = request.ITPNumber,
                Status = "Planning", // Initial status
                PercentComplete = 0,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.WorkPackages.Add(workPackage);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Work package {PackageNumber} created successfully for order {OrderNumber}", packageNumber, order.OrderNumber);

            return CreatedAtAction(
                nameof(GetWorkPackageById),
                new { id = workPackage.Id, companyId = request.CompanyId },
                new
                {
                    success = true,
                    message = "Work package created successfully",
                    data = new
                    {
                        id = workPackage.Id,
                        packageNumber = workPackage.PackageNumber,
                        packageName = workPackage.PackageName,
                        orderId = workPackage.OrderId,
                        orderNumber = order.OrderNumber,
                        customerName = order.Customer.Name,
                        status = workPackage.Status,
                        priority = workPackage.Priority,
                        createdDate = workPackage.CreatedDate
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating work package");
            return StatusCode(500, new { success = false, message = "An error occurred while creating the work package" });
        }
    }

    /// <summary>
    /// Update an existing work package
    /// PUT /api/workpackages/123
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> UpdateWorkPackage(int id, [FromBody] UpdateWorkPackageRequest request)
    {
        try
        {
            if (request.WorkPackageId != id)
            {
                return BadRequest(new { success = false, message = "Work package ID mismatch" });
            }

            var workPackage = await _context.WorkPackages
                .FirstOrDefaultAsync(wp => wp.Id == id && wp.CompanyId == request.CompanyId && !wp.IsDeleted);

            if (workPackage == null)
            {
                return NotFound(new { success = false, message = "Work package not found" });
            }

            // Update fields if provided
            if (!string.IsNullOrEmpty(request.PackageName))
                workPackage.PackageName = request.PackageName;

            if (!string.IsNullOrEmpty(request.Description))
                workPackage.Description = request.Description;

            if (!string.IsNullOrEmpty(request.Priority))
            {
                if (!new[] { "Low", "Normal", "High", "Urgent" }.Contains(request.Priority))
                {
                    return BadRequest(new { success = false, message = "Priority must be Low, Normal, High, or Urgent" });
                }
                workPackage.Priority = request.Priority;
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                if (!new[] { "Planning", "Ready", "InProgress", "OnHold", "Complete", "Cancelled" }.Contains(request.Status))
                {
                    return BadRequest(new { success = false, message = "Invalid status" });
                }
                workPackage.Status = request.Status;

                // Set actual dates based on status
                if (request.Status == "InProgress" && workPackage.ActualStartDate == null)
                {
                    workPackage.ActualStartDate = DateTime.UtcNow;
                }

                if (request.Status == "Complete")
                {
                    workPackage.ActualEndDate = DateTime.UtcNow;
                    workPackage.PercentComplete = 100;
                }
            }

            if (request.PlannedStartDate.HasValue)
                workPackage.PlannedStartDate = request.PlannedStartDate;

            if (request.PlannedEndDate.HasValue)
                workPackage.PlannedEndDate = request.PlannedEndDate;

            if (request.EstimatedHours.HasValue)
                workPackage.EstimatedHours = request.EstimatedHours.Value;

            if (request.EstimatedCost.HasValue)
                workPackage.EstimatedCost = request.EstimatedCost.Value;

            if (request.BillableValue.HasValue)
                workPackage.BillableValue = request.BillableValue.Value;

            if (request.LaborRatePerHour.HasValue)
                workPackage.LaborRatePerHour = request.LaborRatePerHour.Value;

            if (request.PercentComplete.HasValue)
                workPackage.PercentComplete = request.PercentComplete.Value;

            workPackage.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Work package {PackageNumber} updated successfully", workPackage.PackageNumber);

            return Ok(new
            {
                success = true,
                message = "Work package updated successfully",
                data = new
                {
                    id = workPackage.Id,
                    packageNumber = workPackage.PackageNumber,
                    packageName = workPackage.PackageName,
                    status = workPackage.Status,
                    percentComplete = workPackage.PercentComplete,
                    lastModified = workPackage.LastModified
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating work package {WorkPackageId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while updating the work package" });
        }
    }

    /// <summary>
    /// Delete a work package (soft delete)
    /// DELETE /api/workpackages/123?companyId=1
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteWorkPackage(int id, [FromQuery] int companyId)
    {
        try
        {
            var workPackage = await _context.WorkPackages
                .Include(wp => wp.WorkOrders)
                .FirstOrDefaultAsync(wp => wp.Id == id && wp.CompanyId == companyId && !wp.IsDeleted);

            if (workPackage == null)
            {
                return NotFound(new { success = false, message = "Work package not found" });
            }

            // Check if work package has work orders in progress
            var hasActiveWork = workPackage.WorkOrders.Any(wo => wo.Status == "InProgress");

            if (hasActiveWork)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Cannot delete work package with work orders in progress. Complete or cancel work orders first."
                });
            }

            // Soft delete
            workPackage.IsDeleted = true;
            workPackage.LastModified = DateTime.UtcNow;

            // Cancel all work orders
            foreach (var workOrder in workPackage.WorkOrders)
            {
                workOrder.Status = "Cancelled";
                workOrder.LastModified = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Work package {PackageNumber} deleted successfully", workPackage.PackageNumber);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting work package {WorkPackageId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while deleting the work package" });
        }
    }
}

// ==========================================
// REQUEST DTOs
// ==========================================

public record CreateWorkPackageRequest(
    int OrderId,
    int CompanyId,
    string PackageName,
    string? Description,
    string Priority,              // Low, Normal, High, Urgent
    string? PackageType,          // PartsProcessing, AssemblyBuilding, Mixed, Finishing
    DateTime? PlannedStartDate,
    DateTime? PlannedEndDate,
    decimal EstimatedHours,
    decimal EstimatedCost,
    decimal BillableValue,
    decimal LaborRatePerHour,
    bool RequiresITP,
    string? ITPNumber
);

public record UpdateWorkPackageRequest(
    int WorkPackageId,
    int CompanyId,
    string? PackageName,
    string? Description,
    string? Priority,
    string? Status,              // Planning, Ready, InProgress, OnHold, Complete, Cancelled
    DateTime? PlannedStartDate,
    DateTime? PlannedEndDate,
    decimal? EstimatedHours,
    decimal? EstimatedCost,
    decimal? BillableValue,
    decimal? LaborRatePerHour,
    decimal? PercentComplete
);
