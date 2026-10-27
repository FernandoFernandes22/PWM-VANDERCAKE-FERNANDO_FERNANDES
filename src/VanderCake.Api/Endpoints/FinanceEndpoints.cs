using System.Data;
using Microsoft.EntityFrameworkCore;
using VanderCake.Api.Data;
using VanderCake.Api.Services;
using VanderCake.Contracts;
using VanderCake.Core;
using static VanderCake.Api.Services.ApiSupport;
namespace VanderCake.Api.Endpoints;

public static class FinanceEndpoints
{
    public static void MapFinance(this RouteGroupBuilder api)
    {
        api.MapGet("/vendas", async (AppDbContext db, int? page, int? pageSize) =>
        {
            var (p, size) = Paging(page, pageSize);
            var total = await db.Sales.CountAsync();
            var rows = await db.Sales.Include(x => x.Order).ThenInclude(x => x.Client).OrderByDescending(x => x.Id).Skip((p - 1) * size).Take(size).Select(x => new { x.Id, pedidoId = x.OrderId, cliente = x.Order.Client.Name, data = x.Date, total = x.Total }).ToListAsync();
            return Results.Ok(new { items = rows, page = p, pageSize = size, totalItems = total });
        }).RequireAuthorization("Admin");
        api.MapGet("/vendas/{id:long}", async (long id, AppDbContext db) =>
        {
            var sale = await db.Sales.Include(x => x.Order).ThenInclude(x => x.Client).Include(x => x.Order.Items).Include(x => x.Receivables).SingleOrDefaultAsync(x => x.Id == id);
            return sale is null ? Results.NotFound() : Results.Ok(new { sale.Id, data = sale.Date, total = sale.Total, pedido = Dto(sale.Order), parcelas = sale.Receivables.Select(x => new { x.Id, numero = x.Number, vencimento = x.DueDate, valor = x.Amount }) });
        }).RequireAuthorization("Admin");
        api.MapGet("/contas-receber", async (AppDbContext db, int? page, int? pageSize, long? clienteId, bool? aberta) =>
        {
            var (p, size) = Paging(page, pageSize); var q = Accounts(db);
            if (clienteId.HasValue) q = q.Where(x => x.Sale.Order.ClientId == clienteId.Value);
            if (aberta == true) q = q.Where(x => x.Amount > x.Receipts.Sum(r => (decimal?)r.PrincipalSettled).GetValueOrDefault());
            var total = await q.CountAsync(); var rows = await q.OrderBy(x => x.DueDate).ThenBy(x => x.Id).Skip((p - 1) * size).Take(size).ToListAsync();
            return Results.Ok(new { items = rows.Select(AccountDto), page = p, pageSize = size, totalItems = total });
        }).RequireAuthorization("Admin");
        api.MapGet("/contas-receber/{id:long}", async (long id, AppDbContext db) =>
        {
            var row = await Accounts(db).SingleOrDefaultAsync(x => x.Id == id); return row is null ? Results.NotFound() : Results.Ok(AccountDto(row));
        }).RequireAuthorization("Admin");
        api.MapGet("/contas-receber/{id:long}/simulacao", async (long id, DateOnly dataRecebimento, AppDbContext db) =>
        {
            var row = await Accounts(db).SingleOrDefaultAsync(x => x.Id == id);
            if (row is null) return Results.NotFound();
            ValidateDate(row, dataRecebimento);
            return Results.Ok(Quote(row, dataRecebimento));
        }).RequireAuthorization("Admin");
        api.MapPost("/contas-receber/{id:long}/recebimentos", async (long id, ReceiptInput input, AppDbContext db, HttpContext c) =>
        {
            var key = Key(c.Request); var hash = Hash(new { id, input }); var admin = c.User.UserId();
            var existing = await db.Receipts.AsNoTracking().SingleOrDefaultAsync(x => x.AdminId == admin && x.RequestKey == key);
            if (existing is not null)
            {
                if (existing.RequestHash != hash) throw new DomainException(409, "CHAVE_REUTILIZADA", "Chave usada em outro recebimento.");
                return Results.Ok(new { recebimentoId = existing.Id });
            }
            Require(input.Valor > 0 && decimal.Round(input.Valor, 2) == input.Valor, "Informe valor positivo com até duas casas decimais.");
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var row = await Accounts(db).SingleOrDefaultAsync(x => x.Id == id); if (row is null) return Results.NotFound();
            ValidateDate(row, input.DataRecebimento);
            var quote = Quote(row, input.DataRecebimento);
            if (input.SaldoConferido.HasValue && input.SaldoConferido.Value != quote.Total) throw new DomainException(409, "SALDO_ALTERADO", "O saldo mudou. Reabra o recebimento e confira uma nova simulação.");
            if (input.Valor > quote.Total) throw new DomainException(409, "VALOR_EXCEDE_SALDO", "O valor excede o saldo atualizado da parcela.");
            var allocation = SettlementRules.Allocate(quote, input.Valor);
            var receipt = new Receipt { ReceivableId = id, AdminId = admin, Amount = input.Valor, PrincipalSettled = allocation.PrincipalSettled, Interest = allocation.Interest, Discount = allocation.Discount, Date = input.DataRecebimento, RequestKey = key, RequestHash = hash };
            db.Receipts.Add(receipt); await db.SaveChangesAsync(); await tx.CommitAsync();
            return Results.Ok(new { recebimentoId = receipt.Id, saldo = Quote(row, input.DataRecebimento).Total, principalBaixado = receipt.PrincipalSettled, juros = receipt.Interest, desconto = receipt.Discount });
        }).RequireAuthorization("Admin");
        api.MapGet("/clientes/{id:long}/historico", async (long id, AppDbContext db, HttpContext c, int? page, int? pageSize) =>
        {
            if (!await CatalogEndpoints.AccessibleClients(db, c).AnyAsync(x => x.Id == id)) return Results.NotFound();
            var (p, size) = Paging(page, pageSize); var q = db.Sales.Where(x => x.Order.ClientId == id);
            var total = await q.CountAsync();
            var rows = await q.Include(x => x.Order).ThenInclude(x => x.Items).Include(x => x.Order).ThenInclude(x => x.Client).OrderByDescending(x => x.Id).Skip((p - 1) * size).Take(size).ToListAsync();
            return Results.Ok(new { items = rows.Select(x => new { x.Id, data = x.Date, total = x.Total, itens = Dto(x.Order).Itens.Where(i => i.QuantidadeAprovada > 0) }), page = p, pageSize = size, totalItems = total });
        });
        api.MapGet("/clientes/{id:long}/financeiro", async (long id, AppDbContext db, HttpContext c) =>
        {
            if (!await CatalogEndpoints.AccessibleClients(db, c).AnyAsync(x => x.Id == id)) return Results.NotFound();
            var rows = await Accounts(db).Where(x => x.Sale.Order.ClientId == id).ToListAsync(); var today = Today();
            decimal Balance(Receivable r) => Quote(r, today).Total;
            return Results.Ok(new { saldoTotal = rows.Sum(Balance), saldoVencido = rows.Where(x => x.DueDate < today).Sum(Balance), saldoAVencer = rows.Where(x => x.DueDate >= today).Sum(Balance), parcelas = rows.OrderBy(x => x.DueDate).Select(AccountDto) });
        });
    }
    private static IQueryable<Receivable> Accounts(AppDbContext db) => db.Receivables.Include(x => x.Sale).ThenInclude(x => x.Order).ThenInclude(x => x.Client).Include(x => x.Receipts);
    private static SettlementQuote Quote(Receivable row, DateOnly date) => SettlementRules.Quote(row.Amount - row.Receipts.Sum(x => x.PrincipalSettled), row.DueDate, date, row.DailyInterestPercent, row.DailyDiscountPercent);
    private static void ValidateDate(Receivable row, DateOnly date)
    {
        Require(date >= row.Sale.Date && date <= Today(), "Data deve estar entre a venda e hoje.");
        Require(!row.Receipts.Any(x => x.Date > date), "A data não pode ser anterior a um recebimento já registrado nesta parcela.");
    }
    private static object AccountDto(Receivable x)
    {
        decimal paid = x.Receipts.Sum(r => r.Amount); var quote = Quote(x, Today());
        return new { x.Id, vendaId = x.SaleId, cliente = x.Sale.Order.Client.Name, numero = x.Number, vencimento = x.DueDate, valor = x.Amount, recebido = paid, saldoPrincipal = quote.Principal, saldo = quote.Total, juros = quote.Interest, desconto = quote.Discount, jurosDiaPercentual = x.DailyInterestPercent, descontoDiaPercentual = x.DailyDiscountPercent, situacao = quote.Principal == 0 ? "PAGA" : paid > 0 ? "PARCIAL" : "ABERTA", atrasada = quote.Principal > 0 && x.DueDate < Today(), formaPagamento = x.Sale.Order.MethodName, recebimentos = x.Receipts.Select(r => new { r.Id, valor = r.Amount, principalBaixado = r.PrincipalSettled, juros = r.Interest, desconto = r.Discount, data = r.Date }) };
    }
}
