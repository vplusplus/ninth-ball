
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

    /// <summary>
    /// Optional configuration for MBB internals
    /// </summary>
    public sealed record MovingBlockBootstrapOptions
    (
        [property: Required] IReadOnlyList<int> BlockSizes,
        [property: Required] bool NoBackToBackOverlaps
    );

    /// <summary>
    /// Replays random blocks of historical returns and inflation.
    /// </summary>
    internal sealed class MovingBlockBootstrapper(SimulationSeed SimSeed, HistoricalReturns History, HistoricalBlocks HBlocks, MovingBlockBootstrapOptions Options) : IBootstrapper
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

            var allBlocks  = HBlocks.Blocks;
            var thresholds = LazyDisasterAndJackpotThresholds.Value;

            // We are collecting random indexes on available blocks.
            while (idx < numYears)
            {
                // Sample next random block with uniform distribution (with replacement).
                var nextBlock = allBlocks[iterRand.Next(0, allBlocks.Count)];
                Interlocked.Increment(ref MBBStats.Samples);

                // Did we pick overlapping blocks?
                if (Options.NoBackToBackOverlaps && null != prevBlock && HBlock.Overlaps(prevBlock.Value, nextBlock))
                {
                    // Yes, we picked an overlapping block.
                    Interlocked.Increment(ref MBBStats.Overlaps);

                    // We interfere ONLY if back-to-back overlapping blocks exceed luck-threshold (good or bad)
                    bool backToBackDisaster = prevBlock.Value.ARRScore <= thresholds.DisasterScore && nextBlock.ARRScore <= thresholds.DisasterScore;
                    bool backToBackJackpot  = prevBlock.Value.ARRScore >= thresholds.JackpotScore  && nextBlock.ARRScore >= thresholds.JackpotScore;

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
        #region DisasterAndJackpotThresholds - Discovered once
        //......................................................................
        readonly record struct DisasterAndJackpotThresholds
        (
            double DisasterScore,   // Blocks ARRScore same or below represents worst performing window
            double JackpotScore     // Blocks ARRScore same or above represents best performing window
        );

        // Prepare all available blocks once.
        readonly Lazy<DisasterAndJackpotThresholds> LazyDisasterAndJackpotThresholds = new(() => 
        {
            const double TenthPctl     = 0.1;
            const double NinetiethPctl = 0.9;

            // Read ARRScore of all blocks, sort them worst to best.
            var sortedScores = HBlocks.Blocks.Select(b => b.ARRScore).OrderBy(s => s).ToArray();

            // Determine indices of disaster and jackpot percentiles.
            // 10th percentile = index at 10% mark
            // 90th percentile = index at 90% mark
            int disasterIdx = (int)(sortedScores.Length  * TenthPctl);
            int jackpotIdx  = (int)(sortedScores.Length  * NinetiethPctl);

            // Clip the edges
            disasterIdx = Math.Clamp(disasterIdx, 0, sortedScores.Length - 1);
            jackpotIdx  = Math.Clamp(jackpotIdx,  0,  sortedScores.Length - 1);

            // Return the ARRScore at the disaster and jackpot percentiles.
            return new (
                DisasterScore: sortedScores[disasterIdx],
                JackpotScore:  sortedScores[jackpotIdx]
            );
        });

        #endregion

        // Describe...
        public override string ToString() => $"Random historical growth and inflation using {CSVBlockSizes} year blocks from {History.MinYear} to {History.MaxYear} data{TxtNoBackToBack}";
        string CSVBlockSizes => string.Join("/", Options.BlockSizes);
        string TxtNoBackToBack => Options.NoBackToBackOverlaps ? " (Avoids back-to-back repetition of extreme outcomes)" : string.Empty;

    }
}

/*
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
*/
