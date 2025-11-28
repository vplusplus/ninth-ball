
namespace NinthBall
{
    // Partial classes to generate readable description of the simulation objectives.

    public partial record PCTWithdrawal
    {
        public override string ToString() => $"{WithdrawPctToString()} {ResetYearsToString()}";

        string WithdrawPctToString()
        {
            return 0.0 == IncrementPct
                ? $"Withdraw {FirstYearPct:P1} each year."
                : $"Withdraw {FirstYearPct:P1} first year with {IncrementPct:P1} increment each year.";
        }

        string ResetYearsToString()
        {
            if (null == ResetYears) return string.Empty;
            if (0 == ResetYears.Count) return string.Empty;
            if (1 == ResetYears.Count) return $"Reset to {FirstYearPct:P1} on year {ResetYears[0]}.";
            if (2 == ResetYears.Count) return $"Reset to {FirstYearPct:P1} on years {ResetYears[0]} and {ResetYears[1]}.";

            var allButOne = ResetYears.Take(ResetYears.Count - 1);
            var last = ResetYears.Last();
            var csvAllButOne = String.Join(',', allButOne);
            return $"Reset to {FirstYearPct:P1} on years {csvAllButOne} and {last}.";
        }
    }

    public partial record PrecalculatedWithdrawal
    {
        public override string ToString() => $"Predefined withdrawal sequence from {Path.GetFileName(FileName)}";
    }

    public partial record UseBufferCash
    {
        public override string ToString() => $"{Amount:C0} buffer cash tapped if prior-year growth < {GrowthThreshold:P1}";
    }

    public partial record ReduceWithdrawal
    {
        public override string ToString() => 1.0 == ReductionPct
            ? $"Skip withdrawal if prior year growth is less than {GrowthThreshold:P1}, only {MaxSkips} times in first {CutOffYear} years."
            : $"Reduce withdrawal by {ReductionPct:P0} if prior year growth is less than {GrowthThreshold:P1}, only {MaxSkips} times in first {CutOffYear} years.";
    }

    public partial record FlatGrowth
    {
        public override string ToString() => $"Assume flat growth. Stocks: {StocksGrowthRate:P1} Bonds: {BondGrowthRate:P1}";
    }

    public partial record HistoricalGrowth
    {
        public override string ToString() => $"{TxtSampling}{TxtSkipConsecutive}{TxtSkip1932}";
        string TxtSkip1932 => Skip1931 ? " *** Skip 1931, the single worst year ***" : string.Empty;
        string TxtSkipConsecutive => UseRandomBlocks && NoConsecutiveBlocks ? " No consecutive repetition." : string.Empty;
        string TxtSampling => UseRandomBlocks
            ? $"Random Blocks ({TxtSeqLengths(BlockSizes)}-years) using historical returns from {Path.GetFileName(FileName)}."
            : $"Sequential historical returns from {Path.GetFileName(FileName)}.";
        
        static string TxtSeqLengths(IReadOnlyList<int> seqLengths) => string.Join("/", seqLengths);
    }

    public partial record Fees
    {
        public override string ToString() => $"Annual fees: {AnnualFeesPct:P1}";
    }
}

