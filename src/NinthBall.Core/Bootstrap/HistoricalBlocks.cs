
using System.Collections.ObjectModel;

namespace NinthBall.Core
{
    // Represents a small window into the historical returns.
    // HBlock(s) are nothing more than an index (and length) into a block-of-memory.
    // ARRScore is a ranking-score, NOT to be to measure portfolio performance.
    readonly record struct HBlock(int StartIndex, int Length, double ARRScore)
    {
        private readonly int EndIndex => StartIndex + Length - 1;
        public static bool Overlaps(HBlock prevBlock, HBlock nextBlock) => nextBlock.StartIndex <= prevBlock.EndIndex && nextBlock.EndIndex >= prevBlock.StartIndex;
    }

    // Prepare all available blocks once.
    // Note: Blocks are indexes into history. We do not copy the data.
    internal sealed class HistoricalBlocks(HistoricalReturns History, MovingBlockBootstrapOptions Options)
    {
        public IReadOnlyList<HBlock> Blocks => LazyBlocks.Value;

        // Prepare all available blocks once.
        readonly Lazy<ReadOnlyCollection<HBlock>> LazyBlocks = new(() =>
        {
            // All input data are pre-validated elsewhere.
            // No additional validations needed here.
            var availableYears = History.History.Length;
            var blkSizes = Options.BlockSizes;

            // Prepare overlapping blocks of suggested sequence lengths.
            List<HBlock> availableBlocks = [];

            foreach (var blockLength in blkSizes)
            {
                var maxBlocks = availableYears - blockLength + 1;
                for (int startIndex = 0; startIndex < maxBlocks; startIndex++)
                {
                    double ARRScore = CalculateARRScore(History, startIndex, blockLength);
                    availableBlocks.Add(new HBlock(startIndex, blockLength, ARRScore));
                }
            }

            // The growth strategy uses uniform sampling; therefore, ordering does not affect the outcome.
            // Sorting is performed solely to ensure repeatability across runs.
            // Historical data is already sorted by year.
            // Blocks are arranged chronologically, then by sequence length.
            availableBlocks = availableBlocks.OrderBy(x => x.StartIndex).ThenBy(x => x.Length).ToList();

            return availableBlocks.AsReadOnly();
        });

        // Computes a 'ranking-score' for the blocks.
        // It may look and feel like Real annualized return, but it is not.
        // Other than ranking the blocks, the number doesn't serve any other purpose.
        static double CalculateARRScore(HistoricalReturns History, int startIndex, int blockLength)
        {
            // Opinionated portfolio split for ranking purposes
            const double SixtyPct = 0.6;
            const double FourtyPct = 0.4;

            double compoundedRealMultiplier = 1.0;

            for (int i = startIndex; i < startIndex + blockLength; i++)
            {
                // Lookup the ROI and Inflation information
                var hroi = History.History.Span[i];

                // Calculate nominal multiplier for an imaginary 60/40 portfolio
                // Adjust for inflation (Exact math: (1+n)/(1+i))
                double nominalMultiplier = 1.0 + (hroi.StocksROI * SixtyPct + hroi.BondsROI * FourtyPct);
                double realMultiplier = nominalMultiplier / (1.0 + hroi.InflationRate);

                // Accumulate compounding effect
                compoundedRealMultiplier *= realMultiplier;
            }

            // Annualize the compounded result (Geometric Mean)
            // This allows ranking a 3-year "good" block against a 5-year "excellent" block fairly.
            return Math.Pow(compoundedRealMultiplier, 1.0 / blockLength) - 1.0;
        }
    }
}
