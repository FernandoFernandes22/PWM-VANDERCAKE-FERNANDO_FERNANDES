using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VanderCake.Api.Data;
using VanderCake.Api.Services;
using VanderCake.Contracts;
using VanderCake.Core;
using System.Text.Json;
namespace VanderCake.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuth(this RouteGroupBuilder api)
    {
        api.MapGet("/auth/csrf", (HttpContext context, IAntiforgery antiforgery) => Results.Ok(new { token = antiforgery.GetAndStoreTokens(context).RequestToken })).AllowAnonymous();
        api.MapPost("/auth/web/login", async (LoginRequest input, AppDbContext db, HttpContext context, IAntiforgery antiforgery) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            var user = await Validate(input, db);
            if (user.Role != "ADMIN") return Results.Forbid();
            await context.SignInAsync("Web", MobileAuthentication.Identity(user.Id, user.Name, user.Role, "Web"), new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(4) });
            return Results.Ok(new UserDto(user.Id, user.Name, user.Role));
        }).AllowAnonymous().RequireRateLimiting("login");
        api.MapPost("/auth/mobile/login", async (LoginRequest input, AppDbContext db, IDataProtectionProvider provider) =>
        {
            var user = await Validate(input, db);
            if (user.Role != "VENDEDOR") return Results.Forbid();
            var expiry = DateTime.UtcNow.AddHours(1);
            var token = provider.CreateProtector("VanderCake.Mobile.v1").Protect(JsonSerializer.Serialize(new MobileTicket(user.Id, expiry)));
            return Results.Ok(new LoginResult(token, expiry, new UserDto(user.Id, user.Name, user.Role)));
        }).AllowAnonymous().RequireRateLimiting("login");
        api.MapGet("/auth/me", (HttpContext c) => Results.Ok(new UserDto(c.User.UserId(), c.User.Identity!.Name!, c.User.IsAdmin() ? "ADMIN" : "VENDEDOR")));
        api.MapPost("/auth/web/logout", async (HttpContext c) => { await c.SignOutAsync("Web"); return Results.NoContent(); });
    }
    private static async Task<User> Validate(LoginRequest input, AppDbContext db)
    {
        ApiSupport.Require(input.Login is { Length: > 0 and <= 100 } && input.Senha is { Length: > 0 and <= 200 }, "Informe login e senha.");
        var user = await db.Users.SingleOrDefaultAsync(x => x.Login == input.Login.Trim().ToLowerInvariant() && x.Active);
        var hasher = new PasswordHasher<User>();
        var target = user ?? new User { PasswordHash = DummyHash.Value };
        var valid = hasher.VerifyHashedPassword(target, target.PasswordHash, input.Senha);
        if (user is null || valid == PasswordVerificationResult.Failed) throw new DomainException(401, "LOGIN_INVALIDO", "Login ou senha inválidos.");
        return user;
    }
    private static readonly Lazy<string> DummyHash = new(() => new PasswordHasher<User>().HashPassword(new User(), Guid.NewGuid().ToString()));
}
