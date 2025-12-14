
using System.ComponentModel.DataAnnotations;
using System.Transactions;

namespace NinthBall
{
    // TODO: Remove need for ValidateNested. 
    // All config structures should be validated nested
    // Or, switch to IConfiguration. ISimObjevtives are responsible for validations.

    /// <summary>
    /// Immutable representation of simulation configurations, learnt from yaml config files.
    /// </summary>
    public partial record SimConfig
    (   
        string RandomSeedHint,

        [property: Range(1, 50)]
        int NoOfYears,

        [property: Range(1, 50_000)]
        int Iterations,

        [property: Required()]
        string Output,

        [property: ValidateNested()] InitPortfolio InitPortfolio,
        [property: ValidateNested()] YearlyRebalance YearlyRebalance,
        [property: ValidateNested()] AdditionalIncomes AdditionalIncomes,
        [property: ValidateNested()] LivingExpenses LivingExpenses,
        [property: ValidateNested()] PrecalculatedLivingExpenses PrecalculatedLivingExpenses,
        [property: ValidateNested()] Taxes Taxes,
        [property: ValidateNested()] PreTaxWithdrawal PreTaxWithdrawal,
        [property: ValidateNested()] UseBufferCash UseBufferCash,
        [property: ValidateNested()] ReduceWithdrawal ReduceWithdrawal,
        [property: ValidateNested()] FlatGrowth FlatGrowth,
        [property: ValidateNested()] HistoricalGrowth HistoricalGrowth,
        [property: ValidateNested()] FeesPCT Fees
    )
    {
        public int SessionSeed => (RandomSeedHint ?? "JSR").GetPredictableHashCode();
    }


    //..........................................................................
    // Inital portfiolio balances
    //..........................................................................
    public readonly record struct InitBalance
    (
        [property: Min(0.0)]
        double Amount,

        [property: Range(0.0, 1.0)]
        double Allocation
    );

    public readonly record struct InitPortfolio(InitBalance PreTax, InitBalance PostTax, InitBalance Cash);

    //..........................................................................
    // Rebalancing objective
    //..........................................................................
    public readonly record struct YearlyRebalance
    (
        [property: Range(0.0, 1.0)]
        double MaxDrift
    );


    //..........................................................................
    // Additional known incomes
    //..........................................................................
    public readonly record struct AdditionalIncome
    (
        [property: Min(0)]
        double Amount,

        [property: Range(1, 50)]
        int FromYear,

        [property: Range(0.0, 0.1)]
        double YearlyIncrement
    );

    public readonly record struct AdditionalIncomes(AdditionalIncome SS, AdditionalIncome Ann);


    //..........................................................................
    // Tax model
    //..........................................................................
    public readonly record struct Taxes
    (
        [property: Min(0)]
        double YearZeroTaxAmount,

        [property: Range(0.0, 0.5)]
        double OrdinaryIncomeTaxRate,

        [property: Range(0.0, 0.5)]
        double CapitalGainsTaxRate
    );


    //..........................................................................
    // Expense model
    //..........................................................................
    public sealed record LivingExpenses
    (
        [property: Min(0)]
        double FirstYearAmount,

        [property: Range(0.0, 1.0)]
        double IncrementPct
    );


    public sealed record PrecalculatedLivingExpenses
    (
        [property: Required()]
        [property: FileExists()]
        string FileName,

        [property: Required()]
        string SheetName
    )
    {
        IReadOnlyList<double> __livingExpensesSequence = null!;

        public IReadOnlyList<double> LivingExpensesSequence => __livingExpensesSequence ??= ReadPrecalculatedLivingExpenses(FileName, SheetName);

        static IReadOnlyList<double> ReadPrecalculatedLivingExpenses(string xlFileName, string sheetName)
        {
            var sequence = new List<double>();

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
                        sequence.Add(amount);
                    }
                }
            }

            return sequence.AsReadOnly();
        }
    }

    //..........................................................................
    // Withdrawal configurations
    //..........................................................................
    public partial record PreTaxWithdrawal
    (
        [property: Range(0.0, 1.0)]
        double FirstYearPct,

        [property: Range(0.0, 1.0)]
        double IncrementPct, 
        
        IReadOnlyList<int> ResetYears
    )
    {
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
}
