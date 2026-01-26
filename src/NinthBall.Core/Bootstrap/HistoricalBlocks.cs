
using System.Collections.ObjectModel;

namespace NinthBall.Core
{
    // Represents a small window into the historical returns.
    // ARRScore is a ranking-score, an 'indicator' of good/bad windows.
    readonly record struct HBlock(ReadOnlyMemory<HROI> Slice, double ARRScore)
    {
        public readonly int StartYear => Slice.Span[0].Year;
        public readonly int EndYear   => Slice.Span[^1].Year;
        public static bool Overlaps(HBlock prevBlock, HBlock nextBlock) => nextBlock.StartYear <= prevBlock.EndYear && nextBlock.EndYear >= prevBlock.StartYear;
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
                    var slice = history.Slice(startIndex, blockLength);
                    var score = CalculateARRScore(slice);
                    availableBlocks.Add(new HBlock(slice, score));
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

        // Computes a 'ranking-score' for the blocks.
        // It may look and feel like Real annualized return, but it is not.
        // Other than ranking the blocks, the number doesn't serve any other purpose.
        static double CalculateARRScore(ReadOnlyMemory<HROI> window)
        {
            // Opinionated portfolio split for ranking purposes
            const double SixtyPct = 0.6;
            const double FourtyPct = 0.4;

            double compoundedRealMultiplier = 1.0;

            for (int i = 0; i < window.Length; i++)
            {
                // Lookup the ROI and Inflation information
                var hroi = window.Span[i];

                // Calculate nominal multiplier for an imaginary 60/40 portfolio
                // Adjust for inflation (Exact math: (1+n)/(1+i))
                double nominalMultiplier = 1.0 + (hroi.StocksROI * SixtyPct + hroi.BondsROI * FourtyPct);
                double realMultiplier = nominalMultiplier / (1.0 + hroi.InflationRate);

                // Accumulate compounding effect
                compoundedRealMultiplier *= realMultiplier;
            }

            // Annualize the compounded result (Geometric Mean)
            // This allows ranking a 3-year "good" block against a 5-year "excellent" block fairly.
            return Math.Pow(compoundedRealMultiplier, 1.0 / window.Length) - 1.0;
        }
    }
}
