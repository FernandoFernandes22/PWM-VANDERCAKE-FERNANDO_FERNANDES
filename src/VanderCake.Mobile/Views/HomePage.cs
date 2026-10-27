using System.Text.Json;
using VanderCake.Contracts;
using VanderCake.Mobile.Services;
using VanderCake.Mobile.ViewModels;
namespace VanderCake.Mobile.Views;

public class HomePage : ContentPage
{
    private readonly ApiClient api; private readonly DraftStore drafts; private readonly VerticalStackLayout clients = Ui.Stack();
    private readonly Label syncStatus = Ui.Text("", 13);
    public HomePage(ApiClient api, DraftStore drafts)
    {
        this.api = api; this.drafts = drafts; Title = "Minha carteira"; BackgroundColor = Color.FromArgb("#F7F5F0");
        var stack = Ui.Stack(); stack.Add(Ui.Text("Olá, " + api.User!.Nome, 25)); stack.Add(Ui.Text("Seu roteiro de visitas e próximos pedidos."));
        stack.Add(Ui.Button("Sincronizar pedidos", Synchronize)); stack.Add(syncStatus);
        stack.Add(Ui.Button("Atualizar carteira", Refresh)); stack.Add(Ui.Button("Novo cliente", () => Navigation.PushAsync(new NewClientPage(api))));
        stack.Add(Ui.Button("Rascunhos salvos", ShowDrafts)); stack.Add(Ui.Button("Meus pedidos", ShowOrders)); stack.Add(clients);
        stack.Add(Ui.Button("Sair", async () => { api.Logout(); Application.Current!.Windows[0].Page = new NavigationPage(new LoginPage(api, drafts)); await Task.CompletedTask; }));
        Content = new ScrollView { Content = stack };
    }
    protected override async void OnAppearing() { base.OnAppearing(); try { await Refresh(); } catch (Exception e) { clients.Clear(); clients.Add(Ui.Text(e.Message)); var cached = await drafts.Cached<List<ClientDto>>(api.User!.Id, "clients"); if (cached is not null) { clients.Add(Ui.Text("Dados locais — podem estar desatualizados.")); ShowClients(cached); } } }
    private async Task Refresh()
    {
        var rows = await api.Get<List<ClientDto>>("carteira"); await drafts.Cache(api.User!.Id, "clients", rows); clients.Clear(); ShowClients(rows);
        await CacheCatalog();
        var pending = await drafts.List(api.User!.Id);
        syncStatus.Text = $"Dados atualizados · {pending.Count(x => x.State is "PENDENTE_ENVIO" or "ENVIO_INCERTO")} pedido(s) na fila.";
    }
    private async Task CacheCatalog()
    {
        var userId = api.User!.Id;
        var products = await api.GetAll<ProductDto>("produtos");
        var terms = await api.Get<List<TermsDto>>("condicoes-pagamento");
        var methods = await api.Get<List<MethodDto>>("formas-pagamento");
        await drafts.Cache(userId, "products", products);
        await drafts.Cache(userId, "terms", terms.Where(x => x.Ativo).ToList());
        await drafts.Cache(userId, "methods", methods.Where(x => x.Ativo).ToList());
    }
    private async Task Synchronize()
    {
        int sent = 0; var problems = new List<string>();
        var pending = (await drafts.List(api.User!.Id)).Where(x => x.State is "PENDENTE_ENVIO" or "ENVIO_INCERTO").ToList();
        foreach (var draft in pending)
        {
            try { var vm = new OrderViewModel(api, drafts, draft); await vm.Load(); await vm.Send(); sent++; }
            catch (ApiConnectionException) { problems.Add("A conexão foi interrompida. Os pedidos restantes continuam salvos."); break; }
            catch (ApiException e) when (e.Status is 401 or 403) { problems.Add("Entre novamente para renovar sua sessão. A fila permanece salva."); break; }
            catch (Exception e) { problems.Add($"{draft.ClientName}: {e.Message}"); }
        }
        try { await Refresh(); }
        catch (ApiConnectionException) { problems.Add("Não foi possível atualizar o catálogo. Os dados locais foram preservados."); }
        syncStatus.Text = $"{sent} pedido(s) enviado(s).";
        await DisplayAlertAsync("Sincronização", $"{sent} pedido(s) enviado(s) ao site.\n" + (problems.Count > 0 ? string.Join("\n", problems) : "Carteira, produtos e condições atualizados. Consulte Meus pedidos para acompanhar o atendimento."), "OK");
    }
    private void ShowClients(List<ClientDto> rows)
    {
        if (rows.Count == 0) clients.Add(Ui.Text("Nenhum cliente na sua carteira."));
        foreach (var c in rows)
        {
            var box = Ui.Stack(); box.Padding = 0; box.Add(Ui.Text(c.Nome, 20)); box.Add(Ui.Text($"Visita {c.OrdemVisita} · {c.Cidade}/{c.Estado}", 13)); box.Add(Ui.Text(c.Endereco, 13));
            box.Add(Ui.Button("Novo pedido", () => Navigation.PushAsync(new OrderPage(api, drafts, new LocalDraft { UserId = api.User!.Id, ClientId = c.Id, ClientName = c.Nome }))));
            box.Add(Ui.Button("Histórico e débitos", () => ShowClient(c))); clients.Add(Ui.Card(box));
        }
    }
    private async Task ShowClient(ClientDto c)
    {
        var finance = await api.Get<JsonElement>($"clientes/{c.Id}/financeiro"); var history = await api.Get<JsonElement>($"clientes/{c.Id}/historico?pageSize=20");
        var stack = Ui.Stack(); stack.Add(Ui.Text(c.Nome, 24)); stack.Add(Ui.Text("Saldo total: " + Ui.Money(finance.GetProperty("saldoTotal").GetDecimal()))); stack.Add(Ui.Text("Vencido: " + Ui.Money(finance.GetProperty("saldoVencido").GetDecimal()))); stack.Add(Ui.Text("Compras recentes", 20));
        foreach (var sale in history.GetProperty("items").EnumerateArray())
        {
            stack.Add(Ui.Text($"{sale.GetProperty("data").GetString()} · {Ui.Money(sale.GetProperty("total").GetDecimal())}"));
            foreach (var item in sale.GetProperty("itens").EnumerateArray()) stack.Add(Ui.Text($"{item.GetProperty("descricao").GetString()} · {item.GetProperty("quantidadeAprovada")} × {Ui.Money(item.GetProperty("precoUnitario").GetDecimal())}", 13));
        }
        await Navigation.PushAsync(new ContentPage { Title = "Histórico e débitos", Content = new ScrollView { Content = stack } });
    }
    private async Task ShowDrafts()
    {
        var stack = Ui.Stack(); var rows = await drafts.List(api.User!.Id); if (rows.Count == 0) stack.Add(Ui.Text("Nenhum rascunho salvo."));
        foreach (var draft in rows) stack.Add(Ui.Button($"{draft.ClientName} · {DraftState(draft.State)}", () => Navigation.PushAsync(new OrderPage(api, drafts, draft))));
        await Navigation.PushAsync(new ContentPage { Title = "Rascunhos", Content = new ScrollView { Content = stack } });
    }
    private static string DraftState(string state) => state switch { "EDITANDO" => "Em elaboração", "PENDENTE_ENVIO" => "Aguardando sincronização", "ENVIO_INCERTO" => "Envio a confirmar", _ => state };
    private async Task ShowOrders()
    {
        var rows = await api.GetAll<OrderDto>("pedidos"); var stack = Ui.Stack(); if (rows.Count == 0) stack.Add(Ui.Text("Nenhum pedido enviado."));
        foreach (var o in rows)
        {
            var box = Ui.Stack(); box.Padding = 0; box.Add(Ui.Text($"#{o.Id} · {o.Cliente}", 19)); box.Add(Ui.Text(o.Situacao)); box.Add(Ui.Text($"Solicitado: {Ui.Money(o.TotalSolicitado)} · Atendido: {Ui.Money(o.TotalAprovado)}"));
            foreach (var i in o.Itens) box.Add(Ui.Text($"{i.Descricao}: {i.QuantidadeSolicitada} solicitado(s), {i.QuantidadeAprovada?.ToString() ?? "aguardando"} aprovado(s)", 13)); stack.Add(Ui.Card(box));
        }
        await Navigation.PushAsync(new ContentPage { Title = "Meus pedidos", Content = new ScrollView { Content = stack } });
    }
}
