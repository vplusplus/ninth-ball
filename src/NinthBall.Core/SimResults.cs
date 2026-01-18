
namespace NinthBall.Core
{
    public sealed record SimResult(SimInput Input, IReadOnlyList<string> Strategies, IReadOnlyList<SimIteration> Iterations);

    public sealed record SimIteration(int Index, bool Success, ReadOnlyMemory<SimYear> ByYear)
    {
        public int SurvivedYears => Success ? ByYear.Length : Math.Max(0, ByYear.Length - 1);
        public SimYear LastGoodYear => SurvivedYears > 0 ? ByYear.Span[SurvivedYears - 1] : new();
    }

    public readonly record struct SimYear
    (
        int         Year,
        int         Age,
        Assets      Jan,
        Fees        Fees,
        Incomes     Incomes,
        Expenses    Expenses,
        Withdrawals Withdrawals,
        Deposits    Deposits,
        ROI         ROI,
        Change      Change,
        Assets      Dec,
        Metrics     Metrics
    );


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
        public Metrics() : this ( InflationMultiplier: 1.0, GrowthMultiplier: 1.0, 0.0, 0.0, 0.0 ) { }
    }

    public readonly record struct ROI(int LikeYear, double StocksROI, double BondsROI, double InflationRate);

    public readonly record struct Asset(double Amount, double Allocation);

    public readonly record struct Assets(Asset PreTax, Asset PostTax, Asset Cash)
    {
        public readonly double Total => PreTax.Amount + PostTax.Amount + Cash.Amount;
    }

    public readonly record struct Fees(double PreTax, double PostTax)
    {
        public readonly double Total => PreTax + PostTax;
    }

    public readonly record struct Withdrawals(double PreTax, double PostTax, double Cash)
    {
        public readonly double Total => PreTax + PostTax + Cash;
    }

    public readonly record struct Deposits(double PostTax, double Cash)
    {
        public readonly double Total => PostTax + Cash;
    }

    public readonly record struct Incomes(double SS, double Ann)
    {
        public readonly double Total => SS + Ann;
    }

    public readonly record struct Expenses(Tax PYTax, double LivExp)
    {
        public readonly double Total => PYTax.Total + LivExp;
    }

    public readonly record struct Change(double PreTax, double PostTax)
    {
        public readonly double Total => PreTax + PostTax;
    }

    public readonly record struct Tax(double StandardDeduction, double TaxOnOrdInc, double TaxOnDiv, double TaxOnInt, double TaxOnCapGain) 
    {
        public readonly double Total => TaxOnOrdInc + TaxOnDiv + TaxOnInt + TaxOnCapGain;
    }
}
