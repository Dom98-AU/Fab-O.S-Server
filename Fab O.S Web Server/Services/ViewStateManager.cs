using FabOS.WebServer.Models.Columns;
using FabOS.WebServer.Models.Filtering;
using FabOS.WebServer.Models.ViewState;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FabOS.WebServer.Services;

public interface IViewStateManager
{
    event EventHandler<ViewStateChangedEventArgs>? StateChanged;

    ViewState GetCurrentState();
    void UpdateColumns(List<ColumnDefinition> columns);
    void UpdateFilters(List<FilterRule> filters);
    void UpdateSorting(string? sortColumn, bool ascending);
    void UpdatePagination(int pageSize, int currentPage);
    bool HasUnsavedChanges { get; }
    void MarkAsSaved();
    void Reset();
}

public class ViewStateChangedEventArgs : EventArgs
{
    public ViewState NewState { get; set; }
    public string ChangeType { get; set; }
    public bool HasUnsavedChanges { get; set; }

    public ViewStateChangedEventArgs(ViewState newState, string changeType, bool hasUnsavedChanges)
    {
        NewState = newState;
        ChangeType = changeType;
        HasUnsavedChanges = hasUnsavedChanges;
    }
}

public class ViewStateManager : IViewStateManager
{
    private ViewState _currentState;
    private ViewState? _savedState;
    private bool _hasUnsavedChanges;

    public event EventHandler<ViewStateChangedEventArgs>? StateChanged;

    public bool HasUnsavedChanges => _hasUnsavedChanges;

    public ViewStateManager()
    {
        _currentState = new ViewState();
        _hasUnsavedChanges = false;
    }

    public ViewState GetCurrentState()
    {
        return _currentState.Clone();
    }

    public void UpdateColumns(List<ColumnDefinition> columns)
    {
        _currentState.Columns = columns.Select(c => (ColumnDefinition)c.Clone()).ToList();
        CheckForChanges("Columns");
    }

    public void UpdateFilters(List<FilterRule> filters)
    {
        _currentState.Filters = filters.ToList();
        CheckForChanges("Filters");
    }

    public void UpdateSorting(string? sortColumn, bool ascending)
    {
        _currentState.SortColumn = sortColumn;
        _currentState.SortAscending = ascending;
        CheckForChanges("Sorting");
    }

    public void UpdatePagination(int pageSize, int currentPage)
    {
        _currentState.PageSize = pageSize;
        _currentState.CurrentPage = currentPage;
        CheckForChanges("Pagination");
    }

    public void MarkAsSaved()
    {
        _savedState = _currentState.Clone();
        _hasUnsavedChanges = false;
        NotifyStateChanged("Saved");
    }

    public void Reset()
    {
        if (_savedState != null)
        {
            _currentState = _savedState.Clone();
        }
        else
        {
            _currentState.ResetToDefaults();
        }
        _hasUnsavedChanges = false;
        NotifyStateChanged("Reset");
    }

    private void CheckForChanges(string changeType)
    {
        if (_savedState != null)
        {
            // Compare current state with saved state
            var currentJson = _currentState.SerializeState();
            var savedJson = _savedState.SerializeState();
            _hasUnsavedChanges = currentJson != savedJson;
        }
        else
        {
            // If no saved state, any change marks as unsaved
            _hasUnsavedChanges = true;
        }

        NotifyStateChanged(changeType);
    }

    private void NotifyStateChanged(string changeType)
    {
        StateChanged?.Invoke(this, new ViewStateChangedEventArgs(
            _currentState.Clone(),
            changeType,
            _hasUnsavedChanges
        ));
    }
}