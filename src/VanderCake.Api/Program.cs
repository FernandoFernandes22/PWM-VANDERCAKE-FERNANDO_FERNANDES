using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using VanderCake.Api.Data;
using VanderCake.Api.Endpoints;
using VanderCake.Api.Services;
using VanderCake.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders().AddConsole();
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true).AddEnvironmentVariables();
var connection = builder.Configuration.GetConnectionString("MySql") ?? "Server=127.0.0.1;Port=3307;Database=vandercake;User ID=vandercake;Password=CONFIGURE_LOCALMENTE";
builder.Services.AddDbContext<AppDbContext>(o => o.UseMySQL(connection));
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new IdConverter()));
builder.Services.AddOpenApi();
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".local", "keys")));
builder.Services.AddAntiforgery(o => { o.HeaderName = "X-CSRF-TOKEN"; o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always; });
builder.Services.AddAuthentication(o => { o.DefaultScheme = "Session"; o.DefaultChallengeScheme = "Session"; })
    .AddPolicyScheme("Session", null, o => o.ForwardDefaultSelector = c => c.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? "Mobile" : "Web")
    .AddCookie("Web", o =>
    {
        o.Cookie.Name = "VanderCake.Session";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        o.SlidingExpiration = false;
        o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return Task.CompletedTask; };
        o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = 403; return Task.CompletedTask; };
        o.Events.OnValidatePrincipal = async c =>
        {
            var db = c.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            if (!await db.Users.AnyAsync(u => u.Id == c.Principal!.UserId() && u.Active && u.Role == "ADMIN")) c.RejectPrincipal();
        };
    })
    .AddScheme<AuthenticationSchemeOptions, MobileAuthentication>("Mobile", _ => { });
builder.Services.AddAuthorizationBuilder().AddPolicy("Admin", p => p.RequireRole("ADMIN"));
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("login", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 1_000_000);
var app = builder.Build();
if (args.Contains("--migrate") || args.Contains("--seed") || args.Contains("--reset-demo-passwords") || args.Contains("--prepare-presentation"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (args.Contains("--migrate")) await db.Database.MigrateAsync();
    if (args.Contains("--seed")) await SeedData.Run(db, app.Configuration, app.Environment.IsDevelopment());
    if (args.Contains("--reset-demo-passwords")) await SeedData.ResetDemoPasswords(db, app.Configuration, app.Environment.IsDevelopment());
    if (args.Contains("--prepare-presentation")) await PresentationData.Prepare(db, app.Environment.IsDevelopment());
    return;
}
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "same-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    try { await next(); }
    catch (Exception error) when (!context.Response.HasStarted)
    {
        var (status, code, message) = error switch
        {
            DomainException e => (e.Status, e.Code, e.Message),
            AntiforgeryValidationException => (400, "CSRF_INVALIDO", "Atualize a página e tente novamente."),
            ArgumentException e => (400, "VALIDACAO", e.Message),
            BadHttpRequestException => (400, "FORMATO_INVALIDO", "Verifique os campos enviados."),
            DbUpdateConcurrencyException => (409, "CONCORRENCIA", "Os dados mudaram. Atualize e tente novamente."),
            DbUpdateException => (409, "CONFLITO_DADOS", "A operação conflita com um registro existente. Atualize e tente novamente."),
            MySql.Data.MySqlClient.MySqlException e when e.Number is 1213 or 1205 => (409, "CONCORRENCIA", "Outra operação está em andamento. Repita a mesma tentativa."),
            MySql.Data.MySqlClient.MySqlException => (503, "BANCO_INDISPONIVEL", "Banco indisponível. Verifique a configuração e a conexão."),
            _ => (500, "ERRO_INTERNO", "Não foi possível concluir a operação.")
        };
        if (status >= 500) app.Logger.LogError("Falha {Code}; tipo {Type}; trace {Trace}", code, error.GetType().Name, context.TraceIdentifier);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { type = "about:blank", title = message, status, code, traceId = context.TraceIdentifier }, options: ApiJson.Options, contentType: "application/problem+json", cancellationToken: context.RequestAborted);
    }
});
if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.Use(async (c, next) =>
{
    if (c.Request.Path.StartsWithSegments("/api") && c.User.Identity?.IsAuthenticated == true)
    {
        c.Response.Headers.CacheControl = "no-store";
        if (c.User.Identity.AuthenticationType == "Web" && c.Request.Method is not ("GET" or "HEAD" or "OPTIONS"))
            await c.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(c);
    }
    await next();
});
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "VanderCake.Api" }));
var api = app.MapGroup("/api/v1").RequireAuthorization();
api.MapAuth();
api.MapCatalog();
api.MapOrders();
api.MapFinance();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.Run();
public partial class Program { }
