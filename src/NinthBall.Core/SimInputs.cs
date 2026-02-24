
using NinthBall.Utils;
using System.ComponentModel.DataAnnotations;

namespace NinthBall.Core
{
    public sealed record SimulationSeed(string? SeedHint)
    {
        public readonly int Value = (SeedHint ?? "JSR").GetPredictableHashCode();

        // BY-DESIGN: RegimeDiscoverySeed is NOT same as Simulation seed.
        // IMPORTANT: Trust me, do not change RegimeDiscoverySeed.
        public readonly int RegimeDiscoverySeed = 2767;
    }

    public sealed record SimParams
    (
        [property: Range(25,  100)]     int StartAge,
        [property: Range(1,   100)]     int NoOfYears,
        [property: Range(1, 50000)]     int Iterations,
        [property: Required]            IReadOnlyList<string> Objectives
    );

    public sealed record Initial
    (
        [property: Min(0.01)]           double YearZeroCashBalance,
        [property: Min(0.01)]           double YearZeroTaxAmount,
        [property: ValidateNested]      Initial.AA PreTax,
        [property: ValidateNested]      Initial.AA PostTax
    )
    {
        public readonly record struct AA
        (
            [property: Min(0.01)]       double Amount,
            [property: Max(1.0)]        double Allocation
        );
    }

    public sealed record YearlyRebalance
    (
        [property: Range(0.0, 0.5)]         double MaxDrift,
        [property: ValidateNested]          IReadOnlyList<YearlyRebalance.AAA> Reallocate = null!
    )
    {
        public readonly record struct AAA
        (
            [property: Range(25, 100)]      int AtAge,
            [property: Range(0.0001, 1.0)]  double PreTaxStocksAllocation,
            [property: Range(0.0001, 1.0)]  double PostTaxStocksAllocation
        );
    }

    public sealed record AnnualFees
    (
        [property: Range(0.0001, 1)] double PreTax, 
        [property: Range(0.0001, 1)] double PostTax
    );

    public sealed record FlatTax
    ( 
        [property: Min(0.01)]       double StandardDeduction,
        [property: Min(0.01)]       double StateExemption,
        [property: Min(0.0001)]     double FederalOrdInc,
        [property: Min(0.0001)]     double FederalLTCG,
        [property: Min(0.0001)]     double State
    );

    public sealed record AdditionalIncomes
    (
        [property: ValidateNested] AdditionalIncomes.SSIncome  SS,
        [property: ValidateNested] AdditionalIncomes.ANNIncome Ann
    )
    {
        public readonly record struct SSIncome
        (
            [property: Range(1, 100)]       int FromAge,
            [property: Min(0)]              double Amount
        );

        public readonly record struct ANNIncome
        (
            [property: Range(1, 100)]       int FromAge,
            [property: Min(0)]              double Amount,
            [property: Range(0.0, 0.1)]     double Increment
        );

    }

    public sealed record LivingExpenses
    (
        [property: Min(1)]          double FirstYearAmount,
        [property: ValidateNested]  IReadOnlyList<LivingExpenses.ARD> StepDown
    )
    {
        public readonly record struct ARD
        (
            [property: Min(1)]      int     AtAge,
            [property: Min(1)]      double  Reduction
        );
    }

    public sealed record FixedWithdrawal
    (
        [property: Range(0.0001, 0.1)]  double FirstYearPct,
        [property: Range(0.0001, 0.1)]  double Increment,
        [property: Required]            IReadOnlyList<int> ResetAtAge
    );

    public sealed record VariableWithdrawal
    (
        [property: Range(0.0001, 0.50)]         double FutureROI,
        [property: Range(0.0001, 0.50)]         double FutureInflation,
        [property: Min(0)]                      double? Floor = null,
        [property: Min(0)]                      double? Ceiling = null
    );

    public sealed record TaxAndMarketAssumptions
    (
        [property: Min(1000)]           double SSFederalNonTaxableThreshold,
        [property: Min(1000)]           double SSFederal50PctTaxableThreshold,

        [property: Min(1000)]           double NIITThreshold,
        [property: Range(0.001, 1.0)]   double NIITRate,
        [property: Range(0.001, 1.0)]   double TypicalStocksDividendYield,
        [property: Range(0.001, 1.0)]   double TypicalBondCouponYield,

        [property: Range(0.0001, 1.0)]  double FedTaxInflationLagHaircut,
        [property: Range(0.0001, 1.0)]  double StateTaxInflationLagHaircut
    );

}
