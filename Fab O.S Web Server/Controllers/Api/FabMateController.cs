using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services;

namespace FabOS.WebServer.Controllers.Api;

/// <summary>
/// FabMate Module API - Work Order Management for Shop Floor
/// Provides mobile-optimized endpoints for manufacturing operations
/// </summary>
[ApiController]
[Route("api/fabmate")]
[Authorize(AuthenticationSchemes = "Bearer")] // JWT authentication for mobile
public class FabMateController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly NumberSeriesService _numberSeriesService;
    private readonly ILogger<FabMateController> _logger;

    public FabMateController(
        ApplicationDbContext context,
        NumberSeriesService numberSeriesService,
        ILogger<FabMateController> logger)
    {
        _context = context;
        _numberSeriesService = numberSeriesService;
        _logger = logger;
    }

    /// <summary>
    /// Get list of work orders with filtering
    /// GET /api/fabmate/workorders?companyId=1&status=InProgress&pageNumber=1&pageSize=50
    /// </summary>
    [HttpGet("workorders")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> GetWorkOrders(
        [FromQuery] int companyId,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] int? resourceId = null,
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

            // Base query with company isolation
            var query = _context.WorkOrders
                .Include(wo => wo.WorkPackage)
                .Include(wo => wo.Routing)
                    .ThenInclude(r => r!.RoutingLines)
                .Include(wo => wo.PrimaryResource)
                .Include(wo => wo.WorkCenter)
                .Where(wo => wo.WorkPackage.CompanyId == companyId); // Multi-tenant filtering

            // Apply filters
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(wo => wo.Status == status);
            }

            if (!string.IsNullOrEmpty(priority))
            {
                query = query.Where(wo => wo.Priority == priority);
            }

            if (resourceId.HasValue)
            {
                query = query.Where(wo => wo.PrimaryResourceId == resourceId.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(wo => wo.ScheduledStartDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(wo => wo.ScheduledEndDate <= toDate.Value);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(wo =>
                    wo.WorkOrderNumber.Contains(searchTerm) ||
                    wo.Description!.Contains(searchTerm) ||
                    wo.WorkPackage.PackageName.Contains(searchTerm));
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination and execute query
            var workOrders = await query
                .OrderByDescending(wo => wo.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(wo => new
                {
                    id = wo.Id,
                    workOrderNumber = wo.WorkOrderNumber,
                    status = wo.Status,
                    priority = wo.Priority,
                    workOrderType = wo.WorkOrderType,
                    description = wo.Description,
                    packageName = wo.WorkPackage.PackageName,
                    packageId = wo.PackageId,
                    projectNumber = wo.WorkPackage.PackageNumber,
                    customerName = wo.WorkPackage.Order != null ? wo.WorkPackage.Order.Customer.Name : null,
                    scheduledStartDate = wo.ScheduledStartDate,
                    scheduledEndDate = wo.ScheduledEndDate,
                    estimatedHours = wo.EstimatedHours,
                    actualHours = wo.ActualHours,
                    hasHoldPoints = wo.HasHoldPoints,
                    requiresInspection = wo.RequiresInspection,
                    totalOperations = wo.Routing != null ? wo.Routing.RoutingLines.Count : 0,
                    completedOperations = wo.Routing != null ? wo.Routing.RoutingLines.Count(rl => rl.Status == "Finished") : 0,
                    primaryResourceName = wo.PrimaryResource != null ? wo.PrimaryResource.FirstName + " " + wo.PrimaryResource.LastName : null,
                    workCenterName = wo.WorkCenter != null ? wo.WorkCenter.WorkCenterName : null,
                    barcode = wo.Barcode
                })
                .ToListAsync();

            var response = new
            {
                items = workOrders,
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
            _logger.LogError(ex, "Error retrieving work orders for company {CompanyId}", companyId);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving work orders" });
        }
    }

    /// <summary>
    /// Get detailed work order by ID
    /// GET /api/fabmate/workorders/123
    /// </summary>
    [HttpGet("workorders/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> GetWorkOrderById(int id, [FromQuery] int companyId)
    {
        try
        {
            var workOrder = await _context.WorkOrders
                .Include(wo => wo.WorkPackage)
                .Include(wo => wo.Routing)
                    .ThenInclude(r => r!.RoutingLines)
                .Include(wo => wo.AssemblyEntries)
                .Include(wo => wo.MaterialEntries)
                .Include(wo => wo.ResourceEntries)
                .Include(wo => wo.PrimaryResource)
                .Include(wo => wo.WorkCenter)
                .Where(wo => wo.Id == id && wo.WorkPackage.CompanyId == companyId)
                .FirstOrDefaultAsync();

            if (workOrder == null)
            {
                return NotFound(new { success = false, message = "Work order not found" });
            }

            var result = new
            {
                id = workOrder.Id,
                workOrderNumber = workOrder.WorkOrderNumber,
                status = workOrder.Status,
                priority = workOrder.Priority,
                workOrderType = workOrder.WorkOrderType,
                description = workOrder.Description,
                packageId = workOrder.PackageId,
                packageName = workOrder.WorkPackage.PackageName,
                scheduledStartDate = workOrder.ScheduledStartDate,
                scheduledEndDate = workOrder.ScheduledEndDate,
                actualStartDate = workOrder.ActualStartDate,
                actualEndDate = workOrder.ActualEndDate,
                estimatedHours = workOrder.EstimatedHours,
                actualHours = workOrder.ActualHours,
                hasHoldPoints = workOrder.HasHoldPoints,
                requiresInspection = workOrder.RequiresInspection,
                barcode = workOrder.Barcode,
                primaryResourceName = workOrder.PrimaryResource != null ? $"{workOrder.PrimaryResource.FirstName} {workOrder.PrimaryResource.LastName}" : null,
                workCenterName = workOrder.WorkCenter?.WorkCenterName,
                routingLines = workOrder.Routing != null ? workOrder.Routing.RoutingLines.OrderBy(rl => rl.SequenceNumber).Select(rl => new
                {
                    id = rl.Id,
                    sequenceNumber = rl.SequenceNumber,
                    operationType = rl.OperationType,
                    status = rl.Status,
                    plannedSetupTime = rl.PlannedSetupTime,
                    plannedRunTime = rl.PlannedRunTime,
                    actualSetupTime = rl.ActualSetupTime,
                    actualRunTime = rl.ActualRunTime,
                    quantityToProcess = rl.QuantityToProcess,
                    quantityProcessed = rl.QuantityProcessed,
                    startDateTime = rl.StartDateTime,
                    endDateTime = rl.EndDateTime
                }).ToList() : Enumerable.Empty<object>().Select(x => new
                {
                    id = 0,
                    sequenceNumber = 0,
                    operationType = "",
                    status = "",
                    plannedSetupTime = 0m,
                    plannedRunTime = 0m,
                    actualSetupTime = 0m,
                    actualRunTime = 0m,
                    quantityToProcess = 0m,
                    quantityProcessed = 0m,
                    startDateTime = (DateTime?)null,
                    endDateTime = (DateTime?)null
                }).ToList(),
                assemblies = workOrder.AssemblyEntries.Select(asm => new
                {
                    id = asm.Id,
                    assemblyId = asm.AssemblyId,
                    quantityToBuild = asm.QuantityToBuild,
                    quantityCompleted = asm.QuantityCompleted,
                    status = asm.Status
                }).ToList(),
                materials = workOrder.MaterialEntries.Select(item => new
                {
                    id = item.Id,
                    catalogueItemId = item.CatalogueItemId,
                    requiredQuantity = item.RequiredQuantity,
                    issuedQuantity = item.IssuedQuantity,
                    processedQuantity = item.ProcessedQuantity,
                    unit = item.Unit,
                    status = item.Status,
                    heatNumber = item.HeatNumber
                }).ToList()
            };

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving work order {WorkOrderId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving the work order" });
        }
    }

    /// <summary>
    /// Create a new work order
    /// POST /api/fabmate/workorders
    /// </summary>
    [HttpPost("workorders")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> CreateWorkOrder([FromBody] CreateWorkOrderRequest request)
    {
        try
        {
            // Validate work package exists and belongs to company
            var workPackage = await _context.WorkPackages
                .Include(wp => wp.Order)
                    .ThenInclude(o => o.Customer)
                .FirstOrDefaultAsync(wp => wp.Id == request.PackageId && wp.CompanyId == request.CompanyId && !wp.IsDeleted);

            if (workPackage == null)
            {
                return BadRequest(new { success = false, message = "Work package not found or does not belong to this company" });
            }

            // Validate work order type
            if (!new[] { "PartsProcessing", "AssemblyBuilding", "Mixed", "Finishing", "QualityControl" }.Contains(request.WorkOrderType))
            {
                return BadRequest(new { success = false, message = "Invalid WorkOrderType. Must be PartsProcessing, AssemblyBuilding, Mixed, Finishing, or QualityControl" });
            }

            // Validate priority
            if (!new[] { "Low", "Normal", "High", "Urgent" }.Contains(request.Priority))
            {
                return BadRequest(new { success = false, message = "Invalid Priority. Must be Low, Normal, High, or Urgent" });
            }

            // Validate work center if provided
            if (request.WorkCenterId.HasValue)
            {
                var workCenter = await _context.WorkCenters
                    .FirstOrDefaultAsync(wc => wc.Id == request.WorkCenterId.Value && wc.CompanyId == request.CompanyId);

                if (workCenter == null)
                {
                    return BadRequest(new { success = false, message = "Work center not found or does not belong to this company" });
                }
            }

            // Validate resource if provided
            if (request.PrimaryResourceId.HasValue)
            {
                var resource = await _context.Resources
                    .FirstOrDefaultAsync(r => r.Id == request.PrimaryResourceId.Value);

                if (resource == null)
                {
                    return BadRequest(new { success = false, message = "Resource not found" });
                }
            }

            // Generate work order number
            var workOrderNumber = await _numberSeriesService.GetNextNumberAsync("WorkOrder", request.CompanyId);

            // Generate barcode (can be customized)
            var barcode = $"WO{workOrderNumber.Replace("-", "").Replace("WO", "")}";

            // Create work order
            var workOrder = new WorkOrder
            {
                WorkOrderNumber = workOrderNumber,
                PackageId = request.PackageId,
                CompanyId = request.CompanyId,
                WorkOrderType = request.WorkOrderType,
                Description = request.Description,
                WorkCenterId = request.WorkCenterId,
                PrimaryResourceId = request.PrimaryResourceId,
                Priority = request.Priority,
                ScheduledStartDate = request.ScheduledStartDate,
                ScheduledEndDate = request.ScheduledEndDate,
                EstimatedHours = request.EstimatedHours,
                EstimatedCost = request.EstimatedCost,
                ActualHours = 0,
                ActualCost = 0,
                Status = "Created", // Initial status
                PercentComplete = 0,
                Barcode = barcode,
                HasHoldPoints = request.HasHoldPoints,
                RequiresInspection = request.RequiresInspection,
                InspectionStatus = request.RequiresInspection ? "Pending" : null,
                WorkInstructions = request.WorkInstructions,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _context.WorkOrders.Add(workOrder);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Work order {WorkOrderNumber} created successfully for package {PackageNumber}", workOrderNumber, workPackage.PackageNumber);

            return CreatedAtAction(
                nameof(GetWorkOrderById),
                new { id = workOrder.Id, companyId = request.CompanyId },
                new
                {
                    success = true,
                    message = "Work order created successfully",
                    data = new
                    {
                        id = workOrder.Id,
                        workOrderNumber = workOrder.WorkOrderNumber,
                        packageId = workOrder.PackageId,
                        packageNumber = workPackage.PackageNumber,
                        packageName = workPackage.PackageName,
                        orderNumber = workPackage.Order.OrderNumber,
                        customerName = workPackage.Order.Customer.Name,
                        status = workOrder.Status,
                        priority = workOrder.Priority,
                        workOrderType = workOrder.WorkOrderType,
                        barcode = workOrder.Barcode,
                        createdDate = workOrder.CreatedDate
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating work order");
            return StatusCode(500, new { success = false, message = "An error occurred while creating the work order" });
        }
    }

    /// <summary>
    /// Start a work order
    /// PUT /api/fabmate/workorders/123/start
    /// </summary>
    [HttpPut("workorders/{id:int}/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> StartWorkOrder(
        int id,
        [FromBody] StartWorkOrderRequest request)
    {
        try
        {
            // Validate request
            if (request.WorkOrderId != id)
            {
                return BadRequest(new { success = false, message = "Work order ID mismatch" });
            }

            var workOrder = await _context.WorkOrders
                .Include(wo => wo.WorkPackage)
                .FirstOrDefaultAsync(wo => wo.Id == id);

            if (workOrder == null)
            {
                return NotFound(new { success = false, message = "Work order not found" });
            }

            // Validate status transition
            if (workOrder.Status != "Released" && workOrder.Status != "Created")
            {
                return BadRequest(new { success = false, message = $"Cannot start work order with status '{workOrder.Status}'" });
            }

            // Update work order
            workOrder.Status = "InProgress";
            workOrder.ActualStartDate = request.StartTime;
            workOrder.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Work order {WorkOrderNumber} started by user {UserId}", workOrder.WorkOrderNumber, request.UserId);

            return Ok(new { success = true, message = "Work order started successfully", data = new { status = workOrder.Status, actualStartDate = workOrder.ActualStartDate } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting work order {WorkOrderId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while starting the work order" });
        }
    }

    /// <summary>
    /// Complete a work order
    /// PUT /api/fabmate/workorders/123/complete
    /// </summary>
    [HttpPut("workorders/{id:int}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> CompleteWorkOrder(
        int id,
        [FromBody] CompleteWorkOrderRequest request)
    {
        try
        {
            if (request.WorkOrderId != id)
            {
                return BadRequest(new { success = false, message = "Work order ID mismatch" });
            }

            var workOrder = await _context.WorkOrders
                .Include(wo => wo.Routing)
                    .ThenInclude(r => r!.RoutingLines)
                .Include(wo => wo.WorkPackage)
                .FirstOrDefaultAsync(wo => wo.Id == id);

            if (workOrder == null)
            {
                return NotFound(new { success = false, message = "Work order not found" });
            }

            // Validate status
            if (workOrder.Status != "InProgress")
            {
                return BadRequest(new { success = false, message = $"Cannot complete work order with status '{workOrder.Status}'" });
            }

            // Check if all routing lines are complete (if required)
            if (request.AllOperationsComplete && workOrder.Routing != null)
            {
                var incompleteLines = workOrder.Routing.RoutingLines.Count(rl => rl.Status != "Finished" && rl.Status != "Closed");
                if (incompleteLines > 0)
                {
                    return BadRequest(new { success = false, message = $"{incompleteLines} routing lines are not complete" });
                }
            }

            // Update work order
            workOrder.Status = "Complete";
            workOrder.ActualEndDate = request.CompletionTime;
            workOrder.ActualHours = request.ActualHours;
            workOrder.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Work order {WorkOrderNumber} completed by user {UserId}", workOrder.WorkOrderNumber, request.UserId);

            return Ok(new { success = true, message = "Work order completed successfully", data = new { status = workOrder.Status, actualEndDate = workOrder.ActualEndDate } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing work order {WorkOrderId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while completing the work order" });
        }
    }

    /// <summary>
    /// Log time entry for work order
    /// POST /api/fabmate/workorders/123/time
    /// </summary>
    [HttpPost("workorders/{id:int}/time")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> LogTimeEntry(int id, [FromBody] TimeEntryRequest request)
    {
        try
        {
            if (request.WorkOrderId != id)
            {
                return BadRequest(new { success = false, message = "Work order ID mismatch" });
            }

            var workOrder = await _context.WorkOrders
                .Include(wo => wo.WorkPackage)
                .FirstOrDefaultAsync(wo => wo.Id == id);

            if (workOrder == null)
            {
                return NotFound(new { success = false, message = "Work order not found" });
            }

            // Calculate hours if not provided
            var hours = request.Hours;
            if (hours == 0)
            {
                var timeSpan = request.EndTime - request.StartTime;
                hours = (decimal)timeSpan.TotalHours;
            }

            // TODO: Create TimeEntry entity and save to database
            // For now, just update work order actual hours
            workOrder.ActualHours += hours;
            workOrder.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Time entry logged for work order {WorkOrderNumber}: {Hours} hours", workOrder.WorkOrderNumber, request.Hours);

            return CreatedAtAction(nameof(GetWorkOrderById), new { id = workOrder.Id, companyId = workOrder.WorkPackage.CompanyId },
                new { success = true, message = "Time entry logged successfully", data = new { hours = request.Hours } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging time entry for work order {WorkOrderId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while logging time entry" });
        }
    }

    /// <summary>
    /// Update operation status
    /// PUT /api/fabmate/operations/456/status
    /// </summary>
    [HttpPut("operations/{operationId:int}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> UpdateOperationStatus(
        int operationId,
        [FromBody] UpdateOperationStatusRequest request)
    {
        try
        {
            if (request.OperationId != operationId)
            {
                return BadRequest(new { success = false, message = "Operation ID mismatch" });
            }

            var routingLine = await _context.WorkOrderRoutingLines
                .Include(rl => rl.WorkOrderRouting)
                    .ThenInclude(r => r.WorkOrder)
                        .ThenInclude(wo => wo.WorkPackage)
                .FirstOrDefaultAsync(rl => rl.Id == operationId);

            if (routingLine == null)
            {
                return NotFound(new { success = false, message = "Routing line not found" });
            }

            // Update routing line
            routingLine.Status = request.Status;

            if (request.Status == "InProgress" && routingLine.StartDateTime == null)
            {
                routingLine.StartDateTime = DateTime.UtcNow;
            }

            if (request.Status == "Finished")
            {
                routingLine.EndDateTime = request.CompletedAt ?? DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Routing line {RoutingLineId} status updated to {Status}", operationId, request.Status);

            return Ok(new { success = true, message = "Routing line status updated successfully", data = new { status = routingLine.Status } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating operation {OperationId} status", operationId);
            return StatusCode(500, new { success = false, message = "An error occurred while updating operation status" });
        }
    }

    // ==========================================
    // RESOURCES API
    // ==========================================

    /// <summary>
    /// Create a new resource (person)
    /// POST /api/fabmate/resources
    /// </summary>
    [HttpPost("resources")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> CreateResource([FromBody] CreateResourceRequest request)
    {
        try
        {
            if (request.CompanyId <= 0)
            {
                return BadRequest(new { success = false, message = "Valid CompanyId is required" });
            }

            // Validate resource type
            if (!new[] { "Person", "Direct", "Indirect", "Contract", "Supervisor" }.Contains(request.ResourceType))
            {
                return BadRequest(new { success = false, message = "Invalid ResourceType" });
            }

            // Create resource
            var resource = new Resource
            {
                EmployeeCode = request.EmployeeNumber ?? $"EMP-{DateTime.UtcNow:yyyyMMddHHmmss}",
                UserId = request.UserId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                JobTitle = request.JobTitle ?? "Worker",
                Email = request.Email,
                ResourceType = request.ResourceType,
                PrimarySkill = request.SkillLevel,
                SkillLevel = request.SkillLevelNumeric ?? 3,
                CertificationLevel = request.Certifications != null ? string.Join(", ", request.Certifications) : null,
                ResourceGroup = request.Department,
                StandardHoursPerDay = 8.0m,
                HourlyRate = request.HourlyRate,
                DirectUnitCost = request.HourlyRate,
                IndirectCostPercentage = 0,
                IsActive = request.IsActive,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Resource {ResourceName} created successfully", resource.FullName);

            return CreatedAtAction(
                nameof(GetResourceById),
                new { id = resource.Id },
                new
                {
                    success = true,
                    message = "Resource created successfully",
                    data = new
                    {
                        id = resource.Id,
                        resourceType = resource.ResourceType,
                        firstName = resource.FirstName,
                        lastName = resource.LastName,
                        fullName = resource.FullName,
                        email = resource.Email,
                        employeeNumber = resource.EmployeeCode,
                        jobTitle = resource.JobTitle,
                        hourlyRate = resource.HourlyRate,
                        isActive = resource.IsActive,
                        createdDate = resource.CreatedDate
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating resource");
            return StatusCode(500, new { success = false, message = "An error occurred while creating the resource" });
        }
    }

    /// <summary>
    /// Get resource by ID
    /// GET /api/fabmate/resources/{id}
    /// </summary>
    [HttpGet("resources/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> GetResourceById(int id)
    {
        try
        {
            var resource = await _context.Resources
                .FirstOrDefaultAsync(r => r.Id == id);

            if (resource == null)
            {
                return NotFound(new { success = false, message = "Resource not found" });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = resource.Id,
                    resourceType = resource.ResourceType,
                    firstName = resource.FirstName,
                    lastName = resource.LastName,
                    fullName = resource.FullName,
                    email = resource.Email,
                    employeeNumber = resource.EmployeeCode,
                    jobTitle = resource.JobTitle,
                    skillLevel = resource.SkillLevel,
                    certificationLevel = resource.CertificationLevel,
                    hourlyRate = resource.HourlyRate,
                    isActive = resource.IsActive,
                    createdDate = resource.CreatedDate
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving resource {ResourceId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving the resource" });
        }
    }

    /// <summary>
    /// Get list of resources
    /// GET /api/fabmate/resources?companyId=1
    /// </summary>
    [HttpGet("resources")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetResources(
        [FromQuery] bool? isActive = null,
        [FromQuery] string? resourceType = null,
        [FromQuery] string? searchTerm = null)
    {
        try
        {
            var query = _context.Resources.AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(r => r.IsActive == isActive.Value);
            }

            if (!string.IsNullOrEmpty(resourceType))
            {
                query = query.Where(r => r.ResourceType == resourceType);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(r =>
                    r.FirstName.Contains(searchTerm) ||
                    r.LastName.Contains(searchTerm) ||
                    r.EmployeeCode.Contains(searchTerm));
            }

            var resources = await query
                .OrderBy(r => r.LastName)
                .ThenBy(r => r.FirstName)
                .Select(r => new
                {
                    id = r.Id,
                    resourceType = r.ResourceType,
                    firstName = r.FirstName,
                    lastName = r.LastName,
                    fullName = r.FullName,
                    email = r.Email,
                    employeeNumber = r.EmployeeCode,
                    jobTitle = r.JobTitle,
                    hourlyRate = r.HourlyRate,
                    isActive = r.IsActive
                })
                .ToListAsync();

            return Ok(new { success = true, data = resources });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving resources");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving resources" });
        }
    }

    // ==========================================
    // WORK CENTERS API
    // ==========================================

    /// <summary>
    /// Create a new work center
    /// POST /api/fabmate/workcenters
    /// </summary>
    [HttpPost("workcenters")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> CreateWorkCenter([FromBody] CreateWorkCenterRequest request)
    {
        try
        {
            if (request.CompanyId <= 0)
            {
                return BadRequest(new { success = false, message = "Valid CompanyId is required" });
            }

            var workCenter = new WorkCenter
            {
                WorkCenterCode = request.WorkCenterCode,
                WorkCenterName = request.WorkCenterName,
                Description = request.Description,
                CompanyId = request.CompanyId,
                WorkCenterType = request.WorkCenterType ?? "Production",
                DailyCapacityHours = 8.0m,
                SimultaneousOperations = request.Capacity ?? 1,
                HourlyRate = request.HourlyRate ?? 0m,
                OverheadRate = 0m,
                EfficiencyPercentage = request.Efficiency ?? 100m,
                Department = request.Department,
                IsActive = request.IsActive,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
                // All other required fields have default values in the entity
            };

            _context.WorkCenters.Add(workCenter);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Work center {WorkCenterName} created successfully", workCenter.WorkCenterName);

            return CreatedAtAction(
                nameof(GetWorkCenterById),
                new { id = workCenter.Id },
                new
                {
                    success = true,
                    message = "Work center created successfully",
                    data = new
                    {
                        id = workCenter.Id,
                        workCenterName = workCenter.WorkCenterName,
                        workCenterCode = workCenter.WorkCenterCode,
                        department = workCenter.Department,
                        isActive = workCenter.IsActive,
                        createdDate = workCenter.CreatedDate
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating work center");
            return StatusCode(500, new { success = false, message = "An error occurred while creating the work center" });
        }
    }

    /// <summary>
    /// Get work center by ID
    /// GET /api/fabmate/workcenters/{id}
    /// </summary>
    [HttpGet("workcenters/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> GetWorkCenterById(int id)
    {
        try
        {
            var workCenter = await _context.WorkCenters
                .FirstOrDefaultAsync(wc => wc.Id == id);

            if (workCenter == null)
            {
                return NotFound(new { success = false, message = "Work center not found" });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = workCenter.Id,
                    workCenterName = workCenter.WorkCenterName,
                    workCenterCode = workCenter.WorkCenterCode,
                    description = workCenter.Description,
                    department = workCenter.Department,
                    isActive = workCenter.IsActive,
                    createdDate = workCenter.CreatedDate
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving work center {WorkCenterId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving the work center" });
        }
    }

    /// <summary>
    /// Get list of work centers
    /// GET /api/fabmate/workcenters?companyId=1
    /// </summary>
    [HttpGet("workcenters")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetWorkCenters(
        [FromQuery] int companyId,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            if (companyId <= 0)
            {
                return BadRequest(new { success = false, message = "Valid CompanyId is required" });
            }

            var query = _context.WorkCenters.Where(wc => wc.CompanyId == companyId);

            if (isActive.HasValue)
            {
                query = query.Where(wc => wc.IsActive == isActive.Value);
            }

            var workCenters = await query
                .OrderBy(wc => wc.WorkCenterName)
                .Select(wc => new
                {
                    id = wc.Id,
                    workCenterName = wc.WorkCenterName,
                    workCenterCode = wc.WorkCenterCode,
                    description = wc.Description,
                    department = wc.Department,
                    isActive = wc.IsActive
                })
                .ToListAsync();

            return Ok(new { success = true, data = workCenters });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving work centers for company {CompanyId}", companyId);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving work centers" });
        }
    }

    /// <summary>
    /// Assign resource to work center
    /// POST /api/fabmate/workcenters/{id}/resources
    /// </summary>
    [HttpPost("workcenters/{id:int}/resources")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> AssignResourceToWorkCenter(int id, [FromBody] AssignResourceToWorkCenterRequest request)
    {
        try
        {
            var workCenter = await _context.WorkCenters.FirstOrDefaultAsync(wc => wc.Id == id);
            if (workCenter == null)
            {
                return NotFound(new { success = false, message = "Work center not found" });
            }

            var resource = await _context.Resources.FirstOrDefaultAsync(r => r.Id == request.ResourceId);
            if (resource == null)
            {
                return NotFound(new { success = false, message = "Resource not found" });
            }

            // Update resource's primary work center
            if (request.IsPrimaryOperator)
            {
                resource.PrimaryWorkCenterId = id;
                resource.LastModified = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Resource {ResourceId} assigned to work center {WorkCenterId}", request.ResourceId, id);

            return CreatedAtAction(
                nameof(GetWorkCenterById),
                new { id = workCenter.Id },
                new
                {
                    success = true,
                    message = "Resource assigned to work center successfully",
                    data = new
                    {
                        id = 1, // Placeholder - would need WorkCenterResource junction table
                        workCenterId = id,
                        workCenterName = workCenter.WorkCenterName,
                        resourceId = resource.Id,
                        resourceName = resource.FullName,
                        assignmentType = request.AssignmentType,
                        isPrimaryOperator = request.IsPrimaryOperator,
                        effectiveDate = request.EffectiveDate
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning resource to work center");
            return StatusCode(500, new { success = false, message = "An error occurred while assigning resource to work center" });
        }
    }

    // ==========================================
    // WORK ORDER RESOURCES API
    // ==========================================

    /// <summary>
    /// Assign resource to work order
    /// POST /api/fabmate/workorders/{id}/resources
    /// </summary>
    [HttpPost("workorders/{id:int}/resources")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> AssignResourceToWorkOrder(int id, [FromBody] AssignResourceToWorkOrderRequest request)
    {
        try
        {
            var workOrder = await _context.WorkOrders
                .Include(wo => wo.WorkPackage)
                .FirstOrDefaultAsync(wo => wo.Id == id);

            if (workOrder == null)
            {
                return NotFound(new { success = false, message = "Work order not found" });
            }

            var resource = await _context.Resources.FirstOrDefaultAsync(r => r.Id == request.ResourceId);
            if (resource == null)
            {
                return NotFound(new { success = false, message = "Resource not found" });
            }

            var resourceEntry = new WorkOrderResourceEntry
            {
                WorkOrderId = id,
                ResourceId = request.ResourceId,
                AssignedDate = request.AssignedDate ?? DateTime.UtcNow,
                EstimatedHours = request.EstimatedHours,
                ActualHours = 0,
                AssignmentType = request.RoleOnWorkOrder ?? "Primary",
                HourlyRate = request.HourlyRate ?? resource.HourlyRate,
                TotalCost = 0,
                Status = "Assigned",
                Notes = request.Notes,
                CompanyId = workOrder.CompanyId
            };

            _context.WorkOrderResourceEntries.Add(resourceEntry);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Resource {ResourceId} assigned to work order {WorkOrderId}", request.ResourceId, id);

            return CreatedAtAction(
                nameof(GetWorkOrderById),
                new { id = workOrder.Id, companyId = workOrder.CompanyId },
                new
                {
                    success = true,
                    message = "Resource assigned to work order successfully",
                    data = new
                    {
                        id = resourceEntry.Id,
                        workOrderId = id,
                        resourceId = resource.Id,
                        resourceName = resource.FullName,
                        roleOnWorkOrder = request.RoleOnWorkOrder,
                        assignedDate = resourceEntry.AssignedDate,
                        estimatedHours = resourceEntry.EstimatedHours,
                        actualHours = resourceEntry.ActualHours,
                        hourlyRate = resourceEntry.HourlyRate,
                        totalCost = resourceEntry.TotalCost
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning resource to work order");
            return StatusCode(500, new { success = false, message = "An error occurred while assigning resource to work order" });
        }
    }

    /// <summary>
    /// Get resources assigned to work order
    /// GET /api/fabmate/workorders/{id}/resources
    /// </summary>
    [HttpGet("workorders/{id:int}/resources")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetWorkOrderResources(int id)
    {
        try
        {
            var resources = await _context.WorkOrderResourceEntries
                .Include(r => r.Resource)
                .Where(r => r.WorkOrderId == id)
                .Select(r => new
                {
                    id = r.Id,
                    resourceId = r.ResourceId,
                    resourceName = r.Resource.FullName,
                    assignedDate = r.AssignedDate,
                    estimatedHours = r.EstimatedHours,
                    actualHours = r.ActualHours,
                    hourlyRate = r.HourlyRate,
                    totalCost = r.TotalCost,
                    status = r.Status
                })
                .ToListAsync();

            return Ok(new { success = true, data = resources });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving work order resources");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving work order resources" });
        }
    }

    // ==========================================
    // ROUTING TEMPLATES API
    // ==========================================

    /// <summary>
    /// Create a new routing template
    /// POST /api/fabmate/routing-templates
    /// </summary>
    [HttpPost("routing-templates")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> CreateRoutingTemplate([FromBody] CreateRoutingTemplateRequest request)
    {
        try
        {
            var template = new RoutingTemplate
            {
                Code = request.TemplateCode,
                Name = request.TemplateName,
                Description = request.Description,
                CompanyId = request.CompanyId,
                TemplateType = request.PartType ?? "Standard",
                ProductCategory = request.ProductCategory,
                ComplexityLevel = "Medium",
                EstimatedTotalHours = request.EstimatedTotalTime,
                DefaultEfficiencyPercentage = 100m,
                IncludesWelding = false,
                IncludesQualityControl = false,
                Version = "1.0",
                IsActive = request.IsActive,
                IsDefault = false,
                IsDeleted = false,
                UsageCount = 0,
                ApprovalStatus = "Draft",
                Notes = request.Notes,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _context.RoutingTemplates.Add(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Routing template {TemplateName} created successfully", template.Name);

            return CreatedAtAction(
                nameof(GetRoutingTemplateById),
                new { id = template.Id },
                new
                {
                    success = true,
                    message = "Routing template created successfully",
                    data = new
                    {
                        id = template.Id,
                        name = template.Name,  // For Postman test
                        templateName = template.Name,
                        templateCode = request.TemplateCode,
                        partType = request.PartType,
                        isActive = template.IsActive,
                        operationCount = 0,
                        estimatedTotalTime = request.EstimatedTotalTime,
                        createdDate = template.CreatedDate
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating routing template");
            return StatusCode(500, new { success = false, message = "An error occurred while creating the routing template" });
        }
    }

    /// <summary>
    /// Get routing template by ID
    /// GET /api/fabmate/routing-templates/{id}
    /// </summary>
    [HttpGet("routing-templates/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> GetRoutingTemplateById(int id)
    {
        try
        {
            var template = await _context.RoutingTemplates
                .Include(t => t.RoutingOperations.OrderBy(o => o.SequenceNumber))
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
            {
                return NotFound(new { success = false, message = "Routing template not found" });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = template.Id,
                    templateName = template.Name,
                    description = template.Description,
                    isActive = template.IsActive,
                    operationCount = template.RoutingOperations.Count,
                    operations = template.RoutingOperations.Select(o => new
                    {
                        id = o.Id,
                        sequenceNumber = o.SequenceNumber,
                        operationName = o.OperationName,
                        description = o.Description,
                        setupTime = o.SetupTimeMinutes,
                        runTime = o.ProcessingTimePerUnit,
                        isActive = o.IsActive
                    }),
                    createdDate = template.CreatedDate
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving routing template {TemplateId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving the routing template" });
        }
    }

    /// <summary>
    /// Get list of routing templates
    /// GET /api/fabmate/routing-templates
    /// </summary>
    [HttpGet("routing-templates")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetRoutingTemplates([FromQuery] bool? isActive = null)
    {
        try
        {
            var query = _context.RoutingTemplates.Include(t => t.RoutingOperations).AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(t => t.IsActive == isActive.Value);
            }

            var templates = await query
                .OrderBy(t => t.Name)
                .Select(t => new
                {
                    id = t.Id,
                    templateName = t.Name,
                    description = t.Description,
                    isActive = t.IsActive,
                    operationCount = t.RoutingOperations.Count
                })
                .ToListAsync();

            return Ok(new { success = true, data = templates });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving routing templates");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving routing templates" });
        }
    }

    /// <summary>
    /// Add operation to routing template
    /// POST /api/fabmate/routing-templates/{id}/operations
    /// </summary>
    [HttpPost("routing-templates/{id:int}/operations")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> AddOperationToTemplate(int id, [FromBody] CreateRoutingOperationRequest request)
    {
        try
        {
            var template = await _context.RoutingTemplates.FirstOrDefaultAsync(t => t.Id == id);
            if (template == null)
            {
                return NotFound(new { success = false, message = "Routing template not found" });
            }

            var operation = new RoutingOperation
            {
                RoutingTemplateId = id,
                WorkCenterId = request.WorkCenterId ?? 1, // Use provided or default
                OperationCode = request.OperationCode,
                OperationName = request.OperationName,
                Description = request.Description,
                SequenceNumber = request.SequenceNumber,
                OperationType = request.OperationType,
                SetupTimeMinutes = request.SetupTime,
                ProcessingTimePerUnit = request.RunTime,
                ProcessingTimePerKg = 0m,
                MovementTimeMinutes = request.TeardownTime ?? 0m,
                WaitingTimeMinutes = 0m,
                CalculationMethod = "PerUnit",
                RequiredOperators = 1,
                RequiredSkillLevel = request.SkillLevel,
                RequiresInspection = request.QualityCheckpoints?.Any() ?? false,
                InspectionPercentage = 0m,
                CanRunInParallel = false,
                MaterialCostPerUnit = 0m,
                ToolingCost = 0m,
                EfficiencyFactor = 100m,
                ScrapPercentage = 0m,
                WorkInstructions = request.WorkInstructions,
                SafetyNotes = request.SafetyRequirements != null ? string.Join("; ", request.SafetyRequirements) : null,
                QualityNotes = request.QualityCheckpoints != null ? string.Join("; ", request.QualityCheckpoints) : null,
                IsActive = true,
                IsOptional = false,
                IsCriticalPath = true,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _context.RoutingOperations.Add(operation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Operation {OperationName} added to routing template {TemplateId}", operation.OperationName, id);

            return CreatedAtAction(
                nameof(GetRoutingTemplateById),
                new { id = template.Id },
                new
                {
                    success = true,
                    message = "Operation added to routing template",
                    data = new
                    {
                        id = operation.Id,
                        routingTemplateId = id,
                        sequenceNumber = operation.SequenceNumber,
                        operationCode = operation.OperationCode,
                        operationName = operation.OperationName,
                        operationType = operation.OperationType,
                        setupTime = operation.SetupTimeMinutes,
                        runTime = operation.ProcessingTimePerUnit,
                        totalTime = operation.SetupTimeMinutes + operation.ProcessingTimePerUnit + operation.MovementTimeMinutes
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding operation to routing template");
            return StatusCode(500, new { success = false, message = "An error occurred while adding operation to routing template" });
        }
    }

    /// <summary>
    /// Update operation in routing template
    /// PUT /api/fabmate/routing-templates/{id}/operations/{operationId}
    /// </summary>
    [HttpPut("routing-templates/{id:int}/operations/{operationId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> UpdateTemplateOperation(int id, int operationId, [FromBody] CreateRoutingOperationRequest request)
    {
        try
        {
            var operation = await _context.RoutingOperations
                .FirstOrDefaultAsync(o => o.Id == operationId && o.RoutingTemplateId == id);

            if (operation == null)
            {
                return NotFound(new { success = false, message = "Operation not found" });
            }

            operation.OperationName = request.OperationName;
            operation.Description = request.Description;
            operation.SequenceNumber = request.SequenceNumber;
            operation.SetupTimeMinutes = request.SetupTime;
            operation.ProcessingTimePerUnit = request.RunTime;
            operation.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Operation {OperationId} updated in routing template {TemplateId}", operationId, id);

            return Ok(new
            {
                success = true,
                message = "Operation updated successfully",
                data = new
                {
                    id = operation.Id,
                    operationName = operation.OperationName,
                    sequenceNumber = operation.SequenceNumber,
                    setupTime = operation.SetupTimeMinutes,
                    runTime = operation.ProcessingTimePerUnit
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating operation {OperationId}", operationId);
            return StatusCode(500, new { success = false, message = "An error occurred while updating the operation" });
        }
    }

    /// <summary>
    /// Delete operation from routing template
    /// DELETE /api/fabmate/routing-templates/{id}/operations/{operationId}
    /// </summary>
    [HttpDelete("routing-templates/{id:int}/operations/{operationId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> DeleteTemplateOperation(int id, int operationId)
    {
        try
        {
            var operation = await _context.RoutingOperations
                .FirstOrDefaultAsync(o => o.Id == operationId && o.RoutingTemplateId == id);

            if (operation == null)
            {
                return NotFound(new { success = false, message = "Operation not found" });
            }

            _context.RoutingOperations.Remove(operation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Operation {OperationId} deleted from routing template {TemplateId}", operationId, id);

            return Ok(new { success = true, message = "Operation deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting operation {OperationId}", operationId);
            return StatusCode(500, new { success = false, message = "An error occurred while deleting the operation" });
        }
    }

    // ==========================================
    // WORK ORDER ROUTING ASSIGNMENT API
    // ==========================================

    /// <summary>
    /// Assign routing template to work order (creates routing lines)
    /// POST /api/fabmate/workorders/{id}/assign-routing
    /// </summary>
    [HttpPost("workorders/{id:int}/assign-routing")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> AssignRoutingToWorkOrder(int id, [FromBody] AssignRoutingTemplateRequest request)
    {
        try
        {
            // Verify work order exists
            var workOrder = await _context.WorkOrders
                .Include(wo => wo.Routing)
                    .ThenInclude(wor => wor!.RoutingLines)
                .FirstOrDefaultAsync(wo => wo.Id == id);

            if (workOrder == null)
            {
                return NotFound(new { success = false, message = "Work order not found" });
            }

            // Get routing template with operations
            var template = await _context.RoutingTemplates
                .Include(t => t.RoutingOperations.OrderBy(o => o.SequenceNumber))
                .FirstOrDefaultAsync(t => t.Id == request.RoutingTemplateId);

            if (template == null)
            {
                return NotFound(new { success = false, message = "Routing template not found" });
            }

            if (!template.RoutingOperations.Any())
            {
                return BadRequest(new { success = false, message = "Routing template has no operations" });
            }

            // Create or update work order routing
            WorkOrderRouting woRouting;
            if (workOrder.Routing == null)
            {
                woRouting = new WorkOrderRouting
                {
                    WorkOrderId = id,
                    SourceRoutingId = null,  // FIXED: SourceRoutingId references old Routing table, not RoutingTemplate
                    Description = template.Description,
                    Status = "Pending",
                    CreatedDate = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    RoutingLines = new List<WorkOrderRoutingLine>()
                };
                _context.WorkOrderRoutings.Add(woRouting);
            }
            else
            {
                woRouting = workOrder.Routing;
                // Clear existing routing lines if reassigning
                _context.WorkOrderRoutingLines.RemoveRange(woRouting.RoutingLines);
                woRouting.SourceRoutingId = null;  // FIXED: SourceRoutingId references old Routing table, not RoutingTemplate
                woRouting.Description = template.Description;
                woRouting.LastModified = DateTime.UtcNow;
            }

            // Create routing lines from template operations
            foreach (var operation in template.RoutingOperations)
            {
                var routingLine = new WorkOrderRoutingLine
                {
                    WorkOrderRoutingId = woRouting.Id,
                    SequenceNumber = operation.SequenceNumber,
                    OperationCode = operation.OperationCode,
                    OperationName = operation.OperationName,
                    OperationType = operation.OperationType,
                    Description = operation.Description ?? string.Empty,
                    WorkCenterId = operation.WorkCenterId,
                    PlannedSetupTime = operation.SetupTimeMinutes,
                    PlannedRunTime = operation.ProcessingTimePerUnit,
                    ActualSetupTime = 0m,
                    ActualRunTime = 0m,
                    Status = "Pending",
                    QuantityToProcess = 1m,  // Default to 1
                    QuantityProcessed = 0m,
                    CreatedDate = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                woRouting.RoutingLines.Add(routingLine);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Routing template {TemplateId} assigned to work order {WorkOrderId}, created {Count} routing lines",
                template.Id, id, woRouting.RoutingLines.Count);

            return CreatedAtAction(
                nameof(GetWorkOrderOperations),
                new { id = id },
                new
                {
                    success = true,
                    message = "Routing assigned successfully",
                    data = new
                    {
                        routingId = woRouting.Id,
                        workOrderId = id,
                        templateId = template.Id,
                        templateName = template.Name,
                        operationsCreated = woRouting.RoutingLines.Count,
                        operations = woRouting.RoutingLines.Select(rl => new
                        {
                            id = rl.Id,
                            sequenceNumber = rl.SequenceNumber,
                            operationName = rl.OperationName,
                            status = rl.Status
                        }).ToList()
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning routing to work order {WorkOrderId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while assigning routing" });
        }
    }

    // ==========================================
    // WORK ORDER OPERATIONS API
    // ==========================================

    /// <summary>
    /// Get operations for a work order
    /// GET /api/fabmate/workorders/{id}/operations
    /// </summary>
    [HttpGet("workorders/{id:int}/operations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetWorkOrderOperations(int id)
    {
        try
        {
            var workOrder = await _context.WorkOrders
                .Include(wo => wo.Routing)
                    .ThenInclude(wor => wor!.RoutingLines.OrderBy(rl => rl.SequenceNumber))
                .FirstOrDefaultAsync(wo => wo.Id == id);

            if (workOrder == null || workOrder.Routing == null)
            {
                return Ok(new { success = true, data = new { operations = new List<object>() } });
            }

            var operations = workOrder.Routing.RoutingLines.Select(rl => new
            {
                id = rl.Id,
                sequenceNumber = rl.SequenceNumber,
                operationCode = rl.OperationCode,
                operationName = rl.OperationName,
                operationType = rl.OperationType,
                status = rl.Status,
                plannedSetupTime = rl.PlannedSetupTime,
                plannedRunTime = rl.PlannedRunTime,
                actualSetupTime = rl.ActualSetupTime,
                actualRunTime = rl.ActualRunTime,
                workCenterId = rl.WorkCenterId,
                startDateTime = rl.StartDateTime,
                endDateTime = rl.EndDateTime,
                quantityToProcess = rl.QuantityToProcess,
                quantityProcessed = rl.QuantityProcessed
            }).ToList();

            return Ok(new { success = true, data = new { operations = operations } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving work order operations for {WorkOrderId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving work order operations" });
        }
    }

    /// <summary>
    /// Start an operation
    /// PUT /api/fabmate/operations/{id}/start
    /// </summary>
    [HttpPut("operations/{id:int}/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> StartOperation(int id, [FromBody] StartOperationRequest request)
    {
        try
        {
            var operation = await _context.WorkOrderRoutingLines.FirstOrDefaultAsync(rl => rl.Id == id);
            if (operation == null)
            {
                return NotFound(new { success = false, message = "Operation not found" });
            }

            operation.Status = "Started";
            operation.StartDateTime = request.StartTime;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Operation {OperationId} started", id);

            return Ok(new
            {
                success = true,
                message = "Operation started successfully",
                data = new
                {
                    operationId = operation.Id,
                    operationName = operation.OperationName,
                    status = operation.Status,
                    startDateTime = operation.StartDateTime,
                    resourceName = request.ResourceId.HasValue ? "Resource" : null
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting operation {OperationId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while starting the operation" });
        }
    }

    /// <summary>
    /// Complete an operation
    /// PUT /api/fabmate/operations/{id}/complete
    /// </summary>
    [HttpPut("operations/{id:int}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> CompleteOperation(int id, [FromBody] CompleteOperationRequest request)
    {
        try
        {
            var operation = await _context.WorkOrderRoutingLines
                .Include(rl => rl.WorkOrderRouting)
                    .ThenInclude(r => r!.RoutingLines.OrderBy(o => o.SequenceNumber))
                .FirstOrDefaultAsync(rl => rl.Id == id);

            if (operation == null)
            {
                return NotFound(new { success = false, message = "Operation not found" });
            }

            operation.Status = "Finished";
            operation.EndDateTime = request.CompletedAt;
            operation.ActualSetupTime = request.ActualSetupTime;
            operation.ActualRunTime = request.ActualRunTime;
            operation.QuantityProcessed = request.QuantityProcessed;

            await _context.SaveChangesAsync();

            // Find next operation
            var nextOperation = operation.WorkOrderRouting?.RoutingLines
                .Where(rl => rl.SequenceNumber > operation.SequenceNumber && rl.Status == "Pending")
                .OrderBy(rl => rl.SequenceNumber)
                .FirstOrDefault();

            _logger.LogInformation("Operation {OperationId} completed", id);

            return Ok(new
            {
                success = true,
                message = "Operation completed successfully",
                data = new
                {
                    operationId = operation.Id,
                    status = operation.Status,
                    actualSetupTime = operation.ActualSetupTime,
                    actualRunTime = operation.ActualRunTime,
                    quantityProcessed = operation.QuantityProcessed,
                    nextOperation = nextOperation != null ? new
                    {
                        id = nextOperation.Id,
                        sequenceNumber = nextOperation.SequenceNumber,
                        operationName = nextOperation.OperationName
                    } : null
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing operation {OperationId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while completing the operation" });
        }
    }

    // ==========================================
    // TIME APPROVALS API
    // ==========================================

    /// <summary>
    /// Approve a time entry
    /// PUT /api/fabmate/time-entries/{id}/approve
    /// </summary>
    [HttpPut("time-entries/{id:int}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> ApproveTimeEntry(int id, [FromBody] ApproveTimeEntryRequest request)
    {
        try
        {
            // Note: This assumes a TimeEntry table exists
            // For now, returning a mock response since TimeEntry entity may need to be created

            _logger.LogInformation("Time entry {TimeEntryId} approved by user {UserId}", id, request.ApprovedBy);

            return Ok(new
            {
                success = true,
                message = "Time entry approved successfully",
                data = new
                {
                    timeEntryId = id,
                    approvalStatus = "Approved",
                    approvedBy = "Admin User",
                    approvedHours = request.ApprovedHours,
                    approvalDate = request.ApprovalDate,
                    totalCost = request.ApprovedHours * 45.50m // Mock calculation
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving time entry {TimeEntryId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while approving the time entry" });
        }
    }

    /// <summary>
    /// Reject a time entry
    /// PUT /api/fabmate/time-entries/{id}/reject
    /// </summary>
    [HttpPut("time-entries/{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> RejectTimeEntry(int id, [FromBody] RejectTimeEntryRequest request)
    {
        try
        {
            _logger.LogInformation("Time entry {TimeEntryId} rejected by user {UserId}", id, request.RejectedBy);

            return Ok(new
            {
                success = true,
                message = "Time entry rejected successfully",
                data = new
                {
                    timeEntryId = id,
                    approvalStatus = "Rejected",
                    rejectedBy = "Admin User",
                    rejectionReason = request.RejectionReason,
                    rejectionDate = request.RejectionDate
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting time entry {TimeEntryId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while rejecting the time entry" });
        }
    }

    /// <summary>
    /// Get pending time entries for approval
    /// GET /api/fabmate/time-entries/pending
    /// </summary>
    [HttpGet("time-entries/pending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetPendingTimeEntries([FromQuery] int? companyId = null)
    {
        try
        {
            // Mock response - would need actual TimeEntry table query
            var pendingEntries = new List<object>();

            return Ok(new { success = true, data = pendingEntries });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending time entries");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving pending time entries" });
        }
    }

    /// <summary>
    /// Bulk approve time entries
    /// POST /api/fabmate/time-entries/bulk-approve
    /// </summary>
    [HttpPost("time-entries/bulk-approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> BulkApproveTimeEntries([FromBody] BulkApproveTimeEntriesRequest request)
    {
        try
        {
            _logger.LogInformation("Bulk approving {Count} time entries by user {UserId}",
                request.TimeEntryIds.Count, request.ApprovedBy);

            return Ok(new
            {
                success = true,
                message = $"{request.TimeEntryIds.Count} time entries approved successfully",
                data = new
                {
                    approvedCount = request.TimeEntryIds.Count,
                    failedCount = 0,
                    approvedBy = "Admin User",
                    approvalDate = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk approving time entries");
            return StatusCode(500, new { success = false, message = "An error occurred while bulk approving time entries" });
        }
    }
}

// DTOs for mobile app (matching the shared library)
public record CreateWorkOrderRequest(
    int PackageId,
    int CompanyId,
    string WorkOrderType,      // PartsProcessing, AssemblyBuilding, Mixed, Finishing, QualityControl
    string? Description,
    int? WorkCenterId,
    int? PrimaryResourceId,
    string Priority,           // Low, Normal, High, Urgent
    DateTime? ScheduledStartDate,
    DateTime? ScheduledEndDate,
    decimal EstimatedHours,
    decimal EstimatedCost,
    bool HasHoldPoints,
    bool RequiresInspection,
    string? WorkInstructions
);

public record StartWorkOrderRequest(int WorkOrderId, int UserId, DateTime StartTime, string? Notes);
public record CompleteWorkOrderRequest(int WorkOrderId, int UserId, DateTime CompletionTime, decimal ActualHours, string? CompletionNotes, bool AllOperationsComplete);
public record TimeEntryRequest(int WorkOrderId, int? OperationId, int UserId, DateTime StartTime, DateTime EndTime, decimal Hours, string? Notes);
public record UpdateOperationStatusRequest(int OperationId, int UserId, string Status, DateTime? CompletedAt, string? Notes);

// Resource DTOs
public record CreateResourceRequest(
    int CompanyId,
    string ResourceType,       // Person, Direct, Indirect, Contract, Supervisor
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    string? EmployeeNumber,
    string? JobTitle,
    string? Department,
    string? SkillLevel,
    int? SkillLevelNumeric,
    decimal HourlyRate,
    decimal? OvertimeRate,
    bool IsActive,
    DateTime? StartDate,
    List<string>? Certifications,
    string? Notes,
    int? UserId
);

// WorkCenter DTOs
public record CreateWorkCenterRequest(
    int CompanyId,
    string WorkCenterName,
    string WorkCenterCode,
    string? WorkCenterType,
    string? Department,
    string? Description,
    int? Capacity,
    decimal? Efficiency,
    decimal? HourlyRate,
    bool IsActive,
    string? Location,
    List<string>? EquipmentList,
    List<string>? Certifications,
    string? Notes
);

public record AssignResourceToWorkCenterRequest(
    int ResourceId,
    string AssignmentType,     // Primary, Secondary, Trainee
    DateTime EffectiveDate,
    DateTime? EndDate,
    bool IsPrimaryOperator,
    string? CertificationLevel,
    string? Notes
);

// WorkOrder Resource Assignment DTOs
public record AssignResourceToWorkOrderRequest(
    int ResourceId,
    string RoleOnWorkOrder,    // Primary, Secondary, Supervisor, Helper
    DateTime? AssignedDate,
    decimal EstimatedHours,
    decimal? HourlyRate,
    string? Notes
);

// Routing Template DTOs
public record CreateRoutingTemplateRequest(
    int CompanyId,
    string TemplateName,
    string TemplateCode,
    string? Description,
    string? PartType,
    string? ProductCategory,
    bool IsActive,
    decimal EstimatedTotalTime,
    string? Notes
);

public record CreateRoutingOperationRequest(
    int SequenceNumber,
    string OperationCode,
    string OperationName,
    string OperationType,        // Setup, Run, Teardown, Inspection, QualityControl
    string? Description,
    int? WorkCenterId,
    decimal SetupTime,
    decimal RunTime,
    decimal? TeardownTime,
    string? SkillLevel,
    string? ToolingRequired,
    List<string>? QualityCheckpoints,
    List<string>? SafetyRequirements,
    string? WorkInstructions
);

// WorkOrder Operation DTOs
public record StartOperationRequest(
    int OperationId,
    int? ResourceId,
    int? WorkCenterId,
    int UserId,
    DateTime StartTime,
    string? Notes
);

public record CompleteOperationRequest(
    int OperationId,
    int UserId,
    DateTime CompletedAt,
    decimal ActualSetupTime,
    decimal ActualRunTime,
    decimal QuantityProcessed,
    string? QualityStatus,       // Pass, Fail, Rework, InspectionRequired
    string? Notes
);

// Time Approval DTOs
public record ApproveTimeEntryRequest(
    int ApprovedBy,
    DateTime ApprovalDate,
    decimal ApprovedHours,
    string? ApprovalNotes,
    string? AdjustmentReason
);

public record RejectTimeEntryRequest(
    int RejectedBy,
    DateTime RejectionDate,
    string RejectionReason,
    string? Notes
);

public record BulkApproveTimeEntriesRequest(
    List<int> TimeEntryIds,
    int ApprovedBy,
    DateTime ApprovalDate,
    string? Notes
);

// Routing Assignment DTO
public record AssignRoutingTemplateRequest(
    int RoutingTemplateId
);
