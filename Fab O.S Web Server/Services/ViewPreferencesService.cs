using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Models.ViewState;
using FabOS.WebServer.Data.Contexts;
using System.Text.Json;

namespace FabOS.WebServer.Services;

public class ViewPreferencesService : IViewPreferencesService
{
    private readonly ApplicationDbContext _context;
    
    public ViewPreferencesService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<SavedViewPreference>> GetUserViews(string userId, string entityType)
    {
        return await _context.SavedViewPreferences
            .Where(v => v.UserId == userId && v.EntityType == entityType)
            .OrderByDescending(v => v.IsDefault)
            .ThenBy(v => v.Name)
            .ToListAsync();
    }
    
    public async Task<SavedViewPreference?> GetView(int viewId)
    {
        var view = await _context.SavedViewPreferences
            .FirstOrDefaultAsync(v => v.Id == viewId);
        
        if (view != null && !string.IsNullOrEmpty(view.ViewStateJson))
        {
            view.ViewState = JsonSerializer.Deserialize<ViewState>(view.ViewStateJson);
        }
        
        return view;
    }
    
    public async Task<SavedViewPreference> SaveView(SavedViewPreference view)
    {
        if (view.ViewState != null)
        {
            view.ViewStateJson = JsonSerializer.Serialize(view.ViewState);
        }
        
        if (view.Id == 0)
        {
            _context.SavedViewPreferences.Add(view);
        }
        else
        {
            _context.SavedViewPreferences.Update(view);
        }
        
        await _context.SaveChangesAsync();
        return view;
    }
    
    public async Task DeleteView(int viewId)
    {
        var view = await _context.SavedViewPreferences.FindAsync(viewId);
        if (view != null)
        {
            _context.SavedViewPreferences.Remove(view);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<SavedViewPreference?> GetDefaultView(string userId, string entityType)
    {
        var view = await _context.SavedViewPreferences
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