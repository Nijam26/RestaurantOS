using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantOS.Models;

public class Order
{
    public int Id { get; set; }

    [Column(TypeName = "varchar(20)")]
    public string OrderNumber { get; set; } = string.Empty; // e.g. ORD-20260903-0007

    public OrderType Type { get; set; } = OrderType.DineIn;
    public OrderStatus Status { get; set; } = OrderStatus.Open;

    public int? DiningTableId { get; set; }
    public DiningTable? DiningTable { get; set; }

    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? DeliveryAddress { get; set; }

    public int EmployeeId { get; set; } // who took the order
    public Employee? Employee { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal DiscountPercent { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal VatPercent { get; set; } = 5m; // Bangladesh restaurant VAT default

    [Column(TypeName = "decimal(10,2)")]
    public decimal VatAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal ServiceChargeAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal GrandTotal { get; set; }

    public string? Notes { get; set; }

    public List<OrderItem> Items { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    public int Quantity { get; set; } = 1;

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; } // snapshot at order time

    public string? SpecialInstructions { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal LineTotal => UnitPrice * Quantity;
}

public class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    // For mobile financial services / card gateways
    public string? TransactionReference { get; set; }
    public string? PayerNumber { get; set; } // bKash/Nagad sender number

    public int ReceivedByEmployeeId { get; set; }
    public Employee? ReceivedByEmployee { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? Notes { get; set; }
}
