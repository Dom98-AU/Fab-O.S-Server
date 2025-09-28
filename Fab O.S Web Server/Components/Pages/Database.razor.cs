using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Components.Pages;

public partial class Database : ComponentBase
{
    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;

    private bool loading = true;
    private string? errorMessage;
    private List<Company> companies = new();
    private List<User> users = new();
    private List<Project> projects = new();
    private int machineCenters = 0;
    private int workCenters = 0;
    private int traceDrawings = 0;
    private int weldingConnections = 0;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            loading = true;

            // Load companies with related data
            companies = await DbContext.Companies
                .Include(c => c.Users)
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Load users with related data
            users = await DbContext.Users
                .Include(u => u.Company)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();

            // Load projects with related data
            projects = await DbContext.Projects
                .Include(p => p.Owner)
                .Include(p => p.Packages)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            // Get counts for manufacturing entities
            machineCenters = await DbContext.MachineCenters.CountAsync();
            workCenters = await DbContext.WorkCenters.CountAsync();
            traceDrawings = await DbContext.TraceDrawings.CountAsync();
            weldingConnections = await DbContext.WeldingConnections.CountAsync();
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load data: {ex.Message}";
        }
        finally
        {
            loading = false;
        }
    }
}