using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VanderCake.Contracts;
using VanderCake.Core;
namespace VanderCake.Api.Services;

public sealed class DomainException(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}
public static class ApiSupport
{
    public static long UserId(this ClaimsPrincipal user) => long.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole("ADMIN");
    public static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, ApiJson.Options))));
    public static Guid Key(HttpRequest request) => Guid.TryParse(request.Headers["Idempotency-Key"], out var key) && key != Guid.Empty ? key : throw new ArgumentException("Envie uma chave UUID no cabeçalho Idempotency-Key.");
    public static void Require(bool condition, string message) { if (!condition) throw new ArgumentException(message); }
    public static string Text(string? text, string name, int max = 150)
    {
        text = text?.Trim();
        Require(!string.IsNullOrWhiteSpace(text) && text.Length <= max, $"{name}: preencha entre 1 e {max} caracteres.");
        return text!;
    }
    public static string Optional(string? value, int max = 200)
    {
        value = value?.Trim() ?? "";
        Require(value.Length <= max, $"Campo excede {max} caracteres.");
        return value;
    }
    public static DateOnly Today() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")));
    public static OrderDto Dto(Order o) => new(o.Id, o.ClientId, o.Client.Name, o.SellerId, o.Status, o.SentAtUtc, o.RequestedTotal, o.ApprovedTotal, o.TermsName, o.MethodName, o.Items.OrderBy(i => i.Id).Select(i => new OrderItemDto(i.ProductId, i.Description, i.Unit, i.Requested, i.Approved, i.UnitPrice)).ToArray());
    public static ClientDto Dto(Client c) => new(c.Id, c.Name, c.City, c.State, c.Address, c.Phone, c.SellerId, c.VisitOrder, c.Active);
    public static ProductDto Dto(Product p) => new(p.Id, p.Code, p.Description, p.Group, p.Unit, p.Price, p.Active);
    public static TermsDto Dto(PaymentTerms t) => new(t.Id, t.Name, t.Active, t.Parts.OrderBy(x => x.Number).Select(x => new PartDto(x.Number, x.Days, x.Percentage)).ToArray(), t.DailyInterestPercent, t.DailyDiscountPercent);
    public static (int Page, int Size) Paging(int? page, int? pageSize) => (Math.Clamp(page ?? 1, 1, 1000000), Math.Clamp(pageSize ?? 20, 1, 100));
}
