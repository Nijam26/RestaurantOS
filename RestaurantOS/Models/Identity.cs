using System.ComponentModel.DataAnnotations;

namespace RestaurantOS.Models;

public class Employee
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? PhotoUrl { get; set; }

    public int RoleId { get; set; }
    public Role? Role { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastLoginAt { get; set; }
}

public class Role
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Name { get; set; } = string.Empty; // Admin, Manager, Cashier, Waiter, Kitchen

    public string? Description { get; set; }
    public bool IsSystemRole { get; set; } // seeded roles that can't be deleted

    public List<Employee> Employees { get; set; } = new();
    public List<RolePermission> RolePermissions { get; set; } = new();
}

public class Permission
{
    public int Id { get; set; }

    [Required, MaxLength(60)]
    public string Key { get; set; } = string.Empty; // "pos.access", "reports.view", "menu.manage"

    [Required, MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    public string? Group { get; set; } // "POS", "Reports", "Admin"

    public List<RolePermission> RolePermissions { get; set; } = new();
}

public class RolePermission
{
    public int RoleId { get; set; }
    public Role? Role { get; set; }

    public int PermissionId { get; set; }
    public Permission? Permission { get; set; }
}
