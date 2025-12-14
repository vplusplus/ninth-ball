
using DocumentFormat.OpenXml.Bibliography;

namespace NinthBall
{
    //..........................................................................
    #region The immutable data structures
    //..........................................................................
    public readonly record struct ROI(int LikeYear, double StocksROI, double BondsROI, double CashROI)
    {
        public override string ToString() => $"[Y:{LikeYear}]{StocksROI:P1}/{BondsROI:P1}/{CashROI:P1}";
    }

    public readonly record struct Asset(double Amount, double Allocation);

    public readonly record struct Assets(Asset PreTax, Asset PostTax, Asset Cash)
    {
        public readonly double Total() => PreTax.Amount + PostTax.Amount + Cash.Amount;
    }

    public readonly record struct FeesPCT(double PreTaxFeesPCT, double PostTaxFeesPCT, double CashFeesPCT)
    {
        public readonly Fees CalculateFees(Assets p) => new
        (
            PreTax:  p.PreTax.Amount * this.PreTaxFeesPCT,
            PostTax: p.PostTax.Amount * this.PostTaxFeesPCT,
            Cash:    p.Cash.Amount * this.CashFeesPCT
        );
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

    #endregion

    //..........................................................................
    #region SimResult
    //..........................................................................
    /// <summary>
    /// Immutable structure that describe result of a single year in an iteration.
    /// </summary>
    public readonly record struct SimYear
    (
        int Year,

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

    /// <summary>
    /// Immutable structure that describe result of an iteration.
    /// </summary>
    public sealed record SimIteration(int Index, bool Success, IReadOnlyList<SimYear> ByYear)
    {
        public double StartingBalance => ByYear[0].Jan.Total();
        public double EndingBalance => ByYear[^1].Dec.Total();
        public int SurvivedYears => Success ? ByYear.Count : ByYear.Count - 1;
    }

    /// <summary>
    /// Immutable structure that represents results of a simulation.
    /// </summary>
    public sealed record SimResult(IReadOnlyList<ISimObjective> Objectives, IReadOnlyList<SimIteration> Iterations)
    {
        public int NoOfYears { get; init; } = Iterations.Max(x => x.ByYear.Count);
        public double SurvivalRate { get; init; } = (double)Iterations.Count(x => x.Success) / (double)Iterations.Count;

        public SimIteration Percentile(double percentile) =>
            percentile < 0.0 || percentile > 1.0 ? throw new ArgumentOutOfRangeException(nameof(percentile), "Percentile must be between 0.0 and 1.0") :
            Iterations.Count == 0 ? throw new InvalidOperationException("No results available") :
            Iterations[(int)(percentile * (Iterations.Count - 1))];
    }

    #endregion

    //..........................................................................
    #region Contracts - ISimObjective, ISimStrategy, ISimContect
    //..........................................................................
    public interface ISimObjective
    {
        int Order { get => 50; }
        ISimStrategy CreateStrategy(int iterationIndex);
    }

    public interface ISimStrategy
    {
        void Apply(ISimContext context);
    }

    public interface IBalance
    {
        double Amount { get; }
        double Allocation { get; }
        void Init(double amount, double allocation);
        bool RebalanceIf(double maxDrift);
        bool Reallocate(double newAllocation, double maxDrift);
    }

    public interface ISimContext
    {
        public int IterationIndex { get; }
        public int YearIndex { get; }

        // Prior year results
        IReadOnlyList<SimYear> PriorYears { get; }

        // Running balance (Jan)
        IBalance PreTaxBalance { get; }
        IBalance PostTaxBalance { get; }
        IBalance CashBalance { get; }

        // Current year 
        FeesPCT FeesPCT { get; set; }
        Incomes Incomes { get; set; }
        Expenses Expenses { get; set; }
        Withdrawals Withdrawals { get; set; }
        Deposits Refills { get; set; }
        ROI ROI { get; set; }
    }

    #endregion

}
