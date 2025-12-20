
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace NinthBall
{
    public sealed record SimulationSeed(IConfiguration config)
    {
        public readonly int Value = config.GetValue("RandomSeedHint", "JSR").GetPredictableHashCode();
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
        InitialBalance.AA Cash
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
        [Range(0.0, 0.5)]
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
        [property:Min(0)]       double FirstYearAmount,
        [property:Range(0,1)]   double Increment
    );

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
        [property: Range(0, 1)] double Escalation
    );

    public sealed record UseBufferCash
    (
        [property: Range(0, 1)] 
        double GrowthThreshold, 

        [property: Min(0)] 
        double MaxAmount
    );

    public sealed record BufferRefill
    (
        [property: Range(0, 1)] double GrowthThreshold, 
        [property: Min(0)] double MaxAmount
    );

    public sealed record Growth
    (
        [property: Required]    string Bootstrapper, 
        [property: Range(0, 1)] double CashROI
    );

    public sealed record ROIHistory
    (
        [property: Required] string XLFileName, 
        [property: Required] string XLSheetName
    );

    public sealed record MovingBlockBootstrapOptions(
        [property: Required] IReadOnlyList<int> BlockSizes, bool NoConsecutiveBlocks
    );
    
    public sealed record FlatBootstrap
    (
        [property: Range(0, 1)] double Stocks,
        [property: Range(0, 1)] double Bonds
    );

    public sealed record ParametricBootstrap
    (
        [property: Required]
        string DistributionType, 

        [property: Range(0, 1)] 
        double StocksBondCorrelation,

        [property: ValidateNested]
        ParametricBootstrap.Dist Stocks,

        [property: ValidateNested]
        ParametricBootstrap.Dist Bonds)
    {
        public readonly record struct Dist(
            double MeanReturn, double Volatility, double Skewness, double Kurtosis, double AutoCorrelation
        );

    }
}
