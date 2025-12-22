
namespace NinthBall.Core
{
    internal sealed class SequentialBootstrapper(HistoricalReturns History) : IBootstrapper
    {
        // We have limited data. 
        public int GetMaxIterations(int numYears) => History.AllYears.Count - numYears + 1;

        // Return exact sequence of history.
        public IReadOnlyList<HROI> GetROISequence(int iterationIndex, int numYears)
        {
            var availableYears = History.AllYears.Count - iterationIndex;
            if (numYears > availableYears) throw new FatalWarning($"Iteration #{iterationIndex} is outside the range of {nameof(SequentialBootstrapper)}");

            return History.AllYears.Skip(iterationIndex).Take(numYears).ToList();
        }
    }
}
