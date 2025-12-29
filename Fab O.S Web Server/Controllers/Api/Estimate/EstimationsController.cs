using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces.Estimate;
using System.Security.Claims;

namespace FabOS.WebServer.Controllers.Api.Estimate;

/// <summary>
/// API Controller for managing Estimations.
/// Base endpoint: /api/{tenantSlug}/estimate/estimations
/// </summary>
[Route("api/{tenantSlug}/estimate/estimations")]
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
public class EstimationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IEstimationCalculationService _calculationService;
    private readonly ILogger<EstimationsController> _logger;

    public EstimationsController(
        ApplicationDbContext context,
        IEstimationCalculationService calculationService,
        ILogger<EstimationsController> logger)
    {
        _context = context;
        _calculationService = calculationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all estimations for the tenant.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EstimationListDto>>> GetEstimations(
        string tenantSlug,
        [FromQuery] string? status = null,
        [FromQuery] int? customerId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var query = _context.Estimations
                .Include(e => e.Customer)
                .Include(e => e.Revisions.Where(r => !r.IsDeleted))
                .Where(e => e.CompanyId == companyId && !e.IsDeleted);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(e => e.Status == status);

            if (customerId.HasValue)
                query = query.Where(e => e.CustomerId == customerId);

            var total = await query.CountAsync();

            var estimations = await query
                .OrderByDescending(e => e.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EstimationListDto
                {
                    Id = e.Id,
                    EstimationNumber = e.EstimationNumber,
                    Name = e.Name,
                    CustomerName = e.Customer != null ? e.Customer.Name : null,
                    Status = e.Status,
                    CurrentRevisionLetter = e.CurrentRevisionLetter,
                    CurrentTotal = e.CurrentTotal,
                    RevisionCount = e.Revisions.Count(r => !r.IsDeleted),
                    CreatedDate = e.CreatedDate
                })
                .ToListAsync();

            return Ok(new
            {
                Data = estimations,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting estimations for tenant {TenantSlug}", tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific estimation by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EstimationDetailDto>> GetEstimation(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var estimation = await _context.Estimations
                .Include(e => e.Customer)
                .Include(e => e.Revisions.Where(r => !r.IsDeleted).OrderBy(r => r.RevisionLetter))
                    .ThenInclude(r => r.Packages.Where(p => !p.IsDeleted))
                .FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId && !e.IsDeleted);

            if (estimation == null)
                return NotFound($"Estimation {id} not found");

            var dto = new EstimationDetailDto
            {
                Id = estimation.Id,
                EstimationNumber = estimation.EstimationNumber,
                Name = estimation.Name,
                Description = estimation.Description,
                CustomerId = estimation.CustomerId,
                CustomerName = estimation.Customer?.Name,
                ProjectId = estimation.ProjectId,
                SourceTakeoffId = estimation.SourceTakeoffId,
                Status = estimation.Status,
                CurrentRevisionLetter = estimation.CurrentRevisionLetter,
                CurrentTotal = estimation.CurrentTotal,
                CreatedDate = estimation.CreatedDate,
                ModifiedDate = estimation.ModifiedDate,
                Revisions = estimation.Revisions.Select(r => new RevisionSummaryDto
                {
                    Id = r.Id,
                    RevisionLetter = r.RevisionLetter,
                    Status = r.Status,
                    TotalAmount = r.TotalAmount,
                    PackageCount = r.Packages.Count(p => !p.IsDeleted),
                    CreatedDate = r.CreatedDate
                }).ToList()
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting estimation {Id} for tenant {TenantSlug}", id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new estimation.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<EstimationDetailDto>> CreateEstimation(
        string tenantSlug,
        [FromBody] CreateEstimationRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            // Generate estimation number
            var nextNumber = await GetNextEstimationNumber(companyId.Value);

            var estimation = new Estimation
            {
                CompanyId = companyId.Value,
                EstimationNumber = nextNumber,
                Name = request.Name,
                Description = request.Description,
                CustomerId = request.CustomerId ?? 0,
                ProjectName = request.Name, // Use Name as default ProjectName
                ProjectId = request.ProjectId,
                SourceTakeoffId = request.SourceTakeoffId,
                Status = "Draft",
                CurrentRevisionLetter = "A",
                CreatedBy = userId.Value,
                LastModifiedBy = userId.Value,
                CreatedDate = DateTime.UtcNow
            };

            _context.Estimations.Add(estimation);
            await _context.SaveChangesAsync();

            // Create initial revision A
            var revision = new EstimationRevision
            {
                CompanyId = companyId.Value,
                EstimationId = estimation.Id,
                RevisionLetter = "A",
                Status = "Draft",
                ValidUntilDate = DateTime.UtcNow.AddDays(30),
                OverheadPercentage = request.DefaultOverheadPercentage ?? 15m,
                MarginPercentage = request.DefaultMarginPercentage ?? 20m,
                CreatedBy = userId.Value,
                CreatedDate = DateTime.UtcNow
            };

            _context.EstimationRevisions.Add(revision);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created estimation {EstimationNumber} with revision A for tenant {TenantSlug}",
                estimation.EstimationNumber, tenantSlug);

            // Reload estimation with revisions for response
            var createdEstimation = await _context.Estimations
                .Include(e => e.Customer)
                .Include(e => e.Revisions.Where(r => !r.IsDeleted).OrderBy(r => r.RevisionLetter))
                    .ThenInclude(r => r.Packages.Where(p => !p.IsDeleted))
                .FirstOrDefaultAsync(e => e.Id == estimation.Id);

            var responseDto = new EstimationDetailDto
            {
                Id = createdEstimation!.Id,
                EstimationNumber = createdEstimation.EstimationNumber,
                Name = createdEstimation.Name,
                Description = createdEstimation.Description,
                CustomerId = createdEstimation.CustomerId,
                CustomerName = createdEstimation.Customer?.Name,
                ProjectId = createdEstimation.ProjectId,
                SourceTakeoffId = createdEstimation.SourceTakeoffId,
                Status = createdEstimation.Status,
                CurrentRevisionLetter = createdEstimation.CurrentRevisionLetter,
                CurrentTotal = createdEstimation.CurrentTotal,
                CreatedDate = createdEstimation.CreatedDate,
                ModifiedDate = createdEstimation.ModifiedDate,
                Revisions = createdEstimation.Revisions.Select(r => new RevisionSummaryDto
                {
                    Id = r.Id,
                    RevisionLetter = r.RevisionLetter,
                    Status = r.Status,
                    TotalAmount = r.TotalAmount,
                    PackageCount = r.Packages.Count(p => !p.IsDeleted),
                    CreatedDate = r.CreatedDate
                }).ToList()
            };

            return CreatedAtAction(nameof(GetEstimation), new { tenantSlug, id = estimation.Id }, responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating estimation for tenant {TenantSlug}", tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update an estimation.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<EstimationDetailDto>> UpdateEstimation(
        string tenantSlug,
        int id,
        [FromBody] UpdateEstimationRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var estimation = await _context.Estimations
                .FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId && !e.IsDeleted);

            if (estimation == null)
                return NotFound($"Estimation {id} not found");

            // Update fields
            estimation.Name = request.Name ?? estimation.Name;
            estimation.Description = request.Description ?? estimation.Description;
            estimation.CustomerId = request.CustomerId ?? estimation.CustomerId;
            estimation.ProjectId = request.ProjectId ?? estimation.ProjectId;
            estimation.Status = request.Status ?? estimation.Status;
            estimation.ModifiedBy = userId.Value;
            estimation.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated estimation {Id} for tenant {TenantSlug}", id, tenantSlug);

            // Reload estimation with revisions for response
            var updatedEstimation = await _context.Estimations
                .Include(e => e.Customer)
                .Include(e => e.Revisions.Where(r => !r.IsDeleted).OrderBy(r => r.RevisionLetter))
                    .ThenInclude(r => r.Packages.Where(p => !p.IsDeleted))
                .FirstOrDefaultAsync(e => e.Id == id);

            var responseDto = new EstimationDetailDto
            {
                Id = updatedEstimation!.Id,
                EstimationNumber = updatedEstimation.EstimationNumber,
                Name = updatedEstimation.Name,
                Description = updatedEstimation.Description,
                CustomerId = updatedEstimation.CustomerId,
                CustomerName = updatedEstimation.Customer?.Name,
                ProjectId = updatedEstimation.ProjectId,
                SourceTakeoffId = updatedEstimation.SourceTakeoffId,
                Status = updatedEstimation.Status,
                CurrentRevisionLetter = updatedEstimation.CurrentRevisionLetter,
                CurrentTotal = updatedEstimation.CurrentTotal,
                CreatedDate = updatedEstimation.CreatedDate,
                ModifiedDate = updatedEstimation.ModifiedDate,
                Revisions = updatedEstimation.Revisions.Select(r => new RevisionSummaryDto
                {
                    Id = r.Id,
                    RevisionLetter = r.RevisionLetter,
                    Status = r.Status,
                    TotalAmount = r.TotalAmount,
                    PackageCount = r.Packages.Count(p => !p.IsDeleted),
                    CreatedDate = r.CreatedDate
                }).ToList()
            };

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating estimation {Id} for tenant {TenantSlug}", id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete an estimation (soft delete).
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEstimation(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var estimation = await _context.Estimations
                .FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId && !e.IsDeleted);

            if (estimation == null)
                return NotFound($"Estimation {id} not found");

            estimation.IsDeleted = true;
            estimation.ModifiedBy = userId.Value;
            estimation.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted estimation {Id} for tenant {TenantSlug}", id, tenantSlug);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting estimation {Id} for tenant {TenantSlug}", id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    #region Private Helpers

    private int? GetCompanyId()
    {
        var claim = User.FindFirst("company_id");
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    private async Task<string> GetNextEstimationNumber(int companyId)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"EST-{year}-";

        var lastNumber = await _context.Estimations
            .Where(e => e.CompanyId == companyId && e.EstimationNumber.StartsWith(prefix))
            .OrderByDescending(e => e.EstimationNumber)
            .Select(e => e.EstimationNumber)
            .FirstOrDefaultAsync();

        int nextSeq = 1;
        if (lastNumber != null)
        {
            var seqStr = lastNumber.Replace(prefix, "");
            if (int.TryParse(seqStr, out var seq))
                nextSeq = seq + 1;
        }

        return $"{prefix}{nextSeq:D4}";
    }

    #endregion
}

#region DTOs

public class EstimationListDto
{
    public int Id { get; set; }
    public string EstimationNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CurrentRevisionLetter { get; set; }
    public decimal CurrentTotal { get; set; }
    public int RevisionCount { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class EstimationDetailDto
{
    public int Id { get; set; }
    public string EstimationNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? ProjectId { get; set; }
    public int? SourceTakeoffId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CurrentRevisionLetter { get; set; }
    public decimal CurrentTotal { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public IList<RevisionSummaryDto> Revisions { get; set; } = new List<RevisionSummaryDto>();
}

public class RevisionSummaryDto
{
    public int Id { get; set; }
    public string RevisionLetter { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int PackageCount { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateEstimationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CustomerId { get; set; }
    public int? ProjectId { get; set; }
    public int? SourceTakeoffId { get; set; }
    public decimal? DefaultOverheadPercentage { get; set; }
    public decimal? DefaultMarginPercentage { get; set; }
}

public class UpdateEstimationRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? CustomerId { get; set; }
    public int? ProjectId { get; set; }
    public string? Status { get; set; }
}

#endregion
