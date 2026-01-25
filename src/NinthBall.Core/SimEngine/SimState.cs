
namespace NinthBall.Core
{
    /// <summary>
    /// Represents working-memory of one iteration.
    /// </summary>
    internal interface ISimState
    {
        //....................................................
        // Current iteration
        //....................................................
        int IterationIndex { get; }
        int StartAge { get; }
        int YearIndex { get; }
        int Age { get; }

        SimYear PriorYear { get; }
        Metrics PriorYearMetrics { get; }

        Assets Initial { get; }
        Assets Jan { get; }
        Fees Fees { get; set; }
        Taxes Taxes { get; set; }
        Incomes Incomes { get; set; }
        Expenses Expenses { get; set; }
        Withdrawals Withdrawals { get; set; }
        ROI ROI { get; set; }

        void Rebalance(double preTaxAllocation, double postTaxAllocation, double maxDrift);
    }

    /// <summary>
    /// Readonly access to the working-memory of one iteration.
    /// </summary>
    internal interface IReadOnlySimState
    {
        int IterationIndex { get; }
        int StartAge { get; }
        int YearIndex { get; }
        int Age { get; }

        SimYear PriorYear { get; }
        Metrics PriorYearMetrics { get; }

        Assets Initial { get; }
        Assets Jan { get; }
        Fees Fees { get; }
        Taxes Taxes { get; }
        Incomes Incomes { get; }
        Expenses Expenses { get; }
        Withdrawals Withdrawals { get; }
        ROI ROI { get; }
    }

    /// <summary>
    /// Working-memory of one iteration.
    /// </summary>
    internal sealed record class SimState(int IterationIndex, int StartAge, Assets Initial, Memory<SimYear> Storage) : ISimState, IReadOnlySimState
    {
        // History - PriorYears and PriorYear
        public ReadOnlyMemory<SimYear> PriorYears => Storage.Slice(0, _completedYears);
        public SimYear PriorYear => YearIndex > 0 ? PriorYears.Span[^1] : new();
        public Metrics PriorYearMetrics => 0 == YearIndex ? new() : PriorYear.Metrics;

        // About current year
        public int YearIndex { get; private set; } = 0;
        public int Age => StartAge + YearIndex;

        // Current year memory
        public Assets Jan { get; private set; } = Initial;
        public Fees Fees { get; set; }
        public Taxes Taxes { get; set; }
        public Incomes Incomes { get; set; }
        public Expenses Expenses { get; set; }
        public Withdrawals Withdrawals { get; set; }
        public ROI ROI { get; set; }

        public void BeginYear(int yearIndex)
        {
            YearIndex = yearIndex;
            Jan = (yearIndex == 0) ? Initial : PriorYear.Dec;
            (Fees, Taxes, Incomes, Expenses, Withdrawals, ROI) = (default, default, default, default, default, default);
        }

        void ISimState.Rebalance(double preTaxAllocation, double postTaxAllocation, double maxDrift)
        {
            Jan = Jan.Rebalance(preTaxAllocation, postTaxAllocation, maxDrift);
        }

        public void EndYear(SimYear aboutThisYear)
        {
            Storage.Span[_completedYears++] = aboutThisYear;
        }
        
        int _completedYears = 0;

    }

}
