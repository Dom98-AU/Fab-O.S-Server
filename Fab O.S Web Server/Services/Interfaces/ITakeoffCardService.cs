using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces
{
    /// <summary>
    /// Service for Takeoff card CRUD operations with proper company isolation
    /// Used by Blazor pages to abstract data access from UI layer
    /// </summary>
    public interface ITakeoffCardService
    {
        /// <summary>
        /// Get a takeoff by ID with company isolation
        /// </summary>
        Task<Takeoff?> GetTakeoffByIdAsync(int takeoffId, int companyId);

        /// <summary>
        /// Get all takeoffs for a company with optional filtering
        /// </summary>
        Task<List<Takeoff>> GetTakeoffsAsync(int companyId, TakeoffFilterOptions? filter = null);

        /// <summary>
        /// Create a new takeoff
        /// </summary>
        Task<Takeoff> CreateTakeoffAsync(Takeoff takeoff, int companyId, int userId);

        /// <summary>
        /// Update an existing takeoff
        /// </summary>
        Task<Takeoff?> UpdateTakeoffAsync(Takeoff takeoff, int companyId, int userId);

        /// <summary>
        /// Delete a takeoff (soft delete)
        /// </summary>
        Task<bool> DeleteTakeoffAsync(int takeoffId, int companyId, int userId);

        /// <summary>
        /// Get packages for a takeoff
        /// </summary>
        Task<List<Package>> GetPackagesByTakeoffAsync(int takeoffId, int companyId, int? revisionId = null);

        /// <summary>
        /// Soft delete packages by IDs
        /// </summary>
        Task<bool> DeletePackagesAsync(List<int> packageIds, int takeoffId, int companyId, int userId);

        /// <summary>
        /// Get customer contacts for a customer
        /// </summary>
        Task<List<CustomerContact>> GetCustomerContactsAsync(int customerId, int companyId);
    }

    /// <summary>
    /// Filter options for takeoff queries
    /// </summary>
    public class TakeoffFilterOptions
    {
        public string? SearchTerm { get; set; }
        public int? CustomerId { get; set; }
        public int? ProjectId { get; set; }
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageSize { get; set; } = 50;
        public int PageNumber { get; set; } = 1;
    }
}
