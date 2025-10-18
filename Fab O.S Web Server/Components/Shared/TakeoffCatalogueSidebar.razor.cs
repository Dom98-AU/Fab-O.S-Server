using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FabOS.WebServer.Components.Shared
{
    public partial class TakeoffCatalogueSidebar : ComponentBase, IAsyncDisposable
    {
        [Inject] private ITakeoffCatalogueService CatalogueService { get; set; } = default!;
        [Inject] private ILogger<TakeoffCatalogueSidebar> Logger { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        [Parameter] public bool IsVisible { get; set; } = true;
        [Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
        [Parameter] public CatalogueItem? SelectedItem { get; set; }
        [Parameter] public EventCallback<CatalogueItem?> SelectedItemChanged { get; set; }

        private bool isLoading = true;
        private bool isLoadingCategories = false;
        private bool isLoadingItems = false;
        private bool isSearching = false;
        private string? errorMessage = null;

        private List<string> materials = new();
        private string? expandedMaterial = null;
        private List<string> materialCategories = new();
        private string? expandedCategory = null;
        private List<CatalogueItem> categoryItems = new();

        private string searchTerm = string.Empty;
        private List<CatalogueItem> searchResults = new();
        private System.Timers.Timer? searchDebounceTimer;

        // Width management
        private SidebarWidth currentWidth = SidebarWidth.Default;
        private enum SidebarWidth
        {
            Default,   // 320px
            Expanded,  // 480px
            Full       // 100vw - main sidebar
        }

        private const int companyId = 1; // TODO: Get from tenant context

        private IJSObjectReference? jsModule;

        protected override async Task OnInitializedAsync()
        {
            await LoadMaterials();
        }

        protected override async Task OnParametersSetAsync()
        {
            // React to IsVisible parameter changes from parent component
            if (jsModule != null)
            {
                Logger.LogInformation("[TakeoffCatalogueSidebar] OnParametersSetAsync - IsVisible changed to: {IsVisible}", IsVisible);
                await UpdateBodyClasses();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "./js/catalogue-sidebar-helpers.js");
                    Logger.LogInformation("[TakeoffCatalogueSidebar] First render - IsVisible parameter value: {IsVisible}", IsVisible);
                    await UpdateBodyClasses();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "[TakeoffCatalogueSidebar] Error loading JS module");
                }
            }
        }

        private async Task UpdateBodyClasses()
        {
            Logger.LogInformation("[TakeoffCatalogueSidebar] UpdateBodyClasses called with IsVisible={IsVisible}, WidthClass={WidthClass}", IsVisible, GetWidthClass());

            if (jsModule != null)
            {
                try
                {
                    await jsModule.InvokeVoidAsync("updateCatalogueSidebarState", IsVisible, GetWidthClass());
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "[TakeoffCatalogueSidebar] Error updating body classes");
                }
            }
            else
            {
                Logger.LogWarning("[TakeoffCatalogueSidebar] jsModule is null, cannot update body classes");
            }
        }

        private async Task LoadMaterials()
        {
            try
            {
                isLoading = true;
                errorMessage = null;
                StateHasChanged();

                Logger.LogInformation("[TakeoffCatalogueSidebar] Loading catalogue materials");

                materials = await CatalogueService.GetMaterialsAsync(companyId);
                Logger.LogInformation("[TakeoffCatalogueSidebar] Loaded {Count} materials", materials.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[TakeoffCatalogueSidebar] Error loading materials");
                errorMessage = $"Error loading materials: {ex.Message}";
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task ToggleMaterial(string material)
        {
            if (expandedMaterial == material)
            {
                // Collapse material
                expandedMaterial = null;
                materialCategories.Clear();
                expandedCategory = null;
                categoryItems.Clear();
            }
            else
            {
                // Expand material and load its categories
                expandedMaterial = material;
                expandedCategory = null;
                categoryItems.Clear();
                await LoadMaterialCategories(material);
            }
        }

        private async Task LoadMaterialCategories(string material)
        {
            try
            {
                isLoadingCategories = true;
                materialCategories.Clear();
                StateHasChanged();

                Logger.LogInformation("[TakeoffCatalogueSidebar] Loading categories for material: {Material}", material);

                materialCategories = await CatalogueService.GetCategoriesByMaterialAsync(material, companyId);
                Logger.LogInformation("[TakeoffCatalogueSidebar] Loaded {Count} categories for material {Material}",
                    materialCategories.Count, material);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[TakeoffCatalogueSidebar] Error loading categories for material {Material}", material);
            }
            finally
            {
                isLoadingCategories = false;
                StateHasChanged();
            }
        }

        private async Task ToggleCategory(string category)
        {
            if (expandedCategory == category)
            {
                // Collapse category
                expandedCategory = null;
                categoryItems.Clear();
            }
            else
            {
                // Expand category and load items
                expandedCategory = category;
                if (!string.IsNullOrEmpty(expandedMaterial))
                {
                    await LoadCategoryItems(expandedMaterial, category);
                }
            }
        }

        private async Task LoadCategoryItems(string material, string category)
        {
            try
            {
                isLoadingItems = true;
                categoryItems.Clear();
                StateHasChanged();

                Logger.LogInformation("[TakeoffCatalogueSidebar] Loading items for material: {Material}, category: {Category}",
                    material, category);

                categoryItems = await CatalogueService.GetItemsByMaterialAndCategoryAsync(material, category, companyId);
                Logger.LogInformation("[TakeoffCatalogueSidebar] Loaded {Count} items for material {Material} and category {Category}",
                    categoryItems.Count, material, category);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[TakeoffCatalogueSidebar] Error loading items for material {Material} and category {Category}",
                    material, category);
            }
            finally
            {
                isLoadingItems = false;
                StateHasChanged();
            }
        }

        private void HandleSearchKeyup()
        {
            // Debounce search - wait 500ms after user stops typing
            searchDebounceTimer?.Stop();
            searchDebounceTimer?.Dispose();

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                isSearching = false;
                searchResults.Clear();
                StateHasChanged();
                return;
            }

            searchDebounceTimer = new System.Timers.Timer(500);
            searchDebounceTimer.Elapsed += async (sender, e) =>
            {
                await InvokeAsync(async () =>
                {
                    await PerformSearch();
                });
            };
            searchDebounceTimer.AutoReset = false;
            searchDebounceTimer.Start();
        }

        private async Task PerformSearch()
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return;
            }

            try
            {
                isSearching = true;
                searchResults.Clear();
                StateHasChanged();

                Logger.LogInformation("[TakeoffCatalogueSidebar] Searching for: {SearchTerm}", searchTerm);

                searchResults = await CatalogueService.SearchItemsAsync(searchTerm, companyId);
                Logger.LogInformation("[TakeoffCatalogueSidebar] Found {Count} results for search '{SearchTerm}'",
                    searchResults.Count, searchTerm);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[TakeoffCatalogueSidebar] Error performing search");
            }
            finally
            {
                StateHasChanged();
            }
        }

        private async Task SelectItem(CatalogueItem item)
        {
            SelectedItem = item;
            await SelectedItemChanged.InvokeAsync(item);
            Logger.LogInformation("[TakeoffCatalogueSidebar] Selected item: {ItemCode} - {Description}",
                item.ItemCode, item.Description);
        }

        private async Task ClearSelection()
        {
            SelectedItem = null;
            await SelectedItemChanged.InvokeAsync(null);
            Logger.LogInformation("[TakeoffCatalogueSidebar] Cleared item selection");
        }

        private void ClearSearch()
        {
            searchTerm = string.Empty;
            isSearching = false;
            searchResults.Clear();
            StateHasChanged();
        }

        private async Task ToggleSidebar()
        {
            Logger.LogInformation("[TakeoffCatalogueSidebar] ToggleSidebar called - Current IsVisible: {Current}", IsVisible);
            IsVisible = !IsVisible;
            Logger.LogInformation("[TakeoffCatalogueSidebar] ToggleSidebar - New IsVisible: {New}", IsVisible);
            await IsVisibleChanged.InvokeAsync(IsVisible);
            await UpdateBodyClasses();
        }

        private async Task CycleWidth()
        {
            currentWidth = currentWidth switch
            {
                SidebarWidth.Default => SidebarWidth.Expanded,
                SidebarWidth.Expanded => SidebarWidth.Full,
                SidebarWidth.Full => SidebarWidth.Default,
                _ => SidebarWidth.Default
            };
            Logger.LogInformation("[TakeoffCatalogueSidebar] Width changed to: {Width}", currentWidth);
            StateHasChanged();
            await UpdateBodyClasses();
        }

        private string GetWidthClass()
        {
            return currentWidth switch
            {
                SidebarWidth.Expanded => "width-expanded",
                SidebarWidth.Full => "width-full",
                _ => ""
            };
        }

        private string GetExpandIcon()
        {
            return currentWidth switch
            {
                SidebarWidth.Default => "fa-expand-alt",
                SidebarWidth.Expanded => "fa-expand-arrows-alt",
                SidebarWidth.Full => "fa-compress-alt",
                _ => "fa-expand-alt"
            };
        }

        public void Dispose()
        {
            searchDebounceTimer?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            Dispose();

            if (jsModule != null)
            {
                try
                {
                    // Clear body classes on disposal
                    await jsModule.InvokeVoidAsync("updateCatalogueSidebarState", false, "");
                    await jsModule.DisposeAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "[TakeoffCatalogueSidebar] Error disposing JS module");
                }
            }
        }
    }
}
