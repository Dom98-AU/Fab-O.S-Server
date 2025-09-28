using Microsoft.AspNetCore.Components;
using FabOS.WebServer.Models.Columns;
using System.Collections.Generic;
using System.Linq;

namespace FabOS.WebServer.Components.Shared;

public partial class ColumnReorderManager : ComponentBase
{
    [Parameter] public List<ColumnDefinition> Columns { get; set; } = new();
    [Parameter] public EventCallback<List<ColumnDefinition>> OnColumnsChanged { get; set; }
    [Parameter] public List<ColumnDefinition>? DefaultColumns { get; set; }
    [Parameter] public string CssClass { get; set; } = "";

    private bool IsOpen = false;
    private List<ColumnDefinition> workingColumns = new();
    private string searchTerm = "";
    private ColumnDefinition? draggedColumn;
    private string? openFreezeMenuId;

    protected override void OnInitialized()
    {
        InitializeWorkingColumns();
    }

    protected override void OnParametersSet()
    {
        if (!IsOpen)
        {
            InitializeWorkingColumns();
        }
    }

    private void InitializeWorkingColumns()
    {
        workingColumns = Columns.Select(c => (ColumnDefinition)c.Clone()).ToList();
    }

    private void ToggleColumnSettings()
    {
        IsOpen = !IsOpen;
        if (IsOpen)
        {
            InitializeWorkingColumns();
            searchTerm = "";
            openFreezeMenuId = null;
        }
    }

    private List<ColumnDefinition> GetFilteredColumns()
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return workingColumns.OrderBy(c => c.Order).ToList();

        return workingColumns
            .Where(c => c.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       c.PropertyName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Order)
            .ToList();
    }

    private void OnVisibilityChange(ColumnDefinition column, bool isVisible)
    {
        column.IsVisible = isVisible;
        StateHasChanged();
    }

    private void ShowAll()
    {
        foreach (var column in workingColumns)
        {
            column.IsVisible = true;
        }
        StateHasChanged();
    }

    private void HideAll()
    {
        foreach (var column in workingColumns.Where(c => !c.IsRequired))
        {
            column.IsVisible = false;
        }
        StateHasChanged();
    }

    private async Task ResetToDefaults()
    {
        if (DefaultColumns != null)
        {
            workingColumns = DefaultColumns.Select(c => (ColumnDefinition)c.Clone()).ToList();
        }
        else
        {
            InitializeWorkingColumns();
        }
        StateHasChanged();
    }

    private void OnDragStart(ColumnDefinition column)
    {
        if (column.IsReorderable)
        {
            draggedColumn = column;
        }
    }

    private void OnDragOver(Microsoft.AspNetCore.Components.Web.DragEventArgs e, ColumnDefinition targetColumn)
    {
        e.DataTransfer.DropEffect = draggedColumn != null && targetColumn.IsReorderable ? "move" : "none";
    }

    private void OnDrop(ColumnDefinition targetColumn)
    {
        if (draggedColumn != null && targetColumn.IsReorderable && draggedColumn.Id != targetColumn.Id)
        {
            var draggedIndex = workingColumns.IndexOf(draggedColumn);
            var targetIndex = workingColumns.IndexOf(targetColumn);

            if (draggedIndex >= 0 && targetIndex >= 0)
            {
                workingColumns.RemoveAt(draggedIndex);
                workingColumns.Insert(targetIndex, draggedColumn);

                // Update order values
                for (int i = 0; i < workingColumns.Count; i++)
                {
                    workingColumns[i].Order = i;
                }

                StateHasChanged();
            }
        }
    }

    private void OnDragEnd()
    {
        draggedColumn = null;
        StateHasChanged();
    }

    private void ToggleFreezeMenu(string columnId)
    {
        openFreezeMenuId = openFreezeMenuId == columnId ? null : columnId;
        StateHasChanged();
    }

    private void SetFreezePosition(ColumnDefinition column, FreezePosition position)
    {
        column.FreezePosition = position;
        openFreezeMenuId = null;
        StateHasChanged();
    }

    private void Cancel()
    {
        InitializeWorkingColumns();
        IsOpen = false;
    }

    private async Task ApplyChanges()
    {
        Columns.Clear();
        Columns.AddRange(workingColumns.Select(c => (ColumnDefinition)c.Clone()));

        if (OnColumnsChanged.HasDelegate)
        {
            await OnColumnsChanged.InvokeAsync(Columns);
        }

        IsOpen = false;
    }
}