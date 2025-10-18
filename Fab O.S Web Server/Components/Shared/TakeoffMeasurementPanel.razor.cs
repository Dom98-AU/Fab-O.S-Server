using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.AspNetCore.Components;

namespace FabOS.WebServer.Components.Shared
{
    public partial class TakeoffMeasurementPanel : ComponentBase
    {
        [Inject] private ITakeoffCatalogueService CatalogueService { get; set; } = default!;
        [Inject] private ILogger<TakeoffMeasurementPanel> Logger { get; set; } = default!;

        [Parameter] public int PackageDrawingId { get; set; }
        [Parameter] public bool IsVisible { get; set; } = true;
        [Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
        [Parameter] public EventCallback OnMeasurementSaved { get; set; }

        // Current measurement (being created/edited)
        public MeasurementCalculationResult? CurrentMeasurement { get; set; }

        private List<TraceTakeoffMeasurement> measurements = new();
        private bool isLoadingMeasurements = false;
        private string? errorMessage = null;
        private string? expandedCategory = null;

        // Height management
        private FooterHeight currentHeight = FooterHeight.Default;
        private enum FooterHeight
        {
            Default,   // 200px
            Expanded,  // 350px
            Full       // 60vh
        }

        private const int companyId = 1; // TODO: Get from tenant context

        private decimal TotalWeight => measurements.Sum(m => m.CalculatedWeight ?? 0);

        private Dictionary<string, List<TraceTakeoffMeasurement>> GroupedMeasurements =>
            measurements
                .Where(m => m.CatalogueItem != null)
                .GroupBy(m => m.CatalogueItem!.Category)
                .ToDictionary(g => g.Key, g => g.ToList());

        protected override async Task OnParametersSetAsync()
        {
            if (PackageDrawingId > 0)
            {
                await LoadMeasurements();
            }
        }

        /// <summary>
        /// Called from JavaScript when a measurement is completed
        /// </summary>
        public async Task ShowMeasurementResult(MeasurementCalculationResult result)
        {
            CurrentMeasurement = result;
            Logger.LogInformation("[TakeoffMeasurementPanel] ============================================");
            Logger.LogInformation("[TakeoffMeasurementPanel] MEASUREMENT RESULT RECEIVED:");
            Logger.LogInformation("[TakeoffMeasurementPanel] ItemCode: {ItemCode}", result.ItemCode);
            Logger.LogInformation("[TakeoffMeasurementPanel] Description: {Description}", result.Description);
            Logger.LogInformation("[TakeoffMeasurementPanel] MeasurementValue: {Value} {Unit}", result.MeasurementValue, result.Unit);
            Logger.LogInformation("[TakeoffMeasurementPanel] Quantity: {Quantity} {QuantityUnit}", result.Quantity, result.QuantityUnit);
            Logger.LogInformation("[TakeoffMeasurementPanel] Weight: {Weight} {WeightUnit}", result.Weight, result.WeightUnit);
            Logger.LogInformation("[TakeoffMeasurementPanel] Panel Visible: {Visible}", IsVisible);
            Logger.LogInformation("[TakeoffMeasurementPanel] CurrentMeasurement set: {IsSet}", CurrentMeasurement != null);
            Logger.LogInformation("[TakeoffMeasurementPanel] ============================================");

            // Ensure panel is visible
            if (!IsVisible)
            {
                Logger.LogWarning("[TakeoffMeasurementPanel] Panel is hidden! Making it visible...");
                IsVisible = true;
                await IsVisibleChanged.InvokeAsync(IsVisible);
            }

            StateHasChanged();
            await Task.CompletedTask;
        }

        private async Task LoadMeasurements()
        {
            try
            {
                isLoadingMeasurements = true;
                errorMessage = null;
                StateHasChanged();

                Logger.LogInformation("[TakeoffMeasurementPanel] Loading measurements for drawing {DrawingId}", PackageDrawingId);

                measurements = await CatalogueService.GetMeasurementsByDrawingAsync(PackageDrawingId, companyId);
                Logger.LogInformation("[TakeoffMeasurementPanel] Loaded {Count} measurements", measurements.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[TakeoffMeasurementPanel] Error loading measurements");
                errorMessage = $"Error loading measurements: {ex.Message}";
            }
            finally
            {
                isLoadingMeasurements = false;
                StateHasChanged();
            }
        }

        private async Task SaveCurrentMeasurement()
        {
            if (CurrentMeasurement == null)
            {
                return;
            }

            try
            {
                Logger.LogInformation("[TakeoffMeasurementPanel] Saving measurement: {ItemCode}", CurrentMeasurement.ItemCode);

                // Create measurement via service
                await CatalogueService.CreateMeasurementAsync(
                    traceTakeoffId: 1, // TODO: Get actual TraceTakeoffId from context
                    packageDrawingId: PackageDrawingId,
                    catalogueItemId: CurrentMeasurement.CatalogueItemId,
                    measurementType: CurrentMeasurement.MeasurementType,
                    value: CurrentMeasurement.MeasurementValue,
                    unit: CurrentMeasurement.Unit,
                    coordinates: null, // TODO: Get coordinates from Nutrient annotation
                    companyId: companyId
                );

                Logger.LogInformation("[TakeoffMeasurementPanel] Measurement saved successfully");

                // Clear current measurement
                CurrentMeasurement = null;

                // Reload measurements list
                await LoadMeasurements();

                // Notify parent component
                await OnMeasurementSaved.InvokeAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[TakeoffMeasurementPanel] Error saving measurement");
                errorMessage = $"Error saving measurement: {ex.Message}";
            }
            finally
            {
                StateHasChanged();
            }
        }

        private void CancelCurrentMeasurement()
        {
            Logger.LogInformation("[TakeoffMeasurementPanel] Canceling current measurement");
            CurrentMeasurement = null;
            StateHasChanged();
        }

        private void ToggleCategory(string category)
        {
            expandedCategory = expandedCategory == category ? null : category;
            StateHasChanged();
        }

        private async Task DeleteMeasurement(int measurementId)
        {
            try
            {
                Logger.LogInformation("[TakeoffMeasurementPanel] Deleting measurement {Id}", measurementId);

                await CatalogueService.DeleteMeasurementAsync(measurementId, companyId);
                Logger.LogInformation("[TakeoffMeasurementPanel] Measurement deleted successfully");
                await LoadMeasurements();
                await OnMeasurementSaved.InvokeAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[TakeoffMeasurementPanel] Error deleting measurement");
                errorMessage = $"Error deleting measurement: {ex.Message}";
            }
            finally
            {
                StateHasChanged();
            }
        }

        private async Task RefreshMeasurements()
        {
            await LoadMeasurements();
        }

        private async Task ClearAllMeasurements()
        {
            // TODO: Add confirmation dialog
            Logger.LogWarning("[TakeoffMeasurementPanel] Clear all measurements requested");

            // For now, just log - implement bulk delete if needed
            // Or delete one by one
            foreach (var measurement in measurements.ToList())
            {
                await DeleteMeasurement(measurement.Id);
            }
        }

        private async Task TogglePanel()
        {
            IsVisible = !IsVisible;
            await IsVisibleChanged.InvokeAsync(IsVisible);
        }

        private void CycleHeight()
        {
            currentHeight = currentHeight switch
            {
                FooterHeight.Default => FooterHeight.Expanded,
                FooterHeight.Expanded => FooterHeight.Full,
                FooterHeight.Full => FooterHeight.Default,
                _ => FooterHeight.Default
            };
            Logger.LogInformation("[TakeoffMeasurementPanel] Height changed to: {Height}", currentHeight);
            StateHasChanged();
        }

        private string GetHeightClass()
        {
            return currentHeight switch
            {
                FooterHeight.Expanded => "height-expanded",
                FooterHeight.Full => "height-full",
                _ => ""
            };
        }

        private string GetExpandIcon()
        {
            return currentHeight switch
            {
                FooterHeight.Default => "fa-expand-alt",
                FooterHeight.Expanded => "fa-expand-arrows-alt",
                FooterHeight.Full => "fa-compress-alt",
                _ => "fa-expand-alt"
            };
        }
    }
}
