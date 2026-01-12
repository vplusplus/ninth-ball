
namespace NinthBall.Core
{
    internal interface ISimObjective
    {
        int Order { get => 50; }
        int MaxIterations { get => int.MaxValue; }
        ISimStrategy CreateStrategy(int iterationIndex);
    }

    internal interface ISimStrategy
    {
        void Apply(ISimContext context);
    }

    internal interface IBalance
    {
        double Amount { get; }
        double Allocation { get; }
        bool Rebalance(double maxDrift);
        bool Reallocate(double newAllocation, double maxDrift);
    }

    internal interface ISimContext
    {
        //....................................................
        // Running balance (Jan)
        //....................................................
        IBalance PreTaxBalance { get; }
        IBalance PostTaxBalance { get; }
        IBalance CashBalance { get; }

        //....................................................
        // Prior year results
        //....................................................
        ReadOnlyMemory<SimYear> PriorYears { get; }

        //....................................................
        // Current year
        //....................................................
        public int IterationIndex { get; }
        public int YearIndex { get; }
        public int Age { get; }

        //....................................................
        // Current year strategy recommendations
        //....................................................
        Fees Fees { get; set; }
        Incomes Incomes { get; set; }
        Expenses Expenses { get; set; }
        Withdrawals Withdrawals { get; set; }
        ROI ROI { get; set; }

        //....................................................
        // Inflation tracking
        //....................................................
        // Cumulative inflation factor from the start of the simulation up to current year.
        // Strategies see BEGENNING-OF-YEAR multiplier (On Jan 1st, we do not know current year inflation yet).
        // SimResult captures END-OF-YEAR multiplier (we know what happened that year).
        double RunningInflationMultiplier { get; }
    }
}
