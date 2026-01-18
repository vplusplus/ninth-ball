
namespace NinthBall.Core
{
    [SimInput(typeof(GrowthStrategy), typeof(Growth))]
    sealed class GrowthStrategy(SimParams SimParams, IBootstrapper Bootstrapper) : ISimObjective
    {
        int ISimObjective.Order => 40;

        int ISimObjective.MaxIterations => Bootstrapper.GetMaxIterations(SimParams.NoOfYears);

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) 
        {
            return new Strategy(
                Bootstrapper.GetROISequence(iterationIndex, SimParams.NoOfYears)
            );
        }

        sealed class Strategy(IROISequence ROISequence) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimState context)
            {
                // Obtain ROI for the current year.
                var roi = ROISequence[context.YearIndex];

                // Apply ROI suggested by bootstrapper.
                context.ROI = new ROI(
                    LikeYear: roi.Year, 
                    StocksROI: roi.StocksROI, 
                    BondsROI: roi.BondsROI, 
                    InflationRate: roi.InflationRate
                );
            }
        }

        public override string ToString() => $"Growth | {Bootstrapper}";
    }
}
