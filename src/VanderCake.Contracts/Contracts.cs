using System.Text.Json;
using System.Text.Json.Serialization;

namespace VanderCake.Contracts;

public static class ApiJson
{
    public static JsonSerializerOptions Options { get; } = Create();
    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new IdConverter());
        return options;
    }
}
public sealed class IdConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String ? long.Parse(reader.GetString()!, System.Globalization.CultureInfo.InvariantCulture) : reader.GetInt64();
    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
}
public record LoginRequest(string Login, string Senha);
public record UserDto(long Id, string Nome, string Perfil);
public record LoginResult(string AccessToken, DateTime ExpiresAtUtc, UserDto Usuario);
public record PageResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems);
public record ClientDto(long Id, string Nome, string Cidade, string Estado, string Endereco, string Telefone, long VendedorId, int OrdemVisita, bool Ativo);
public record ClientInput(string Nome, string Cidade, string Estado, string Endereco, string Telefone, long? VendedorId, int OrdemVisita, bool Ativo = true);
public record ProductDto(long Id, string Codigo, string Descricao, string Grupo, string Unidade, decimal Preco, bool Ativo);
public record ProductInput(string Codigo, string Descricao, string Grupo, string Unidade, decimal Preco, bool Ativo = true);
public record PartDto(int Numero, int Dias, decimal Percentual);
public record TermsDto(long Id, string Nome, bool Ativo, IReadOnlyList<PartDto> Parcelas, decimal JurosDiaPercentual = 0, decimal DescontoDiaPercentual = 0);
public record TermsInput(string Nome, IReadOnlyList<PartDto> Parcelas, bool Ativo = true, decimal JurosDiaPercentual = 0, decimal DescontoDiaPercentual = 0);
public record MethodDto(long Id, string Nome, bool Ativo);
public record MethodInput(string Nome, bool Ativo = true);
public record OrderItemInput(long ProdutoId, int Quantidade, decimal PrecoConsultado);
public record OrderInput(long ClienteId, long CondicaoPagamentoId, long FormaPagamentoId, IReadOnlyList<OrderItemInput> Itens);
public record OrderItemDto(long ProdutoId, string Descricao, string Unidade, int QuantidadeSolicitada, int? QuantidadeAprovada, decimal PrecoUnitario);
public record OrderDto(long Id, long ClienteId, string Cliente, long VendedorId, string Situacao, DateTime EnviadoEmUtc, decimal TotalSolicitado, decimal TotalAprovado, string CondicaoPagamento, string FormaPagamento, IReadOnlyList<OrderItemDto> Itens);
public record PreviewInput(long[] PedidoIds);
public record PreviewItem(long PedidoId, long ClienteId, string Cliente, long ProdutoId, string Produto, int Estoque, int Solicitado, int Aprovado);
public record PreviewResult(string PreviewToken, DateTime ExpiresAtUtc, IReadOnlyList<PreviewItem> Itens);
public record ConfirmInput(string PreviewToken);
public record BatchResult(long LoteId, IReadOnlyList<long> VendaIds);
public record ReceiptInput(decimal Valor, DateOnly DataRecebimento, decimal? SaldoConferido = null);
public record StockInput(long ProdutoId, int Quantidade, string Motivo);
