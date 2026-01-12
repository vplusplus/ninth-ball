
using System.Collections.ObjectModel;

namespace NinthBall.Core
{
    /// <summary>
    /// Replays random blocks (with replacement) of historical returns.
    /// </summary>
    internal sealed class MovingBlockBootstrapper(SimulationSeed SimSeed, HistoricalReturns History, MovingBlockBootstrap Options) : IBootstrapper
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

            // Return an indexed-view into historical ROI data.
            return new ROISequence(History.History, indices);
        }

        public override string ToString() => $"Moving Block Bootstrap (MBB) using random blocks [{CSVBlockSizes}] from {History.MinYear} to {History.MaxYear} data.{TxtNoConsecutiveBlocks}";
        string CSVBlockSizes => string.Join(",", Options.BlockSizes);
        string TxtNoConsecutiveBlocks => Options.NoConsecutiveBlocks ? " (No back to back repetition)" : string.Empty;

        private readonly record struct ROISequence(ReadOnlyMemory<HROI> MemoryBlock, int[] Indices) : IROISequence
        {
            readonly HROI IROISequence.this[int yearIndex] => MemoryBlock.Span[Indices[yearIndex]];
        }

        //......................................................................
        #region HBlock and AllBlocks
        //......................................................................
        // Represents a small window into the historical returns.
        // HBlock(s) are nothing more than an index (and length) into a block-of-memory.
        //......................................................................
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

            // The nested for-loops below ensures blocks are ordered by block-size and start-yearIndex. 
            // Simulation uses uniform sampling, hence order is irrelevant (no shuffle needed).
            List<HBlock> availableBlocks = [];
            
            foreach (var blockLength in blkSizes)
            {
                var maxBlocks = availableYears - blockLength + 1;
                for (int startIndex = 0; startIndex < maxBlocks; startIndex++)
                {
                    availableBlocks.Add(new HBlock(startIndex, blockLength));
                }
            }

            // For repeatability, blocks are arranged by start-index and then the sequence length.
            availableBlocks = availableBlocks.OrderBy(x => x.StartIndex).ThenBy(x => x.Length).ToList();

            // Immutable...
            return availableBlocks.AsReadOnly();
        });

        #endregion
    }
}
