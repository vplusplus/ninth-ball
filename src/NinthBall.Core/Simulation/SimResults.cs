using System;
using System.Collections.Generic;
using System.Text;

namespace NinthBall.Core
{
    public readonly record struct ROI(int LikeYear, double StocksROI, double BondsROI, double CashROI)
    {
        public override string ToString() => $"[Y:{LikeYear}]{StocksROI:P1}/{BondsROI:P1}/{CashROI:P1}";
    }

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

    public readonly record struct Expenses(double PYTax, double CYExp)
    {
        public readonly double Total() => PYTax + CYExp;
    }

    public readonly record struct Change(double PreTax, double PostTax, double Cash)
    {
        public readonly double Total() => PreTax + PostTax + Cash;
    }

    public readonly record struct SimYear
    (
        int Year,
        int Age,

        Assets Jan,
        Fees Fees,
        Incomes Incomes,
        Expenses Expenses,
        Withdrawals Withdrawals,
        Deposits Deposits,
        ROI ROI,
        Change Change,
        Assets Dec
    );

    public sealed record SimIteration(int Index, bool Success, ReadOnlyMemory<SimYear> ByYear)
    {
        public double StartingBalance => ByYear.Span[0].Jan.Total();
        public double EndingBalance => ByYear.Span[^1].Dec.Total();
        public int SurvivedYears => Success ? ByYear.Length : ByYear.Length - 1;
    }

    public sealed record SimResult(IReadOnlyList<ISimObjective> Objectives, IReadOnlyList<SimIteration> Iterations)
    {
        public int NoOfYears { get; init; } = Iterations.Count == 0 ? 0 : Iterations.Max(x => x.ByYear.Length);
        public double SurvivalRate { get; init; } = Iterations.Count == 0 ? 0.0 : (double)Iterations.Count(x => x.Success) / (double)Iterations.Count;

        public SimIteration Percentile(double percentile) =>
            percentile < 0.0 || percentile > 1.0 ? throw new ArgumentOutOfRangeException(nameof(percentile), "Percentile must be between 0.0 and 1.0") :
            Iterations.Count == 0 ? throw new InvalidOperationException("No results available") :
            Iterations[(int)(percentile * (Iterations.Count - 1))];
    }
}
