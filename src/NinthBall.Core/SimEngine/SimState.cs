
namespace NinthBall.Core
{
    /// <summary>
    /// Working-memory of one iteration.
    /// </summary>
    internal sealed record class SimState(int IterationIndex, int StartAge, Assets Initial, Memory<SimYear> Storage) : ISimState, IReadOnlySimState
    {
        // History - PriorYears and PriorYear
        public ReadOnlyMemory<SimYear> PriorYears => Storage.Slice(0, _completedYears);
        public SimYear PriorYear        => YearIndex > 0 ? PriorYears.Span[^1] : new();
        public Metrics PriorYearMetrics => YearIndex > 0 ? PriorYear.Metrics   : new();

        // About current year
        public int YearIndex { get; private set; } = 0;
        public int Age => StartAge + YearIndex;

        // Current year memory
        public Assets Jan { get; private set; } = Initial;
        public Rebalanced Rebalanced { get; set; }
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
