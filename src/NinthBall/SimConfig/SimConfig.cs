
using System.ComponentModel.DataAnnotations;

namespace NinthBall
{
    /// <summary>
    /// Immutable representation of simulation configurations, learnt from yaml config files.
    /// </summary>
    public partial record SimConfig
    (   
        string RandomSeedHint,

        Asset InitialFourK,
        Asset InitialInv,
        Asset InitialSav,

        [property: Range(1, 50)]
        int NoOfYears,

        [property: Range(1, 100000)]
        int Iterations,

        [property: Required()]
        string Output,

        [property: ValidateNested()] PCTWithdrawal PCTWithdrawal,
        [property: ValidateNested()] PrecalculatedWithdrawal PrecalculatedWithdrawal,
        [property: ValidateNested()] UseBufferCash UseBufferCash,
        [property: ValidateNested()] ReduceWithdrawal ReduceWithdrawal,
        [property: ValidateNested()] FlatGrowth FlatGrowth,
        [property: ValidateNested()] HistoricalGrowth HistoricalGrowth,
        [property: ValidateNested()] Fees Fees
    )
    {
        public int SessionSeed => (RandomSeedHint ?? "JSR").GetPredictableHashCode();
    }

    //..........................................................................
    // Withdrawal configurations
    //..........................................................................
    
    public partial record PCTWithdrawal
    (
        [property: Range(0.0, 1.0)]
        double FirstYearPct,

        [property: Range(0.0, 1.0)]
        double IncrementPct, 
        
        IReadOnlyList<int> ResetYears
    )
    {
        public override string ToString() => $"{WithdrawPctToString} {ResetYearsToString}";
        string WithdrawPctToString => $"Withdraw {FirstYearPct:P1} first year with {IncrementPct:P1} increment each year.";
        string ResetYearsToString => (null == ResetYears || 0 == ResetYears.Count) ? string.Empty : $"Reset to {FirstYearPct:P1} on years [{string.Join(',', ResetYears)}]";
    }

    public partial record PrecalculatedWithdrawal
    (
        [property: Required()]
        [property: FileExists()]
        string FileName,

        [property: Required()]
        string SheetName
    )
    {
        IReadOnlyList<double> __withdrawalSequence = null!;

        public IReadOnlyList<double> WithdrawalSequence => __withdrawalSequence ??= 
            ReadPrecalculatedWithdrawals(FileName, SheetName);

        static IReadOnlyList<double> ReadPrecalculatedWithdrawals(string xlFileName, string sheetName)
        {
            var withdrawalSequence = new List<double>();

            using (var xlReader = new ExcelReader(xlFileName))
            {
                var sheet = xlReader.GetSheets().Where(s => sheetName.Equals(s.SheetName, StringComparison.OrdinalIgnoreCase)).SingleOrDefault()
                    ?? throw new FatalWarning($"Sheet not found | File: {Path.GetFileName(xlFileName)} | Sheet: '{sheetName}'");

                foreach (var row in sheet.GetRows())
                {
                    if (null == row) continue;

                    // Skip first (header) row.  Do not use IEnumerable.Skip(1) option; Use Rowindex.
                    var isFirstRow = null != row.RowIndex && 1 == row.RowIndex.Value;
                    if (isFirstRow) continue;

                    var cells = row.GetCellValues().ToArray();

                    if (
                        null != cells
                        && cells.Length >= 1
                        && double.TryParse(cells[0], out var amount)
                    )
                    {
                        withdrawalSequence.Add(amount);
                    }
                }
            }

            return withdrawalSequence.AsReadOnly();
        }

        public override string ToString() => $"Predefined withdrawal sequence from {Path.GetFileName(FileName)}";
    }

    //..........................................................................
    // Withdrawal optimization configurations
    //..........................................................................
    public partial record UseBufferCash
    (
        [property: Range(1, 100000)]
        double Amount,

        [property: Range(1, 100000)]
        double GrowthThreshold
    )
    {
        public override string ToString() => $"{Amount:C0} buffer cash tapped if prior-year growth < {GrowthThreshold:P1}";
    }

    public partial record ReduceWithdrawal
    (
        [property: Range(1, 30)]
        int MaxSkips,

        [property: Range(1, 30)]
        int CutOffYear, 

        [property: Range(-0.5, 0.5)] 
        double GrowthThreshold, 
        
        [property: Range(0.0, 1.0)] 
        double ReductionPct
    )
    {
        public override string ToString() => 1.0 == ReductionPct
            ? $"Skip withdrawal if prior year growth is less than {GrowthThreshold:P1}, only {MaxSkips} times in first {CutOffYear} years."
            : $"Reduce withdrawal by {ReductionPct:P0} if prior year growth is less than {GrowthThreshold:P1}, only {MaxSkips} times in first {CutOffYear} years.";
    }

    //..........................................................................
    // Growth and ROI configurations
    //..........................................................................
    public partial record FlatGrowth
    (
        [property: Range(-1.0, 1.0)]
        double StocksGrowthRate, 
        
        [property: Range(-1.0, 1.0)] 
        double BondGrowthRate
    )
    {
        public override string ToString() => $"Assume flat growth. Stocks: {StocksGrowthRate:P1} Bonds: {BondGrowthRate:P1}";
    }

    public partial record HistoricalGrowth
    (
        [property: Required]
        [property: FileExists]
        string FileName,

        [property : Required]
        string SheetName, 

        bool UseRandomBlocks,

        IReadOnlyList<int> BlockSizes, 

        bool NoConsecutiveBlocks, 

        bool Skip1931
    )
    {
        public IReadOnlyList<int> BlockSizes { get; init; } = BlockSizes ?? [3, 4, 5];

        IReadOnlyList<YROI> __allYears = null!;

        IReadOnlyList<Block> __alBlocks = null!;

        public IReadOnlyList<YROI> AllYears => __allYears ??= Bootstrap.ReadHistory(FileName, SheetName, Skip1931);
        public IReadOnlyList<Block> AllBlocks => __alBlocks ??= AllYears.ReadBlocks(BlockSizes);

        public override string ToString() => $"{TxtBootstrap}{TxtSkipConsecutive}{TxtSkip1932}";

        string TxtBootstrap => UseRandomBlocks
            ? $"Random Blocks ({string.Join("/", BlockSizes)}-years) using historical returns from {Path.GetFileName(FileName)}."
            : $"Sequential historical returns from {Path.GetFileName(FileName)}.";
        string TxtSkipConsecutive => UseRandomBlocks && NoConsecutiveBlocks ? " No consecutive repetition." : string.Empty;
        string TxtSkip1932 => Skip1931 ? " *** Skip 1931, the single worst year ***" : string.Empty;

    }

    //..........................................................................
    // Fees
    //..........................................................................
    public partial record Fees
    (
        [property : Range(0.0, 1.0)]
        double Fees401K = 0.0,

        [property : Range(0.0, 1.0)]
        double FeesInv = 0.0
    )
    {
        public override string ToString() => $"Annual fees: 401K {Fees401K:P1} Inv: {FeesInv:P1}";
    }
}
