using Microsoft.EntityFrameworkCore;
using VanderCake.Api.Data;
using VanderCake.Api.Services;
using VanderCake.Contracts;
using VanderCake.Core;
using static VanderCake.Api.Services.ApiSupport;
namespace VanderCake.Api.Endpoints;

public static class CatalogEndpoints
{
    public static void MapCatalog(this RouteGroupBuilder api)
    {
        api.MapGet("/vendedores", async (AppDbContext db) => Results.Ok(await db.Users.Where(x => x.Role == "VENDEDOR" && x.Active).Select(x => new UserDto(x.Id, x.Name, x.Role)).ToListAsync())).RequireAuthorization("Admin");
        api.MapGet("/clientes", async (AppDbContext db, HttpContext c, int? page, int? pageSize, string? busca) =>
        {
            var (p, size) = Paging(page, pageSize);
            var q = AccessibleClients(db, c);
            if (!string.IsNullOrWhiteSpace(busca)) q = q.Where(x => x.Name.Contains(busca));
            var count = await q.CountAsync();
            var rows = await q.OrderBy(x => x.VisitOrder).ThenBy(x => x.Id).Skip((p - 1) * size).Take(size).ToListAsync();
            return Results.Ok(new PageResult<ClientDto>(rows.Select(Dto).ToArray(), p, size, count));
        });
        api.MapGet("/carteira", async (AppDbContext db, HttpContext c) => Results.Ok((await AccessibleClients(db, c).Where(x => x.Active).OrderBy(x => x.VisitOrder).ThenBy(x => x.Id).ToListAsync()).Select(Dto))).RequireAuthorization(p => p.RequireRole("VENDEDOR"));
        api.MapGet("/clientes/{id:long}", async (long id, AppDbContext db, HttpContext c) =>
        {
            var row = await AccessibleClients(db, c).SingleOrDefaultAsync(x => x.Id == id);
            return row is null ? Results.NotFound() : Results.Ok(Dto(row));
        });
        api.MapPost("/clientes", async (ClientInput input, AppDbContext db, HttpContext c) =>
        {
            var client = new Client();
            await ApplyClient(client, input, c.User.IsAdmin() ? input.VendedorId ?? 0 : c.User.UserId(), db);
            db.Clients.Add(client); await db.SaveChangesAsync();
            return Results.Created($"/api/v1/clientes/{client.Id}", Dto(client));
        });
        api.MapPut("/clientes/{id:long}", async (long id, ClientInput input, AppDbContext db) =>
        {
            var row = await db.Clients.FindAsync(id);
            if (row is null) return Results.NotFound();
            var seller = input.VendedorId ?? row.SellerId;
            if (seller != row.SellerId && await db.Orders.AnyAsync(x => x.ClientId == id && x.Status == "ENVIADO")) throw new DomainException(409, "PEDIDO_PENDENTE", "Conclua os pedidos antes de trocar o vendedor.");
            await ApplyClient(row, input, seller, db); await db.SaveChangesAsync(); return Results.Ok(Dto(row));
        }).RequireAuthorization("Admin");
        api.MapGet("/produtos", async (AppDbContext db, HttpContext c, int? page, int? pageSize, string? busca) =>
        {
            var (p, size) = Paging(page, pageSize);
            var q = db.Products.AsNoTracking().AsQueryable();
            if (!c.User.IsAdmin()) q = q.Where(x => x.Active);
            if (!string.IsNullOrWhiteSpace(busca)) q = q.Where(x => x.Description.Contains(busca) || x.Code.Contains(busca));
            var count = await q.CountAsync();
            return Results.Ok(new PageResult<ProductDto>((await q.OrderBy(x => x.Description).Skip((p - 1) * size).Take(size).ToListAsync()).Select(Dto).ToArray(), p, size, count));
        });
        api.MapPost("/produtos", async (ProductInput input, AppDbContext db) =>
        {
            var row = new Product(); ApplyProduct(row, input); db.Products.Add(row); await db.SaveChangesAsync(); return Results.Created($"/api/v1/produtos/{row.Id}", Dto(row));
        }).RequireAuthorization("Admin");
        api.MapPut("/produtos/{id:long}", async (long id, ProductInput input, AppDbContext db) =>
        {
            var row = await db.Products.FindAsync(id); if (row is null) return Results.NotFound();
            ApplyProduct(row, input); row.Version++; await db.SaveChangesAsync(); return Results.Ok(Dto(row));
        }).RequireAuthorization("Admin");
        api.MapGet("/condicoes-pagamento", async (AppDbContext db) => Results.Ok((await db.PaymentTerms.Include(x => x.Parts).OrderBy(x => x.Id).ToListAsync()).Select(Dto)));
        api.MapPost("/condicoes-pagamento", async (TermsInput input, AppDbContext db) =>
        {
            var row = new PaymentTerms(); ApplyTerms(row, input); db.PaymentTerms.Add(row); await db.SaveChangesAsync(); return Results.Created($"/api/v1/condicoes-pagamento/{row.Id}", Dto(row));
        }).RequireAuthorization("Admin");
        api.MapPut("/condicoes-pagamento/{id:long}", async (long id, TermsInput input, AppDbContext db) =>
        {
            var row = await db.PaymentTerms.Include(x => x.Parts).SingleOrDefaultAsync(x => x.Id == id);
            if (row is null) return Results.NotFound();
            // Remove e recria as partes na mesma transação para preservar a chave número/condição.
            await using var tx = await db.Database.BeginTransactionAsync();
            db.RemoveRange(row.Parts); await db.SaveChangesAsync();
            ApplyTerms(row, input); await db.SaveChangesAsync(); await tx.CommitAsync(); return Results.Ok(Dto(row));
        }).RequireAuthorization("Admin");
        api.MapGet("/formas-pagamento", async (AppDbContext db) => Results.Ok(await db.PaymentMethods.OrderBy(x => x.Id).Select(x => new MethodDto(x.Id, x.Name, x.Active)).ToListAsync()));
        api.MapPost("/formas-pagamento", async (MethodInput input, AppDbContext db) =>
        {
            var row = new PaymentMethod { Name = Text(input.Nome, "Nome"), Active = input.Ativo }; db.PaymentMethods.Add(row); await db.SaveChangesAsync(); return Results.Created($"/api/v1/formas-pagamento/{row.Id}", new MethodDto(row.Id, row.Name, row.Active));
        }).RequireAuthorization("Admin");
        api.MapGet("/estoque", async (AppDbContext db) => Results.Ok(await db.Products.OrderBy(x => x.Description).Select(x => new { produtoId = x.Id, produto = x.Description, saldo = x.Stock }).ToListAsync())).RequireAuthorization("Admin");
        api.MapPost("/estoque/movimentos", async (StockInput input, AppDbContext db, HttpContext c) =>
        {
            Require(input.Quantidade != 0 && Math.Abs((long)input.Quantidade) <= 1000000, "Informe um ajuste válido de estoque.");
            var reason = Text(input.Motivo, "Motivo");
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var product = await db.Products.SingleOrDefaultAsync(x => x.Id == input.ProdutoId);
            if (product is null) return Results.NotFound();
            Require((long)product.Stock + input.Quantidade is >= 0 and <= int.MaxValue, "O ajuste produziria saldo inválido.");
            product.Stock += input.Quantidade; product.Version++;
            db.StockMovements.Add(new StockMovement { ProductId = product.Id, Quantity = input.Quantidade, Reason = reason, UserId = c.User.UserId(), CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync(); await tx.CommitAsync(); return Results.Ok(new { produtoId = product.Id, saldo = product.Stock });
        }).RequireAuthorization("Admin");
    }
    public static IQueryable<Client> AccessibleClients(AppDbContext db, HttpContext c) => c.User.IsAdmin() ? db.Clients : db.Clients.Where(x => x.SellerId == c.User.UserId());
    private static async Task ApplyClient(Client row, ClientInput input, long sellerId, AppDbContext db)
    {
        Require(await db.Users.AnyAsync(x => x.Id == sellerId && x.Role == "VENDEDOR" && x.Active), "Escolha um vendedor ativo.");
        row.Name = Text(input.Nome, "Nome"); row.City = Text(input.Cidade, "Cidade"); row.State = Text(input.Estado, "UF", 2).ToUpperInvariant();
        Require(row.State.Length == 2 && row.State.All(char.IsLetter), "Informe a UF com duas letras.");
        row.Address = Text(input.Endereco, "Endereço", 200); row.Phone = Optional(input.Telefone, 30); row.SellerId = sellerId;
        Require(input.OrdemVisita >= 0, "Ordem de visita deve ser positiva ou zero."); row.VisitOrder = input.OrdemVisita; row.Active = input.Ativo;
    }
    private static void ApplyProduct(Product row, ProductInput input)
    {
        Require(input.Preco > 0 && input.Preco <= 1000000 && decimal.Round(input.Preco, 2) == input.Preco, "Preço inválido; use até duas casas decimais.");
        row.Code = Text(input.Codigo, "Código", 40); row.Description = Text(input.Descricao, "Descrição"); row.Group = Text(input.Grupo, "Grupo"); row.Unit = Text(input.Unidade, "Unidade", 20); row.Price = input.Preco; row.Active = input.Ativo;
    }
    private static void ApplyTerms(PaymentTerms row, TermsInput input)
    {
        SettlementRules.ValidateRates(input.JurosDiaPercentual, input.DescontoDiaPercentual);
        row.DailyInterestPercent = input.JurosDiaPercentual; row.DailyDiscountPercent = input.DescontoDiaPercentual;
        Require(input.Parcelas is { Count: > 0 and <= 24 }, "Informe de 1 a 24 parcelas.");
        var ordered = input.Parcelas.OrderBy(x => x.Numero).ToArray();
        Require(ordered.Select(x => x.Numero).SequenceEqual(Enumerable.Range(1, ordered.Length)) && ordered.All(x => x.Dias is >= 0 and <= 3650 && x.Percentual > 0 && decimal.Round(x.Percentual, 2) == x.Percentual) && ordered.Sum(x => x.Percentual) == 100m, "Confira os números, prazos e percentuais; a soma deve ser 100%.");
        row.Name = Text(input.Nome, "Nome"); row.Active = input.Ativo;
        row.Parts = ordered.Select(x => new TermPart { Number = x.Numero, Days = x.Dias, Percentage = x.Percentual }).ToList();
    }
}
