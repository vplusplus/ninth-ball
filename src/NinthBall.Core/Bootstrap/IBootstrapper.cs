
namespace NinthBall.Core
{
    /// <summary>
    /// Provides reproducible sequences of returns for bootstrap-style simulations.
    /// </summary>
    internal interface IBootstrapper
    {
        /// <summary>
        /// Returns the maximum number of distinct iterations supported
        /// for the specified simulation horizon.
        /// </summary>
        int GetMaxIterations(int numYears);

        /// <summary>
        /// Returns the return-on-investment (ROI) sequence for a specific iteration
        /// over the requested simulation horizon.
        /// </summary>
        IReadOnlyList<HROI> GetROISequence(int iterationIndex, int numYears);
    }
}
