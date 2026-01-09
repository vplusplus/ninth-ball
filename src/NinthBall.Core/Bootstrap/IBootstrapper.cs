
namespace NinthBall.Core
{
    /// <summary>
    /// Represents stocks and bonds ROI on a specific year.
    /// </summary>
    internal readonly record struct HROI(int Year, double StocksROI, double BondROI);

    /// <summary>
    /// Provides reproducible sequences of ROI.
    /// </summary>
    internal interface IBootstrapper
    {
        /// <summary>
        /// Returns the maximum number of distinct iterations supported 
        /// for the specified simulation horizon.
        /// </summary>
        int GetMaxIterations(int numYears);

        /// <summary>
        /// Returns the ROI sequence for a specific iteration
        /// over the requested simulation horizon.
        /// </summary>
        IROISequence GetROISequence(int iterationIndex, int numYears);
    }
    /// <summary>
    /// Provides ROI for a specific year.
    /// </summary>
    internal interface IROISequence
    {
        HROI this[int yearIndex] { get; }
    }
}
