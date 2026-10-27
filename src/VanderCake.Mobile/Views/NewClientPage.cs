using VanderCake.Contracts;
using VanderCake.Mobile.Services;
namespace VanderCake.Mobile.Views;
public class NewClientPage : ContentPage
{
    public NewClientPage(ApiClient api)
    {
        Title = "Novo cliente"; var layout = Ui.Stack();
        var name = new Entry { Placeholder = "Nome" }; var city = new Entry { Placeholder = "Cidade" }; var state = new Entry { Placeholder = "UF", MaxLength = 2, TextTransform = TextTransform.Uppercase }; var address = new Entry { Placeholder = "Endereço completo" }; var phone = new Entry { Placeholder = "Telefone (opcional)", Keyboard = Keyboard.Telephone };
        layout.Add(Ui.Text("Adicionar à minha carteira", 24)); foreach (var field in new[] { name, city, state, address, phone }) layout.Add(field);
        layout.Add(Ui.Button("Cadastrar cliente", async () => { await api.Post<ClientDto>("clientes", new ClientInput(name.Text ?? "", city.Text ?? "", state.Text ?? "", address.Text ?? "", phone.Text ?? "", null, 0)); await Navigation.PopAsync(); }));
        Content = new ScrollView { Content = layout };
    }
}
