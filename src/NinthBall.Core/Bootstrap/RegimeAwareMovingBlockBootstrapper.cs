
using NinthBall.Utils;
using System.Collections.ObjectModel;

namespace NinthBall.Core
{
    using RegimeAwareBlocks = ReadOnlyCollection<ReadOnlyCollection<HBlock>>;

    /// <summary>
    /// Replays random blocks of historical returns and inflation.
    /// Follows historical regime transitions.
    /// </summary>
    internal sealed class RegimeAwareMovingBlockBootstrapper(SimulationSeed SimSeed, BootstrapOptions Options, HistoricalBlocks HistoricalBlocks, HistoricalRegimes HistoricalRegimes) : IBootstrapper
    {
        readonly Lazy<RegimeAwareBlocks> LazyRegimeAwareBlocks = new ( MapBlocksToRegimesOnce(HistoricalBlocks.Blocks, HistoricalRegimes.Regimes) );

        int IBootstrapper.GetMaxIterations(int numYears) => int.MaxValue;

        IROISequence IBootstrapper.GetROISequence(int iterationIndex, int numYears)
        {
            var iterRand = new Random(PredictableHashCode.Combine(SimSeed.Value, iterationIndex));

            // Generate regime aware ROI sequence specific to this iteration.
            var sequence = GenerateRegimeAwareROISequence(iterRand, numYears);

            // Hand it over to ROISequence, it will honor the IROISequence contract.
            return new ROISequence(sequence.AsMemory());
        }

        private readonly record struct ROISequence(ReadOnlyMemory<HROI> MemoryBlock) : IROISequence
        {
            readonly HROI IROISequence.this[int yearIndex] => MemoryBlock.Span[yearIndex];
        }

        private HROI[] GenerateRegimeAwareROISequence(Random R, int numYears)
        {
            var regimes = HistoricalRegimes.Regimes;
            var blocksByRegime = LazyRegimeAwareBlocks.Value;
            var sequence = new HROI[numYears];
            var idx = 0;

            // Smooth the regime transition matrix.
            var adjustedRegimeTransitions = regimes.RegimeTransitions.ApplySmoothing(regimes.RegimeDistribution.Span, Options.RegimeAwareness);

            // Choose a random first regime, weighted by the regime size.
            var currentRegimeIdx = R.NextWeightedIndex(regimes.RegimeDistribution.Span);

            while (idx < numYears)
            {
                // Pick a block from the current regime using uniform distribution. 
                var eligibleBlocks = blocksByRegime[currentRegimeIdx];
                int blockIndex = R.Next(eligibleBlocks.Count);
                var nextBlock = eligibleBlocks[blockIndex];

                // Collect ROI and inflation data from the sampled block.
                for (int j = 0; j < nextBlock.Slice.Length && idx < numYears; j++, idx++) sequence[idx] = nextBlock.Slice.Span[j];

                // Consult transition matrix to see where the economy may be heading.
                // Pick next regime based on the transition probabilities.
                currentRegimeIdx = R.NextWeightedIndex(adjustedRegimeTransitions[currentRegimeIdx]);
            }

            return sequence;
        }

        static RegimeAwareBlocks MapBlocksToRegimesOnce(IReadOnlyList<HBlock> blocks, HRegimes regimes)
        {
            // Extract features of all blocks.
            // Standardize using z-params learnt during training.
            var zFeatureMatrix = blocks.ExtractFeatures().StandardizeFeatureMatrix(regimes.ZParams);

            // Map each block to the nearest regime.
            var blocksWithRegimeIndex = blocks.Select((block, blockIndex) => 
            ( 
                RegimeIndex: regimes.FindNearestRegime(zFeatureMatrix[blockIndex]), 
                Block:       block
            ));

            // Group the blocks by Regime. 
            // Preserve the regime index order.
            // Prepare ReadOnlyList of ReadOnlyList.
            var blocksByRegime = blocksWithRegimeIndex
                .GroupBy(x => x.RegimeIndex)
                .OrderBy(g => g.Key)
                .Select(rg => rg.Select(x => x.Block).ToList().AsReadOnly())
                .ToList()
                .AsReadOnly();

            // We can trust GroupBy() since K-Means training rejects clusters with zero members.
            // Defensive validations since LINQ-GroupBy() will happily skip zero-length regimes.
            if (
                blocksByRegime.Sum(x => x.Count) != blocks.Count || 
                blocksByRegime.Count != regimes.Regimes.Count
            ) throw new Exception("Unexpected | Detected empty regime or count mismatch.");

            return blocksByRegime;
        }

        public override string ToString() => $"Random historical sequence using {CSVBlockSizes}-year moving blocks from {HistoricalBlocks.MinYear} to {HistoricalBlocks.MaxYear} data | Regime awareness: {Options.RegimeAwareness:P0}";
        string CSVBlockSizes => string.Join("/", Options.BlockSizes);

    }
}
