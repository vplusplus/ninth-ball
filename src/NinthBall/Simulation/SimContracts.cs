
namespace NinthBall
{
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
        bool Rebalance(double maxDrift);
        bool Reallocate(double newAllocation, double maxDrift);
    }

    public interface ISimContext
    {
        // Running balance (Jan)
        IBalance PreTaxBalance { get; }
        IBalance PostTaxBalance { get; }
        IBalance CashBalance { get; }

        // Prior year results
        IReadOnlyList<SimYear> PriorYears { get; }

        // Current year
        public int IterationIndex { get; }
        public int YearIndex { get; }
        public int Age { get; }

        // Current year 
        Fees Fees { get; set; }
        Incomes Incomes { get; set; }
        Expenses Expenses { get; set; }
        Withdrawals Withdrawals { get; set; }
        Deposits Refills { get; set; }
        ROI ROI { get; set; }
    }
}

