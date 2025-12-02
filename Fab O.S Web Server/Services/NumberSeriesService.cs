using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using Microsoft.Extensions.Logging;

namespace FabOS.WebServer.Services
{
    public class NumberSeriesService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NumberSeriesService> _logger;

        public NumberSeriesService(ApplicationDbContext context, ILogger<NumberSeriesService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets the next number in the series for a given entity type
        /// </summary>
        public async Task<string> GetNextNumberAsync(string entityType, int companyId = 1)
        {
            try
            {
                // Get the number series configuration
                var numberSeries = await _context.NumberSeries
                    .FirstOrDefaultAsync(ns => ns.EntityType == entityType
                        && ns.CompanyId == companyId
                        && ns.IsActive);

                if (numberSeries == null)
                {
                    // If no number series exists, create a default one
                    numberSeries = await CreateDefaultNumberSeriesAsync(entityType, companyId);
                }

                // Check if we need to reset based on year or month
                await CheckAndResetIfNeededAsync(numberSeries);

                // Generate the number
                var generatedNumber = numberSeries.GenerateNextNumber();

                // Update the current number and last used date
                numberSeries.CurrentNumber += numberSeries.IncrementBy;
                numberSeries.LastUsed = DateTime.Now;
                numberSeries.LastModified = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Generated number {generatedNumber} for entity type {entityType}");
                return generatedNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating number for entity type {entityType}");
                throw;
            }
        }

        /// <summary>
        /// Creates a default number series for an entity type
        /// </summary>
        private async Task<NumberSeries> CreateDefaultNumberSeriesAsync(string entityType, int companyId)
        {
            var prefix = entityType switch
            {
                "Takeoff" => "TO-",
                "Quote" => "QTE-",
                "Estimation" => "EST-",
                "Order" => "ORD-",
                "WorkPackage" => "WP-",
                "WorkOrder" => "WO-",
                "Customer" => "CUS-",
                _ => $"{entityType.Substring(0, Math.Min(3, entityType.Length)).ToUpper()}-"
            };

            var numberSeries = new NumberSeries
            {
                CompanyId = companyId,
                EntityType = entityType,
                Prefix = prefix,
                CurrentNumber = 1,
                StartingNumber = 1,
                IncrementBy = 1,
                MinDigits = 4,
                IncludeYear = true,
                IncludeMonth = false,
                ResetYearly = true,
                IsActive = true,
                AllowManualEntry = false,
                Description = $"Default number series for {entityType}",
                CreatedDate = DateTime.Now,
                LastModified = DateTime.Now,
                LastUsed = DateTime.Now
            };

            _context.NumberSeries.Add(numberSeries);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created default number series for entity type {entityType}");
            return numberSeries;
        }

        /// <summary>
        /// Checks if the number series needs to be reset based on year/month
        /// </summary>
        private async Task CheckAndResetIfNeededAsync(NumberSeries numberSeries)
        {
            var now = DateTime.Now;
            bool needsReset = false;

            if (numberSeries.ResetYearly)
            {
                if (!numberSeries.LastResetYear.HasValue || numberSeries.LastResetYear.Value < now.Year)
                {
                    needsReset = true;
                    numberSeries.LastResetYear = now.Year;
                }
            }

            if (numberSeries.ResetMonthly)
            {
                if (!numberSeries.LastResetMonth.HasValue ||
                    numberSeries.LastResetMonth.Value < now.Month ||
                    (numberSeries.LastResetYear.HasValue && numberSeries.LastResetYear.Value < now.Year))
                {
                    needsReset = true;
                    numberSeries.LastResetMonth = now.Month;
                    numberSeries.LastResetYear = now.Year;
                }
            }

            if (needsReset)
            {
                numberSeries.CurrentNumber = numberSeries.StartingNumber;
                _logger.LogInformation($"Reset number series for {numberSeries.EntityType} to {numberSeries.StartingNumber}");
            }
        }

        /// <summary>
        /// Updates number series configuration
        /// </summary>
        public async Task<NumberSeries> UpdateNumberSeriesAsync(NumberSeries updatedSeries)
        {
            var existingSeries = await _context.NumberSeries
                .FirstOrDefaultAsync(ns => ns.Id == updatedSeries.Id);

            if (existingSeries == null)
            {
                throw new InvalidOperationException($"Number series with ID {updatedSeries.Id} not found");
            }

            // Update properties
            existingSeries.Prefix = updatedSeries.Prefix;
            existingSeries.Suffix = updatedSeries.Suffix;
            existingSeries.MinDigits = updatedSeries.MinDigits;
            existingSeries.IncludeYear = updatedSeries.IncludeYear;
            existingSeries.IncludeMonth = updatedSeries.IncludeMonth;
            existingSeries.IncludeCompanyCode = updatedSeries.IncludeCompanyCode;
            existingSeries.ResetYearly = updatedSeries.ResetYearly;
            existingSeries.ResetMonthly = updatedSeries.ResetMonthly;
            existingSeries.AllowManualEntry = updatedSeries.AllowManualEntry;
            existingSeries.Description = updatedSeries.Description;
            existingSeries.Format = updatedSeries.Format;
            existingSeries.LastModified = DateTime.Now;
            existingSeries.LastModifiedByUserId = updatedSeries.LastModifiedByUserId;

            // Generate preview
            existingSeries.PreviewExample = existingSeries.GenerateNextNumber();

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated number series for entity type {existingSeries.EntityType}");
            return existingSeries;
        }

        /// <summary>
        /// Gets number series configuration for an entity type
        /// </summary>
        public async Task<NumberSeries?> GetNumberSeriesAsync(string entityType, int companyId = 1)
        {
            return await _context.NumberSeries
                .FirstOrDefaultAsync(ns => ns.EntityType == entityType
                    && ns.CompanyId == companyId
                    && ns.IsActive);
        }

        /// <summary>
        /// Generates a preview of what the next number will look like
        /// </summary>
        public async Task<string> GeneratePreviewAsync(string entityType, int companyId = 1)
        {
            var numberSeries = await GetNumberSeriesAsync(entityType, companyId);

            if (numberSeries == null)
            {
                numberSeries = await CreateDefaultNumberSeriesAsync(entityType, companyId);
            }

            return numberSeries.GenerateNextNumber();
        }

        /// <summary>
        /// Validates if a manually entered number is valid and available
        /// </summary>
        public async Task<bool> ValidateManualNumberAsync(string entityType, string manualNumber, int companyId = 1)
        {
            var numberSeries = await GetNumberSeriesAsync(entityType, companyId);

            if (numberSeries == null || !numberSeries.AllowManualEntry)
            {
                return false;
            }

            // Check if the number already exists in the relevant table
            bool exists = entityType switch
            {
                "Takeoff" => await _context.TraceDrawings.AnyAsync(td => td.TakeoffNumber == manualNumber),
                "Customer" => await _context.Customers.AnyAsync(c => c.Code == manualNumber),
                _ => false
            };

            return !exists;
        }
    }
}