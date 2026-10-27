using VanderCake.Mobile.Services;
using VanderCake.Mobile.ViewModels;
namespace VanderCake.Mobile.Views;

public class OrderPage : ContentPage
{
    private readonly OrderViewModel vm;
    private readonly VerticalStackLayout layout = Ui.Stack();
    private readonly Label total = Ui.Text("", 23);
    private readonly Label status = Ui.Text("", 12);
    private readonly DraftStore store;
    private readonly Dictionary<long, Entry> quantityFields = [];
    public OrderPage(ApiClient api, DraftStore store, LocalDraft draft)
    {
        vm = new(api, store, draft); this.store = store; Title = "Novo pedido"; BackgroundColor = Color.FromArgb("#F7F5F0"); Content = new ScrollView { Content = layout };
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try { await vm.Load(); Build(); }
        catch (Exception e) { layout.Clear(); layout.Add(Ui.Text(e.Message)); layout.Add(Ui.Button("Tentar carregar novamente", async () => { await vm.Load(); Build(); })); }
    }
    private void Build()
    {
        layout.Clear(); quantityFields.Clear(); layout.Add(Ui.Text(vm.Draft.ClientName, 25));
        status.Text = vm.Draft.State == "PENDENTE_ENVIO" ? "Pedido na fila. Use Sincronizar pedidos na carteira quando tiver conexão." : vm.Locked ? "Envio em confirmação. Sincronize novamente para consultar o resultado sem duplicar." : vm.UsingCache ? "Catálogo salvo no aparelho. O pedido será conferido pelo servidor ao sincronizar." : "Rascunho salvo neste aparelho. Informe quantidades inteiras; zero remove o produto.";
        layout.Add(status);
        var terms = new Picker { Title = "Condição de pagamento", ItemsSource = vm.Terms.Select(x => x.Nome).ToList(), SelectedIndex = vm.Terms.FindIndex(x => x.Id == vm.TermsId), IsEnabled = !vm.Locked };
        terms.SelectedIndexChanged += async (_, _) => { if (terms.SelectedIndex >= 0) { vm.TermsId = vm.Terms[terms.SelectedIndex].Id; await Persist(); } };
        var methods = new Picker { Title = "Forma de pagamento", ItemsSource = vm.Methods.Select(x => x.Nome).ToList(), SelectedIndex = vm.Methods.FindIndex(x => x.Id == vm.MethodId), IsEnabled = !vm.Locked };
        methods.SelectedIndexChanged += async (_, _) => { if (methods.SelectedIndex >= 0) { vm.MethodId = vm.Methods[methods.SelectedIndex].Id; await Persist(); } };
        layout.Add(Ui.Text("Condição de pagamento")); layout.Add(terms); layout.Add(Ui.Text("Forma de pagamento")); layout.Add(methods);
        var search = new SearchBar { Placeholder = "Buscar produto, código ou grupo", TextColor = Color.FromArgb("#302D26"), PlaceholderColor = Colors.DimGray };
        var catalogCards = new List<(VanderCake.Contracts.ProductDto Product, Border Card)>();
        var noResults = Ui.Text("Nenhum produto encontrado."); noResults.IsVisible = false;
        search.TextChanged += (_, _) =>
        {
            var query = search.Text?.Trim() ?? "";
            foreach (var item in catalogCards)
                item.Card.IsVisible = System.Globalization.CultureInfo.GetCultureInfo("pt-BR").CompareInfo.IndexOf($"{item.Product.Descricao} {item.Product.Codigo} {item.Product.Grupo}", query, System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace) >= 0;
            noResults.IsVisible = !catalogCards.Any(x => x.Card.IsVisible);
        };
        layout.Add(search); layout.Add(noResults);
        foreach (var product in vm.Products)
        {
            var box = Ui.Stack(); box.Padding = 0; box.Add(Ui.Text(product.Descricao, 18)); box.Add(Ui.Text($"{Ui.Money(product.Preco)} / {product.Unidade}", 13));
            var qty = new Entry { Keyboard = Keyboard.Numeric, Text = vm.Items.GetValueOrDefault(product.Id)?.Quantidade.ToString() ?? "0", IsEnabled = !vm.Locked, AutomationId = "qty-" + product.Id };
            quantityFields[product.Id] = qty;
            qty.TextChanged += async (_, _) =>
            {
                if (!int.TryParse(qty.Text, out int value) || value < 0 || value > 1000000) { status.Text = "Quantidade inválida. Use inteiro de 0 a 1.000.000."; return; }
                try { await vm.SetQuantity(product, value); UpdateTotal(); status.Text = "Rascunho salvo neste aparelho."; } catch (Exception e) { status.Text = e.Message; }
            };
            box.Add(qty); var card = Ui.Card(box); catalogCards.Add((product, card)); layout.Add(card);
        }
        foreach (var missing in vm.Items.Values.Where(x => vm.Products.All(p => p.Id != x.ProdutoId)))
        {
            layout.Add(Ui.Text($"Produto {missing.ProdutoId} não está mais no catálogo. Quantidade salva: {missing.Quantidade}."));
            if (!vm.Locked) layout.Add(Ui.Button("Remover produto indisponível", async () => { vm.Items.Remove(missing.ProdutoId); await vm.Save(); Build(); }));
        }
        UpdateTotal(); layout.Add(total);
        layout.Add(Ui.Button(vm.Draft.State == "PENDENTE_ENVIO" ? "Sincronizar este pedido" : vm.Locked ? "Confirmar resultado do envio" : "Enviar pedido / guardar na fila", async () =>
        {
            if (!vm.Locked)
            {
                foreach (var product in vm.Products)
                {
                    if (!int.TryParse(quantityFields[product.Id].Text, out var quantity) || quantity is < 0 or > 1000000) throw new InvalidOperationException("Corrija as quantidades antes de enviar.");
                    await vm.SetQuantity(product, quantity);
                }
            }
            if (!vm.Locked && !await DisplayAlertAsync("Confirmar pedido?", $"{vm.Draft.ClientName}\nTotal estimado: {Ui.Money(vm.Total)}\nSe estiver sem conexão, ficará na fila para sincronizar depois.", "Confirmar", "Voltar")) return;
            layout.IsEnabled = false;
            try
            {
                if (!vm.Locked) await vm.Queue();
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet || vm.UsingCache)
                {
                    await DisplayAlertAsync("Pedido guardado", "O pedido ficou na fila deste aparelho. Quando voltar a conexão, use Sincronizar pedidos na carteira.", "OK");
                    await Navigation.PopAsync(); return;
                }
                var order = await vm.Send(); await DisplayAlertAsync("Pedido recebido", $"Pedido #{order.Id} enviado à distribuidora.", "OK"); await Navigation.PopAsync();
            }
            catch (ApiConnectionException)
            {
                await DisplayAlertAsync("Pedido salvo para sincronizar", "A conexão não confirmou o envio. O pedido está salvo; use Sincronizar pedidos quando conectar. A mesma tentativa será repetida sem duplicar o pedido.", "OK");
                await Navigation.PopAsync();
            }
            catch { Build(); throw; }
            finally { layout.IsEnabled = true; }
        }));
        if (!vm.Locked)
        {
            layout.Add(Ui.Button("Atualizar preços do catálogo", async () =>
            {
                if (!await DisplayAlertAsync("Atualizar preços?", "Os itens serão atualizados com os preços consultados no catálogo. Confira o total antes de enviar.", "Atualizar", "Voltar")) return;
                await vm.Load(); foreach (var product in vm.Products) if (vm.Items.TryGetValue(product.Id, out var item)) vm.Items[product.Id] = item with { PrecoConsultado = product.Preco }; await vm.Save(); Build();
            }));
            layout.Add(Ui.Button("Excluir rascunho", async () => { if (await DisplayAlertAsync("Excluir rascunho?", "Este pedido ainda não foi enviado.", "Excluir", "Voltar")) { await store.Delete(vm.Draft); await Navigation.PopAsync(); } }));
        }
    }
    private async Task Persist() { try { await vm.Save(); } catch (Exception e) { status.Text = e.Message; } }
    private void UpdateTotal() => total.Text = "Total estimado: " + Ui.Money(vm.Total);
}
