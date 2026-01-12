

namespace NinthBall.Core
{
    [SimInput(typeof(GrowthStrategy), typeof(Growth))]
    sealed class GrowthStrategy(SimParams SimParams, Growth Options, IBootstrapper Bootstrapper) : ISimObjective
    {
        int ISimObjective.Order => 40;

        int ISimObjective.MaxIterations => Bootstrapper.GetMaxIterations(SimParams.NoOfYears);

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) 
        {
            return new Strategy(
                Bootstrapper.GetROISequence(iterationIndex, SimParams.NoOfYears), 
                Options.CashROI
            );
        }

        sealed class Strategy(IROISequence ROISequence, double cashROI) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimContext context)
            {
                // Obtain ROI for the current year.
                var roi = ROISequence[context.YearIndex];

                // Apply ROI suggested by bootstrapper, add CashROI from Growth options.
                context.ROI = new ROI(
                    LikeYear: roi.Year, 
                    StocksROI: roi.StocksROI, 
                    BondsROI: roi.BondsROI, 
                    CashROI: cashROI, 
                    InflationRate: roi.InflationRate
                );
            }
        }

        public override string ToString() => $"Growth | {Bootstrapper.ToString()}";
    }
}
