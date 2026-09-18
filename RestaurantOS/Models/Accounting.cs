using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantOS.Models;

public class Expense
{
    public int Id { get; set; }

    public ExpenseCategory Category { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    public DateTime Date { get; set; } = DateTime.Now;

    public int RecordedByEmployeeId { get; set; }
    public Employee? RecordedByEmployee { get; set; }
}

/// <summary>
/// Single-row table holding branding + operational settings so the UI
/// can be customized without redeploying (name, logo, theme, tax rates).
/// </summary>
public class RestaurantSettings
{
    public int Id { get; set; } = 1;

    public string RestaurantName { get; set; } = "My Restaurant";
    public string? LogoUrl { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }

    // Theming — applied as CSS custom properties at runtime
    public string PrimaryColor { get; set; } = "#E8590C";   // brand accent
    public string SecondaryColor { get; set; } = "#1B1F23";  // dark surface
    public string AccentColor { get; set; } = "#2F9E44";     // success/accent
    public string FontFamily { get; set; } = "'Plus Jakarta Sans', sans-serif";
    public bool DarkMode { get; set; } = false;

    [Column(TypeName = "decimal(5,2)")]
    public decimal DefaultVatPercent { get; set; } = 5m;

    [Column(TypeName = "decimal(5,2)")]
    public decimal DefaultServiceChargePercent { get; set; } = 0m;

    public string CurrencySymbol { get; set; } = "৳";
    public string ReceiptFooterNote { get; set; } = "Thank you for dining with us!";
}
