
using System.Collections.ObjectModel;

namespace NinthBall.Core
{
    internal sealed record Block(IReadOnlyList<HROI> Segment)
    {
        public readonly IReadOnlyList<HROI> Segment = Segment ?? throw new ArgumentNullException(nameof(Segment));
        public readonly int ChronoIndex = 0 == Segment.Count ? 0 : Segment[0].Year;
    }

    internal sealed class MovingBlockBootstrapper(SimulationSeed SimSeed, HistoricalReturns History, MovingBlockBootstrap Options) : IBootstrapper
    {
        readonly Lazy<IReadOnlyList<Block>> AllBlocks = new(() =>
            ReadBlocksOnce(History.AllYears.ToArray(), Options.BlockSizes.ToArray())
        );

        // We can produce theoretically unlimited possible combinations.
        public int GetMaxIterations(int numYears) => int.MaxValue;

        public IReadOnlyList<HROI> GetROISequence(int iterationIndex, int numYears)
        {
            var iterRand = new Random(PredictableHashCode.Combine(SimSeed.Value, iterationIndex));
            var roiSequence = SampleRandomMovingBlocks(iterRand, AllBlocks.Value, numYears, Options.NoConsecutiveBlocks);
            return roiSequence;
        }

        static HROI[] SampleRandomMovingBlocks(Random rand, IReadOnlyList<Block> allBlocks, int numYears, bool noConsecutiveRepetition)
        {
            ArgumentNullException.ThrowIfNull(rand);
            ArgumentNullException.ThrowIfNull(allBlocks);

            List<HROI> roiRandomSamples = new(numYears);
            Block prevBlock = null!;

            while (roiRandomSamples.Count < numYears)
            {
                // Next random block index, ranging from 0 to Count-1
                var blockIndex = rand.Next(0, allBlocks.Count);
                var nextBlock = allBlocks[blockIndex];

                // OPTIONAL constraint intended only for stress-testing / what-if scenarios.
                // Prevents drawing consecutive allBlocks with at least two years of overlapping sequence.
                // Intention is to reducing the chance of unrealistic repeated historical regimes (e.g., severe crash periods repeating back-to-back).
                // In normal runs, leave disabled to preserve pure bootstrap behavior.
                const int TwoYears = 2;
                if (noConsecutiveRepetition && null != prevBlock && HasOverlappingYears(prevBlock, nextBlock, maxOverlap: TwoYears)) continue;

                // Collect
                roiRandomSamples.AddRange(nextBlock.Segment);
                prevBlock = nextBlock;
            }

            // The last sample may provide more years than what we need.
            // Clip the sequence for number of years of interest.
            var roiSequence = roiRandomSamples.Take(numYears).ToArray();

            return roiSequence.ToArray();


            static bool HasOverlappingYears(Block prevBlock, Block nextBlock, int maxOverlap)
            {
                int overlapCount = 0;

                foreach (var yPrev in prevBlock.Segment)
                    foreach (var yNext in nextBlock.Segment)
                        if (yPrev == yNext && ++overlapCount >= maxOverlap) return true;

                return false;
            }

        }

        static ReadOnlyCollection<Block> ReadBlocksOnce(HROI[] history, int[] blockLengths)
        {
            if (0 == blockLengths.Length) throw new ArgumentException("Invalid blockLength(s). Please specify at least one.");
            if (blockLengths.Length != blockLengths.Distinct().Count()) throw new ArgumentException("Invalid blockLength(s). Expecting distinct numbers.");
            if (blockLengths.Any(x => x > history.Length)) throw new ArgumentException($"Invalid blockLength(s). Block size cannot be larger than history length ({history.Length}).");

            // We want repeatability in results.
            // The source data may not be pre-sorted, though its likely, we do not know.
            // The sampling technique will draw random block with uniform distribution.
            // Ordering the allBlocks here is only for repeatability across runs even if the source data sequence changes.
            return blockLengths
                .SelectMany(blockLength => ReadBlocks(history, blockLength))
                .OrderBy(b => b.ChronoIndex)
                .ThenBy(b => b.Segment.Count)
                .ToList()
                .AsReadOnly();

            static IEnumerable<Block> ReadBlocks(HROI[] history, int sequenceLength)
            {
                if (history.Length < sequenceLength) throw new Exception($"Too few elements in history | Expecting at least {sequenceLength}");

                for (int i = 0; i <= (history.Length - sequenceLength); i++)
                {
                    HROI[] segment = new HROI[sequenceLength];
                    Array.Copy(history, i, segment, 0, sequenceLength);
                    yield return new Block(segment);
                }
            }
        }
    }
}
