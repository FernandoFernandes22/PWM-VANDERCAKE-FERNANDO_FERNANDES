namespace VanderCake.Mobile.Views;
public static class Ui
{
    public static Color Accent = Color.FromArgb("#B46532");
    public static Label Text(string text, double size = 15) => new() { Text = text, FontSize = size, TextColor = Color.FromArgb("#302D26") };
    public static Button Button(string text, Func<Task> action)
    {
        var button = new Button { Text = text, BackgroundColor = Accent, TextColor = Colors.White, CornerRadius = 10 };
        button.Clicked += async (_, _) => { button.IsEnabled = false; try { await action(); } catch (Exception e) { if (Shell.Current is not null) await Shell.Current.DisplayAlertAsync("Não foi possível concluir", e.Message, "OK"); else if (Application.Current!.Windows.FirstOrDefault()?.Page is Page page) await page.DisplayAlertAsync("Não foi possível concluir", e.Message, "OK"); } finally { button.IsEnabled = true; } };
        return button;
    }
    public static VerticalStackLayout Stack() => new() { Spacing = 14, Padding = 22 };
    public static Border Card(View view) => new() { Content = view, Padding = 15, BackgroundColor = Colors.White, Stroke = Color.FromArgb("#E8E3DA"), StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 } };
    public static string Money(decimal amount) => amount.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
}
