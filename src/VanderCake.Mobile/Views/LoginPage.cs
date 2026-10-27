using VanderCake.Mobile.Services;
namespace VanderCake.Mobile.Views;

public class LoginPage : ContentPage
{
    private readonly ApiClient api;
    private readonly DraftStore drafts;
    private bool restored;
    public LoginPage(ApiClient api, DraftStore drafts)
    {
        this.api = api; this.drafts = drafts;
        Title = "VanderCake"; BackgroundColor = Color.FromArgb("#F7F5F0");
        var url = new Entry { Text = Preferences.Default.Get("api-url", "http://localhost:5268"), Keyboard = Keyboard.Url };
        var login = new Entry { Placeholder = "Seu login", TextTransform = TextTransform.Lowercase };
        var password = new Entry { Placeholder = "Sua senha", IsPassword = true };
        var layout = Ui.Stack();
        layout.Add(Ui.Text("VanderCake", 36)); layout.Add(Ui.Text("Sua carteira, seus pedidos. Tudo por aqui.", 18));
        layout.Add(Ui.Text("Endereço da API")); layout.Add(url); layout.Add(Ui.Text("Login")); layout.Add(login); layout.Add(Ui.Text("Senha")); layout.Add(password);
        layout.Add(Ui.Button("Entrar", async () =>
        {
            await api.Login(url.Text ?? "", login.Text ?? "", password.Text ?? ""); password.Text = "";
            await drafts.ClearOtherCaches(api.User!.Id);
            Application.Current!.Windows[0].Page = new NavigationPage(new HomePage(api, drafts)) { BarBackgroundColor = Colors.White, BarTextColor = Ui.Accent };
        }));
        layout.Add(Ui.Text("Entre com internet para baixar sua carteira. Depois, prepare pedidos sem conexão e use Sincronizar pedidos quando conectar.", 12));
        Content = new ScrollView { Content = layout };
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing(); if (restored) return; restored = true;
        try
        {
            if (await api.RestoreSession()) Application.Current!.Windows[0].Page = new NavigationPage(new HomePage(api, drafts)) { BarBackgroundColor = Colors.White, BarTextColor = Ui.Accent };
        }
        catch { /* O login manual continua disponível se o armazenamento seguro não puder ser lido. */ }
    }
}
