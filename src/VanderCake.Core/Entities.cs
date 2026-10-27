namespace VanderCake.Core;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Login { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "VENDEDOR";
    public bool Active { get; set; } = true;
}
public class Client
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string Address { get; set; } = "";
    public string Phone { get; set; } = "";
    public long SellerId { get; set; }
    public User Seller { get; set; } = null!;
    public int VisitOrder { get; set; }
    public bool Active { get; set; } = true;
}
public class Product
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public string Group { get; set; } = "";
    public string Unit { get; set; } = "UN";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool Active { get; set; } = true;
    public long Version { get; set; }
}
public class PaymentTerms
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public bool Active { get; set; } = true;
    public decimal DailyInterestPercent { get; set; }
    public decimal DailyDiscountPercent { get; set; }
    public List<TermPart> Parts { get; set; } = [];
}
public class TermPart
{
    public long Id { get; set; }
    public long PaymentTermsId { get; set; }
    public int Number { get; set; }
    public int Days { get; set; }
    public decimal Percentage { get; set; }
}
public class PaymentMethod
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public bool Active { get; set; } = true;
}
public class Order
{
    public long Id { get; set; }
    public long ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public long SellerId { get; set; }
    public string Status { get; set; } = "ENVIADO";
    public DateTime SentAtUtc { get; set; }
    public Guid RequestKey { get; set; }
    public string RequestHash { get; set; } = "";
    public decimal RequestedTotal { get; set; }
    public decimal ApprovedTotal { get; set; }
    public string TermsName { get; set; } = "";
    public string MethodName { get; set; } = "";
    public string TermsSnapshot { get; set; } = "";
    public decimal DailyInterestPercent { get; set; }
    public decimal DailyDiscountPercent { get; set; }
    public long Version { get; set; }
    public string? CancellationReason { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}
public class OrderItem
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long ProductId { get; set; }
    public string Description { get; set; } = "";
    public string Unit { get; set; } = "";
    public int Requested { get; set; }
    public int? Approved { get; set; }
    public decimal UnitPrice { get; set; }
}
public class Batch
{
    public long Id { get; set; }
    public long AdminId { get; set; }
    public Guid RequestKey { get; set; }
    public string RequestHash { get; set; } = "";
    public DateTime ConfirmedAtUtc { get; set; }
    public List<Sale> Sales { get; set; } = [];
}
public class Sale
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public long BatchId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Total { get; set; }
    public List<Receivable> Receivables { get; set; } = [];
}
public class Receivable
{
    public long Id { get; set; }
    public long SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public int Number { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal DailyInterestPercent { get; set; }
    public decimal DailyDiscountPercent { get; set; }
    public List<Receipt> Receipts { get; set; } = [];
}
public class Receipt
{
    public long Id { get; set; }
    public long ReceivableId { get; set; }
    public decimal Amount { get; set; }
    public decimal PrincipalSettled { get; set; }
    public decimal Interest { get; set; }
    public decimal Discount { get; set; }
    public DateOnly Date { get; set; }
    public long AdminId { get; set; }
    public Guid RequestKey { get; set; }
    public string RequestHash { get; set; } = "";
}
public class StockMovement
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; } = "";
    public long UserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
