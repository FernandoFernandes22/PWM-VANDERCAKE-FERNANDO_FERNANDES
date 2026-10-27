using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using VanderCake.Api.Data;
using VanderCake.Api.Services;
using VanderCake.Contracts;
using VanderCake.Core;
using static VanderCake.Api.Services.ApiSupport;
namespace VanderCake.Api.Endpoints;

public record PreviewTicket(long AdminId, long[] OrderIds, string Fingerprint, DateTime ExpiresAtUtc);

public static class OrderEndpoints
{
    private static IQueryable<Order> Scope(AppDbContext db, HttpContext c) => (c.User.IsAdmin() ? db.Orders : db.Orders.Where(x => x.SellerId == c.User.UserId())).Include(x => x.Client).Include(x => x.Items);
    public static void MapOrders(this RouteGroupBuilder api)
    {
        api.MapGet("/pedidos", async (AppDbContext db, HttpContext c, int? page, int? pageSize, string? situacao, long? clienteId) =>
        {
            var (p, size) = Paging(page, pageSize); var q = Scope(db, c).AsNoTracking();
            if (!string.IsNullOrEmpty(situacao)) q = q.Where(x => x.Status == situacao);
            if (clienteId.HasValue) q = q.Where(x => x.ClientId == clienteId.Value);
            var count = await q.CountAsync();
            var rows = await q.OrderByDescending(x => x.SentAtUtc).ThenByDescending(x => x.Id).Skip((p - 1) * size).Take(size).ToListAsync();
            return Results.Ok(new PageResult<OrderDto>(rows.Select(Dto).ToArray(), p, size, count));
        });
        api.MapGet("/pedidos/{id:long}", async (long id, AppDbContext db, HttpContext c) =>
        {
            var row = await Scope(db, c).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id); return row is null ? Results.NotFound() : Results.Ok(Dto(row));
        });
        api.MapGet("/pedidos/por-chave/{key:guid}", async (Guid key, AppDbContext db, HttpContext c) =>
        {
            var row = await Scope(db, c).AsNoTracking().SingleOrDefaultAsync(x => x.SellerId == c.User.UserId() && x.RequestKey == key); return row is null ? Results.NotFound() : Results.Ok(Dto(row));
        }).RequireAuthorization(p => p.RequireRole("VENDEDOR"));
        api.MapPost("/pedidos", async (OrderInput input, HttpContext c, AppDbContext db) =>
        {
            var key = Key(c.Request); var hash = Hash(input); var sellerId = c.User.UserId();
            var existing = await Scope(db, c).SingleOrDefaultAsync(x => x.SellerId == sellerId && x.RequestKey == key);
            if (existing is not null)
            {
                if (existing.RequestHash != hash) throw new DomainException(409, "CHAVE_REUTILIZADA", "Esta chave já foi usada para outro conteúdo.");
                return Results.Ok(Dto(existing));
            }
            Require(input.Itens is { Count: > 0 and <= 100 } && input.Itens.All(x => x is not null && x.Quantidade is > 0 and <= 1000000) && input.Itens.Select(x => x.ProdutoId).Distinct().Count() == input.Itens.Count, "Informe até 100 produtos distintos com quantidades positivas.");
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var client = await db.Clients.SingleOrDefaultAsync(x => x.Id == input.ClienteId && x.SellerId == sellerId && x.Active);
            if (client is null) return Results.NotFound();
            var terms = await db.PaymentTerms.Include(x => x.Parts).SingleOrDefaultAsync(x => x.Id == input.CondicaoPagamentoId && x.Active);
            var method = await db.PaymentMethods.SingleOrDefaultAsync(x => x.Id == input.FormaPagamentoId && x.Active);
            Require(terms is not null && method is not null, "Escolha condição e forma de pagamento ativas.");
            var ids = input.Itens.Select(x => x.ProdutoId).ToArray();
            var products = await db.Products.Where(x => ids.Contains(x.Id) && x.Active).OrderBy(x => x.Id).ToDictionaryAsync(x => x.Id);
            Require(products.Count == ids.Length, "Há produto indisponível no pedido.");
            var order = new Order { Client = client, SellerId = sellerId, SentAtUtc = DateTime.UtcNow, RequestKey = key, RequestHash = hash, TermsName = terms!.Name, MethodName = method!.Name, TermsSnapshot = JsonSerializer.Serialize(terms.Parts.OrderBy(x => x.Number).Select(x => new PartDto(x.Number, x.Days, x.Percentage)), ApiJson.Options) };
            order.DailyInterestPercent = terms.DailyInterestPercent; order.DailyDiscountPercent = terms.DailyDiscountPercent;
            foreach (var item in input.Itens)
            {
                var product = products[item.ProdutoId];
                if (product.Price != item.PrecoConsultado) throw new DomainException(409, "PRECO_ALTERADO", $"O preço de {product.Description} mudou. Atualize o catálogo e confirme novamente.");
                order.Items.Add(new OrderItem { ProductId = product.Id, Description = product.Description, Unit = product.Unit, Requested = item.Quantidade, UnitPrice = product.Price });
            }
            order.RequestedTotal = order.Items.Sum(x => x.UnitPrice * x.Requested);
            Require(order.RequestedTotal <= 9999999999999.99m, "Total excede o limite suportado.");
            db.Orders.Add(order); await db.SaveChangesAsync(); await tx.CommitAsync();
            return Results.Created($"/api/v1/pedidos/{order.Id}", Dto(order));
        }).RequireAuthorization(p => p.RequireRole("VENDEDOR"));

        api.MapPost("/pedidos/{id:long}/cancelamento", async (long id, CancellationInput input, AppDbContext db) =>
        {
            var row = await db.Orders.FindAsync(id); if (row is null) return Results.NotFound();
            if (row.Status != "ENVIADO") throw new DomainException(409, "ESTADO_INVALIDO", "Somente pedidos enviados podem ser cancelados.");
            row.Status = "CANCELADO"; row.CancellationReason = Text(input.Motivo, "Motivo"); row.Version++;
            await db.SaveChangesAsync(); return Results.NoContent();
        }).RequireAuthorization("Admin");

        api.MapPost("/atendimentos/previa", async (PreviewInput input, AppDbContext db, IDataProtectionProvider provider, HttpContext c) =>
        {
            Require(input.PedidoIds is { Length: > 0 and <= 100 } && input.PedidoIds.Distinct().Count() == input.PedidoIds.Length, "Selecione de 1 a 100 pedidos distintos.");
            var (orders, products) = await Load(db, input.PedidoIds);
            var expiry = DateTime.UtcNow.AddMinutes(10);
            var ticket = new PreviewTicket(c.User.UserId(), input.PedidoIds.Order().ToArray(), Fingerprint(orders, products), expiry);
            var token = provider.CreateProtector("VanderCake.Preview.v1").Protect(JsonSerializer.Serialize(ticket));
            return Results.Ok(new PreviewResult(token, expiry, Allocate(orders, products)));
        }).RequireAuthorization("Admin");

        api.MapPost("/atendimentos/confirmacao", async (ConfirmInput input, HttpContext c, AppDbContext db, IDataProtectionProvider provider) =>
        {
            var key = Key(c.Request); var hash = Hash(input); var admin = c.User.UserId();
            var existing = await db.Batches.Include(x => x.Sales).SingleOrDefaultAsync(x => x.AdminId == admin && x.RequestKey == key);
            if (existing is not null)
            {
                if (existing.RequestHash != hash) throw new DomainException(409, "CHAVE_REUTILIZADA", "Chave já usada em outra confirmação.");
                return Results.Ok(new BatchResult(existing.Id, existing.Sales.Select(x => x.Id).ToArray()));
            }
            PreviewTicket? ticket;
            try { ticket = JsonSerializer.Deserialize<PreviewTicket>(provider.CreateProtector("VanderCake.Preview.v1").Unprotect(input.PreviewToken)); }
            catch { throw new DomainException(409, "PREVIA_INVALIDA", "Gere uma nova prévia."); }
            if (ticket is null || ticket.AdminId != admin || ticket.ExpiresAtUtc <= DateTime.UtcNow) throw new DomainException(409, "PREVIA_EXPIRADA", "Gere uma nova prévia.");
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var (orders, products) = await Load(db, ticket.OrderIds);
            if (Fingerprint(orders, products) != ticket.Fingerprint) throw new DomainException(409, "PREVIA_DESATUALIZADA", "Pedidos ou estoque mudaram. Confira uma nova prévia.");
            var allocations = Allocate(orders, products).ToDictionary(x => (x.PedidoId, x.ProdutoId));
            var batch = new Batch { AdminId = admin, RequestKey = key, RequestHash = hash, ConfirmedAtUtc = DateTime.UtcNow };
            var today = Today();
            foreach (var order in orders)
            {
                foreach (var item in order.Items)
                {
                    item.Approved = allocations[(order.Id, item.ProductId)].Aprovado;
                    if (item.Approved > 0)
                    {
                        var product = products.Single(x => x.Id == item.ProductId);
                        product.Stock -= item.Approved.Value; product.Version++;
                        db.StockMovements.Add(new StockMovement { ProductId = product.Id, Quantity = -item.Approved.Value, Reason = $"Atendimento do pedido {order.Id}", UserId = admin, CreatedAtUtc = DateTime.UtcNow });
                    }
                }
                order.ApprovedTotal = order.Items.Sum(x => x.UnitPrice * x.Approved!.Value); order.Version++;
                order.Status = order.ApprovedTotal > 0 ? "ATENDIDO" : "NAO_ATENDIDO";
                if (order.ApprovedTotal == 0) continue;
                var terms = JsonSerializer.Deserialize<PartDto[]>(order.TermsSnapshot, ApiJson.Options)!;
                var amounts = CommercialRules.Installments(order.ApprovedTotal, terms.Select(x => x.Percentual).ToArray());
                batch.Sales.Add(new Sale { Order = order, Date = today, Total = order.ApprovedTotal,
                    Receivables = terms.Select((x, i) => new Receivable { Number = x.Numero, DueDate = today.AddDays(x.Dias), Amount = amounts[i], DailyInterestPercent = order.DailyInterestPercent, DailyDiscountPercent = order.DailyDiscountPercent }).ToList() });
            }
            db.Batches.Add(batch); await db.SaveChangesAsync(); await tx.CommitAsync();
            return Results.Ok(new BatchResult(batch.Id, batch.Sales.Select(x => x.Id).ToArray()));
        }).RequireAuthorization("Admin");
        api.MapGet("/atendimentos/{id:long}", async (long id, AppDbContext db) =>
        {
            var b = await db.Batches.AsNoTracking().Include(x => x.Sales).SingleOrDefaultAsync(x => x.Id == id);
            return b is null ? Results.NotFound() : Results.Ok(new BatchResult(b.Id, b.Sales.Select(x => x.Id).ToArray()));
        }).RequireAuthorization("Admin");
    }
    private static async Task<(List<Order>, List<Product>)> Load(AppDbContext db, long[] ids)
    {
        var orders = await db.Orders.Include(x => x.Client).Include(x => x.Items).Where(x => ids.Contains(x.Id)).OrderBy(x => x.Id).ToListAsync();
        if (orders.Count != ids.Length || orders.Any(x => x.Status != "ENVIADO" || !x.Client.Active)) throw new DomainException(409, "PEDIDO_INDISPONIVEL", "Há pedido já processado ou cliente inativo. Atualize a seleção.");
        var productIds = orders.SelectMany(x => x.Items).Select(x => x.ProductId).Distinct().ToArray();
        var products = await db.Products.Where(x => productIds.Contains(x.Id)).OrderBy(x => x.Id).ToListAsync();
        return (orders, products);
    }
    private static string Fingerprint(List<Order> orders, List<Product> products) => Hash(new
    {
        orders = orders.Select(x => new { x.Id, x.Version, x.Status, x.Client.Active }),
        products = products.Select(x => new { x.Id, x.Version, x.Stock, x.Active })
    });
    private static List<PreviewItem> Allocate(List<Order> orders, List<Product> products)
    {
        var result = new List<PreviewItem>();
        foreach (var product in products)
        {
            var rows = orders.SelectMany(o => o.Items.Where(i => i.ProductId == product.Id).Select(i => new { Order = o, Item = i })).ToArray();
            var demand = rows.GroupBy(x => x.Order.ClientId).Select(g => new Demand(g.Key, checked(g.Sum(x => x.Item.Requested)), g.Min(x => x.Order.SentAtUtc))).ToArray();
            var shares = CommercialRules.Allocate(product.Active ? product.Stock : 0, demand);
            foreach (var client in rows.GroupBy(x => x.Order.ClientId))
            {
                int remaining = shares[client.Key];
                foreach (var row in client.OrderBy(x => x.Order.SentAtUtc).ThenBy(x => x.Order.Id))
                {
                    int amount = Math.Min(remaining, row.Item.Requested); remaining -= amount;
                    result.Add(new PreviewItem(row.Order.Id, row.Order.ClientId, row.Order.Client.Name, product.Id, product.Description, product.Stock, row.Item.Requested, amount));
                }
            }
        }
        return result;
    }
    public record CancellationInput(string Motivo);
}
