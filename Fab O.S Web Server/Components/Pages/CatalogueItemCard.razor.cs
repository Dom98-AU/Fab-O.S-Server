using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Components.Shared;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace FabOS.WebServer.Components.Pages
{
    public partial class CatalogueItemCard : ComponentBase, IToolbarActionProvider
    {
        [Parameter]
        public string TenantSlug { get; set; } = string.Empty;

        [Parameter]
        public int CatalogueId { get; set; }

        [Parameter]
        public int ItemId { get; set; }

        [Inject] private ICatalogueService CatalogueService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private ILogger<CatalogueItemCard> Logger { get; set; } = default!;

        private bool isLoading = true;
        private string errorMessage = string.Empty;
        private CatalogueItem? catalogueItem;
        private Catalogue? catalogue;
        private int companyId;
        private int currentUserId;

        // Section collapse management
        private Dictionary<string, bool> sectionStates = new Dictionary<string, bool>
        {
            { "general", true },
            { "dimensions", true },
            { "properties", false },
            { "audit", false }
        };

        protected override async Task OnInitializedAsync()
        {
            // Get current user ID and company ID from authentication
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = user.FindFirst("UserId") ?? user.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                {
                    currentUserId = userId;
                }

                var companyIdClaim = user.FindFirst("CompanyId");
                if (companyIdClaim != null && int.TryParse(companyIdClaim.Value, out var compId))
                {
                    companyId = compId;
                }
            }

            if (currentUserId == 0 || companyId == 0)
            {
                Logger.LogWarning("User is not authenticated or missing required claims");
                errorMessage = "User is not authenticated. Please log in and try again.";
                isLoading = false;
                return;
            }

            await LoadData();
        }

        private async Task LoadData()
        {
            isLoading = true;
            errorMessage = string.Empty;

            try
            {
                // Load the catalogue item
                catalogueItem = await DbContext.CatalogueItems
                    .Where(ci => ci.Id == ItemId && ci.CatalogueId == CatalogueId && ci.CompanyId == companyId)
                    .FirstOrDefaultAsync();

                if (catalogueItem == null)
                {
                    errorMessage = "Catalogue item not found.";
                    return;
                }

                // Load the catalogue
                catalogue = await CatalogueService.GetCatalogueByIdAsync(CatalogueId, companyId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading catalogue item {ItemId} from catalogue {CatalogueId}", ItemId, CatalogueId);
                errorMessage = "Failed to load catalogue item details.";
            }
            finally
            {
                isLoading = false;
            }
        }

        private bool IsSectionExpanded(string sectionKey)
        {
            return sectionStates.ContainsKey(sectionKey) && sectionStates[sectionKey];
        }

        private void ToggleSection(string sectionKey)
        {
            if (sectionStates.ContainsKey(sectionKey))
            {
                sectionStates[sectionKey] = !sectionStates[sectionKey];
            }
            else
            {
                sectionStates[sectionKey] = true;
            }
        }

        private void GoBack()
        {
            Navigation.NavigateTo($"/{TenantSlug}/trace/catalogues/{CatalogueId}");
        }

        private void EditItem()
        {
            Navigation.NavigateTo($"/{TenantSlug}/trace/catalogues/{CatalogueId}/items/{ItemId}/edit");
        }

        private async Task DeleteItem()
        {
            // TODO: Implement confirmation dialog
            try
            {
                var success = await CatalogueService.DeleteCatalogueItemAsync(ItemId, companyId);
                if (success)
                {
                    Logger.LogInformation("Catalogue item {ItemId} deleted successfully by user {UserId}", ItemId, currentUserId);
                    Navigation.NavigateTo($"/{TenantSlug}/trace/catalogues/{CatalogueId}");
                }
                else
                {
                    errorMessage = "Failed to delete catalogue item.";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting catalogue item {ItemId}", ItemId);
                errorMessage = "An error occurred while deleting the item.";
            }
        }

        // IToolbarActionProvider implementation
        public ToolbarActionGroup GetActions()
        {
            var group = new ToolbarActionGroup();

            // Back button
            group.PrimaryActions.Add(new ToolbarAction
            {
                Label = "Back",
                Text = "Back to Catalogue",
                Icon = "fas fa-arrow-left",
                ActionFunc = async () => { GoBack(); await Task.CompletedTask; },
                Style = ToolbarActionStyle.Secondary
            });

            // Only show Edit/Delete if not a system catalogue
            if (catalogue != null && !catalogue.IsSystemCatalogue)
            {
                group.PrimaryActions.Add(new ToolbarAction
                {
                    Label = "Edit",
                    Text = "Edit",
                    Icon = "fas fa-edit",
                    ActionFunc = async () => { EditItem(); await Task.CompletedTask; },
                    Style = ToolbarActionStyle.Primary
                });

                group.MenuActions.Add(new ToolbarAction
                {
                    Label = "Delete",
                    Text = "Delete",
                    Icon = "fas fa-trash",
                    ActionFunc = DeleteItem,
                    Style = ToolbarActionStyle.Danger
                });
            }

            return group;
        }
    }
}
