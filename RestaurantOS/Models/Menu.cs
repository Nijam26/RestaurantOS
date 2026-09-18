using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantOS.Models;

public class MenuCategory
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    public string? Icon { get; set; } // e.g. "🍕" or an icon class
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public List<MenuItem> Items { get; set; } = new();
}

public class MenuItem
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? CostPrice { get; set; } // for margin reporting

    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsVegetarian { get; set; }
    public bool IsSpicy { get; set; }

    public int MenuCategoryId { get; set; }
    public MenuCategory? MenuCategory { get; set; }

    public List<OrderItem> OrderItems { get; set; } = new();
}

public class DiningTable
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string Name { get; set; } = string.Empty; // "T1", "Patio 2"

    public int Capacity { get; set; } = 4;
    public TableStatus Status { get; set; } = TableStatus.Available;
    public string? Zone { get; set; } // "Main Hall", "Rooftop"

    public List<Order> Orders { get; set; } = new();
}
