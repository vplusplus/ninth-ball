
namespace NinthBall.Core
{
    public sealed record SimResult(SimInput Input, IReadOnlyList<string> Strategies, IReadOnlyList<SimIteration> Iterations);

    public sealed record SimIteration(int Index, bool Success, ReadOnlyMemory<SimYear> ByYear)
    {
        public double FinalInflationMultiplier => ByYear.Length > 0 ? ByYear.Span[ByYear.Span.Length - 1].RunningInflationMultiplier : 1.0;
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
        double      RunningInflationMultiplier,     // WARNING: Temporal coupling. Intended, but please be aware.
        double      RunningAnnualizedROI            // WARNING: Temporal coupling. Intended, but please be aware.
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
