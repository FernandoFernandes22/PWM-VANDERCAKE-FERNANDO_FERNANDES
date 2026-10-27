using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VanderCake.Core;
namespace VanderCake.Api.Data;

public static class SeedData
{
    private static string Password(IConfiguration config, string key, bool development)
    {
        var minimum = development && config.GetValue<bool>("Seed:SimpleDemoPasswords") ? 4 : 12;
        var value = config[$"Seed:{key}"];
        return value is not null && value.Length >= minimum ? value : throw new InvalidOperationException($"Configure Seed:{key} com pelo menos {minimum} caracteres.");
    }
    public static async Task ResetDemoPasswords(AppDbContext db, IConfiguration config, bool development)
    {
        if (!development || !config.GetValue<bool>("Seed:SimpleDemoPasswords")) throw new InvalidOperationException("Redefinição de contas demonstrativas disponível somente no modo de desenvolvimento explicitamente configurado.");
        var hasher = new PasswordHasher<User>();
        foreach (var account in new[] { (Login: "admin", Role: "ADMIN", Key: "AdminPassword"), (Login: "vendedor", Role: "VENDEDOR", Key: "SellerPassword"), (Login: "vendedor2", Role: "VENDEDOR", Key: "OtherSellerPassword") })
        {
            var user = await db.Users.SingleOrDefaultAsync(x => x.Login == account.Login && x.Role == account.Role) ?? throw new InvalidOperationException("Uma das contas de demonstração não existe; execute a carga inicial primeiro.");
            user.PasswordHash = hasher.HashPassword(user, Password(config, account.Key, development));
        }
        await db.SaveChangesAsync();
        Console.WriteLine("Senhas das três contas de demonstração atualizadas; pedidos e demais dados preservados.");
    }
    public static async Task Run(AppDbContext db, IConfiguration config, bool development = false)
    {
        if (await db.Users.AnyAsync()) { Console.WriteLine("Carga inicial já existente; nenhum dado alterado."); return; }
        User Create(string login, string name, string role, string password)
        {
            var u = new User { Login = login, Name = name, Role = role };
            u.PasswordHash = new PasswordHasher<User>().HashPassword(u, password); return u;
        }
        var admin = Create("admin", "Administração", "ADMIN", Password(config, "AdminPassword", development));
        var seller = Create("vendedor", "Vendedor demonstração", "VENDEDOR", Password(config, "SellerPassword", development));
        var other = Create("vendedor2", "Outro vendedor", "VENDEDOR", Password(config, "OtherSellerPassword", development));
        await using var tx = await db.Database.BeginTransactionAsync();
        db.Users.AddRange(admin, seller, other); await db.SaveChangesAsync();
        db.Clients.AddRange(new[] { "Padaria Aurora", "Confeitaria Bela Massa", "Mercado Canto Doce" }.Select((name, i) => new Client { Name = name, City = "São Paulo", State = "SP", Address = $"Rua Exemplo, {i + 1}", SellerId = seller.Id, VisitOrder = (i + 1) * 10 }));
        db.Clients.Add(new Client { Name = "Cliente de outra carteira", City = "São Paulo", State = "SP", Address = "Rua Fictícia, 50", SellerId = other.Id, VisitOrder = 10 });
        var products = new[] {
            new Product { Code = "FER001", Description = "Fermento para panificação", Group = "Fermentos", Unit = "PCT", Price = 12.50m, Stock = 15 },
            new Product { Code = "MIS001", Description = "Mistura para bolo", Group = "Misturas", Unit = "CX", Price = 35m, Stock = 50 },
            new Product { Code = "MAR001", Description = "Margarina para confeitaria", Group = "Margarinas", Unit = "BALDE", Price = 89.90m, Stock = 0 }
        };
        db.Products.AddRange(products);
        db.PaymentTerms.AddRange(new PaymentTerms { DailyInterestPercent = 2, DailyDiscountPercent = 1, Name = "À vista", Parts = [new() { Number = 1, Days = 0, Percentage = 100 }] },
            new PaymentTerms { DailyInterestPercent = 2, DailyDiscountPercent = 1, Name = "7 e 14 dias", Parts = [new() { Number = 1, Days = 7, Percentage = 50 }, new() { Number = 2, Days = 14, Percentage = 50 }] },
            new PaymentTerms { DailyInterestPercent = 2, DailyDiscountPercent = 1, Name = "7, 14 e 21 dias", Parts = [new() { Number = 1, Days = 7, Percentage = 33.33m }, new() { Number = 2, Days = 14, Percentage = 33.33m }, new() { Number = 3, Days = 21, Percentage = 33.34m }] });
        db.PaymentMethods.AddRange(new PaymentMethod { Name = "Boleto (registro)" }, new PaymentMethod { Name = "PIX (registro)" }, new PaymentMethod { Name = "Dinheiro" });
        await db.SaveChangesAsync();
        db.StockMovements.AddRange(products.Where(x => x.Stock > 0).Select(x => new StockMovement { ProductId = x.Id, Quantity = x.Stock, Reason = "Saldo inicial fictício", UserId = admin.Id, CreatedAtUtc = DateTime.UtcNow }));
        await db.SaveChangesAsync(); await tx.CommitAsync();
        Console.WriteLine("Carga fictícia criada. Senhas permanecem apenas na configuração local.");
    }
}
