using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Components.Pages;

public partial class Customers : ComponentBase, IToolbarActionProvider
{
    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private List<Customer> customers = new();
    private List<Customer> filteredCustomers = new();
    private string searchTerm = "";
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadCustomers();
    }

    private async Task LoadCustomers()
    {
        try
        {
            isLoading = true;
            customers = await DbContext.Customers
                .OrderBy(c => c.Name)
                .ToListAsync();

            ApplyFilters();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading customers: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ApplyFilters()
    {
        filteredCustomers = customers;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            filteredCustomers = customers.Where(c =>
                (c.Name != null && c.Name.ToLower().Contains(searchLower)) ||
                (c.Code != null && c.Code.ToLower().Contains(searchLower)) ||
                (c.ABN != null && c.ABN.ToLower().Contains(searchLower)) ||
                (c.ContactPerson != null && c.ContactPerson.ToLower().Contains(searchLower)) ||
                (c.Email != null && c.Email.ToLower().Contains(searchLower)) ||
                (c.Industry != null && c.Industry.ToLower().Contains(searchLower))
            ).ToList();
        }
    }

    private void OnSearchChanged(string value)
    {
        searchTerm = value;
        ApplyFilters();
        StateHasChanged();
    }

    private void CreateNewCustomer()
    {
        Navigation.NavigateTo("/customers/new");
    }

    private void ViewCustomer(int customerId)
    {
        Navigation.NavigateTo($"/customers/{customerId}");
    }

    private void EditCustomer(int customerId)
    {
        Navigation.NavigateTo($"/customers/edit/{customerId}");
    }

    private async Task DeleteCustomer(Customer customer)
    {
        // In a real application, you'd want to show a confirmation dialog
        try
        {
            DbContext.Customers.Remove(customer);
            await DbContext.SaveChangesAsync();
            await LoadCustomers();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting customer: {ex.Message}");
            // In a real application, show error message to user
        }
    }

    // IToolbarActionProvider implementation
    public ToolbarActionGroup GetActions()
    {
        var group = new ToolbarActionGroup();
        group.PrimaryActions = new List<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction>
        {
            new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Label = "Add Customer",
                Text = "Add Customer",
                Icon = "fas fa-plus",
                Action = EventCallback.Factory.Create(this, CreateNewCustomer),
                IsDisabled = false,
                Style = FabOS.WebServer.Components.Shared.Interfaces.ToolbarActionStyle.Primary
            }
        };
        group.MenuActions = new List<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction>
        {
            new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Label = "Refresh",
                Text = "Refresh",
                Icon = "fas fa-sync-alt",
                Action = EventCallback.Factory.Create(this, async () => await LoadCustomers()),
                IsDisabled = false
            },
            new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Label = "Export",
                Text = "Export to Excel",
                Icon = "fas fa-file-excel",
                Action = EventCallback.Factory.Create(this, () => Console.WriteLine("Export clicked")),
                IsDisabled = false
            }
        };
        return group;
    }
}