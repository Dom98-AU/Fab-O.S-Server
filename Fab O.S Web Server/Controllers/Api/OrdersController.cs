using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services;

namespace FabOS.WebServer.Controllers.Api;

/// <summary>
/// Orders API - Manage customer orders for FabMate module
/// Orders are the top level of the hierarchy: Order → WorkPackage → WorkOrder
/// </summary>
[ApiController]
[Route("api/orders")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly NumberSeriesService _numberSeriesService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        ApplicationDbContext context,
        NumberSeriesService numberSeriesService,
        ILogger<OrdersController> logger)
    {
        _context = context;
        _numberSeriesService = numberSeriesService;
        _logger = logger;
    }

    /// <summary>
    /// Get all orders with filtering and pagination
    /// GET /api/orders?companyId=1&status=Confirmed&pageNumber=1&pageSize=50
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> GetOrders(
        [FromQuery] int companyId,
        [FromQuery] string? status = null,
        [FromQuery] int? customerId = null,
        [FromQuery] string? source = null,
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
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Estimation)
                .Include(o => o.WorkPackages)
                .Where(o => o.CompanyId == companyId);

            // Apply filters
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.WorkPackages.Any(wp => wp.Status == status));
            }

            if (customerId.HasValue)
            {
                query = query.Where(o => o.CustomerId == customerId.Value);
            }

            if (!string.IsNullOrEmpty(source))
            {
                query = query.Where(o => o.Source == source);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(o => o.OrderDate <= toDate.Value);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(o =>
                    o.OrderNumber.Contains(searchTerm) ||
                    o.Description.Contains(searchTerm) ||
                    (o.ProjectName != null && o.ProjectName.Contains(searchTerm)) ||
                    (o.CustomerPONumber != null && o.CustomerPONumber.Contains(searchTerm)) ||
                    o.Customer.Name.Contains(searchTerm));
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var orders = await query
                .OrderByDescending(o => o.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    id = o.Id,
                    orderNumber = o.OrderNumber,
                    customerId = o.CustomerId,
                    customerName = o.Customer.Name,
                    // description = o.Description, // Column doesn't exist in database
                    // projectName = o.ProjectName, // Column doesn't exist in database
                    customerPONumber = o.CustomerPONumber,
                    customerReference = o.CustomerReference,
                    orderDate = o.OrderDate,
                    source = o.Source,
                    estimationId = o.EstimationId,
                    totalPackages = o.WorkPackages.Count,
                    completedPackages = o.WorkPackages.Count(wp => wp.Status == "Complete"),
                    totalValue = o.WorkPackages.Sum(wp => wp.EstimatedCost), // Use EstimatedCost instead of BillableValue
                    createdDate = o.CreatedDate,
                    lastModified = o.LastModified
                })
                .ToListAsync();

            var response = new
            {
                items = orders,
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
            _logger.LogError(ex, "Error retrieving orders for company {CompanyId}", companyId);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving orders" });
        }
    }

    /// <summary>
    /// Get order by ID with full details
    /// GET /api/orders/123?companyId=1
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> GetOrderById(int id, [FromQuery] int companyId)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Estimation)
                .Include(o => o.WorkPackages)
                    .ThenInclude(wp => wp.WorkOrders)
                .Include(o => o.CreatedByUser)
                .Where(o => o.Id == id && o.CompanyId == companyId)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound(new { success = false, message = "Order not found" });
            }

            var result = new
            {
                id = order.Id,
                orderNumber = order.OrderNumber,
                customerId = order.CustomerId,
                customerName = order.Customer.Name,
                // description = order.Description, // Column doesn't exist
                // projectName = order.ProjectName, // Column doesn't exist
                customerPONumber = order.CustomerPONumber,
                customerReference = order.CustomerReference,
                orderDate = order.OrderDate,
                source = order.Source,
                estimationId = order.EstimationId,
                estimationNumber = order.Estimation?.EstimationNumber,
                createdDate = order.CreatedDate,
                createdBy = order.CreatedByUser != null ? $"{order.CreatedByUser.FirstName} {order.CreatedByUser.LastName}" : null,
                lastModified = order.LastModified,
                workPackages = order.WorkPackages.Select(wp => new
                {
                    id = wp.Id,
                    packageNumber = wp.PackageNumber,
                    packageName = wp.PackageName,
                    status = wp.Status,
                    priority = wp.Priority,
                    estimatedHours = wp.EstimatedHours,
                    actualHours = wp.ActualHours,
                    estimatedCost = wp.EstimatedCost, // Use EstimatedCost instead of BillableValue
                    percentComplete = wp.PercentComplete,
                    totalWorkOrders = wp.WorkOrders.Count,
                    completedWorkOrders = wp.WorkOrders.Count(wo => wo.Status == "Complete")
                }).ToList(),
                summary = new
                {
                    totalPackages = order.WorkPackages.Count,
                    completedPackages = order.WorkPackages.Count(wp => wp.Status == "Complete"),
                    totalWorkOrders = order.WorkPackages.Sum(wp => wp.WorkOrders.Count),
                    completedWorkOrders = order.WorkPackages.Sum(wp => wp.WorkOrders.Count(wo => wo.Status == "Complete")),
                    totalValue = order.WorkPackages.Sum(wp => wp.EstimatedCost), // Use EstimatedCost
                    totalEstimatedHours = order.WorkPackages.Sum(wp => wp.EstimatedHours),
                    totalActualHours = order.WorkPackages.Sum(wp => wp.ActualHours)
                }
            };

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving the order" });
        }
    }

    /// <summary>
    /// Create a new order
    /// POST /api/orders
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            // Validate customer exists and belongs to company
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.CompanyId == request.CompanyId);

            if (customer == null)
            {
                return BadRequest(new { success = false, message = "Customer not found or does not belong to this company" });
            }

            // Validate source-specific references
            if (request.Source == "FromEstimation" && !request.EstimationId.HasValue)
            {
                return BadRequest(new { success = false, message = "EstimationId is required when Source is FromEstimation" });
            }

            // Validate source (must be: FromEstimation, Direct)
            if (!new[] { "FromEstimation", "Direct" }.Contains(request.Source))
            {
                return BadRequest(new { success = false, message = "Source must be FromEstimation or Direct" });
            }

            // Generate order number
            var orderNumber = await _numberSeriesService.GetNextNumberAsync("Order", request.CompanyId);

            // Create order
            var order = new Order
            {
                OrderNumber = orderNumber,
                CustomerId = request.CustomerId,
                CompanyId = request.CompanyId,
                Source = request.Source,
                EstimationId = request.EstimationId,
                // Note: Description and ProjectName columns don't exist in database yet
                // Description = request.Description,
                // ProjectName = request.ProjectName,
                CustomerPONumber = request.CustomerPONumber,
                CustomerReference = request.CustomerReference,
                OrderDate = request.OrderDate ?? DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                CreatedBy = request.UserId
                // Note: LastModifiedBy doesn't exist - database has 'ModifiedBy' instead
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order {OrderNumber} created successfully for customer {CustomerId}", orderNumber, request.CustomerId);

            return CreatedAtAction(
                nameof(GetOrderById),
                new { id = order.Id, companyId = request.CompanyId },
                new
                {
                    success = true,
                    message = "Order created successfully",
                    data = new
                    {
                        id = order.Id,
                        orderNumber = order.OrderNumber,
                        customerId = order.CustomerId,
                        customerName = customer.Name,
                        // description = order.Description, // Column doesn't exist
                        // projectName = order.ProjectName, // Column doesn't exist
                        orderDate = order.OrderDate,
                        source = order.Source,
                        createdDate = order.CreatedDate
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, new { success = false, message = "An error occurred while creating the order" });
        }
    }

    /// <summary>
    /// Update an existing order
    /// PUT /api/orders/123
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> UpdateOrder(int id, [FromBody] UpdateOrderRequest request)
    {
        try
        {
            if (request.OrderId != id)
            {
                return BadRequest(new { success = false, message = "Order ID mismatch" });
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.CompanyId == request.CompanyId);

            if (order == null)
            {
                return NotFound(new { success = false, message = "Order not found" });
            }

            // Update fields
            // Note: Description and ProjectName columns don't exist in database yet
            // if (!string.IsNullOrEmpty(request.Description))
            //     order.Description = request.Description;
            //
            // if (!string.IsNullOrEmpty(request.ProjectName))
            //     order.ProjectName = request.ProjectName;

            if (!string.IsNullOrEmpty(request.CustomerPONumber))
                order.CustomerPONumber = request.CustomerPONumber;

            if (!string.IsNullOrEmpty(request.CustomerReference))
                order.CustomerReference = request.CustomerReference;

            order.LastModified = DateTime.UtcNow;
            order.LastModifiedBy = request.UserId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Order {OrderNumber} updated successfully", order.OrderNumber);

            return Ok(new
            {
                success = true,
                message = "Order updated successfully",
                data = new
                {
                    id = order.Id,
                    orderNumber = order.OrderNumber,
                    // description = order.Description, // Column doesn't exist
                    // projectName = order.ProjectName, // Column doesn't exist
                    lastModified = order.LastModified
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order {OrderId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while updating the order" });
        }
    }

    /// <summary>
    /// Delete an order (soft delete)
    /// DELETE /api/orders/123?companyId=1
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteOrder(int id, [FromQuery] int companyId)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.WorkPackages)
                    .ThenInclude(wp => wp.WorkOrders)
                .FirstOrDefaultAsync(o => o.Id == id && o.CompanyId == companyId);

            if (order == null)
            {
                return NotFound(new { success = false, message = "Order not found" });
            }

            // Check if order has work packages with work orders in progress
            var hasActiveWork = order.WorkPackages.Any(wp =>
                wp.WorkOrders.Any(wo => wo.Status == "InProgress"));

            if (hasActiveWork)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Cannot delete order with work orders in progress. Complete or cancel work orders first."
                });
            }

            // Soft delete - mark work packages and work orders as deleted
            foreach (var workPackage in order.WorkPackages)
            {
                workPackage.IsDeleted = true;
                foreach (var workOrder in workPackage.WorkOrders)
                {
                    workOrder.Status = "Cancelled";
                }
            }

            // Remove the order (or implement soft delete on Order entity if preferred)
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order {OrderNumber} deleted successfully", order.OrderNumber);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order {OrderId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while deleting the order" });
        }
    }
}

// ==========================================
// REQUEST DTOs
// ==========================================

public record CreateOrderRequest(
    int CustomerId,
    int CompanyId,
    string Source,               // FromEstimation, Direct
    int? EstimationId,
    string Description,
    string? ProjectName,
    string? CustomerPONumber,
    string? CustomerReference,
    DateTime? OrderDate,
    int? UserId
);

public record UpdateOrderRequest(
    int OrderId,
    int CompanyId,
    string? Description,
    string? ProjectName,
    string? CustomerPONumber,
    string? CustomerReference,
    int? UserId
);
