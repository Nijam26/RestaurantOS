using Microsoft.EntityFrameworkCore;
using RestaurantOS.Data;
using RestaurantOS.Models;

namespace RestaurantOS.Services.Accounts;

public record AccountSummary(
    decimal TotalRevenue,
    decimal TotalVatCollected,
    decimal TotalDiscounts,
    decimal TotalExpenses,
    decimal NetProfit,
    int OrderCount);

public interface IAccountService
{
    Task<Expense> AddExpenseAsync(Expense expense);
    Task DeleteExpenseAsync(int id);
    Task<List<Expense>> GetExpensesAsync(DateTime from, DateTime to);
    Task<AccountSummary> GetSummaryAsync(DateTime from, DateTime to);
    Task<Dictionary<PaymentMethod, decimal>> GetPaymentMethodBreakdownAsync(DateTime from, DateTime to);
}

public class AccountService : IAccountService
{
    private readonly ApplicationDbContext _db;
    public AccountService(ApplicationDbContext db) => _db = db;

    public async Task<Expense> AddExpenseAsync(Expense expense)
    {
        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();
        return expense;
    }

    public async Task DeleteExpenseAsync(int id)
    {
        var expense = await _db.Expenses.FindAsync(id);
        if (expense is null) return;
        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync();
    }

    public Task<List<Expense>> GetExpensesAsync(DateTime from, DateTime to) =>
        _db.Expenses.Where(e => e.Date >= from && e.Date <= to).OrderByDescending(e => e.Date).ToListAsync();

    public async Task<AccountSummary> GetSummaryAsync(DateTime from, DateTime to)
    {
        var orders = await _db.Orders
            .Where(o => o.Status == OrderStatus.Completed && o.CreatedAt >= from && o.CreatedAt <= to)
            .ToListAsync();

        var expenses = await GetExpensesAsync(from, to);

        var revenue = orders.Sum(o => o.GrandTotal);
        var vat = orders.Sum(o => o.VatAmount);
        var discounts = orders.Sum(o => o.DiscountAmount);
        var expenseTotal = expenses.Sum(e => e.Amount);

        return new AccountSummary(
            TotalRevenue: revenue,
            TotalVatCollected: vat,
            TotalDiscounts: discounts,
            TotalExpenses: expenseTotal,
            NetProfit: revenue - expenseTotal,
            OrderCount: orders.Count);
    }

    public async Task<Dictionary<PaymentMethod, decimal>> GetPaymentMethodBreakdownAsync(DateTime from, DateTime to)
    {
        var payments = await _db.Payments
            .Where(p => p.Status == PaymentStatus.Paid && p.CreatedAt >= from && p.CreatedAt <= to)
            .ToListAsync();

        return payments.GroupBy(p => p.Method).ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));
    }
}
