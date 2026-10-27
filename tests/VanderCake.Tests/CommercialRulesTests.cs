using VanderCake.Core;
namespace VanderCake.Tests;

public class CommercialRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
    [Fact] public void ShortageUsesLargestRemainders()
    {
        var result = CommercialRules.Allocate(15, [new(1, 5, Now), new(2, 8, Now), new(3, 9, Now)]);
        Assert.Equal(3, result[1]); Assert.Equal(6, result[2]); Assert.Equal(6, result[3]);
    }
    [Fact] public void EqualRemaindersPreferOldestClientOrder()
    {
        var result = CommercialRules.Allocate(1, [new(1, 2, Now.AddMinutes(1)), new(2, 2, Now)]);
        Assert.Equal(0, result[1]); Assert.Equal(1, result[2]);
    }
    [Fact] public void FullTieUsesClientId()
    {
        var result = CommercialRules.Allocate(1, [new(9, 1, Now), new(3, 1, Now)]);
        Assert.Equal(1, result[3]);
    }
    [Theory] [InlineData(0,0,0)] [InlineData(2,1,1)] [InlineData(50,4,6)]
    public void StockIsNeverExceeded(int stock, int expectedFirst, int expectedSecond)
    {
        var result = CommercialRules.Allocate(stock, [new(1, 4, Now), new(2, 6, Now)]);
        Assert.Equal(expectedFirst, result[1]); Assert.Equal(expectedSecond, result[2]);
    }
    [Fact] public void BroadRangeConservesUnitsAndRespectsDemand()
    {
        for (int a=1; a<=20; a++) for (int b=1;b<=20;b++) for (int stock=0;stock<=a+b+1;stock++)
        {
            var rows = CommercialRules.Allocate(stock, [new(1,a,Now),new(2,b,Now)]);
            Assert.Equal(Math.Min(stock,a+b), rows.Values.Sum()); Assert.InRange(rows[1],0,a); Assert.InRange(rows[2],0,b);
        }
    }
    [Fact] public void DuplicateClientsMustBeAggregated() => Assert.Throws<ArgumentException>(() => CommercialRules.Allocate(2,[new(1,1,Now),new(1,1,Now)]));
    [Fact] public void IntegerProductsDoNotOverflowDuringMultiplication()
    {
        var result = CommercialRules.Allocate(2000000000,[new(1,1500000000,Now),new(2,1500000000,Now)]);
        Assert.Equal(1000000000,result[1]); Assert.Equal(1000000000,result[2]);
    }
    [Fact] public void EqualHalvesCloseExactly() => Assert.Equal([117.50m,117.50m],CommercialRules.Installments(235m,[50m,50m]));
    [Fact] public void FinalInstallmentAbsorbsRounding() => Assert.Equal([.03m,.03m,.04m],CommercialRules.Installments(.10m,[33.33m,33.33m,33.34m]));
    [Fact] public void TinySalesCannotHaveZeroInstallment() => Assert.Throws<ArgumentException>(()=>CommercialRules.Installments(.01m,[50m,50m]));
    [Fact] public void PercentagesMustCloseToOneHundred() => Assert.Throws<ArgumentException>(()=>CommercialRules.Installments(100m,[33.33m,33.33m,33.33m]));
    [Fact] public void EmptyDemandProducesEmptyAllocation() => Assert.Empty(CommercialRules.Allocate(10,[]));
}
