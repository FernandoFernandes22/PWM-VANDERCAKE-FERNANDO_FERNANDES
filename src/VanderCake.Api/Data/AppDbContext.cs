using Microsoft.EntityFrameworkCore;
using VanderCake.Core;
namespace VanderCake.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<PaymentTerms> PaymentTerms => Set<PaymentTerms>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<Receivable> Receivables => Set<Receivable>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>().HasIndex(x => x.Login).IsUnique();
        model.Entity<Client>().HasOne(x => x.Seller).WithMany().HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Client>().HasIndex(x => new { x.SellerId, x.VisitOrder });
        model.Entity<Product>().HasIndex(x => x.Code).IsUnique();
        model.Entity<Product>().Property(x => x.Version).IsConcurrencyToken();
        model.Entity<Product>().ToTable(t => t.HasCheckConstraint("CK_Product_Stock", "`Stock` >= 0"));
        model.Entity<Order>().HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Order>().HasOne<User>().WithMany().HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Order>().HasIndex(x => new { x.SellerId, x.RequestKey }).IsUnique();
        model.Entity<Order>().HasIndex(x => new { x.Status, x.SentAtUtc });
        model.Entity<Order>().Property(x => x.Version).IsConcurrencyToken();
        model.Entity<Order>().Property(x => x.TermsSnapshot).HasColumnType("longtext");
        model.Entity<OrderItem>().HasIndex(x => new { x.OrderId, x.ProductId }).IsUnique();
        model.Entity<OrderItem>().HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<TermPart>().HasIndex(x => new { x.PaymentTermsId, x.Number }).IsUnique();
        model.Entity<Batch>().HasIndex(x => new { x.AdminId, x.RequestKey }).IsUnique();
        model.Entity<Sale>().HasIndex(x => x.OrderId).IsUnique();
        model.Entity<Sale>().HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Receivable>().HasIndex(x => new { x.SaleId, x.Number }).IsUnique();
        model.Entity<Receipt>().HasIndex(x => new { x.AdminId, x.RequestKey }).IsUnique();
        model.Entity<Sale>().Property(x => x.Date).HasConversion(x => x.ToDateTime(TimeOnly.MinValue), x => DateOnly.FromDateTime(x)).HasColumnType("date");
        model.Entity<Receivable>().Property(x => x.DueDate).HasConversion(x => x.ToDateTime(TimeOnly.MinValue), x => DateOnly.FromDateTime(x)).HasColumnType("date");
        model.Entity<Receipt>().Property(x => x.Date).HasConversion(x => x.ToDateTime(TimeOnly.MinValue), x => DateOnly.FromDateTime(x)).HasColumnType("date");
        model.Entity<StockMovement>().HasIndex(x => new { x.ProductId, x.CreatedAtUtc });
        model.Entity<StockMovement>().HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        foreach (var entity in model.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties())
        {
            if (property.ClrType == typeof(decimal)) { property.SetPrecision(15); property.SetScale(2); }
            if (property.ClrType == typeof(string) && property.Name != nameof(Order.TermsSnapshot)) property.SetMaxLength(250);
        }
    }
}
