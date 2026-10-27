using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VanderCake.Contracts;
namespace VanderCake.Mobile.Services;

public class ApiClient
{
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(25) };
    private string? token;
    public UserDto? User { get; private set; }
    public string BaseUrl { get; private set; } = "";
    public async Task Login(string url, string login, string password)
    {
        if (!Uri.TryCreate(url.TrimEnd('/') + "/", UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) throw new InvalidOperationException("Informe uma URL válida para a API.");
#if DEBUG
        if (uri.Scheme != "https" && uri.Host is not ("localhost" or "127.0.0.1")) throw new InvalidOperationException("HTTP de desenvolvimento é permitido somente no computador local.");
#else
        if (uri.Scheme != "https") throw new InvalidOperationException("A conexão deve utilizar HTTPS.");
#endif
        BaseUrl = uri.AbsoluteUri;
        token = null; User = null;
        var result = await Send<LoginResult>("auth/mobile/login", HttpMethod.Post, new LoginRequest(login, password));
        await SecureStorage.Default.SetAsync("vandercake.token", result.AccessToken);
        await SecureStorage.Default.SetAsync("vandercake.session", JsonSerializer.Serialize(result, ApiJson.Options));
        token = result.AccessToken; User = result.Usuario;
        Preferences.Default.Set("api-url", BaseUrl);
    }
    public async Task<bool> RestoreSession()
    {
        var saved = await SecureStorage.Default.GetAsync("vandercake.session");
        if (saved is null) return false;
        LoginResult? session;
        try { session = JsonSerializer.Deserialize<LoginResult>(saved, ApiJson.Options); }
        catch (JsonException) { Logout(); return false; }
        if (session is null || DateTime.SpecifyKind(session.ExpiresAtUtc, DateTimeKind.Utc) <= DateTime.UtcNow) { Logout(); return false; }
        BaseUrl = Preferences.Default.Get("api-url", "http://localhost:5268/");
        token = session.AccessToken; User = session.Usuario; return true;
    }
    public void Logout() { token = null; User = null; SecureStorage.Default.Remove("vandercake.token"); SecureStorage.Default.Remove("vandercake.session"); }
    public async Task<T> Get<T>(string path) => await Send<T>(path, HttpMethod.Get);
    public async Task<T> Post<T>(string path, object body, string? key = null) => await Send<T>(path, HttpMethod.Post, body, key);
    public async Task<List<T>> GetAll<T>(string path)
    {
        var result = new List<T>(); int page = 1;
        while (true)
        {
            var response = await Get<PageResult<T>>($"{path}{(path.Contains('?') ? '&' : '?')}page={page}&pageSize=100");
            result.AddRange(response.Items); if (result.Count >= response.TotalItems) return result; page++;
        }
    }
    private async Task<T> Send<T>(string path, HttpMethod method, object? body = null, string? key = null)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) throw new ApiConnectionException("Sem conexão. Os pedidos podem ser preparados com os dados já salvos e sincronizados depois.");
        using var request = new HttpRequestMessage(method, new Uri(new Uri(BaseUrl), "api/v1/" + path));
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (key is not null) request.Headers.Add("Idempotency-Key", key);
        if (body is not null) request.Content = new StringContent(JsonSerializer.Serialize(body, ApiJson.Options), Encoding.UTF8, "application/json");
        HttpResponseMessage response;
        try { response = await http.SendAsync(request); }
        catch (TaskCanceledException) { throw new ApiConnectionException("Tempo de conexão esgotado. O envio pode ter sido recebido; sincronize novamente para confirmar sem duplicar."); }
        catch (HttpRequestException) { throw new ApiConnectionException("Não foi possível alcançar a API. Confira endereço, conexão e certificado."); }
        using (response)
        {
            var text = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                string message = response.StatusCode == System.Net.HttpStatusCode.Unauthorized ? "Sessão expirada. Entre novamente." : $"Não foi possível concluir ({(int)response.StatusCode}).";
                string? code = null;
                try { using var problem = JsonDocument.Parse(text); if (problem.RootElement.TryGetProperty("title", out var title)) message = title.GetString() ?? message; if(problem.RootElement.TryGetProperty("code", out var c)) code = c.GetString(); } catch (JsonException) { }
                throw new ApiException((int)response.StatusCode, message, code);
            }
            return JsonSerializer.Deserialize<T>(text, ApiJson.Options)!;
        }
    }
}
public class ApiException(int status, string message, string? code = null) : Exception(message) { public int Status { get; } = status; public string? Code { get; } = code; }
public class ApiConnectionException(string message) : InvalidOperationException(message);
