
using System.ComponentModel.DataAnnotations;

namespace NinthBall.Core
{
    /// <summary>
    /// Immutable data structure(s) representing simulation inputs.
    /// This is the "Domain Contract" that can be deserialized from YAML/Json or populated by a UX.
    /// </summary>
    public sealed record SimInput
    (
        string? RandomSeedHint,

        [property: Required]
        SimParams SimParams,

        [property: Required]
        InitialBalance InitialBalance,

        Rebalance? Rebalance,
        FeesPCT? FeesPCT,
        FlatTax? FlatTax,
        AdditionalIncomes? AdditionalIncomes,
        LivingExpenses? LivingExpenses,

        // Pretax distribution (RMD is given, always added, not configurable)
        FixedWithdrawal? FixedWithdrawal,
        VariableWithdrawal? VariableWithdrawal,

        // Growth strategy, Historical Data & Bootstrapping
        Growth? Growth
    );

    public sealed record SimulationSeed(string? SeedHint)
    {
        public readonly int Value = (SeedHint ?? "JSR").GetPredictableHashCode();
    }

    public sealed record SimParams
    (
        [property: Range(50, 100)]
        int StartAge,

        [property: Range(1, 100)]
        int NoOfYears,

        [property: Range(1, 50000)]
        int Iterations,

        [property: Required]
        IReadOnlyList<string> Strategies
    );

    public sealed record InitialBalance
    (
        [property: ValidateNested]
        InitialBalance.AA PreTax,

        [property: ValidateNested]
        InitialBalance.AA PostTax,

        //[property: ValidateNested]
        //InitialBalance.AA Cash,                 // Allocation is ignored for Cash assets

        [property: Min(0.01)]
        double YearZeroCashBalance,

        [property: Min(1000)]
        double YearZeroTaxAmount
    )
    {
        public readonly record struct AA
        (
            [property:Min(0)]
            double Amount,

            [property: Range(0.0, 1.0)]
            double Allocation
        );
    }

    public sealed record Rebalance
    (
        [property: Range(0.0, 0.5)] 
        double MaxDrift,

        [property: ValidateNested]
        IReadOnlyList<Rebalance.AAA> Reallocate
    )
    {
        public readonly record struct AAA
        (
            [property: Range(25, 125)]
            int AtAge,

            [property: Range(0.001, 1.0)]       // Minimum 0.1% to prevent misconfiguration
            double PreTaxStocksAllocation,

            [property: Range(0.001, 1.0)]       // Minimum 0.1% to prevent misconfiguration
            double PostTaxStocksAllocation
        );
    }

    public sealed record FeesPCT
    (
        [property: Range(0, 1)] double PreTax, 
        [property: Range(0, 1)] double PostTax
    );

    public sealed record FlatTax
    ( 
        [property: Range(1.0, 100000.0)]
        double StandardDeduction,

        [property: Range(1.0, 100000.0)]
        double StateExemption,

        [property: Range(0.12, 0.50)] double FederalOrdInc,     // 12% to 50%
        [property: Range(0.15, 0.50)] double FederalLTCG,       // 15% to 50%
        [property: Range(0.05, 0.50)] double State              //  5% to 50%
    );

    public sealed record AdditionalIncomes
    (
        [property: ValidateNested] AdditionalIncomes.SSIncome  SS,
        [property: ValidateNested] AdditionalIncomes.ANNIncome Ann
    )
    {
        public readonly record struct SSIncome
        (
            [property: Range(1, 100)] 
            int FromAge,

            [property: Min(0)] 
            double Amount
        );

        public readonly record struct ANNIncome
        (
            [property: Range(1, 100)]
            int FromAge,

            [property: Min(0)]
            double Amount,

            [property: Range(0.0, 0.1)]
            double Increment
        );

    }

    public sealed record LivingExpenses
    (
        [property:Min(0)]           double FirstYearAmount,
        [property: ValidateNested]  IReadOnlyList<LivingExpenses.ARD> StepDown
    )
    {
        public readonly record struct ARD
        (
            [property: Min(50)] int     AtAge,
            [property: Min(00)] double  Reduction
        );
    }

    public sealed record FixedWithdrawal
    (
        [property: Range(0,1)] 
        double FirstYearPct,
        
        [property: Range(0,1)] 
        double Increment,

        [property: Required] 
        IReadOnlyList<int> ResetAtAge
    );

    public sealed record VariableWithdrawal
    (
        [property: Range(0, 1)] 
        double FutureROI,

        [property: Range(0, 0.3)]
        double FutureInflation,

        [property: Min(0)] 
        double? Floor = null,

        [property: Min(0)] 
        double? Ceiling = null
    );

    public enum BootstrapKind
    {
        Flat, Sequential, MovingBlock, Parametric
    }

    public sealed record Growth
    (
        [property: Required]
        BootstrapKind Bootstrapper,

        // Options used only by FlatBootstrapper
        // Ignored by other bootstrappers
        [property: Range(0, 1)] double Stocks,
        [property: Range(0, 1)] double Bonds,
        [property: Range(0, 1)] double InflationRate
    );

}
