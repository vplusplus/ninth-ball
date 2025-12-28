
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
        
        SimParams SimParams,
        InitialBalance InitialBalance,

        // Historical Data & Bootstrapping
        ROIHistory? ROIHistory,
        FlatBootstrap? FlatBootstrap,
        MovingBlockBootstrap? MovingBlockBootstrap,
        ParametricBootstrap? ParametricBootstrap,

        // Strategies (Optional)
        Rebalance? Rebalance,
        Reallocate? Reallocate,
        AdditionalIncomes? AdditionalIncomes,
        
        FeesPCT? FeesPCT,
        Taxes? Taxes,
        LivingExpenses? LivingExpenses,
        PrecalculatedLivingExpenses? PrecalculatedLivingExpenses,
        
        FixedWithdrawal? FixedWithdrawal,
        PercentageWithdrawal? PercentageWithdrawal,
        VariablePercentageWithdrawal? VariablePercentageWithdrawal,
        RMD? RMD,
        
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
        int Iterations = 10000
    );

    public sealed record InitialBalance
    (
        [property: ValidateNested]
        InitialBalance.AA PreTax,

        [property: ValidateNested]
        InitialBalance.AA PostTax,

        [property: ValidateNested]
        InitialBalance.AA Cash          // Allocation is ignored for Cash assets
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
        double MaxDrift
    );

    public sealed record Reallocate
    (
        [property: Range(0.0, 0.5)] 
        double MaxDrift,

        [property: Required]
        [property: ValidateNested]
        IReadOnlyList<Reallocate.AA> Steps
    )
    {
        public readonly record struct AA
        (
            [property: Range(1, 100)]
            int AtAge,

            [property: Range(0.0, 1.0)]
            double Allocation
        );
    }

    public sealed record FeesPCT
    (
        [property: Range(0, 1)] double PreTax, 
        [property: Range(0, 1)] double PostTax, 
        [property: Range(0, 1)] double Cash
    );

    public sealed record Taxes
    ( 
        [property: Min(0)] 
        double YearZeroTaxAmount, 

        [property: ValidateNested]
        Taxes.Rates TaxRates
    )
    {
        public readonly record struct Rates
        (
            [property: Range(0, 1)] double OrdinaryIncome, 
            [property: Range(0, 1)] double CapitalGains
        );
    }

    public sealed record AdditionalIncomes
    (
        [property: ValidateNested] AdditionalIncomes.AAI SS,
        [property: ValidateNested] AdditionalIncomes.AAI Ann
    )
    {
        public readonly record struct AAI
        (
            [property: Range(1, 100)] 
            int FromAge,

            [property: Min(0)] 
            double Amount,

            [property:Range(0,1)]
            double Increment
        );
    }

    public sealed record LivingExpenses
    (
        [property:Min(0)]           double FirstYearAmount,
        [property:Range(0,1)]       double Increment,
        [property: ValidateNested]  IReadOnlyList<LivingExpenses.ARD> StepDown
    )
    {
        public readonly record struct ARD
        (
            [property: Min(50)] int     AtAge,
            [property: Min(00)] double  Reduction
        );
    }

    public sealed record PrecalculatedLivingExpenses
    (
        [property: Required()]
        [property: FileExists()]
        string FileName,

        [property: Required()]
        string SheetName
    );

    public sealed record FixedWithdrawal
    (
        [property: Min(0)] double FirstYearAmount, 
        [property: Range(0,1)] double Increment
    );

    public sealed record PercentageWithdrawal
    (
        [property: Range(0,1)] 
        double FirstYearPct,
        
        [property: Range(0,1)] 
        double Increment,

        [property: Required] 
        IReadOnlyList<int> ResetAtAge
    );

    public sealed record VariablePercentageWithdrawal
    (
        [property: Range(0, 1)] double ROI,
        [property: Range(0, 1)] double Inflation,
        [property: Min(0)] double? Floor = null,
        [property: Min(0)] double? Ceiling = null
    );

    public sealed record RMD
    (
        [property: Range(70, 80)] int StartAge = 73
    );

    public enum BootstrapKind
    {
        Flat, Sequential, MovingBlock, Parametric
    }

    public sealed record Growth
    (
        [property: Required]    BootstrapKind Bootstrapper, 
        [property: Range(0, 1)] double CashROI
    );

    public sealed record ROIHistory
    (
        [property: Required, FileExists] string XLFileName, 
        [property: Required] string XLSheetName
    );

    public sealed record FlatBootstrap
    (
        [property: Range(0, 1)] double Stocks,
        [property: Range(0, 1)] double Bonds
    );

    public sealed record MovingBlockBootstrap
    (
        [property: Required] IReadOnlyList<int> BlockSizes, 
        [property: Required] bool NoConsecutiveBlocks
    );
    
    public sealed record ParametricBootstrap
    (
        [property: Required]
        string DistributionType, 

        [property: Range(-1.0, 1.0)] 
        double StocksBondCorrelation,

        [property: ValidateNested]
        ParametricBootstrap.Dist Stocks,

        [property: ValidateNested]
        ParametricBootstrap.Dist Bonds)
    {
        public readonly record struct Dist(
            [property: Range(-1.0, 1.0)] double MeanReturn, 
            [property: Range(0.0, 1.0)]  double Volatility, 
            [property: Range(-10.0, 10.0)] double Skewness, 
            [property: Range(0.0, 100.0)]  double Kurtosis, 
            [property: Range(-1.0, 1.0)]   double AutoCorrelation
        );
    }
}
