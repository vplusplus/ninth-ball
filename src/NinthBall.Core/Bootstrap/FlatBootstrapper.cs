

namespace NinthBall.Core
{
    internal sealed class FlatBootstrapper(FlatGrowth TheFlatGrowth) : IBootstrapper
    {
        // We need only one single-instance-sequence since it's flat growth.
        readonly IROISequence FlatSequence = new ROISequence(
            new(
                Year:          0, 
                StocksROI:     TheFlatGrowth.Stocks, 
                BondsROI:      TheFlatGrowth.Bonds, 
                InflationRate: TheFlatGrowth.InflationRate
            )
        );

        // Growth is flat, meaningless to perform more than one iteration.
        int IBootstrapper.GetMaxIterations(int numYears) => 1;

        // Returns same sequence for all iterations
        IROISequence IBootstrapper.GetROISequence(int iterationIndex, int numYears) => FlatSequence;

        private readonly record struct ROISequence(HROI SameROI) : IROISequence
        {
            // Returns same ROI for all years
            readonly HROI IROISequence.this[int yearIndex] => SameROI;
        }

        // Describe...
        public override string ToString() => $"Flat growth and inflation | Stocks: {TheFlatGrowth.Stocks:P1} Bonds: {TheFlatGrowth.Bonds:P1} Inflation: {TheFlatGrowth.InflationRate:P1}";
    }
}

