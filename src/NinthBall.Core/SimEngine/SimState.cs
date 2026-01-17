
namespace NinthBall.Core
{
    public readonly record struct Allocation(Allocation.AD PreTax, Allocation.AD PostTax)
    {
        public readonly record struct AD(double Allocation, double MaxDrift);
    }

    internal interface ISimState
    {
        //....................................................
        // Current iteration
        //....................................................
        int IterationIndex { get; }
        int StartAge { get; }
        Assets Initial { get; }

        //....................................................
        // History
        //....................................................
        ReadOnlyMemory<SimYear> PriorYears { get; }
        SimYear PriorYear { get; }

        //....................................................
        // Current year
        //....................................................
        int YearIndex { get; }
        int Age { get; }

        Assets Jan { get; }
        Allocation TargetAllocation { get; set; }
        Fees Fees { get; set; }
        Incomes Incomes { get; set; }
        Expenses Expenses { get; set; }
        Withdrawals Withdrawals { get; set; }
        ROI ROI { get; set; }
    }

    internal interface IReadOnlySimState
    {
        int IterationIndex { get; }
        int StartAge { get; }
        int YearIndex { get; }
        int Age { get; }

        Assets Initial { get; }
        ReadOnlyMemory<SimYear> PriorYears { get; }
        SimYear PriorYear { get; }
        Assets Jan { get; }
        Allocation TargetAllocation { get; }
        Fees Fees { get; }
        Incomes Incomes { get; }
        Expenses Expenses { get; }
        Withdrawals Withdrawals { get; }
        ROI ROI { get; }
        Metrics Running { get; }
    }

    internal sealed record class SimState(int IterationIndex, int StartAge, Assets Initial, Memory<SimYear> Storage) : ISimState, IReadOnlySimState
    {
        // History - PriorYears and PriorYear
        public ReadOnlyMemory<SimYear> PriorYears => Storage.Slice(0, _competedYears);
        public SimYear PriorYear => YearIndex > 0 ? PriorYears.Span[^1] : new();


        // Current year
        public int YearIndex { get; private set; } = 0;
        public int Age => StartAge + YearIndex;
        public Assets Jan { get; private set; } = Initial;
        public Allocation TargetAllocation { get; set; }
        public Fees Fees { get; set; }
        public Incomes Incomes { get; set; }
        public Expenses Expenses { get; set; }
        public Withdrawals Withdrawals { get; set; }
        public ROI ROI { get; set; }

        // Running metrics
        public Metrics Running => 0 == YearIndex ? new() : PriorYear.Metrics;

        public void BeginYear(int yearIndex)
        {
            YearIndex = yearIndex;
            Jan = (yearIndex == 0) ? Initial : PriorYear.Dec;
            (TargetAllocation, Fees, Incomes, Expenses, Withdrawals, ROI) = (default, default, default, default, default, default);
        }

        int _competedYears = 0;
        public void EndYear(SimYear aboutThisYear)
        {
            Storage.Span[_competedYears++] = aboutThisYear;
        }
    }

}
