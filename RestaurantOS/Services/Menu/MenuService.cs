using Microsoft.EntityFrameworkCore;
using RestaurantOS.Data;
using RestaurantOS.Models;

namespace RestaurantOS.Services.Menu;

public interface IMenuService
{
    Task<List<MenuCategory>> GetCategoriesWithItemsAsync();
    Task<MenuCategory> SaveCategoryAsync(MenuCategory category);
    Task DeleteCategoryAsync(int id);
    Task<MenuItem> SaveItemAsync(MenuItem item);
    Task DeleteItemAsync(int id);
    Task ToggleAvailabilityAsync(int itemId);
}

public class MenuService : IMenuService
{
    private readonly ApplicationDbContext _db;
    public MenuService(ApplicationDbContext db) => _db = db;

    public Task<List<MenuCategory>> GetCategoriesWithItemsAsync() =>
        _db.MenuCategories.Include(c => c.Items).OrderBy(c => c.SortOrder).ToListAsync();

    public async Task<MenuCategory> SaveCategoryAsync(MenuCategory category)
    {
        if (category.Id == 0) _db.MenuCategories.Add(category);
        else _db.MenuCategories.Update(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var category = await _db.MenuCategories.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return;
        if (category.Items.Any())
            throw new InvalidOperationException("Move or delete items in this category first.");
        _db.MenuCategories.Remove(category);
        await _db.SaveChangesAsync();
    }

    public async Task<MenuItem> SaveItemAsync(MenuItem item)
    {
        if (item.Id == 0) _db.MenuItems.Add(item);
        else _db.MenuItems.Update(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task DeleteItemAsync(int id)
    {
        var item = await _db.MenuItems.FindAsync(id);
        if (item is null) return;
        _db.MenuItems.Remove(item);
        await _db.SaveChangesAsync();
    }

    public async Task ToggleAvailabilityAsync(int itemId)
    {
        var item = await _db.MenuItems.FindAsync(itemId);
        if (item is null) return;
        item.IsAvailable = !item.IsAvailable;
        await _db.SaveChangesAsync();
    }
}
