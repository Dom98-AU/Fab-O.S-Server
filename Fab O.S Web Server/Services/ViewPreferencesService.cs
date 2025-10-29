using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Models.ViewState;
using FabOS.WebServer.Data.Contexts;
using System.Text.Json;

namespace FabOS.WebServer.Services;

public class ViewPreferencesService : IViewPreferencesService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public ViewPreferencesService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<SavedViewPreference>> GetUserViews(string userId, string entityType)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.SavedViewPreferences
            .Where(v => v.UserId == userId && v.EntityType == entityType)
            .OrderByDescending(v => v.IsDefault)
            .ThenBy(v => v.Name)
            .ToListAsync();
    }
    
    public async Task<SavedViewPreference?> GetView(int viewId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var view = await context.SavedViewPreferences
            .FirstOrDefaultAsync(v => v.Id == viewId);

        if (view != null && !string.IsNullOrEmpty(view.ViewStateJson))
        {
            view.ViewState = JsonSerializer.Deserialize<ViewState>(view.ViewStateJson);
        }

        return view;
    }

    public async Task<SavedViewPreference> SaveView(SavedViewPreference view)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (view.ViewState != null)
        {
            view.ViewStateJson = JsonSerializer.Serialize(view.ViewState);
        }

        if (view.Id == 0)
        {
            context.SavedViewPreferences.Add(view);
        }
        else
        {
            context.SavedViewPreferences.Update(view);
        }

        await context.SaveChangesAsync();
        return view;
    }

    public async Task DeleteView(int viewId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var view = await context.SavedViewPreferences.FindAsync(viewId);
        if (view != null)
        {
            context.SavedViewPreferences.Remove(view);
            await context.SaveChangesAsync();
        }
    }

    public async Task<SavedViewPreference?> GetDefaultView(string userId, string entityType)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var view = await context.SavedViewPreferences
            .FirstOrDefaultAsync(v => v.UserId == userId &&
                                      v.EntityType == entityType &&
                                      v.IsDefault);

        if (view != null && !string.IsNullOrEmpty(view.ViewStateJson))
        {
            view.ViewState = JsonSerializer.Deserialize<ViewState>(view.ViewStateJson);
        }

        return view;
    }
}