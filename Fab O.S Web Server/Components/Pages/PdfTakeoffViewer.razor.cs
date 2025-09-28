using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Components.Pages;

public partial class PdfTakeoffViewer : ComponentBase
{
    [Parameter] public int DrawingId { get; set; }

    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private TraceDrawing? drawing;
    private bool isLoading = true;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadDrawing();
    }

    protected override async Task OnParametersSetAsync()
    {
        await LoadDrawing();
    }

    private async Task LoadDrawing()
    {
        try
        {
            isLoading = true;
            errorMessage = null;

            drawing = await DbContext.TraceDrawings
                .FirstOrDefaultAsync(d => d.Id == DrawingId);

            if (drawing == null)
            {
                errorMessage = "Drawing not found";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading drawing: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }
}