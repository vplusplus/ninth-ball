

namespace NinthBall.Core
{
    internal sealed class FlatBootstrapper(FlatGrowth TheFlatGrowth) : IBootstrapper, IROISequence
    {
        readonly HROI SameROI = new
        (
            Year:           0,
            StocksROI:      TheFlatGrowth.Stocks,
            BondsROI:       TheFlatGrowth.Bonds,
            InflationRate:  TheFlatGrowth.InflationRate
        );

        // Growth is flat, meaningless to perform more than one iteration.
        int IBootstrapper.GetMaxIterations(int numYears) => 1;

        // I am the IROISequence as well.
        IROISequence IBootstrapper.GetROISequence(int iterationIndex, int numYears) => this;

        // Returns the same ROI for all years and all iterations.
        HROI IROISequence.this[int yearIndex] => SameROI;

        // Describe...
        public override string ToString() => $"Stocks: {SameROI.StocksROI:P1} Bonds: {SameROI.BondsROI:P1} Inflation: {SameROI.InflationRate:P1}";
    }
}
