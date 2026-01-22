
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace NinthBall.Core
{
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

            // We are collecting random indexes on available blocks.
            while (idx < numYears)
            {
                // Sample next random block with uniform distribution (with replacement).
                var nextBlock = AllBlocks.Value[iterRand.Next(0, AllBlocks.Value.Count)];

                // Optional check to avoid consecutive overlapping blocks.
                // Remember previous block.
                if (Options.NoConsecutiveBlocks && null != prevBlock && HBlock.Overlaps(prevBlock.Value, nextBlock)) continue;
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
        readonly record struct HBlock(int StartIndex, int Length)
        {
            private readonly int EndIndex => StartIndex + Length - 1;
            public static bool Overlaps(HBlock prevBlock, HBlock nextBlock) => nextBlock.StartIndex <= prevBlock.EndIndex && nextBlock.EndIndex >= prevBlock.StartIndex;
        }

        // Prepare all available blocks once.
        readonly Lazy<ReadOnlyCollection<HBlock>> AllBlocks = new(() => 
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
                    availableBlocks.Add(new HBlock(startIndex, blockLength));
                }
            }

            // The growth strategy uses uniform sampling; therefore, ordering does not affect the outcome.
            // Sorting is performed solely to ensure repeatability across runs.
            // Historical data is already sorted by year.
            // Blocks are arranged chronologically, then by sequence length.
            availableBlocks = availableBlocks.OrderBy(x => x.StartIndex).ThenBy(x => x.Length).ToList();

            // Immutable...
            return availableBlocks.AsReadOnly();
        });

        #endregion

        // Describe...
        public override string ToString() => $"Moving Block Bootstrap (MBB) using random blocks [{CSVBlockSizes}] from {History.MinYear} to {History.MaxYear} data.{TxtNoConsecutiveBlocks}";
        string CSVBlockSizes => string.Join(",", Options.BlockSizes);
        string TxtNoConsecutiveBlocks => Options.NoConsecutiveBlocks ? " (No back to back repetition)" : string.Empty;

    }
}
