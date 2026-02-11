
using NinthBall.Utils;
using System.Collections.ObjectModel;
using System.Data;

namespace NinthBall.Core
{
    using RegimeAwareBlocks = ReadOnlyCollection<ReadOnlyCollection<HBlock>>;

    /// <summary>
    /// Replays random blocks of historical returns and inflation.
    /// </summary>
    internal sealed class RegimeAwareMovingBlockBootstrapper(SimulationSeed SimSeed, HistoricalBlocks History, HRegimes Regimes) : IBootstrapper
    {
        readonly Lazy<(RegimeAwareBlocks RBlocks, ReadOnlyMemory<double> RCounts)> LazyRegimeAwareBlocks = new ( MapBlocksToRegimesOnce(History.Blocks, Regimes) );

        int IBootstrapper.GetMaxIterations(int numYears) => int.MaxValue;

        // Random blocks of history (with replacement)
        IROISequence IBootstrapper.GetROISequence(int iterationIndex, int numYears)
        {
            var iterRand = new Random(PredictableHashCode.Combine(SimSeed.Value, iterationIndex));

            // Generate regime aware ROI sequence specific this iteration.
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
            var sequence = new HROI[numYears];
            var idx = 0;

            int currentRegime = PickInitialRegime(R);

            while (idx < numYears)
            {
                // Pick a random block from current regime.
                var nextBlock = PickNextBlock(R, currentRegime);

                // Collect HROI from the sampled block.
                for (int j = 0; j < nextBlock.Slice.Length && idx < numYears; j++, idx++) sequence[idx] = nextBlock.Slice.Span[j];

                // Consult transition matrix, transition to next regime
                if (idx < numYears) currentRegime = PickNextRegime(R, currentRegime);
            }

            return sequence;
        }
        
        private int PickInitialRegime(Random R)
        {
            // Use regime size as a guide (no of blocks in each regime)
            // This approximates historical frequency of blocks.
            var weights = LazyRegimeAwareBlocks.Value.RCounts;

            // Choose a random first regime, but biased by the regime size
            return R.NextWeightedIndex(weights.Span);
        }

        private int PickNextRegime(Random R, int fromRegimeIndex)
        {
            // Consult transition matrix where the economy may be heading
            var transitionProbabilities = Regimes.TransitionMatrix[fromRegimeIndex];

            // Pick next regime basd on the probabilities
            return R.NextWeightedIndex(transitionProbabilities);
        }

        private HBlock PickNextBlock(Random R, int regimeIndex)
        {
            var blocks = LazyRegimeAwareBlocks.Value.RBlocks[regimeIndex];
            int blockIndex = R.Next(blocks.Count);
            var block = blocks[blockIndex];
            return block;
        }

        static (RegimeAwareBlocks, ReadOnlyMemory<double>) MapBlocksToRegimesOnce(IReadOnlyList<HBlock> blocks, HRegimes regimes)
        {
            if (regimes.Regimes.Count != regimes.Centroids.NumRows) throw new Exception("Invalid regimes | NumRegimes and NumCentroids doesn't match.");

            // Extract features of all blocks.
            // Map the features to K-Mean feature space using z-params learnt during training.
            var standardizedFeatureMatrix = blocks.ToFeatureMatrix().StandardizeFeatureMatrix(regimes.StandardizationParams);
            if (standardizedFeatureMatrix.NumColumns != regimes.Centroids.NumColumns) throw new Exception("Invalid regimes | Feature count mismatch between block-features and centroid-features.");
            if (standardizedFeatureMatrix.NumRows != blocks.Count) throw new Exception("Invalid logic | You should never see this error.");

            // Given a blockIndex, consult standardized features and the centroids,
            // find the index of regime it belongs to.
            int FindRegimeIndex(int blockIndex) => standardizedFeatureMatrix[blockIndex].FindNearestCentroid(regimes.Centroids);

            // Map each block to the regime index.
            var rBlocks = blocks.Select((block, blockIndex) => 
            ( 
                RegimeIndex: FindRegimeIndex(blockIndex), 
                Block:       block
            ))
            .ToList();

            // Group the blocks by RegimeIndex. 
            // With-in each regime, group blocks by BlockLength
            // We depend on regime index (order by regimeIndex)
            var blocksByRegime = rBlocks
                .GroupBy(x => x.RegimeIndex)
                .OrderBy(g => g.Key)
                .Select(rg => rg.Select(x => x.Block).ToList().AsReadOnly())
                .ToList().AsReadOnly();

            // Count blocks by regime.
            // We depend on regime index (order by regimeIndex)
            // Ensure the counts are ordered by regime index.
            var countByRegime = rBlocks
                .CountBy(x => x.RegimeIndex)
                .OrderBy(x => x.Key)
                .Select(x => (double)x.Value)
                .ToArray();

            // We depend on regime index and centroid index.
            // Group by and count by can skip index if there are no blocks associated with the regimes.
            // We do not support empty regimes.
            var badData = false
                || blocksByRegime.Sum(x => x.Count) != blocks.Count
                || countByRegime.Sum(x => x) != blocks.Count
                || blocksByRegime.Count != regimes.Regimes.Count
                || countByRegime.Length != regimes.Regimes.Count
                || countByRegime.Any(x => 0 == x)
                ;

            if (badData) throw new Exception("Detected empty regime or count mismatch.");


            return (blocksByRegime, countByRegime);
        }

    }
}
