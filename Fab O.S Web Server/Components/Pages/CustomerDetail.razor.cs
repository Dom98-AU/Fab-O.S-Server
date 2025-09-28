using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Components.Pages;

public partial class CustomerDetail : ComponentBase, IToolbarActionProvider
{
    [Parameter] public int Id { get; set; }

    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private Customer? customer;
    private CustomerAddress? primaryAddress;
    private CustomerContact? primaryContact;
    private List<TraceDrawing>? relatedDrawings;
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadCustomerDetails();
    }

    private async Task LoadCustomerDetails()
    {
        try
        {
            isLoading = true;

            // Load customer with related data
            customer = await DbContext.Customers
                .Include(c => c.Contacts)
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.Id == Id);

            if (customer != null)
            {
                // Get primary address
                primaryAddress = customer.Addresses?.FirstOrDefault(a => a.IsPrimary)
                                ?? customer.Addresses?.FirstOrDefault();

                // Get primary contact
                primaryContact = customer.Contacts?.FirstOrDefault(c => c.IsPrimary)
                                ?? customer.Contacts?.FirstOrDefault();

                // Load related trace drawings
                relatedDrawings = await DbContext.TraceDrawings
                    .Where(td => td.CustomerId == Id)
                    .Include(td => td.Project)
                    .OrderByDescending(td => td.UploadDate)
                    .ToListAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading customer details: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private void EditCustomer()
    {
        Navigation.NavigateTo($"/customers/edit/{Id}");
    }

    private void NavigateBack()
    {
        Navigation.NavigateTo("/customers");
    }

    private void ViewDrawing(int drawingId)
    {
        Navigation.NavigateTo($"/trace-drawings/{drawingId}");
    }

    private void ViewAllDrawings()
    {
        Navigation.NavigateTo($"/trace-drawings?customerId={Id}");
    }

    private void ViewOnMap(CustomerAddress address)
    {
        if (!string.IsNullOrEmpty(address.FormattedAddress))
        {
            var mapUrl = $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(address.FormattedAddress)}";
            Navigation.NavigateTo(mapUrl, forceLoad: true);
        }
        else if (address.Latitude.HasValue && address.Longitude.HasValue)
        {
            var mapUrl = $"https://www.google.com/maps/@{address.Latitude},{address.Longitude},17z";
            Navigation.NavigateTo(mapUrl, forceLoad: true);
        }
    }

    private async Task DeleteCustomer()
    {
        if (customer != null)
        {
            try
            {
                DbContext.Customers.Remove(customer);
                await DbContext.SaveChangesAsync();
                Navigation.NavigateTo("/customers");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting customer: {ex.Message}");
            }
        }
    }

    // IToolbarActionProvider implementation
    public ToolbarActionGroup GetActions()
    {
        var group = new ToolbarActionGroup();
        group.PrimaryActions = new List<ToolbarAction>
        {
            new ToolbarAction
            {
                Label = "Edit",
                Text = "Edit Customer",
                Icon = "fas fa-edit",
                Action = EventCallback.Factory.Create(this, EditCustomer),
                IsDisabled = customer == null,
                Style = ToolbarActionStyle.Primary
            }
        };
        group.MenuActions = new List<ToolbarAction>
        {
            new ToolbarAction
            {
                Label = "Delete",
                Text = "Delete Customer",
                Icon = "fas fa-trash",
                Action = EventCallback.Factory.Create(this, async () => await DeleteCustomer()),
                IsDisabled = customer == null,
                Style = ToolbarActionStyle.Danger
            },
            new ToolbarAction
            {
                Label = "Back",
                Text = "Back to List",
                Icon = "fas fa-arrow-left",
                Action = EventCallback.Factory.Create(this, NavigateBack),
                IsDisabled = false
            }
        };
        return group;
    }
}