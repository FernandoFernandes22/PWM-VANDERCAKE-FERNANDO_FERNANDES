namespace VanderCake.Core;

public record SettlementQuote(decimal Principal, decimal Interest, decimal Discount, decimal Total, int LateDays, int EarlyDays);
public record SettlementAllocation(decimal PrincipalSettled, decimal Interest, decimal Discount);

public static class SettlementRules
{
    private static decimal Money(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    public static void ValidateRates(decimal interest, decimal discount)
    {
        if (interest is < 0 or > 10 || discount is < 0 or > 99 || decimal.Round(interest, 2) != interest || decimal.Round(discount, 2) != discount)
            throw new ArgumentException("Use juros de 0% a 10% ao dia e desconto de 0% a 99% ao dia, com até duas casas decimais.");
    }
    public static SettlementQuote Quote(decimal principal, DateOnly due, DateOnly payment, decimal interestPercent, decimal discountPercent)
    {
        ValidateRates(interestPercent, discountPercent);
        if (principal < 0 || Money(principal) != principal) throw new ArgumentException("Saldo principal inválido.");
        int late = Math.Max(0, payment.DayNumber - due.DayNumber), early = Math.Max(0, due.DayNumber - payment.DayNumber);
        if (principal == 0) return new(0, 0, 0, 0, late, early);
        var factor = late > 0 ? 1 + interestPercent / 100 : 1 - discountPercent / 100;
        decimal adjusted = principal;
        try
        {
            // Decimal em todo o cálculo; arredonda somente o resultado final.
            decimal power = 1;
            for (int days = late + early; days > 0; days >>= 1)
            {
                if ((days & 1) != 0) power = checked(power * factor);
                if (days > 1) factor = checked(factor * factor);
            }
            adjusted = Math.Max(0.01m, Money(checked(principal * power)));
            if (adjusted > 9999999999999.99m) throw new OverflowException();
        }
        catch (OverflowException) { throw new ArgumentException("O valor com encargos excede o limite suportado. Confira a data e a taxa."); }
        return new(principal, Math.Max(0, adjusted - principal), Math.Max(0, principal - adjusted), adjusted, late, early);
    }
    public static SettlementAllocation Allocate(SettlementQuote quote, decimal received)
    {
        if (received <= 0 || Money(received) != received || received > quote.Total) throw new ArgumentException("Informe um recebimento positivo, com duas casas decimais e até o saldo atualizado.");
        // No parcial, juros/desconto e amortização são proporcionais ao valor efetivamente recebido.
        var principal = received == quote.Total ? quote.Principal : Money(quote.Principal * (received / quote.Total));
        if (principal <= 0 || principal >= quote.Principal && received < quote.Total)
            throw new ArgumentException("O recebimento parcial não permite amortização em centavos. Ajuste o valor ou quite a parcela.");
        return new(principal, Math.Max(0, received - principal), Math.Max(0, principal - received));
    }
}
