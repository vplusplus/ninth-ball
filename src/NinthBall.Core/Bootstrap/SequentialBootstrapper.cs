

namespace NinthBall.Core
{
    /// <summary>
    /// Replays historical returns in their original order using a sliding window.
    /// </summary>
    internal sealed class SequentialBootstrapper(HistoricalReturns History) : IBootstrapper
    {
        // We have limited data. We can support only limited number of iterations.
        public int GetMaxIterations(int numYears) => History.History.Length - numYears + 1;

        // Replays the history using iterationIndex as the sliding-window offset.
        IROISequence IBootstrapper.GetROISequence(int iterationIndex, int numYears)
        {
            var availableYears = History.History.Length - iterationIndex;
            if (numYears > availableYears) throw new FatalWarning($"Iteration #{iterationIndex} is outside the range of {nameof(SequentialBootstrapper)}");

            return new ROISequence(History.History, iterationIndex);
        }

        public override string ToString() => $"Sequence of historical returns from {History.MinYear} to {History.MaxYear} data.";

        private readonly record struct ROISequence(ReadOnlyMemory<HROI> MemoryBlock, int Offset) : IROISequence
        {
            readonly HROI IROISequence.this[int index] => MemoryBlock.Span[Offset + index];
        }
    }
}
