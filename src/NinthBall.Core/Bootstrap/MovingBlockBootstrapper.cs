
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
    internal sealed class MovingBlockBootstrapper(SimulationSeed SimSeed, HistoricalBlocks HBlocks, MovingBlockBootstrapOptions Options) : IBootstrapper
    {
        // We can produce theoretically unlimited possible combinations.
        int IBootstrapper.GetMaxIterations(int numYears) => int.MaxValue;

        // Random blocks of history (with replacement)
        IROISequence IBootstrapper.GetROISequence(int iterationIndex, int numYears)
        {
            var iterRand = new Random(PredictableHashCode.Combine(SimSeed.Value, iterationIndex));
            var sequence = new HROI[numYears];
            var idx = 0;

            HBlock? prevBlock = null!;

            var allBlocks  = HBlocks.Blocks;
            var thresholds = LazyDisasterAndJackpotThresholds.Value;

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

                // Collect HROI from the sampled block.
                for (int j = 0; j < nextBlock.Slice.Length && idx < numYears; j++, idx++) sequence[idx] = nextBlock.Slice.Span[j];
            }

            // Logic check...
            if (idx != numYears) throw new Exception("Internal error | Mismatch in expected number of years collected.");

            // Hand it over to ROISequence, it will honor the IROISequence contract.
            return new ROISequence(sequence.AsMemory());
        
        }

        private readonly record struct ROISequence(ReadOnlyMemory<HROI> MemoryBlock) : IROISequence
        {
            readonly HROI IROISequence.this[int yearIndex] => MemoryBlock.Span[yearIndex];
        }

        //......................................................................
        #region DisasterAndJackpotThresholds - Discovered once
        //......................................................................

        /// <summary>
        /// Each block carries a score that represents performance during that window.
        /// It may look and feel like 'real annualized return', but it is not.
        /// Other than ranking the blocks, the number doesn't serve any other purpose.
        /// </summary>
        readonly record struct DisasterAndJackpotThresholds
        (
            double DisasterScore,   // Blocks with ARRScore <= DisasterScore represents worst performing windows.
            double JackpotScore     // Blocks with ARRScore >= JackpotScore represents best performing windows.
        );

        // Prepare disaster and jackpot ARRScore thresholds once.
        readonly Lazy<DisasterAndJackpotThresholds> LazyDisasterAndJackpotThresholds = new(() => 
        {
            const double TenthPctl     = 0.1;
            const double NinetiethPctl = 0.9;

            // Read ARRScore of all blocks, sort them worst to best.
            var sortedScores = HBlocks.Blocks.Select(b => b.ARRScore).OrderBy(s => s).ToArray();

            // Determine indices of disaster and jackpot percentiles.
            // 10th percentile = index at 10% mark
            // 90th percentile = index at 90% mark
            int disasterIdx = (int)(sortedScores.Length * TenthPctl);
            int jackpotIdx  = (int)(sortedScores.Length * NinetiethPctl);

            // Clip the edges
            disasterIdx = Math.Clamp(disasterIdx, 0, sortedScores.Length - 1);
            jackpotIdx  = Math.Clamp(jackpotIdx,  0, sortedScores.Length - 1);

            // Return the ARRScore(s) at the disaster and jackpot percentiles.
            return new (
                DisasterScore: sortedScores[disasterIdx],
                JackpotScore:  sortedScores[jackpotIdx]
            );
        });

        #endregion

        // Describe...
        public override string ToString() => $"Random historical growth and inflation using {CSVBlockSizes} year blocks from {HBlocks.MinYear} to {HBlocks.MaxYear} data {TxtNoBackToBack}";
        string CSVBlockSizes => string.Join("/", Options.BlockSizes);
        string TxtNoBackToBack => Options.NoBackToBackOverlaps ? "(Avoids back-to-back repetition of extreme outcomes)" : string.Empty;

    }
}
