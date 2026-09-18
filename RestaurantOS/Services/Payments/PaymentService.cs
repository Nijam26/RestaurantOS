using Microsoft.EntityFrameworkCore;
using RestaurantOS.Data;
using RestaurantOS.Models;

namespace RestaurantOS.Services.Payments;

public interface IPaymentService
{
    Task<Payment> RecordPaymentAsync(int orderId, decimal amount, PaymentMethod method, int employeeId,
        string? payerNumber = null, string? transactionReference = null, string? notes = null);
    Task<decimal> GetOutstandingBalanceAsync(int orderId);
    Task<Payment> RefundAsync(int paymentId, decimal amount, string reason, int employeeId);
}

/// <summary>
/// Records payments and reconciles order payment status. Card/bKash/Nagad
/// integrations are represented by TransactionReference — plug in the real
/// gateway SDK/webhook call inside RecordPaymentAsync when credentials are available.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _db;

    public PaymentService(ApplicationDbContext db) => _db = db;

    public async Task<Payment> RecordPaymentAsync(int orderId, decimal amount, PaymentMethod method, int employeeId,
        string? payerNumber = null, string? transactionReference = null, string? notes = null)
    {
        var orderExists = await _db.Orders.AnyAsync(o => o.Id == orderId);
        if (!orderExists) throw new InvalidOperationException("Order not found.");

        var payment = new Payment
        {
            OrderId = orderId,
            Amount = amount,
            Method = method,
            Status = PaymentStatus.Paid, // gateway integrations would set Pending until webhook confirms
            PayerNumber = payerNumber,
            TransactionReference = transactionReference,
            ReceivedByEmployeeId = employeeId,
            Notes = notes
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        return payment;
    }

    public async Task<decimal> GetOutstandingBalanceAsync(int orderId)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order is null) return 0;

        var paid = await _db.Payments
            .Where(p => p.OrderId == orderId && p.Status == PaymentStatus.Paid)
            .SumAsync(p => p.Amount);

        return order.GrandTotal - paid;
    }

    public async Task<Payment> RefundAsync(int paymentId, decimal amount, string reason, int employeeId)
    {
        var original = await _db.Payments.FindAsync(paymentId)
            ?? throw new InvalidOperationException("Payment not found.");

        var refund = new Payment
        {
            OrderId = original.OrderId,
            Amount = -Math.Abs(amount),
            Method = original.Method,
            Status = PaymentStatus.Refunded,
            ReceivedByEmployeeId = employeeId,
            Notes = $"Refund for payment #{original.Id}: {reason}"
        };

        _db.Payments.Add(refund);
        await _db.SaveChangesAsync();
        return refund;
    }
}
