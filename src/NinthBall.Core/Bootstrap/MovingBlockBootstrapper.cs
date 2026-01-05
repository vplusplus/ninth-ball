
using System.Collections.ObjectModel;

namespace NinthBall.Core
{
    /// <summary>
    /// Represents a small slice of historical returns.
    /// </summary>
    internal sealed record HBlock(IReadOnlyList<HROI> Segment)
    {
        public readonly int ChronoIndex = 0 == Segment.Count ? 0 : Segment[0].Year;
    }

    /// <summary>
    /// Replays random blocks (with replacement) of historical returns.
    /// </summary>
    internal sealed class MovingBlockBootstrapper(SimulationSeed SimSeed, HistoricalReturns History, MovingBlockBootstrap Options) : IBootstrapper
    {
        // Prepare all available blocks once.
        readonly Lazy<IReadOnlyList<HBlock>> AllBlocks = new( () => ReadBlocksOnce(History.History, Options.BlockSizes) );

        // We can produce theoretically unlimited possible combinations.
        public int GetMaxIterations(int numYears) => int.MaxValue;

        // Random blocks of history (with replacement)
        public IReadOnlyList<HROI> GetROISequence(int iterationIndex, int numYears)
        {
            var iterRand = new Random(PredictableHashCode.Combine(SimSeed.Value, iterationIndex));
            var sequence = new List<HROI>(numYears);

            HBlock prevBlock = null!;

            while (sequence.Count < numYears)
            {
                // Pick next random block with uniform distribution with replacement.
                var nextBlock = AllBlocks.Value[iterRand.Next(0, AllBlocks.Value.Count)];

                // OPTIONAL constraint:
                // Prevents drawing consecutive overlapping sequence.
                // Intention is to avoid two random samples producing unrealistic repeated historical regimes.
                // For example, avoid severe identical crash sequences repeating back-to-back.
                if (Options.NoConsecutiveBlocks && null != prevBlock)
                {
                    // Next block overlaps (repeats in part or full) the previous block
                    var overlaps = prevBlock.Segment.Intersect(nextBlock.Segment).Count() > 0;

                    if (overlaps)
                    {
                        // Remove console print after testing.
                        // Console.WriteLine($"Skipping overlapping blocks | {prevBlock.ChronoIndex} & {nextBlock.ChronoIndex}");
                        continue;
                    }
                } 

                // Collect
                sequence.AddRange(nextBlock.Segment);
                prevBlock = nextBlock;
            }

            // The last sample may provide more years than what we need.
            // Clip the sequence for number of years of interest.
            return sequence.Take(numYears).ToArray().AsReadOnly();
        }

        /// <summary>
        /// Prepare all available overlapping-blocks of suggested sequence lengths.
        /// </summary>
        static ReadOnlyCollection<HBlock> ReadBlocksOnce(IReadOnlyList<HROI> history, IReadOnlyList<int> blockLengths)
        {
            ArgumentNullException.ThrowIfNull(history);
            ArgumentNullException.ThrowIfNull(blockLengths);

            if (0 == blockLengths.Count) throw new ArgumentException("Invalid blockLength(s). Please specify at least one.");
            if (blockLengths.Count != blockLengths.Distinct().Count()) throw new ArgumentException("Invalid blockLength(s). Expecting distinct numbers.");
            if (blockLengths.Any(x => x > history.Count)) throw new ArgumentException($"Invalid blockLength(s). Block size cannot be larger than history length ({history.Count}).");

            // Since we will repeatedly use Array.Copy()
            var arrHistory = history.ToArray();
            var allBlocks  = blockLengths.SelectMany(blockLength => ReadBlocks(arrHistory, blockLength));

            // We want repeatability in results.
            // The source data may not be pre-sorted, may be it is, we do not know.
            // The sampling technique will draw random blocks with uniform distribution.
            // Ordering the blocks here is only for repeatability across runs even if the source data sequence changes.
            return allBlocks
                .OrderBy(b => b.ChronoIndex)
                .ThenBy(b => b.Segment.Count)
                .ToList()
                .AsReadOnly();
        }

        // All available blocks of same length (sequenceLength)
        static IEnumerable<HBlock> ReadBlocks(HROI[] history, int sequenceLength)
        {
            if (history.Length < sequenceLength) throw new Exception($"Too few elements in history | Expecting at least {sequenceLength}");

            for (int i = 0; i <= (history.Length - sequenceLength); i++)
            {
                HROI[] segment = new HROI[sequenceLength];
                Array.Copy(history, i, segment, 0, sequenceLength);
                yield return new HBlock(segment.AsReadOnly());
            }
        }

        public override string ToString() => $"Moving Block Bootstrap (MBB) using random blocks [{CSVBlockSizes}] from  {History.History.Min(x => x.Year)} to {History.History.Max(x => x.Year)} data.{TxtNoConsecutiveBlocks}";
        string CSVBlockSizes => string.Join(",", Options.BlockSizes);
        string TxtNoConsecutiveBlocks => Options.NoConsecutiveBlocks ? " (No back to back repetition)" : string.Empty;
    }
}
