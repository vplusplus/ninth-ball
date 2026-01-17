
namespace NinthBall.Core
{
    public sealed record SimResult(SimInput Input, IReadOnlyList<string> Strategies, IReadOnlyList<SimIteration> Iterations);

    public sealed record SimIteration(int Index, bool Success, ReadOnlyMemory<SimYear> ByYear)
    {
        // The engine's "Stop Order" - how many full years did we actually clear?
        public int SurvivedYears => Success ? ByYear.Length : Math.Max(0, ByYear.Length - 1);

        // The last year that successfully completed (or Empty))
        public SimYear LastGoodYear => SurvivedYears > 0 ? ByYear.Span[SurvivedYears - 1] : new();

        // Use last successful year for final inflation multiplier. 
        public double FinalInflationMultiplier => SurvivedYears == 0 ? 1.0 : LastGoodYear.RunningInflationMultiplier;
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

        double      EffectiveROI,                   // Effective ROI after fees and withdrawals, before deposits. 
        double      RunningInflationMultiplier,     // WARNING: Temporal coupling. Intended, but please be aware.
        double      RunningAnnualizedROI,           // WARNING: Temporal coupling. Intended, but please be aware.
        double      RealAnnualizedROI               // Real annualized ROI (Purchasing Power)
    );


    internal readonly record struct Metrics
    (
        // Multiplication factors
        double InflationMultiplier = 1.0,       // Cumulative inflation factor since year #0
        double GrowthMultiplier = 1.0,          // Nominal growth multiplier since year #0

        // Percentage values
        double PortfolioReturn = 0.0,           // Portfolio-weighted nominal return for the current year
        double AnnualizedReturn = 0.0,          // Annualized nominal return (CAGR) since year #0
        double RealAnnualizedReturn = 0.0       // Inflation-adjusted annualized return since year #0
    );

    public readonly record struct ROI(int LikeYear, double StocksROI, double BondsROI, double CashROI, double InflationRate);

    public readonly record struct Asset(double Amount, double Allocation);

    public readonly record struct Assets(Asset PreTax, Asset PostTax, Asset Cash)
    {
        public readonly double Total() => PreTax.Amount + PostTax.Amount + Cash.Amount;
    }

    public readonly record struct Fees(double PreTax, double PostTax, double Cash)
    {
        public readonly double Total() => PreTax + PostTax + Cash;
    }

    public readonly record struct Withdrawals(double PreTax, double PostTax, double Cash)
    {
        public readonly double Total() => PreTax + PostTax + Cash;
    }

    public readonly record struct Deposits(double PostTax, double Cash)
    {
        public readonly double Total() => PostTax + Cash;
    }

    public readonly record struct Incomes(double SS, double Ann)
    {
        public readonly double Total() => SS + Ann;
    }

    public readonly record struct Expenses(Tax PYTax, double LivExp)
    {
        public readonly double Total() => PYTax.Total() + LivExp;
    }

    public readonly record struct Change(double PreTax, double PostTax, double Cash)
    {
        public readonly double Total() => PreTax + PostTax + Cash;
    }

    public readonly record struct Tax(double TaxOnOrdInc, double TaxOnDiv, double TaxOnInt, double TaxOnCapGain) 
    {
        public readonly double Total() => TaxOnOrdInc + TaxOnDiv + TaxOnInt + TaxOnCapGain;
    }
}
