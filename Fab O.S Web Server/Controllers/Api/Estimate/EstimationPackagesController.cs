using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces.Estimate;
using System.Security.Claims;

namespace FabOS.WebServer.Controllers.Api.Estimate;

/// <summary>
/// API Controller for managing Estimation Packages.
/// Base endpoint: /api/{tenantSlug}/estimate/packages
/// </summary>
[Route("api/{tenantSlug}/estimate/packages")]
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
public class EstimationPackagesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IEstimationCalculationService _calculationService;
    private readonly ILogger<EstimationPackagesController> _logger;

    public EstimationPackagesController(
        ApplicationDbContext context,
        IEstimationCalculationService calculationService,
        ILogger<EstimationPackagesController> logger)
    {
        _context = context;
        _calculationService = calculationService;
        _logger = logger;
    }

    /// <summary>
    /// Get a specific package by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PackageDetailDto>> GetPackage(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var package = await _context.EstimationRevisionPackages
                .Include(p => p.Revision)
                .Include(p => p.Worksheets.Where(w => !w.IsDeleted).OrderBy(w => w.SortOrder))
                .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId && !p.IsDeleted);

            if (package == null)
                return NotFound($"Package {id} not found");

            var dto = new PackageDetailDto
            {
                Id = package.Id,
                RevisionId = package.RevisionId,
                RevisionLetter = package.Revision?.RevisionLetter,
                Name = package.Name,
                Description = package.Description,
                SortOrder = package.SortOrder,
                MaterialCost = package.MaterialCost,
                LaborHours = package.LaborHours,
                LaborCost = package.LaborCost,
                OverheadPercentage = package.OverheadPercentage,
                OverheadCost = package.OverheadCost,
                PackageTotal = package.PackageTotal,
                CreatedDate = package.CreatedDate,
                ModifiedDate = package.ModifiedDate,
                Worksheets = package.Worksheets.Where(w => !w.IsDeleted).Select(w => new WorksheetSummaryDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    WorksheetType = w.WorksheetType,
                    TotalCost = w.TotalCost,
                    RowCount = w.Rows.Count(r => !r.IsDeleted && !r.IsGroupHeader)
                }).ToList()
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting package {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new package in a revision.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PackageDetailDto>> CreatePackage(
        string tenantSlug,
        [FromBody] CreatePackageRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            // Verify revision exists and is in draft status
            var revision = await _context.EstimationRevisions
                .Include(r => r.Packages)
                .FirstOrDefaultAsync(r => r.Id == request.RevisionId && r.CompanyId == companyId && !r.IsDeleted);

            if (revision == null)
                return NotFound($"Revision {request.RevisionId} not found");

            if (revision.Status != "Draft")
                return BadRequest("Can only add packages to draft revisions");

            // Get next sort order
            var nextSortOrder = revision.Packages.Any()
                ? revision.Packages.Max(p => p.SortOrder) + 1
                : 0;

            var package = new EstimationRevisionPackage
            {
                CompanyId = companyId.Value,
                RevisionId = request.RevisionId,
                Name = request.Name,
                Description = request.Description,
                SortOrder = request.SortOrder ?? nextSortOrder,
                OverheadPercentage = request.OverheadPercentage ?? 0m,
                CreatedBy = userId.Value,
                CreatedDate = DateTime.UtcNow
            };

            _context.EstimationRevisionPackages.Add(package);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created package {Name} in revision {RevisionId}", request.Name, request.RevisionId);

            // Reload package with revision for response
            var createdPackage = await _context.EstimationRevisionPackages
                .Include(p => p.Revision)
                .Include(p => p.Worksheets.Where(w => !w.IsDeleted).OrderBy(w => w.SortOrder))
                .FirstOrDefaultAsync(p => p.Id == package.Id);

            var responseDto = new PackageDetailDto
            {
                Id = createdPackage!.Id,
                RevisionId = createdPackage.RevisionId,
                RevisionLetter = createdPackage.Revision?.RevisionLetter,
                Name = createdPackage.Name,
                Description = createdPackage.Description,
                SortOrder = createdPackage.SortOrder,
                MaterialCost = createdPackage.MaterialCost,
                LaborHours = createdPackage.LaborHours,
                LaborCost = createdPackage.LaborCost,
                OverheadPercentage = createdPackage.OverheadPercentage,
                OverheadCost = createdPackage.OverheadCost,
                PackageTotal = createdPackage.PackageTotal,
                CreatedDate = createdPackage.CreatedDate,
                ModifiedDate = createdPackage.ModifiedDate,
                Worksheets = createdPackage.Worksheets.Where(w => !w.IsDeleted).Select(w => new WorksheetSummaryDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    WorksheetType = w.WorksheetType,
                    TotalCost = w.TotalCost,
                    RowCount = w.Rows.Count(r => !r.IsDeleted && !r.IsGroupHeader)
                }).ToList()
            };

            return CreatedAtAction(nameof(GetPackage), new { tenantSlug, id = package.Id }, responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating package");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update a package.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<PackageDetailDto>> UpdatePackage(
        string tenantSlug,
        int id,
        [FromBody] UpdatePackageRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var package = await _context.EstimationRevisionPackages
                .Include(p => p.Revision)
                .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId && !p.IsDeleted);

            if (package == null)
                return NotFound($"Package {id} not found");

            if (package.Revision?.Status != "Draft")
                return BadRequest("Can only modify packages in draft revisions");

            package.Name = request.Name ?? package.Name;
            package.Description = request.Description ?? package.Description;
            package.SortOrder = request.SortOrder ?? package.SortOrder;
            package.OverheadPercentage = request.OverheadPercentage ?? package.OverheadPercentage;
            package.ModifiedBy = userId.Value;
            package.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Recalculate if overhead changed
            if (request.OverheadPercentage.HasValue)
            {
                await _calculationService.RecalculatePackageAsync(id);
            }

            // Reload package with revision for response
            var updatedPackage = await _context.EstimationRevisionPackages
                .Include(p => p.Revision)
                .Include(p => p.Worksheets.Where(w => !w.IsDeleted).OrderBy(w => w.SortOrder))
                .FirstOrDefaultAsync(p => p.Id == id);

            var responseDto = new PackageDetailDto
            {
                Id = updatedPackage!.Id,
                RevisionId = updatedPackage.RevisionId,
                RevisionLetter = updatedPackage.Revision?.RevisionLetter,
                Name = updatedPackage.Name,
                Description = updatedPackage.Description,
                SortOrder = updatedPackage.SortOrder,
                MaterialCost = updatedPackage.MaterialCost,
                LaborHours = updatedPackage.LaborHours,
                LaborCost = updatedPackage.LaborCost,
                OverheadPercentage = updatedPackage.OverheadPercentage,
                OverheadCost = updatedPackage.OverheadCost,
                PackageTotal = updatedPackage.PackageTotal,
                CreatedDate = updatedPackage.CreatedDate,
                ModifiedDate = updatedPackage.ModifiedDate,
                Worksheets = updatedPackage.Worksheets.Where(w => !w.IsDeleted).Select(w => new WorksheetSummaryDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    WorksheetType = w.WorksheetType,
                    TotalCost = w.TotalCost,
                    RowCount = w.Rows.Count(r => !r.IsDeleted && !r.IsGroupHeader)
                }).ToList()
            };

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating package {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a package (soft delete).
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePackage(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var package = await _context.EstimationRevisionPackages
                .Include(p => p.Revision)
                .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId && !p.IsDeleted);

            if (package == null)
                return NotFound($"Package {id} not found");

            if (package.Revision?.Status != "Draft")
                return BadRequest("Can only delete packages in draft revisions");

            package.IsDeleted = true;
            package.ModifiedBy = userId.Value;
            package.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Recalculate revision totals
            await _calculationService.RecalculateRevisionAsync(package.RevisionId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting package {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get worksheets in a package.
    /// </summary>
    [HttpGet("{id:int}/worksheets")]
    public async Task<ActionResult<IEnumerable<WorksheetSummaryDto>>> GetPackageWorksheets(
        string tenantSlug,
        int id)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var worksheets = await _context.EstimationWorksheets
                .Where(w => w.PackageId == id && w.CompanyId == companyId && !w.IsDeleted)
                .OrderBy(w => w.SortOrder)
                .Select(w => new WorksheetSummaryDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    WorksheetType = w.WorksheetType,
                    TotalCost = w.TotalCost,
                    RowCount = w.Rows.Count(r => !r.IsDeleted && !r.IsGroupHeader)
                })
                .ToListAsync();

            return Ok(worksheets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting worksheets for package {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Recalculate package totals.
    /// </summary>
    [HttpPost("{id:int}/recalculate")]
    public async Task<ActionResult<PackageSummary>> RecalculatePackage(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var package = await _context.EstimationRevisionPackages
                .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId && !p.IsDeleted);

            if (package == null)
                return NotFound($"Package {id} not found");

            await _calculationService.RecalculatePackageAsync(id);
            var summary = await _calculationService.GetPackageSummaryAsync(id);

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating package {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Reorder packages within a revision.
    /// </summary>
    [HttpPost("reorder")]
    public async Task<IActionResult> ReorderPackages(
        string tenantSlug,
        [FromBody] ReorderPackagesRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            foreach (var item in request.Packages)
            {
                var package = await _context.EstimationRevisionPackages
                    .FirstOrDefaultAsync(p => p.Id == item.PackageId && p.CompanyId == companyId && !p.IsDeleted);

                if (package != null)
                {
                    package.SortOrder = item.SortOrder;
                    package.ModifiedBy = userId.Value;
                    package.ModifiedDate = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering packages");
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

    #endregion
}

#region DTOs

public class PackageDetailDto
{
    public int Id { get; set; }
    public int RevisionId { get; set; }
    public string? RevisionLetter { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal LaborHours { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OverheadPercentage { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal PackageTotal { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public IList<WorksheetSummaryDto> Worksheets { get; set; } = new List<WorksheetSummaryDto>();
}

public class WorksheetSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string WorksheetType { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public int RowCount { get; set; }
}

public class CreatePackageRequest
{
    public int RevisionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
    public decimal? OverheadPercentage { get; set; }
}

public class UpdatePackageRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
    public decimal? OverheadPercentage { get; set; }
}

public class ReorderPackagesRequest
{
    public IList<PackageOrderItem> Packages { get; set; } = new List<PackageOrderItem>();
}

public class PackageOrderItem
{
    public int PackageId { get; set; }
    public int SortOrder { get; set; }
}

#endregion
