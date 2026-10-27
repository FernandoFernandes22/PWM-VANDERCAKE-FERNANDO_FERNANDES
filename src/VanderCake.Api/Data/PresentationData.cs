using Microsoft.EntityFrameworkCore;
using VanderCake.Core;

namespace VanderCake.Api.Data;

public static class PresentationData
{
    private static string LegacyProductName(long id) => new[]
    {
        "Leite em pó integral 1 kg", "Coco ralado fino 1 kg", "Melhorador para panificação 1 kg", "Amido de milho 1 kg",
        "Gordura vegetal para confeitaria 1 kg", "Glucose de milho 1 kg", "Geleia de brilho 1 kg", "Emulsificante para bolos 1 kg"
    }[(int)(id % 8)];
    public static async Task Prepare(AppDbContext db, bool development)
    {
        if (!development) throw new InvalidOperationException("Preparação de demonstração disponível somente em Development.");
        await using var tx = await db.Database.BeginTransactionAsync();
        var testIds = await db.Products.Where(x => x.Group == "Testes" && x.Code.StartsWith("TEST-"))
            .Select(x => x.Id).ToArrayAsync();
        // Preserva pedidos reais ou mistos; remove somente pedidos compostos inteiramente por produtos dos testes automatizados.
        var testOrders = await db.Orders.Where(x => x.Items.Any() && x.Items.All(i => testIds.Contains(i.ProductId)))
            .Select(x => x.Id).ToArrayAsync();
        var testSales = db.Sales.Where(x => testOrders.Contains(x.OrderId)).Select(x => x.Id);
        var testAccounts = db.Receivables.Where(x => testSales.Contains(x.SaleId)).Select(x => x.Id);
        await db.Receipts.Where(x => testAccounts.Contains(x.ReceivableId)).ExecuteDeleteAsync();
        await db.Receivables.Where(x => testSales.Contains(x.SaleId)).ExecuteDeleteAsync();
        await db.Sales.Where(x => testOrders.Contains(x.OrderId)).ExecuteDeleteAsync();
        await db.Set<OrderItem>().Where(x => testOrders.Contains(x.OrderId)).ExecuteDeleteAsync();
        await db.Orders.Where(x => testOrders.Contains(x.Id)).ExecuteDeleteAsync();
        await db.Batches.Where(x => !x.Sales.Any()).ExecuteDeleteAsync();
        var removableProducts = await db.Products.Where(x => testIds.Contains(x.Id) && !db.Set<OrderItem>().Any(i => i.ProductId == x.Id)).Select(x => x.Id).ToArrayAsync();
        await db.StockMovements.Where(x => removableProducts.Contains(x.ProductId)).ExecuteDeleteAsync();
        await db.Products.Where(x => removableProducts.Contains(x.Id)).ExecuteDeleteAsync();
        foreach (var retained in await db.Products.Where(x => testIds.Contains(x.Id) || x.Code.StartsWith("ING") && x.Description == "Melhorador para panificação 1 kg").ToListAsync())
        {
            // Um produto já usado em pedido do usuário não pode ser excluído. Troca apenas o rótulo fictício.
            retained.Code = "ING" + retained.Id.ToString("D3");
            retained.Description = LegacyProductName(retained.Id); retained.Group = "Ingredientes"; retained.Active = true;
            await db.Set<OrderItem>().Where(x => x.ProductId == retained.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Description, retained.Description));
        }

        var seller = await db.Users.SingleAsync(x => x.Login == "vendedor");
        var other = await db.Users.SingleAsync(x => x.Login == "vendedor2");
        seller.Name = "Mariana Costa"; other.Name = "Rafael Almeida";
        var secondClient = await db.Clients.SingleOrDefaultAsync(x => x.Name == "Cliente de outra carteira");
        if (secondClient is not null) secondClient.Name = "Panificadora São Bento";

        var products = new (string Code, string Name, string Group, string Unit, decimal Price, int Stock)[]
        {
            ("FAR001", "Farinha de trigo especial 25 kg", "Farinhas", "SACO", 89.90m, 80),
            ("FAR002", "Farinha de trigo integral 5 kg", "Farinhas", "PCT", 27.50m, 45),
            ("ACU001", "Açúcar refinado 5 kg", "Açúcares", "PCT", 19.90m, 65),
            ("ACU002", "Açúcar de confeiteiro 1 kg", "Açúcares", "PCT", 14.90m, 40),
            ("CHO001", "Chocolate meio amargo 1 kg", "Chocolates", "BARRA", 42.90m, 35),
            ("CHO002", "Chocolate branco 1 kg", "Chocolates", "BARRA", 45.90m, 30),
            ("CAC001", "Cacau em pó 50% 1 kg", "Chocolates", "PCT", 38.50m, 25),
            ("REC001", "Doce de leite para recheio 4,8 kg", "Recheios", "BALDE", 74.90m, 24),
            ("REC002", "Recheio de morango 1,01 kg", "Recheios", "PCT", 22.90m, 36),
            ("COB001", "Chantilly vegetal 1 litro", "Coberturas", "CX", 18.90m, 60),
            ("COB002", "Granulado de chocolate 1 kg", "Coberturas", "PCT", 21.50m, 40),
            ("MIS002", "Mistura para pão de queijo 5 kg", "Misturas", "PCT", 54.90m, 30),
            ("MIS003", "Mistura para brownie 2 kg", "Misturas", "PCT", 39.90m, 20),
            ("FER002", "Fermento biológico seco 500 g", "Fermentos", "PCT", 16.90m, 50),
            ("ESS001", "Essência de baunilha 960 ml", "Ingredientes", "FRASCO", 24.90m, 18),
            ("EMB001", "Caixa para bolo 30 cm — 10 unidades", "Embalagens", "PCT", 32.90m, 40),
            ("EMB002", "Forma de papel para cupcake — 100 unidades", "Embalagens", "PCT", 12.90m, 55)
        };
        foreach (var p in products)
        {
            if (await db.Products.AnyAsync(x => x.Code == p.Code)) continue;
            var product = new Product { Code = p.Code, Description = p.Name, Group = p.Group, Unit = p.Unit, Price = p.Price, Stock = p.Stock };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            db.StockMovements.Add(new StockMovement { ProductId = product.Id, Quantity = p.Stock, Reason = "Saldo de demonstração", UserId = (await db.Users.SingleAsync(x => x.Login == "admin")).Id, CreatedAtUtc = DateTime.UtcNow });
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        Console.WriteLine($"Apresentação preparada: {testOrders.Length} pedidos exclusivos de teste removidos, catálogo ampliado e pedidos dos usuários preservados.");
    }
}
