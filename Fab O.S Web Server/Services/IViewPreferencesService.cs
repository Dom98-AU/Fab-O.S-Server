using FabOS.WebServer.Models.ViewState;

namespace FabOS.WebServer.Services;

public interface IViewPreferencesService
{
    Task<List<SavedViewPreference>> GetUserViews(string userId, string entityType);
    Task<SavedViewPreference?> GetView(int viewId);
    Task<SavedViewPreference> SaveView(SavedViewPreference view);
    Task DeleteView(int viewId);
    Task<SavedViewPreference?> GetDefaultView(string userId, string entityType);
}