using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Data.Seeders;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(ApplicationDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("[DatabaseSeeder] Starting database seeding...");

            // Ensure database is created and migrations are applied
            await _context.Database.MigrateAsync();
            _logger.LogInformation("[DatabaseSeeder] Database migrations applied");

            // Seed default company if none exists
            await SeedDefaultCompanyAsync();

            _logger.LogInformation("[DatabaseSeeder] Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DatabaseSeeder] Error during database seeding");
            throw;
        }
    }

    private async Task SeedDefaultCompanyAsync()
    {
        try
        {
            // Check if any companies exist
            var hasCompanies = await _context.Companies.AnyAsync();

            if (!hasCompanies)
            {
                _logger.LogInformation("[DatabaseSeeder] No companies found. Creating default company...");

                var defaultCompany = new Company
                {
                    Name = "Steel Estimation Platform",
                    Code = "DEFAULT",
                    ShortName = null,
                    IsActive = true,
                    SubscriptionLevel = "Standard",
                    MaxUsers = 10,
                    CreatedDate = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    Domain = null
                };

                _context.Companies.Add(defaultCompany);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[DatabaseSeeder] Default company 'Steel Estimation Platform' created with ID: {CompanyId}", defaultCompany.Id);
            }
            else
            {
                _logger.LogInformation("[DatabaseSeeder] Companies already exist. Skipping company seeding.");

                // Log existing companies for debugging
                var companies = await _context.Companies.Select(c => new { c.Id, c.Name, c.Code }).ToListAsync();
                foreach (var company in companies)
                {
                    _logger.LogInformation("[DatabaseSeeder] Found company: ID={CompanyId}, Name={Name}, Code={Code}",
                        company.Id, company.Name, company.Code);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DatabaseSeeder] Error seeding default company");
            throw;
        }
    }
}
