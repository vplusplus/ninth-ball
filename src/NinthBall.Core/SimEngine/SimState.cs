
namespace NinthBall.Core
{
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

    internal sealed record class SimState(int IterationIndex, int StartAge, Assets Initial, Memory<SimYear> Storage) : ISimState, IReadOnlySimState
    {
        // History - PriorYears and PriorYear
        public ReadOnlyMemory<SimYear> PriorYears => Storage.Slice(0, _competedYears);
        public SimYear PriorYear => YearIndex > 0 ? PriorYears.Span[^1] : new();
        public Metrics PriorYearMetrics => 0 == YearIndex ? new() : PriorYear.Metrics;

        // Current year
        public int YearIndex { get; private set; } = 0;
        public int Age => StartAge + YearIndex;

        public Assets Jan { get; private set; } = Initial;
        public Fees Fees { get; set; }
        public Taxes Taxes { get; set; }
        public Incomes Incomes { get; set; }
        public Expenses Expenses { get; set; }
        public Withdrawals Withdrawals { get; set; }
        public ROI ROI { get; set; }

        void ISimState.Rebalance(double preTaxAllocation, double postTaxAllocation, double maxDrift)
        {
            Jan = Jan.Rebalance(preTaxAllocation, postTaxAllocation, maxDrift);
        } 

        public void BeginYear(int yearIndex)
        {
            YearIndex = yearIndex;
            Jan = (yearIndex == 0) ? Initial : PriorYear.Dec;
            (Fees, Incomes, Expenses, Withdrawals, ROI) = (default, default, default, default, default);
        }

        public void EndYear(SimYear aboutThisYear)
        {
            Storage.Span[_competedYears++] = aboutThisYear;
        }
        
        int _competedYears = 0;

    }

}
