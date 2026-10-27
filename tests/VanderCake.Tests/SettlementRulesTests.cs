using VanderCake.Core;

namespace VanderCake.Tests;

public class SettlementRulesTests
{
    private static readonly DateOnly Due = new(2026, 10, 20);
    [Fact]
    public void CompoundInterestAndDiscountMatchKnownExamples()
    {
        var late = SettlementRules.Quote(100, Due, Due.AddDays(3), 2, 1);
        Assert.Equal(106.12m, late.Total); Assert.Equal(6.12m, late.Interest); Assert.Equal(0, late.Discount);
        var early = SettlementRules.Quote(100, Due, Due.AddDays(-3), 2, 1);
        Assert.Equal(97.03m, early.Total); Assert.Equal(2.97m, early.Discount); Assert.Equal(0, early.Interest);
    }
    [Fact]
    public void DueDateAndZeroRatesDoNotChangePrincipal()
    {
        Assert.Equal(100, SettlementRules.Quote(100, Due, Due, 2, 1).Total);
        Assert.Equal(100, SettlementRules.Quote(100, Due, Due.AddDays(100), 0, 0).Total);
        Assert.Equal(100, SettlementRules.Quote(100, Due, Due.AddDays(-100), 0, 0).Total);
    }
    [Fact]
    public void PartialPaymentAllocatesChargesProportionallyAndRemainingDebtAccrues()
    {
        var quote = SettlementRules.Quote(100, Due, Due.AddDays(3), 2, 1);
        var receipt = SettlementRules.Allocate(quote, 53.06m);
        Assert.Equal(50, receipt.PrincipalSettled); Assert.Equal(3.06m, receipt.Interest);
        Assert.Equal(54.12m, SettlementRules.Quote(100 - receipt.PrincipalSettled, Due, Due.AddDays(4), 2, 1).Total);
    }
    [Fact]
    public void EarlyPartialAndFinalSettlementPreserveMoneyAndClosePrincipal()
    {
        var first = SettlementRules.Allocate(SettlementRules.Quote(100, Due, Due.AddDays(-3), 2, 1), 50);
        Assert.Equal(first.PrincipalSettled, 50 + first.Discount);
        var rest = 100 - first.PrincipalSettled;
        var finalQuote = SettlementRules.Quote(rest, Due, Due.AddDays(-2), 2, 1);
        var final = SettlementRules.Allocate(finalQuote, finalQuote.Total);
        Assert.Equal(100, first.PrincipalSettled + final.PrincipalSettled);
        Assert.Equal(finalQuote.Total, final.PrincipalSettled + final.Interest - final.Discount);
        Assert.Equal(0, SettlementRules.Quote(0, Due, Due.AddDays(30), 2, 1).Total);
    }
    [Fact]
    public void ExcessAndInvalidValuesAreRejected()
    {
        var quote = SettlementRules.Quote(100, Due, Due, 2, 1);
        Assert.Throws<ArgumentException>(() => SettlementRules.Allocate(quote, 100.01m));
        Assert.Throws<ArgumentException>(() => SettlementRules.Allocate(quote, 0));
        Assert.Throws<ArgumentException>(() => SettlementRules.Allocate(quote, 0.001m));
        Assert.Throws<ArgumentException>(() => SettlementRules.Quote(-1, Due, Due, 2, 1));
        Assert.Throws<ArgumentException>(() => SettlementRules.ValidateRates(-1, 1));
        Assert.Throws<ArgumentException>(() => SettlementRules.ValidateRates(2, 100));
        Assert.Throws<ArgumentException>(() => SettlementRules.ValidateRates(2.001m, 1));
        Assert.Throws<ArgumentException>(() => SettlementRules.Quote(100, Due, Due.AddDays(3650), 10, 1));
    }
    [Fact]
    public void VeryLongEarlyDiscountNeverCreatesNegativeOrUnpayableZeroBalance()
    {
        var quote = SettlementRules.Quote(100, Due, Due.AddDays(-3650), 2, 99);
        Assert.Equal(0.01m, quote.Total);
        Assert.Equal(100, SettlementRules.Allocate(quote, quote.Total).PrincipalSettled);
    }
}
