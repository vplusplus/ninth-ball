
namespace NinthBall.Core
{
    [StrategyFamily(StrategyFamily.Growth)]
    sealed class GrowthStrategy(SimParams SimParams, BootstrapSelector BootstrapSelector) : ISimObjective
    {
        int ISimObjective.Order => 40;

        IBootstrapper ChosenBootstrapper = BootstrapSelector.GetSelectedBootstrapper();

        int ISimObjective.MaxIterations => ChosenBootstrapper.GetMaxIterations(SimParams.NoOfYears);

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) 
        {
            return new Strategy(
                ChosenBootstrapper.GetROISequence(iterationIndex, SimParams.NoOfYears)
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

        public override string ToString() => $"Growth | {ChosenBootstrapper}";
    }
}
