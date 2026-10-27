using System.Text.Json;
using VanderCake.Contracts;
using VanderCake.Mobile.Services;
namespace VanderCake.Mobile.ViewModels;

public class OrderViewModel(ApiClient api, DraftStore store, LocalDraft draft)
{
    public LocalDraft Draft { get; } = draft;
    public List<ProductDto> Products { get; private set; } = [];
    public List<TermsDto> Terms { get; private set; } = [];
    public List<MethodDto> Methods { get; private set; } = [];
    public Dictionary<long, OrderItemInput> Items { get; } = [];
    public long TermsId { get; set; }
    public long MethodId { get; set; }
    public bool Locked => Draft.State != "EDITANDO";
    public bool UsingCache { get; private set; }
    public decimal Total => Items.Values.Sum(x => x.Quantidade * x.PrecoConsultado);
    public async Task Load()
    {
        if (api.User?.Id != Draft.UserId) throw new InvalidOperationException("Rascunho pertence a outra conta.");
        try
        {
            Products = await api.GetAll<ProductDto>("produtos");
            Terms = (await api.Get<List<TermsDto>>("condicoes-pagamento")).Where(x => x.Ativo).ToList();
            Methods = (await api.Get<List<MethodDto>>("formas-pagamento")).Where(x => x.Ativo).ToList();
            await store.Cache(Draft.UserId, "products", Products);
            await store.Cache(Draft.UserId, "terms", Terms);
            await store.Cache(Draft.UserId, "methods", Methods);
            UsingCache = false;
        }
        catch (ApiConnectionException)
        {
            Products = await store.Cached<List<ProductDto>>(Draft.UserId, "products") ?? [];
            Terms = await store.Cached<List<TermsDto>>(Draft.UserId, "terms") ?? [];
            Methods = await store.Cached<List<MethodDto>>(Draft.UserId, "methods") ?? [];
            if (Products.Count == 0 || Terms.Count == 0 || Methods.Count == 0) throw new InvalidOperationException("Conecte-se e use Sincronizar pedidos uma vez para baixar o catálogo antes de trabalhar sem internet.");
            UsingCache = true;
        }
        if (Draft.Payload.Length > 0)
        {
            var input = JsonSerializer.Deserialize<OrderInput>(Draft.Payload, ApiJson.Options)!;
            TermsId = input.CondicaoPagamentoId; MethodId = input.FormaPagamentoId;
            foreach (var item in input.Itens) Items[item.ProdutoId] = item;
        }
        if (TermsId == 0) TermsId = Terms.FirstOrDefault()?.Id ?? 0;
        if (MethodId == 0) MethodId = Methods.FirstOrDefault()?.Id ?? 0;
        await Save();
    }
    public async Task SetQuantity(ProductDto product, int quantity)
    {
        if (Locked) return;
        if (quantity == 0) Items.Remove(product.Id); else Items[product.Id] = new(product.Id, quantity, product.Preco);
        await Save();
    }
    public async Task Save()
    {
        if (!Locked) Draft.Payload = JsonSerializer.Serialize(new OrderInput(Draft.ClientId, TermsId, MethodId, Items.Values.OrderBy(x => x.ProdutoId).ToArray()), ApiJson.Options);
        await store.Save(Draft);
    }
    public async Task<OrderDto> Send()
    {
        if (Items.Count == 0 || Items.Values.Any(x => x.Quantidade <= 0) || TermsId == 0 || MethodId == 0) throw new InvalidOperationException("Escolha produtos, condição e forma de pagamento.");
        if (!Locked)
        {
            await Queue();
        }
        Draft.State = "ENVIO_INCERTO"; await store.Save(Draft);
        var input = JsonSerializer.Deserialize<OrderInput>(Draft.Payload, ApiJson.Options)!;
        try
        {
            var order = await api.Post<OrderDto>("pedidos", input, Draft.AttemptKey);
            Draft.State = "ENVIADO"; Draft.ServerOrderId = order.Id.ToString(); await store.Save(Draft); return order;
        }
        catch (ApiException e) when (e.Status == 400 || e.Status == 404 || e.Code == "PRECO_ALTERADO")
        {
            Draft.State = "EDITANDO"; Draft.AttemptKey = ""; await store.Save(Draft); throw;
        }
    }
    public async Task Queue()
    {
        if (Locked) return;
        if (Items.Count == 0 || Items.Values.Any(x => x.Quantidade <= 0) || TermsId == 0 || MethodId == 0) throw new InvalidOperationException("Escolha produtos, condição e forma de pagamento.");
        await Save(); Draft.AttemptKey = Guid.NewGuid().ToString(); Draft.State = "PENDENTE_ENVIO"; await store.Save(Draft);
    }
}
