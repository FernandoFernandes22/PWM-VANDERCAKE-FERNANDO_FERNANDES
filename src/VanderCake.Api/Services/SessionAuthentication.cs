using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VanderCake.Api.Data;
namespace VanderCake.Api.Services;

public record MobileTicket(long UserId, DateTime ExpiresAtUtc);
public class MobileAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    UrlEncoder encoder, IDataProtectionProvider protection, AppDbContext db)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return AuthenticateResult.NoResult();
        MobileTicket? ticket;
        try { ticket = JsonSerializer.Deserialize<MobileTicket>(protection.CreateProtector("VanderCake.Mobile.v1").Unprotect(header[7..])); }
        catch { return AuthenticateResult.Fail("Sessão inválida."); }
        if (ticket is null || ticket.ExpiresAtUtc <= DateTime.UtcNow) return AuthenticateResult.Fail("Sessão expirada.");
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == ticket.UserId && x.Active);
        if (user is null || user.Role != "VENDEDOR") return AuthenticateResult.Fail("Usuário indisponível.");
        return AuthenticateResult.Success(new AuthenticationTicket(Identity(user.Id, user.Name, user.Role, Scheme.Name), Scheme.Name));
    }
    public static ClaimsPrincipal Identity(long id, string name, string role, string scheme) => new(new ClaimsIdentity([
        new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Name, name), new Claim(ClaimTypes.Role, role)
    ], scheme));
}
