using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Services.Implementations
{
    /// <summary>
    /// Implementation of ITakeoffCardService with proper company isolation
    /// </summary>
    public class TakeoffCardService : ITakeoffCardService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<TakeoffCardService> _logger;

        public TakeoffCardService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<TakeoffCardService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<Takeoff?> GetTakeoffByIdAsync(int takeoffId, int companyId)
        {
            if (companyId <= 0)
            {
                _logger.LogWarning("Invalid companyId {CompanyId} for GetTakeoffByIdAsync", companyId);
                return null;
            }

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var takeoff = await context.TraceDrawings
                    .Include(t => t.Customer)
                    .Include(t => t.Project)
                    .FirstOrDefaultAsync(t => t.Id == takeoffId && t.CompanyId == companyId);

                if (takeoff == null)
                {
                    _logger.LogWarning("Takeoff {TakeoffId} not found or access denied for company {CompanyId}",
                        takeoffId, companyId);
                }

                return takeoff;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving takeoff {TakeoffId} for company {CompanyId}",
                    takeoffId, companyId);
                throw;
            }
        }

        public async Task<List<Takeoff>> GetTakeoffsAsync(int companyId, TakeoffFilterOptions? filter = null)
        {
            if (companyId <= 0)
            {
                _logger.LogWarning("Invalid companyId {CompanyId} for GetTakeoffsAsync", companyId);
                return new List<Takeoff>();
            }

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var query = context.TraceDrawings
                    .Where(t => t.CompanyId == companyId)
                    .Include(t => t.Customer)
                    .Include(t => t.Project)
                    .AsQueryable();

                // Apply filters
                if (filter != null)
                {
                    if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                    {
                        var searchLower = filter.SearchTerm.ToLower();
                        query = query.Where(t =>
                            (t.TakeoffNumber != null && t.TakeoffNumber.ToLower().Contains(searchLower)) ||
                            (t.FileName != null && t.FileName.ToLower().Contains(searchLower)) ||
                            (t.ProjectName != null && t.ProjectName.ToLower().Contains(searchLower)) ||
                            (t.Customer != null && t.Customer.CompanyName != null && t.Customer.CompanyName.ToLower().Contains(searchLower)));
                    }

                    if (filter.CustomerId.HasValue)
                    {
                        query = query.Where(t => t.CustomerId == filter.CustomerId.Value);
                    }

                    if (filter.ProjectId.HasValue)
                    {
                        query = query.Where(t => t.ProjectId == filter.ProjectId.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(filter.Status))
                    {
                        query = query.Where(t => t.Status == filter.Status);
                    }

                    if (filter.FromDate.HasValue)
                    {
                        query = query.Where(t => t.CreatedDate >= filter.FromDate.Value);
                    }

                    if (filter.ToDate.HasValue)
                    {
                        query = query.Where(t => t.CreatedDate <= filter.ToDate.Value);
                    }
                }

                var takeoffs = await query
                    .OrderByDescending(t => t.CreatedDate)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} takeoffs for company {CompanyId}",
                    takeoffs.Count, companyId);

                return takeoffs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving takeoffs for company {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<Takeoff> CreateTakeoffAsync(Takeoff takeoff, int companyId, int userId)
        {
            if (companyId <= 0)
            {
                throw new ArgumentException("Invalid companyId", nameof(companyId));
            }

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                // Ensure company isolation
                takeoff.CompanyId = companyId;
                takeoff.UploadedBy = userId;
                takeoff.CreatedDate = DateTime.UtcNow;
                takeoff.UploadDate = DateTime.UtcNow;
                takeoff.LastModified = DateTime.UtcNow;

                context.TraceDrawings.Add(takeoff);
                await context.SaveChangesAsync();

                _logger.LogInformation("Created takeoff {TakeoffId} for company {CompanyId} by user {UserId}",
                    takeoff.Id, companyId, userId);

                return takeoff;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating takeoff for company {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<Takeoff?> UpdateTakeoffAsync(Takeoff takeoff, int companyId, int userId)
        {
            if (companyId <= 0)
            {
                throw new ArgumentException("Invalid companyId", nameof(companyId));
            }

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                // Verify the takeoff belongs to this company
                var existingTakeoff = await context.TraceDrawings
                    .FirstOrDefaultAsync(t => t.Id == takeoff.Id && t.CompanyId == companyId);

                if (existingTakeoff == null)
                {
                    _logger.LogWarning("Takeoff {TakeoffId} not found or access denied for company {CompanyId}",
                        takeoff.Id, companyId);
                    return null;
                }

                // Update fields (preserve CompanyId and audit fields)
                existingTakeoff.TakeoffNumber = takeoff.TakeoffNumber;
                existingTakeoff.FileName = takeoff.FileName;
                existingTakeoff.FileType = takeoff.FileType;
                existingTakeoff.ProjectName = takeoff.ProjectName;
                existingTakeoff.ProjectId = takeoff.ProjectId;
                existingTakeoff.CustomerId = takeoff.CustomerId;
                existingTakeoff.ContactId = takeoff.ContactId;
                existingTakeoff.Status = takeoff.Status;
                existingTakeoff.ProcessingStatus = takeoff.ProcessingStatus;
                existingTakeoff.BlobUrl = takeoff.BlobUrl;
                existingTakeoff.Scale = takeoff.Scale;
                existingTakeoff.ScaleUnit = takeoff.ScaleUnit;
                existingTakeoff.CalibrationData = takeoff.CalibrationData;
                existingTakeoff.LastModified = DateTime.UtcNow;

                await context.SaveChangesAsync();

                _logger.LogInformation("Updated takeoff {TakeoffId} for company {CompanyId} by user {UserId}",
                    takeoff.Id, companyId, userId);

                return existingTakeoff;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating takeoff {TakeoffId} for company {CompanyId}",
                    takeoff.Id, companyId);
                throw;
            }
        }

        public async Task<bool> DeleteTakeoffAsync(int takeoffId, int companyId, int userId)
        {
            if (companyId <= 0)
            {
                throw new ArgumentException("Invalid companyId", nameof(companyId));
            }

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var takeoff = await context.TraceDrawings
                    .FirstOrDefaultAsync(t => t.Id == takeoffId && t.CompanyId == companyId);

                if (takeoff == null)
                {
                    _logger.LogWarning("Takeoff {TakeoffId} not found or access denied for company {CompanyId}",
                        takeoffId, companyId);
                    return false;
                }

                // Soft delete - just remove from database (or set IsDeleted flag if available)
                context.TraceDrawings.Remove(takeoff);
                await context.SaveChangesAsync();

                _logger.LogInformation("Deleted takeoff {TakeoffId} for company {CompanyId} by user {UserId}",
                    takeoffId, companyId, userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting takeoff {TakeoffId} for company {CompanyId}",
                    takeoffId, companyId);
                throw;
            }
        }

        public async Task<List<Package>> GetPackagesByTakeoffAsync(int takeoffId, int companyId, int? revisionId = null)
        {
            if (companyId <= 0)
            {
                _logger.LogWarning("Invalid companyId {CompanyId} for GetPackagesByTakeoffAsync", companyId);
                return new List<Package>();
            }

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                // First verify the takeoff belongs to this company
                var takeoffExists = await context.TraceDrawings
                    .AnyAsync(t => t.Id == takeoffId && t.CompanyId == companyId);

                if (!takeoffExists)
                {
                    _logger.LogWarning("Takeoff {TakeoffId} not found or access denied for company {CompanyId}",
                        takeoffId, companyId);
                    return new List<Package>();
                }

                IQueryable<Package> query;

                if (revisionId.HasValue)
                {
                    // Filter by specific revision
                    query = context.Packages
                        .Where(p => !p.IsDeleted && p.RevisionId == revisionId.Value);
                }
                else
                {
                    // Show all packages for this takeoff (across all revisions)
                    query = context.Packages
                        .Where(p => !p.IsDeleted && p.RevisionId != null)
                        .Where(p => context.TakeoffRevisions
                            .Any(r => r.Id == p.RevisionId && r.TakeoffId == takeoffId));
                }

                var packages = await query
                    .OrderBy(p => p.PackageNumber)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} packages for takeoff {TakeoffId} company {CompanyId}",
                    packages.Count, takeoffId, companyId);

                return packages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving packages for takeoff {TakeoffId} company {CompanyId}",
                    takeoffId, companyId);
                throw;
            }
        }

        public async Task<List<CustomerContact>> GetCustomerContactsAsync(int customerId, int companyId)
        {
            if (companyId <= 0)
            {
                _logger.LogWarning("Invalid companyId {CompanyId} for GetCustomerContactsAsync", companyId);
                return new List<CustomerContact>();
            }

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                // Verify customer belongs to this company
                var customerExists = await context.Customers
                    .AnyAsync(c => c.Id == customerId && c.CompanyId == companyId);

                if (!customerExists)
                {
                    _logger.LogWarning("Customer {CustomerId} not found or access denied for company {CompanyId}",
                        customerId, companyId);
                    return new List<CustomerContact>();
                }

                var contacts = await context.CustomerContacts
                    .Where(c => c.CustomerId == customerId)
                    .OrderBy(c => c.LastName)
                    .ThenBy(c => c.FirstName)
                    .ToListAsync();

                return contacts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contacts for customer {CustomerId} company {CompanyId}",
                    customerId, companyId);
                throw;
            }
        }

        public async Task<bool> DeletePackagesAsync(List<int> packageIds, int takeoffId, int companyId, int userId)
        {
            if (companyId <= 0)
            {
                _logger.LogWarning("Invalid companyId {CompanyId} for DeletePackagesAsync", companyId);
                return false;
            }

            if (!packageIds.Any())
            {
                return true; // Nothing to delete
            }

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                // First verify the takeoff belongs to this company
                var takeoffExists = await context.TraceDrawings
                    .AnyAsync(t => t.Id == takeoffId && t.CompanyId == companyId);

                if (!takeoffExists)
                {
                    _logger.LogWarning("Takeoff {TakeoffId} not found or access denied for company {CompanyId}",
                        takeoffId, companyId);
                    return false;
                }

                // Get packages that belong to this takeoff
                var packagesToDelete = await context.Packages
                    .Where(p => packageIds.Contains(p.Id) && !p.IsDeleted)
                    .Where(p => p.RevisionId != null && context.TakeoffRevisions
                        .Any(r => r.Id == p.RevisionId && r.TakeoffId == takeoffId))
                    .ToListAsync();

                if (!packagesToDelete.Any())
                {
                    _logger.LogWarning("No valid packages found to delete for takeoff {TakeoffId}", takeoffId);
                    return false;
                }

                // Soft delete the packages
                foreach (var package in packagesToDelete)
                {
                    package.IsDeleted = true;
                    package.LastModified = DateTime.UtcNow;
                }

                await context.SaveChangesAsync();

                _logger.LogInformation("Soft deleted {Count} packages for takeoff {TakeoffId} company {CompanyId} by user {UserId}",
                    packagesToDelete.Count, takeoffId, companyId, userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting packages for takeoff {TakeoffId} company {CompanyId}",
                    takeoffId, companyId);
                throw;
            }
        }
    }
}
