
namespace NinthBall.Core
{
    internal sealed class FlatBootstrapper(FlatBootstrap Options) : IBootstrapper
    {
        // We need only one single-instance-sequence since it's flat growth.
        readonly IROISequence FlatSequence = new ROISequence(
            new(
                Year:          0, 
                StocksROI:     Options.Stocks, 
                BondsROI:      Options.Bonds, 
                InflationRate: Options.InflationRate
            )
        );

        // Growth is flat, meaningless to perform more than one iteration.
        int IBootstrapper.GetMaxIterations(int numYears) => 1;

        // Returns same sequence for all iterations
        IROISequence IBootstrapper.GetROISequence(int iterationIndex, int numYears) => FlatSequence;

        public override string ToString() => $"Flat growth and inflation | Stocks: {Options.Stocks:P1} Bonds: {Options.Bonds:P1} Inflation: {Options.InflationRate:P1}";

        private readonly record struct ROISequence(HROI SameROI) : IROISequence
        {
            // Returns same ROI for all years
            readonly HROI IROISequence.this[int yearIndex] => SameROI;
        }
    }
}

