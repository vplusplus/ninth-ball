 
namespace NinthBall.Core
{
    // Represents a small window into the historical returns.
    // ARRScore is a ranking-score, an 'indicator' of good/bad windows.
    readonly record struct HBlock(ReadOnlyMemory<HROI> Slice, HBlock.F Features)
    {
        // Features of this block
        public readonly record struct F
        (
            double CAGRStocks,              // TODO: Why Nominal, why not both nomimal and real
            double CAGRBonds,               // TODO: Why Nominal, why not both nomimal and real
            double MaxDrawdownStocks,
            double MaxDrawdownBonds,
            double GMeanInflationRate,
            double RealCAGR6040
        );

        public readonly int StartYear => Slice.Span[0].Year;
        public readonly int EndYear   => Slice.Span[^1].Year;
        public static bool Overlaps(in HBlock prevBlock, in HBlock nextBlock) => nextBlock.StartYear <= prevBlock.EndYear && nextBlock.EndYear >= prevBlock.StartYear;
    }

    internal sealed class HistoricalBlocks(HistoricalReturns History, MovingBlockBootstrapOptions Options)
    {
        public IReadOnlyList<HBlock> Blocks => LazyBlocks.Value.Blocks;
        public int MinYear => LazyBlocks.Value.MinYear;
        public int MaxYear => LazyBlocks.Value.MaxYear;

        // Prepare all available blocks once.
        readonly Lazy<(IReadOnlyList<HBlock> Blocks, int MinYear, int MaxYear)> LazyBlocks = new(() =>
        {
            // Here we are not trusting that history is already sorted by year (which is indeed the case).
            // We depend on chronology, hence we sort the history by year (zero-trust).
            // This may be redundant second copy, but no pinky-promises.
            // Also capture the min and max years in the history.
            var sortedHistory = History.History.ToArray().OrderBy(x => x.Year).ToArray();
            var fromYear = sortedHistory.Min(x => x.Year);
            var toYear   = sortedHistory.Max(x => x.Year);

            var history = sortedHistory.AsMemory();
            var availableYears = History.History.Length;
            var blkSizes = Options.BlockSizes;

            // Prepare overlapping blocks of suggested sequence lengths.
            List<HBlock> availableBlocks = [];

            foreach (var blockLength in blkSizes)
            {
                var maxBlocks = availableYears - blockLength + 1;
                for (int startIndex = 0; startIndex < maxBlocks; startIndex++)
                {
                    ReadOnlyMemory<HROI> slice = history.Slice(startIndex, blockLength);

                    var features = slice.ComputeBlockFeatures();
                    //var score = CalculateARRScore(slice);
                    availableBlocks.Add(new HBlock(slice, features));
                }
            }

            // The growth strategy uses uniform sampling; therefore, ordering does not affect the outcome.
            // Sorting is performed solely to ensure repeatability across runs.
            // We just now sorted the history by year.
            // The blocks are arranged chronologically, then by sequence length.
            availableBlocks = availableBlocks.OrderBy(x => x.StartYear).ThenBy(x => x.Slice.Length).ToList();

            return
            (
                Blocks:  availableBlocks.AsReadOnly(),
                MinYear: fromYear,
                MaxYear: toYear
            );
        });

    }
}
