using Microsoft.EntityFrameworkCore;
using RestaurantOS.Data;
using RestaurantOS.Models;

namespace RestaurantOS.Services.Orders;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(OrderType type, int employeeId, int? tableId, string? customerName = null, string? customerPhone = null);
    Task<Order?> GetOrderAsync(int orderId);
    Task<List<Order>> GetOpenOrdersAsync();
    Task<List<Order>> GetOrderHistoryAsync(DateTime? from = null, DateTime? to = null);
    Task AddItemAsync(int orderId, int menuItemId, int quantity, string? instructions = null);
    Task RemoveItemAsync(int orderItemId);
    Task UpdateItemQuantityAsync(int orderItemId, int quantity);
    Task RecalculateTotalsAsync(int orderId, decimal? discountPercent = null);
    Task SetStatusAsync(int orderId, OrderStatus status);
    Task<Order> CompleteOrderAsync(int orderId);
}

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _db;

    public OrderService(ApplicationDbContext db) => _db = db;

    public async Task<Order> CreateOrderAsync(OrderType type, int employeeId, int? tableId, string? customerName = null, string? customerPhone = null)
    {
        var settings = await _db.RestaurantSettings.FirstAsync();

        var todayPrefix = $"ORD-{DateTime.Now:yyyyMMdd}-";
        var todayCount = await _db.Orders.CountAsync(o => o.OrderNumber.StartsWith(todayPrefix));

        var order = new Order
        {
            OrderNumber = $"{todayPrefix}{(todayCount + 1):D4}",
            Type = type,
            EmployeeId = employeeId,
            DiningTableId = tableId,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            VatPercent = settings.DefaultVatPercent,
            ServiceChargeAmount = 0,
        };

        _db.Orders.Add(order);

        if (tableId is not null)
        {
            var table = await _db.DiningTables.FindAsync(tableId);
            if (table is not null) table.Status = TableStatus.Occupied;
        }

        await _db.SaveChangesAsync();
        return order;
    }

    public Task<Order?> GetOrderAsync(int orderId) =>
        _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.MenuItem)
            .Include(o => o.Payments)
            .Include(o => o.DiningTable)
            .Include(o => o.Employee)
            .FirstOrDefaultAsync(o => o.Id == orderId);

    public Task<List<Order>> GetOpenOrdersAsync() =>
        _db.Orders
            .Include(o => o.Items)
            .Include(o => o.DiningTable)
            .Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public Task<List<Order>> GetOrderHistoryAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _db.Orders.Include(o => o.Items).Include(o => o.DiningTable).AsQueryable();
        if (from is not null) query = query.Where(o => o.CreatedAt >= from);
        if (to is not null) query = query.Where(o => o.CreatedAt <= to);
        return query.OrderByDescending(o => o.CreatedAt).ToListAsync();
    }

    public async Task AddItemAsync(int orderId, int menuItemId, int quantity, string? instructions = null)
    {
        var menuItem = await _db.MenuItems.FindAsync(menuItemId)
            ?? throw new InvalidOperationException("Menu item not found.");

        var existing = await _db.OrderItems.FirstOrDefaultAsync(i =>
            i.OrderId == orderId && i.MenuItemId == menuItemId && i.SpecialInstructions == instructions);

        if (existing is not null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            _db.OrderItems.Add(new OrderItem
            {
                OrderId = orderId,
                MenuItemId = menuItemId,
                Quantity = quantity,
                UnitPrice = menuItem.Price,
                SpecialInstructions = instructions
            });
        }

        await _db.SaveChangesAsync();
        await RecalculateTotalsAsync(orderId);
    }

    public async Task RemoveItemAsync(int orderItemId)
    {
        var item = await _db.OrderItems.FindAsync(orderItemId);
        if (item is null) return;
        var orderId = item.OrderId;
        _db.OrderItems.Remove(item);
        await _db.SaveChangesAsync();
        await RecalculateTotalsAsync(orderId);
    }

    public async Task UpdateItemQuantityAsync(int orderItemId, int quantity)
    {
        var item = await _db.OrderItems.FindAsync(orderItemId);
        if (item is null) return;
        if (quantity <= 0) { await RemoveItemAsync(orderItemId); return; }
        item.Quantity = quantity;
        await _db.SaveChangesAsync();
        await RecalculateTotalsAsync(item.OrderId);
    }

    public async Task RecalculateTotalsAsync(int orderId, decimal? discountPercent = null)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstAsync(o => o.Id == orderId);

        if (discountPercent is not null) order.DiscountPercent = discountPercent.Value;

        order.SubTotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        order.DiscountAmount = Math.Round(order.SubTotal * order.DiscountPercent / 100m, 2);
        var afterDiscount = order.SubTotal - order.DiscountAmount;
        order.VatAmount = Math.Round(afterDiscount * order.VatPercent / 100m, 2);
        order.GrandTotal = afterDiscount + order.VatAmount + order.ServiceChargeAmount;

        await _db.SaveChangesAsync();
    }

    public async Task SetStatusAsync(int orderId, OrderStatus status)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order is null) return;
        order.Status = status;
        await _db.SaveChangesAsync();
    }

    public async Task<Order> CompleteOrderAsync(int orderId)
    {
        var order = await _db.Orders.Include(o => o.DiningTable).FirstAsync(o => o.Id == orderId);
        order.Status = OrderStatus.Completed;
        order.CompletedAt = DateTime.Now;
        if (order.DiningTable is not null) order.DiningTable.Status = TableStatus.Cleaning;
        await _db.SaveChangesAsync();
        return order;
    }
}
