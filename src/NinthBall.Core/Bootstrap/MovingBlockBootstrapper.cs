
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace NinthBall.Core
{
    public static partial class MBBStats
    {
        public static int Samples   = 0;
        public static int Overlaps  = 0;
        public static int Resamples = 0;
        public static void Reset() => Samples = Overlaps = Resamples = 0;
    }

    public sealed record MovingBlockBootstrapOptions
    (
        [property: Required] IReadOnlyList<int> BlockSizes,
        [property: Required] bool NoConsecutiveBlocks
    );

    /// <summary>
    /// Replays random blocks (with replacement) of historical returns and inflation.
    /// </summary>
    internal sealed class MovingBlockBootstrapper(SimulationSeed SimSeed, HistoricalReturns History, MovingBlockBootstrapOptions Options) : IBootstrapper
    {
        // We can produce theoretically unlimited possible combinations.
        int IBootstrapper.GetMaxIterations(int numYears) => int.MaxValue;

        // Random blocks of history (with replacement)
        IROISequence IBootstrapper.GetROISequence(int iterationIndex, int numYears)
        {
            var iterRand = new Random(PredictableHashCode.Combine(SimSeed.Value, iterationIndex));
            var indices = new int[numYears];
            var idx = 0;

            HBlock? prevBlock = null!;

            var allBlocks  = LazyBlocks.Value.Blocks;
            var thresholds = LazyBlocks.Value.Score;

            // We are collecting random indexes on available blocks.
            while (idx < numYears)
            {
                // Sample next random block with uniform distribution (with replacement).
                var nextBlock = allBlocks[iterRand.Next(0, allBlocks.Count)];
                Interlocked.Increment(ref MBBStats.Samples);

                // Did we pick overlapping blocks?
                if (Options.NoConsecutiveBlocks && null != prevBlock && HBlock.Overlaps(prevBlock.Value, nextBlock))
                {
                    // Yes, we picked a overlapping block.
                    Interlocked.Increment(ref MBBStats.Overlaps);

                    // We interfere ONLY if back-to-back overlapping blocks exceed luck-threshold (good or bad)
                    bool backToBackDisaster = prevBlock.Value.ARRScore <= thresholds.Disaster && nextBlock.ARRScore <= thresholds.Disaster;
                    bool backToBackJackpot  = prevBlock.Value.ARRScore >= thresholds.Jackpot  && nextBlock.ARRScore >= thresholds.Jackpot;

                    if (backToBackDisaster || backToBackJackpot)
                    {
                        Interlocked.Increment(ref MBBStats.Resamples);
                        continue;
                    }
                }

                // Remember previous block.
                prevBlock = nextBlock;

                // Collect indices from the sampled block.
                for (int j = 0; j < nextBlock.Length && idx < numYears; j++, idx++) indices[idx] = nextBlock.StartIndex + j;
            }

            // Logic check...
            if (idx != numYears) throw new Exception("Internal error | Mismatch in expected number of years collected.");

            // We prepared random indices into blocks of historical data. 
            // Return an indexed-view into historical data.
            return new ROISequence(History.History, indices);
        
        }

        private readonly record struct ROISequence(ReadOnlyMemory<HROI> MemoryBlock, int[] Indices) : IROISequence
        {
            readonly HROI IROISequence.this[int yearIndex] => MemoryBlock.Span[Indices[yearIndex]];
        }

        //......................................................................
        #region HBlock and AllBlocks
        //......................................................................

        // Represents a small window into the historical returns.
        // HBlock(s) are nothing more than an index (and length) into a block-of-memory.
        // ARRScore is a ranking-score, NOT to be to measure portfolio performance.
        readonly record struct HBlock(int StartIndex, int Length, double ARRScore)
        {
            private readonly int EndIndex => StartIndex + Length - 1;
            public static bool Overlaps(HBlock prevBlock, HBlock nextBlock) => nextBlock.StartIndex <= prevBlock.EndIndex && nextBlock.EndIndex >= prevBlock.StartIndex;
        }

        readonly record struct ARRScores(double Disaster, double Jackpot);

        readonly record struct BlocksAndScores(ReadOnlyCollection<HBlock> Blocks, ARRScores Score);

        // Prepare all available blocks once.
        readonly Lazy<BlocksAndScores> LazyBlocks = new(() => 
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

            // Discover the score of worst and best performing blocks.
            const double TenthPctl = 0.1;
            const double NinetiethPctl = 0.9;
            var arrscores = DiscoverDisasterAndJackpotScores(availableBlocks, disasterPctl: TenthPctl, jackpotPctl: NinetiethPctl);

            // Immutable...
            return new BlocksAndScores
            (
                availableBlocks.AsReadOnly(),
                arrscores
            );
        });

        // Computes a 'ranking-score' for the blocks.
        // It may look and feel like Real annualized return, but it is not.
        // Other than ranking the blocks, the number doesn't serve any other purpose.
        private static double CalculateARRScore(HistoricalReturns History, int startIndex, int blockLength)
        {
            // Opinionated portfolio split for ranking purposes
            const double SixtyPct = 0.6;
            const double FourtyPct = 0.4;

            double compoundedRealMultiplier = 1.0;

            for (int i = startIndex; i < startIndex + blockLength; i++)
            {
                var hroi = History.History.Span[i];

                // Calculate nominal multiplier for the 60/40 portfolio
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

        static ARRScores DiscoverDisasterAndJackpotScores(IReadOnlyList<HBlock> allBlocks, double disasterPctl, double jackpotPctl)
        {
            // Sort the ARRScores of all blocks, worst to best.
            var sortedScores = allBlocks.Select(b => b.ARRScore).OrderBy(s => s).ToArray();

            // Determine indices (simple nearest-rank approach)
            // 10th percentile = index at 10% mark
            // 90th percentile = index at 90% mark
            int disasterIdx = (int)(sortedScores.Length * disasterPctl);
            int jackpotIdx  = (int)(sortedScores.Length * jackpotPctl);

            // Defensive clamps. Below will not happen.
            disasterIdx = Math.Clamp(disasterIdx, 0, sortedScores.Length - 1);
            jackpotIdx  = Math.Clamp(jackpotIdx, 0, sortedScores.Length - 1);

            return new ARRScores(
                Disaster: sortedScores[disasterIdx],
                Jackpot:  sortedScores[jackpotIdx]
            );
        } 

        #endregion

        // Describe...
        public override string ToString() => $"Moving Block Bootstrap (MBB) using random blocks [{CSVBlockSizes}] from {History.MinYear} to {History.MaxYear} data.{TxtNoConsecutiveBlocks}";
        string CSVBlockSizes => string.Join(",", Options.BlockSizes);
        string TxtNoConsecutiveBlocks => Options.NoConsecutiveBlocks ? " (No back to back repetition)" : string.Empty;

    }
}
