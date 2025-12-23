

namespace NinthBall.Core
{
    /// <summary>
    /// Replays historical returns in their original order using a sliding window.
    /// Intended as a deterministic baseline for comparison against stochastic bootstrapping.
    /// </summary>
    internal sealed class SequentialBootstrapper(HistoricalReturns History) : IBootstrapper
    {
        // We have limited data. We can support only limited number of iterations.
        public int GetMaxIterations(int numYears) => History.History.Count - numYears + 1;

        // Replays the history, a contiguous sequence of historical returns
        // using iterationIndex as the sliding-window offset.
        public IReadOnlyList<HROI> GetROISequence(int iterationIndex, int numYears)
        {
            var availableYears = History.History.Count - iterationIndex;
            if (numYears > availableYears) throw new FatalWarning($"Iteration #{iterationIndex} is outside the range of {nameof(SequentialBootstrapper)}");

            return History.History.Skip(iterationIndex).Take(numYears).ToList();
        }

        public override string ToString() => $"Sequence of historical returns from {History.History.Min(x => x.Year)} to {History.History.Max(x => x.Year)} data.";
    }
}
