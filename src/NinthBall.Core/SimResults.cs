
namespace NinthBall.Core
{
    public sealed record SimResult(SimParams SimParams, IReadOnlyList<string> Strategies, IReadOnlyList<SimIteration> Iterations);

    public sealed record SimIteration(int Index, bool Success, ReadOnlyMemory<SimYear> ByYear);

    public readonly record struct SimYear
    (
        int             Year,
        int             Age,
        Rebalanced      Rebalanced,
        Assets          Jan,
        Fees            Fees,
        Taxes           Taxes,
        Incomes         Incomes,
        Expenses        Expenses,
        ROI             ROI,
        Withdrawals     Withdrawals,
        Deposits        Deposits,
        Change          Change,
        Assets          Dec,
        Growth          Growth,
        InflationIndex  InflationIndex
    );

    public readonly record struct Asset(double Amount, double Allocation);

    public readonly record struct Assets(Asset PreTax, Asset PostTax, Asset Cash);

    public readonly record struct Fees(double PreTax, double PostTax);

    public readonly record struct Incomes(double SS, double Ann);

    public readonly record struct Expenses(double LivExp);

    public readonly record struct Withdrawals(double PreTax, double PostTax, double Cash);

    public readonly record struct Deposits(double PostTax, double Cash);

    public readonly record struct ROI(int LikeYear, double StocksROI, double BondsROI, double InflationRate);

    public readonly record struct Change(double PreTax, double PostTax);

    public readonly record struct Rebalanced(Rebalanced.SB PreTax, Rebalanced.SB PostTax)
    {
        public readonly record struct SB(double StocksChange, double BondsChange);
    }
}

