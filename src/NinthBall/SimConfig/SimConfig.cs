
using System.ComponentModel.DataAnnotations;

namespace NinthBall
{
    /// <summary>
    /// Immutable representation of simulation configurations, learnt from yaml config files.
    /// </summary>
    public partial record SimConfig
    (   
        string RandomSeedHint,

        [property: Min(10)]
        double InitialBalance,

        [property: Range(0.0, 1.0)]
        double StockAllocation,

        [property: Range(0.0, 1.0)]
        double MaxDrift,

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
    );

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
    );

    public partial record ReduceWithdrawal
    (
        [property: Range(1, 30)]
        int MaxSkips,

        [property: Range(1, 30)]
        int CutOffYear, 

        [property: Range(-0.5, 0.5)] 
        double GrowthThreshold, 
        
        [property: Range(0.0, 0.5)] 
        double ReductionPct
    );

    //..........................................................................
    // Growth and ROI configurations
    //..........................................................................
    public partial record FlatGrowth
    (
        [property: Range(-1.0, 1.0)]
        double StocksGrowthRate, 
        
        [property: Range(-1.0, 1.0)] 
        double BondGrowthRate
    );

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
    }

    //..........................................................................
    // Fees
    //..........................................................................
    public partial record Fees
    (
        [property : Range(0.0, 1.0)]
        double AnnualFeesPct
    );
}
