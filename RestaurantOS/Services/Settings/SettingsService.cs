using Microsoft.EntityFrameworkCore;
using RestaurantOS.Data;
using RestaurantOS.Models;

namespace RestaurantOS.Services.Settings;

public interface ISettingsService
{
    Task<RestaurantSettings> GetAsync();
    Task<RestaurantSettings> SaveAsync(RestaurantSettings settings);
}

public class SettingsService : ISettingsService
{
    private readonly ApplicationDbContext _db;
    public SettingsService(ApplicationDbContext db) => _db = db;

    public async Task<RestaurantSettings> GetAsync() =>
        await _db.RestaurantSettings.FirstOrDefaultAsync() ?? new RestaurantSettings();

    public async Task<RestaurantSettings> SaveAsync(RestaurantSettings settings)
    {
        var existing = await _db.RestaurantSettings.FirstOrDefaultAsync();
        if (existing is null)
        {
            _db.RestaurantSettings.Add(settings);
        }
        else
        {
            existing.RestaurantName = settings.RestaurantName;
            existing.LogoUrl = settings.LogoUrl;
            existing.Address = settings.Address;
            existing.Phone = settings.Phone;
            existing.PrimaryColor = settings.PrimaryColor;
            existing.SecondaryColor = settings.SecondaryColor;
            existing.AccentColor = settings.AccentColor;
            existing.FontFamily = settings.FontFamily;
            existing.DarkMode = settings.DarkMode;
            existing.DefaultVatPercent = settings.DefaultVatPercent;
            existing.DefaultServiceChargePercent = settings.DefaultServiceChargePercent;
            existing.CurrencySymbol = settings.CurrencySymbol;
            existing.ReceiptFooterNote = settings.ReceiptFooterNote;
        }
        await _db.SaveChangesAsync();
        return settings;
    }
}
