using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FabOS.WebServer.Components.Pages
{
    public partial class CatalogueCard : ComponentBase
    {
        [Parameter]
        public string TenantSlug { get; set; } = string.Empty;

        [Parameter]
        public int CatalogueId { get; set; }

        [Inject] private ICatalogueService CatalogueService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private ApplicationDbContext DbContext { get; set; } = default!;

        private bool isLoading = true;
        private Catalogue? catalogue;
        private List<CatalogueItem> allItems = new();
        private List<CatalogueItem> filteredItems = new();
        private string activeTab = "details";
        private string itemSearchTerm = string.Empty;
        private int itemCount = 0;
        private int companyId;

        protected override async Task OnInitializedAsync()
        {
            // Get companyId from database based on tenant slug
            // Tenant slug is derived from Company.Code (lowercase with hyphens instead of spaces)
            var normalizedSlug = TenantSlug.Replace("-", " ");
            var company = await DbContext.Companies
                .FirstOrDefaultAsync(c => c.Code.ToLower() == normalizedSlug.ToLower() ||
                                         c.Code.ToLower().Replace(" ", "-") == TenantSlug.ToLower());

            if (company == null)
            {
                // Handle invalid tenant
                return;
            }

            companyId = company.Id;
            await LoadCatalogue();
        }

        private async Task LoadCatalogue()
        {
            isLoading = true;

            try
            {
                catalogue = await CatalogueService.GetCatalogueByIdAsync(CatalogueId, companyId);

                if (catalogue != null)
                {
                    allItems = await CatalogueService.GetItemsByCatalogueAsync(CatalogueId, companyId);
                    filteredItems = allItems;
                    itemCount = allItems.Count;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading catalogue: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        private void FilterItems()
        {
            if (string.IsNullOrWhiteSpace(itemSearchTerm))
            {
                filteredItems = allItems;
            }
            else
            {
                var searchLower = itemSearchTerm.ToLower();
                filteredItems = allItems.Where(item =>
                    item.ItemCode.ToLower().Contains(searchLower) ||
                    item.Description.ToLower().Contains(searchLower) ||
                    (item.Category?.ToLower().Contains(searchLower) ?? false) ||
                    (item.Material?.ToLower().Contains(searchLower) ?? false)
                ).ToList();
            }

            StateHasChanged();
        }

        private void ViewAllItems()
        {
            Navigation.NavigateTo($"/{TenantSlug}/trace/catalogues/{CatalogueId}/items");
        }

        private void ViewItem(int itemId)
        {
            Navigation.NavigateTo($"/{TenantSlug}/trace/catalogues/{CatalogueId}/items/{itemId}");
        }

        private void EditCatalogue()
        {
            Navigation.NavigateTo($"/{TenantSlug}/trace/catalogues/{CatalogueId}/edit");
        }

        private async Task DeleteCatalogue()
        {
            // TODO: Implement confirmation dialog
            var userId = 1; // TODO: Get from auth context

            var success = await CatalogueService.DeleteCatalogueAsync(CatalogueId, companyId, userId);
            if (success)
            {
                Navigation.NavigateTo($"/{TenantSlug}/trace/catalogues");
            }
        }

        private void GoBack()
        {
            Navigation.NavigateTo($"/{TenantSlug}/trace/catalogues");
        }
    }
}
