namespace VanderCake.Core;

public record Demand(long ClientId, int Quantity, DateTime FirstOrderUtc);

public static class CommercialRules
{
    public static IReadOnlyDictionary<long, int> Allocate(int stock, IEnumerable<Demand> demands)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(stock);
        var rows = demands.ToArray();
        if (rows.Any(x => x.Quantity <= 0) || rows.Select(x => x.ClientId).Distinct().Count() != rows.Length)
            throw new ArgumentException("Agrupe demandas positivas por cliente.");
        long total = rows.Sum(x => (long)x.Quantity);
        if (total <= stock) return rows.ToDictionary(x => x.ClientId, x => x.Quantity);
        var quotas = rows.Select(x => new
        {
            Row = x,
            Whole = (int)((long)stock * x.Quantity / total),
            Remainder = (long)stock * x.Quantity % total
        }).ToArray();
        var result = quotas.ToDictionary(x => x.Row.ClientId, x => x.Whole);
        int remaining = stock - result.Values.Sum();
        foreach (var row in quotas.OrderByDescending(x => x.Remainder)
                     .ThenBy(x => x.Row.FirstOrderUtc).ThenBy(x => x.Row.ClientId).Take(remaining))
            result[row.Row.ClientId]++;
        return result;
    }

    public static decimal[] Installments(decimal total, IReadOnlyList<decimal> percentages)
    {
        if (total <= 0 || percentages.Count == 0 || percentages.Any(x => x <= 0) || percentages.Sum() != 100m)
            throw new ArgumentException("Total e percentuais de parcelamento inválidos.");
        var values = new decimal[percentages.Count];
        for (int i = 0; i < values.Length - 1; i++)
            values[i] = decimal.Round(total * percentages[i] / 100m, 2, MidpointRounding.AwayFromZero);
        values[^1] = total - values.Take(values.Length - 1).Sum();
        if (values.Any(x => x <= 0)) throw new ArgumentException("O valor gera parcela igual ou inferior a zero.");
        return values;
    }
}
