
namespace NinthBall
{
    /// <summary>
    /// Immutable representation of simulation configurations, learnt from yaml config files.
    /// </summary>
    public partial record SimConfig
    (   
        string RandomSeedHint, double StartingBalance, double StocksAllocationPct, double MaxDrift, int NoOfYears, int Iterations, string Output,
        PCTWithdrawal PCTWithdrawal, 
        PrecalculatedWithdrawal PrecalculatedWithdrawal,
        UseBufferCash UseBufferCash, 
        ReduceWithdrawal ReduceWithdrawal,
        FlatGrowth FlatGrowth, 
        HistoricalGrowth HistoricalGrowth,
        Fees Fees
    )
    {
        public int SessionSeed => (RandomSeedHint ?? "JSR").GetPredictableHashCode();
    }

    //..........................................................................
    // Withdrawal configurations
    //..........................................................................
    
    public partial record PCTWithdrawal(double FirstYearPct, double IncrementPct, IReadOnlyList<int> ResetYears);

    public partial record PrecalculatedWithdrawal(string FileName, string SheetName)
    {
        IReadOnlyList<double> __withdrawalSequence = null!;

        public IReadOnlyList<double> WithdrawalSequence => ReadPrecalculatedWithdrawals(FileName, SheetName);

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
    }

    //..........................................................................
    // Withdrawal optimization configurations
    //..........................................................................
    public partial record UseBufferCash(double Amount, double GrowthThreshold);

    public partial record ReduceWithdrawal(int MaxSkips, int CutOffYear, double GrowthThreshold, double ReductionPct);

    //..........................................................................
    // Growth and ROI configurations
    //..........................................................................

    public partial record FlatGrowth(double StocksGrowthRate, double BondGrowthRate);

    public partial record HistoricalGrowth(string FileName, string SheetName, bool UseRandomBlocks, IReadOnlyList<int> BlockSizes, bool NoConsecutiveBlocks, bool Skip1931)
    {
        public IReadOnlyList<int> BlockSizes { get; init; } = BlockSizes ?? [3, 4, 5];

        IReadOnlyList<YROI> __allYears = null!;

        IReadOnlyList<Block> __alBlocks = null!;

        public IReadOnlyList<YROI> AllYears => __allYears ??= Bootstrap.ReadHistory(FileName, SheetName, Skip1931);
        public IReadOnlyList<Block> AllBlocks => __alBlocks ??= AllYears.ReadBlocks(BlockSizes);
    }

    //..........................................................................
    // Fees
    //..........................................................................
    public partial record Fees(double AnnualFeesPct);

}
