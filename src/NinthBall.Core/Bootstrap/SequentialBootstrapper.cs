

namespace NinthBall.Core
{
    /// <summary>
    /// Replays historical returns in their original order using a sliding window.
    /// </summary>
    internal sealed class SequentialBootstrapper(HistoricalReturns History) : IBootstrapper
    {
        // We have limited data. We can support only a limited number of iterations.
        int IBootstrapper.GetMaxIterations(int numYears) => History.Returns.Length - numYears + 1;

        // Replays the history using iterationIndex as the sliding-window offset.
        IROISequence IBootstrapper.GetROISequence(int iterationIndex, int numYears)
        {
            var availableYears = History.Returns.Length - iterationIndex;
            if (numYears > availableYears) throw new FatalWarning($"Iteration #{iterationIndex} is outside the range of {nameof(SequentialBootstrapper)}");

            return new ROISequence(History.Returns, iterationIndex);
        }

        private readonly record struct ROISequence(ReadOnlyMemory<HROI> MemoryBlock, int Offset) : IROISequence
        {
            readonly HROI IROISequence.this[int yearIndex] => MemoryBlock.Span[Offset + yearIndex];
        }

        public override string ToString() => $"Sequence of historical returns and inflation from {History.FromYear} to {History.ToYear}";

    }
}
