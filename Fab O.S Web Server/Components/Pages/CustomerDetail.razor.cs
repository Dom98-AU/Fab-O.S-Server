using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.AspNetCore.WebUtilities;

namespace FabOS.WebServer.Components.Pages;

public partial class CustomerDetail : ComponentBase, IToolbarActionProvider, IDisposable
{
    [Parameter] public string TenantSlug { get; set; } = string.Empty;
    [Parameter] public int Id { get; set; }

    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IAbnLookupService AbnLookupService { get; set; } = default!;
    [Inject] private NumberSeriesService NumberSeriesService { get; set; } = default!;

    private Customer? customer;
    private CustomerAddress? primaryAddress;
    private CustomerContact? primaryContact;
    private List<Takeoff>? relatedDrawings;
    private bool isLoading = false;
    private bool isEditMode = false;
    private bool isNewCustomer = false;
    private string? returnUrl;
    private string breadcrumb = "Customers";

    // Unsaved changes tracking
    private bool hasUnsavedChanges = false;
    private bool showUnsavedChangesDialog = false;
    private string? pendingNavigationUrl = null;
    private IDisposable? navigationInterceptor;

    // Original values for change detection
    private string originalName = "";
    private string? originalCode = null;
    private string? originalABN = null;
    private string? originalIndustry = null;
    private string? originalWebsite = null;
    private string? originalNotes = null;

    // Section collapse management
    private Dictionary<string, bool> sectionStates = new Dictionary<string, bool>
    {
        { "general", true },      // Customer Information - expanded by default
        { "contact", true },       // Contact & Location - expanded by default
        { "related", false },      // Related Drawings - collapsed
        { "activity", false }      // Activity - collapsed
    };

    // ABN Lookup
    private bool showAbnLookupModal = false;
    private string searchMode = "abn"; // "abn" or "name"
    private string abnSearchTerm = "";
    private string nameSearchTerm = "";
    private string stateFilter = "";
    private string postcodeFilter = "";
    private bool isSearching = false;
    private string? abnErrorMessage = null;
    private AbnSearchResult? abnSearchResult = null;
    private List<AbnSearchResult>? nameSearchResults = null;

    // Customer code generation tracking
    private bool customerCodeGenerated = false;

    // Save operation guard to prevent concurrent saves
    private bool isSaving = false;

    protected override async Task OnInitializedAsync()
    {
        // Register navigation interception
        navigationInterceptor = Navigation.RegisterLocationChangingHandler(OnLocationChanging);

        // Get the return URL from query parameters
        var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
        if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("returnUrl", out var returnUrlValue))
        {
            returnUrl = System.Net.WebUtility.UrlDecode(returnUrlValue.ToString());
        }

        // Check if this is a new customer (Id = 0)
        isNewCustomer = (Id == 0);
        isEditMode = isNewCustomer;

        if (isNewCustomer)
        {
            // Create a new customer object
            customer = new Customer
            {
                IsActive = true,
                CreatedDate = DateTime.Now,
                LastModified = DateTime.Now
            };

            // Generate customer code for new customer
            if (!customerCodeGenerated)
            {
                customer.Code = await NumberSeriesService.GetNextNumberAsync("Customer", 1);
                customerCodeGenerated = true;
            }

            // Set loading to false for new customer
            isLoading = false;

            // Force state change to ensure toolbar updates
            StateHasChanged();

            // Ensure toolbar re-renders after initialization by invoking another state change
            await InvokeAsync(StateHasChanged);
        }
        else
        {
            await LoadCustomerDetails();
        }
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
                // Store original values for change detection
                StoreOriginalValues();

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

    private void StoreOriginalValues()
    {
        if (customer == null) return;

        originalName = customer.Name;
        originalCode = customer.Code;
        originalABN = customer.ABN;
        originalIndustry = customer.Industry;
        originalWebsite = customer.Website;
        originalNotes = customer.Notes;
    }

    private void OnFieldChanged()
    {
        if (customer == null) return;

        // Check if any field has changed
        hasUnsavedChanges = customer.Name != originalName ||
                           customer.Code != originalCode ||
                           customer.ABN != originalABN ||
                           customer.Industry != originalIndustry ||
                           customer.Website != originalWebsite ||
                           customer.Notes != originalNotes;
    }

    private void OnNameChanged(ChangeEventArgs e)
    {
        if (customer == null) return;

        customer.Name = e.Value?.ToString() ?? "";
        OnFieldChanged();
        StateHasChanged(); // Re-render to update Save button state
    }

    private async ValueTask OnLocationChanging(LocationChangingContext context)
    {
        // If no unsaved changes, allow navigation
        if (!hasUnsavedChanges)
        {
            return;
        }

        // Prevent the navigation
        context.PreventNavigation();

        // Store the pending URL
        pendingNavigationUrl = context.TargetLocation;

        // Show confirmation dialog
        showUnsavedChangesDialog = true;
        StateHasChanged();

        await Task.CompletedTask;
    }

    private void StayOnPage()
    {
        showUnsavedChangesDialog = false;
        pendingNavigationUrl = null;
        StateHasChanged();
    }

    private void LeaveWithoutSaving()
    {
        // Clear the flag and navigate
        hasUnsavedChanges = false;
        showUnsavedChangesDialog = false;

        if (!string.IsNullOrEmpty(pendingNavigationUrl))
        {
            Navigation.NavigateTo(pendingNavigationUrl);
        }
    }

    private async Task SaveCustomer()
    {
        Console.WriteLine("===== SaveCustomer method called =====");

        // Guard against concurrent saves
        if (isSaving)
        {
            Console.WriteLine("Save already in progress, skipping duplicate save attempt");
            return;
        }

        if (customer == null)
        {
            Console.WriteLine("Customer is null, cannot save");
            return;
        }

        // Validate customer name
        if (string.IsNullOrWhiteSpace(customer.Name))
        {
            Console.WriteLine("ERROR: Customer name is required but is empty");
            return;
        }

        try
        {
            isSaving = true;
            Console.WriteLine($"Saving customer: Name='{customer.Name}', Code='{customer.Code}', ABN='{customer.ABN}', IsNew={isNewCustomer}");
            Console.WriteLine($"Customer entity state - CompanyId={customer.CompanyId}, CompanyName='{customer.CompanyName}', CreatedById={customer.CreatedById}");

            if (isNewCustomer)
            {
                // Add new customer
                Console.WriteLine("Adding new customer to DbContext");
                DbContext.Customers.Add(customer);
            }
            else
            {
                // Update existing customer
                customer.LastModified = DateTime.Now;
                DbContext.Customers.Update(customer);
            }

            Console.WriteLine("Calling SaveChangesAsync...");
            await DbContext.SaveChangesAsync();
            Console.WriteLine("SaveChangesAsync completed successfully!");

            // Clear unsaved changes flag
            hasUnsavedChanges = false;

            // Navigate back to return URL or customers list
            Console.WriteLine($"Navigating back to: {returnUrl ?? $"/{TenantSlug}/trace/customers"}");
            NavigateBack();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"===== ERROR SAVING CUSTOMER =====");
            Console.WriteLine($"Error message: {ex.Message}");
            Console.WriteLine($"Error type: {ex.GetType().Name}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                Console.WriteLine($"Inner exception type: {ex.InnerException.GetType().Name}");
            }
            Console.WriteLine($"===== END ERROR =====");
        }
        finally
        {
            isSaving = false;
        }
    }

    private Task CancelEdit()
    {
        if (hasUnsavedChanges)
        {
            // Show warning if there are unsaved changes
            pendingNavigationUrl = returnUrl ?? $"/{TenantSlug}/trace/customers";
            showUnsavedChangesDialog = true;
            StateHasChanged();
        }
        else
        {
            NavigateBack();
        }
        return Task.CompletedTask;
    }

    private void EditCustomer()
    {
        isEditMode = true;
        StateHasChanged();
    }

    private void NavigateBack()
    {
        if (!string.IsNullOrEmpty(returnUrl))
        {
            Navigation.NavigateTo(returnUrl);
        }
        else
        {
            Navigation.NavigateTo($"/{TenantSlug}/trace/customers");
        }
    }

    private void ViewDrawing(int drawingId)
    {
        Navigation.NavigateTo($"/{TenantSlug}/trace/takeoffs/{drawingId}");
    }

    private void ViewAllDrawings()
    {
        Navigation.NavigateTo($"/{TenantSlug}/trace/takeoffs?customerId={Id}");
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
                hasUnsavedChanges = false; // Clear flag before navigation
                Navigation.NavigateTo($"/{TenantSlug}/trace/customers");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting customer: {ex.Message}");
            }
        }
    }

    private void ToggleSection(string sectionName)
    {
        if (sectionStates.ContainsKey(sectionName))
        {
            sectionStates[sectionName] = !sectionStates[sectionName];
            StateHasChanged();
        }
    }

    private bool IsSectionExpanded(string sectionName)
    {
        return sectionStates.ContainsKey(sectionName) && sectionStates[sectionName];
    }

    // IToolbarActionProvider implementation
    public ToolbarActionGroup GetActions()
    {
        var group = new ToolbarActionGroup();

        // PRIMARY ACTIONS - [Back] [Edit/Save]
        group.PrimaryActions = new List<ToolbarAction>
        {
            new ToolbarAction
            {
                Label = "Back",
                Text = "Back",
                Icon = "fas fa-arrow-left",
                Tooltip = "Return to customers list",
                Action = EventCallback.Factory.Create(this, NavigateBack),
                Style = ToolbarActionStyle.Secondary
            },
            new ToolbarAction
            {
                Label = isEditMode ? "Save" : "Edit",
                Text = isEditMode ? "Save" : "Edit",
                Icon = isEditMode ? "fas fa-save" : "fas fa-edit",
                Tooltip = isEditMode ? "Save changes" : "Edit customer details",
                Action = isEditMode
                    ? EventCallback.Factory.Create(this, SaveCustomer)
                    : EventCallback.Factory.Create(this, EditCustomer),
                IsDisabled = customer == null,
                Style = ToolbarActionStyle.Primary
            }
        };

        // MENU ACTIONS - [Delete] (only for existing customers)
        if (!isNewCustomer && customer != null)
        {
            group.MenuActions = new List<ToolbarAction>
            {
                new ToolbarAction
                {
                    Label = "Delete",
                    Text = "Delete",
                    Icon = "fas fa-trash",
                    Tooltip = "Delete this customer",
                    Action = EventCallback.Factory.Create(this, async () => await DeleteCustomer()),
                    Style = ToolbarActionStyle.Danger
                }
            };
        }

        // RELATED ACTIONS - [Contacts] (only for existing customers)
        if (!isNewCustomer && customer != null)
        {
            group.RelatedActions = new List<ToolbarAction>
            {
                new ToolbarAction
                {
                    Label = "Contacts",
                    Text = "Contacts",
                    Icon = "fas fa-address-book",
                    Tooltip = "View customer contacts",
                    Action = EventCallback.Factory.Create(this, NavigateToContacts)
                }
            };
        }

        return group;
    }

    private void NavigateToContacts()
    {
        Navigation.NavigateTo($"/{TenantSlug}/trace/contacts?customerId={Id}");
    }

    // ABN Lookup Methods
    private void ShowAbnLookupModal()
    {
        showAbnLookupModal = true;
        searchMode = "abn";
        abnSearchTerm = customer?.ABN ?? "";
        nameSearchTerm = "";
        stateFilter = "";
        postcodeFilter = "";
        abnSearchResult = null;
        nameSearchResults = null;
        abnErrorMessage = null;
        StateHasChanged();
    }

    private void CloseAbnLookupModal()
    {
        showAbnLookupModal = false;
        searchMode = "abn";
        abnSearchTerm = "";
        nameSearchTerm = "";
        stateFilter = "";
        postcodeFilter = "";
        abnSearchResult = null;
        nameSearchResults = null;
        abnErrorMessage = null;
        StateHasChanged();
    }

    private void SetSearchMode(string mode)
    {
        searchMode = mode;
        abnErrorMessage = null;
        abnSearchResult = null;
        nameSearchResults = null;
        StateHasChanged();
    }

    private void SetAbnSearchMode()
    {
        SetSearchMode("abn");
    }

    private void SetNameSearchMode()
    {
        SetSearchMode("name");
    }

    private async Task HandleAbnSearchKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(abnSearchTerm))
        {
            await SearchByAbn();
        }
    }

    private async Task HandleNameSearchKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(nameSearchTerm))
        {
            await SearchByName();
        }
    }

    private async Task SearchByAbn()
    {
        if (string.IsNullOrWhiteSpace(abnSearchTerm))
            return;

        try
        {
            isSearching = true;
            abnErrorMessage = null;
            abnSearchResult = null;
            StateHasChanged();

            var result = await AbnLookupService.SearchByAbnAsync(abnSearchTerm);

            if (result == null)
            {
                abnErrorMessage = "No business found with this ABN. Please check the number and try again.";
            }
            else if (result.AbnStatus != "Active")
            {
                abnErrorMessage = $"This ABN is {result.AbnStatus}. You can still use this information, but please verify the business details.";
                abnSearchResult = result;
            }
            else
            {
                abnSearchResult = result;
            }
        }
        catch (Exception ex)
        {
            abnErrorMessage = $"Error searching ABN: {ex.Message}";
            Console.WriteLine($"ABN lookup error: {ex}");
        }
        finally
        {
            isSearching = false;
            StateHasChanged();
        }
    }

    private async Task SearchByName()
    {
        if (string.IsNullOrWhiteSpace(nameSearchTerm))
            return;

        try
        {
            isSearching = true;
            abnErrorMessage = null;
            nameSearchResults = null;
            StateHasChanged();

            var results = await AbnLookupService.SearchByNameAsync(
                nameSearchTerm,
                string.IsNullOrEmpty(stateFilter) ? null : stateFilter,
                string.IsNullOrEmpty(postcodeFilter) ? null : postcodeFilter
            );

            if (results == null || !results.Any())
            {
                abnErrorMessage = "No businesses found matching your search criteria. Try different search terms or remove filters.";
            }
            else
            {
                nameSearchResults = results;
            }
        }
        catch (Exception ex)
        {
            abnErrorMessage = $"Error searching by name: {ex.Message}";
            Console.WriteLine($"Name search error: {ex}");
        }
        finally
        {
            isSearching = false;
            StateHasChanged();
        }
    }

    private void SelectResult(AbnSearchResult result)
    {
        if (result == null || customer == null)
            return;

        // Populate customer fields from selected result
        customer.Name = result.DisplayName;
        customer.ABN = result.Abn;

        // Mark as changed
        OnFieldChanged();

        // Close modal
        CloseAbnLookupModal();
        StateHasChanged();
    }

    public void Dispose()
    {
        // Unregister navigation interception
        navigationInterceptor?.Dispose();
    }
}
