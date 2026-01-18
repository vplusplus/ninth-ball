
namespace NinthBall.Core
{
    public sealed record SimResult(SimInput Input, IReadOnlyList<string> Strategies, IReadOnlyList<SimIteration> Iterations);

    public sealed record SimIteration(int Index, bool Success, ReadOnlyMemory<SimYear> ByYear);

    public readonly record struct SimYear
    (
        int         Year,
        int         Age,
        Assets      Jan,
        Fees        Fees,
        Taxes       Taxes,
        Incomes     Incomes,
        Expenses    Expenses,
        ROI         ROI,
        Withdrawals Withdrawals,
        Deposits    Deposits,
        Change      Change,
        Assets      Dec,
        Metrics     Metrics
    );

    public readonly record struct Asset(double Amount, double Allocation);

    public readonly record struct Assets(Asset PreTax, Asset PostTax, Asset Cash);

    public readonly record struct Fees(double PreTax, double PostTax);

    public readonly record struct Taxable(double OrdInc, double DIV, double INT, double LTCG);

    public readonly record struct Tax(double OrdInc, double DIV, double INT, double LTCG);

    public readonly record struct TaxRate(double OrdInc, double LTCG);

    public readonly record struct Taxes(double StandardDeduction, TaxRate TaxRates, Taxable Taxable, Tax Tax);

    public readonly record struct Incomes(double SS, double Ann);

    public readonly record struct Expenses(double LivExp);

    public readonly record struct Withdrawals(double PreTax, double PostTax, double Cash);

    public readonly record struct Deposits(double PostTax, double Cash);

    public readonly record struct ROI(int LikeYear, double StocksROI, double BondsROI, double InflationRate);

    public readonly record struct Change(double PreTax, double PostTax);

    public readonly record struct Metrics
    (
        // Multiplication factors
        double InflationMultiplier,     // Cumulative inflation multiplier since year #0
        double GrowthMultiplier,        // Nominal growth multiplier since year #0

        // Percentage values
        double PortfolioReturn,         // Portfolio-weighted nominal return for the current year
        double AnnualizedReturn,        // Annualized nominal return (CAGR) since year #0
        double RealAnnualizedReturn     // Inflation-adjusted annualized return since year #0
    )
    {
        // CRITICAL: Multipliers start with 1.0, rest are zero
        public Metrics() : this(InflationMultiplier: 1.0, GrowthMultiplier: 1.0, 0.0, 0.0, 0.0) { }
    }

}
