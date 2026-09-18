namespace RestaurantOS.Models;

public enum OrderType
{
    DineIn,
    TakeAway,
    Delivery
}

public enum OrderStatus
{
    Open,
    SentToKitchen,
    Preparing,
    Ready,
    Served,
    Completed,
    Cancelled
}

public enum TableStatus
{
    Available,
    Occupied,
    Reserved,
    Cleaning
}

public enum PaymentMethod
{
    Cash,
    Card,
    Bkash,
    Nagad,
    Rocket,
    BankTransfer
}

public enum PaymentStatus
{
    Pending,
    Paid,
    PartiallyPaid,
    Refunded,
    Failed
}

public enum ExpenseCategory
{
    Ingredients,
    Utilities,
    Rent,
    Salary,
    Maintenance,
    Marketing,
    Other
}
