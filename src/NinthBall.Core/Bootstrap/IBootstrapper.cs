
namespace NinthBall.Core
{
    /// <summary>
    /// Provides sequence of returns for a given iteration.
    /// </summary>
    internal interface IBootstrapper
    {
        /// <summary>
        /// Max no of iterations this Bootstrapper can support.
        /// </summary>
        int GetMaxIterations(int numYears);

        /// <summary>
        /// ROI sequence for the given iteration.
        /// </summary>
        IReadOnlyList<HROI> GetROISequence(int iterationIndex, int numYears);
    }
}
