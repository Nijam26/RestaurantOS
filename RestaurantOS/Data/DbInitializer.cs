using RestaurantOS.Models;

namespace RestaurantOS.Data;

public static class DbInitializer
{
    // Central list of every permission key the app understands.
    public static readonly (string Key, string DisplayName, string Group)[] AllPermissions =
    {
        ("pos.access",        "Access POS Terminal",     "POS"),
        ("orders.manage",     "Manage Orders",           "POS"),
        ("payments.take",     "Take Payments",           "POS"),
        ("payments.refund",   "Issue Refunds",           "POS"),
        ("menu.manage",       "Manage Menu & Categories","Admin"),
        ("tables.manage",     "Manage Tables",           "Admin"),
        ("staff.manage",      "Manage Staff & Roles",    "Admin"),
        ("reports.view",      "View Reports",            "Reports"),
        ("reports.export",    "Export Reports",          "Reports"),
        ("accounts.view",     "View Accounts",           "Accounts"),
        ("accounts.manage",   "Manage Expenses",         "Accounts"),
        ("settings.manage",   "Manage App Settings & UI","Admin"),
    };

    public static void Seed(ApplicationDbContext db)
    {
        db.Database.EnsureCreated();

        if (!db.Permissions.Any())
        {
            foreach (var (key, name, group) in AllPermissions)
                db.Permissions.Add(new Permission { Key = key, DisplayName = name, Group = group });
            db.SaveChanges();
        }

        if (!db.Roles.Any())
        {
            var allPerms = db.Permissions.ToList();

            var admin = new Role { Name = "Admin", Description = "Full system access", IsSystemRole = true };
            admin.RolePermissions = allPerms.Select(p => new RolePermission { PermissionId = p.Id }).ToList();

            var manager = new Role { Name = "Manager", Description = "Day-to-day operations", IsSystemRole = true };
            manager.RolePermissions = allPerms
                .Where(p => p.Key != "settings.manage" && p.Key != "staff.manage")
                .Select(p => new RolePermission { PermissionId = p.Id }).ToList();

            var cashier = new Role { Name = "Cashier", Description = "POS & payments", IsSystemRole = true };
            cashier.RolePermissions = allPerms
                .Where(p => p.Key is "pos.access" or "orders.manage" or "payments.take")
                .Select(p => new RolePermission { PermissionId = p.Id }).ToList();

            var waiter = new Role { Name = "Waiter", Description = "Order taking only", IsSystemRole = true };
            waiter.RolePermissions = allPerms
                .Where(p => p.Key is "pos.access" or "orders.manage")
                .Select(p => new RolePermission { PermissionId = p.Id }).ToList();

            db.Roles.AddRange(admin, manager, cashier, waiter);
            db.SaveChanges();
        }

        if (!db.Employees.Any())
        {
            var adminRole = db.Roles.Single(r => r.Name == "Admin");
            db.Employees.Add(new Employee
            {
                FullName = "System Admin",
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                RoleId = adminRole.Id,
                IsActive = true
            });
            db.SaveChanges();
        }

        if (!db.RestaurantSettings.Any())
        {
            db.RestaurantSettings.Add(new RestaurantSettings());
            db.SaveChanges();
        }

        if (!db.DiningTables.Any())
        {
            for (int i = 1; i <= 12; i++)
            {
                db.DiningTables.Add(new DiningTable
                {
                    Name = $"T{i}",
                    Capacity = i % 3 == 0 ? 6 : 4,
                    Zone = i <= 8 ? "Main Hall" : "Rooftop"
                });
            }
            db.SaveChanges();
        }

        if (!db.MenuCategories.Any())
        {
            var starters = new MenuCategory { Name = "Starters", Icon = "🥗", SortOrder = 1 };
            var mains = new MenuCategory { Name = "Main Course", Icon = "🍛", SortOrder = 2 };
            var breads = new MenuCategory { Name = "Breads", Icon = "🫓", SortOrder = 3 };
            var beverages = new MenuCategory { Name = "Beverages", Icon = "🥤", SortOrder = 4 };
            var desserts = new MenuCategory { Name = "Desserts", Icon = "🍮", SortOrder = 5 };

            starters.Items.AddRange(new[]
            {
                new MenuItem { Name = "Chicken Fry", Price = 220, CostPrice = 90 },
                new MenuItem { Name = "Vegetable Spring Roll", Price = 180, CostPrice = 70, IsVegetarian = true },
            });
            mains.Items.AddRange(new[]
            {
                new MenuItem { Name = "Beef Bhuna", Price = 380, CostPrice = 190, IsSpicy = true },
                new MenuItem { Name = "Chicken Kacchi", Price = 350, CostPrice = 170 },
                new MenuItem { Name = "Vegetable Curry", Price = 220, CostPrice = 90, IsVegetarian = true },
            });
            breads.Items.AddRange(new[]
            {
                new MenuItem { Name = "Butter Naan", Price = 60, CostPrice = 20 },
                new MenuItem { Name = "Plain Paratha", Price = 30, CostPrice = 10 },
            });
            beverages.Items.AddRange(new[]
            {
                new MenuItem { Name = "Borhani", Price = 80, CostPrice = 30 },
                new MenuItem { Name = "Soft Drink", Price = 60, CostPrice = 30 },
            });
            desserts.Items.Add(new MenuItem { Name = "Firni", Price = 90, CostPrice = 35 });

            db.MenuCategories.AddRange(starters, mains, breads, beverages, desserts);
            db.SaveChanges();
        }
    }
}
