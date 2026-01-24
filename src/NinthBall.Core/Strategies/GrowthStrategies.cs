
namespace NinthBall.Core
{
    abstract class GrowthStrategyBase(SimParams SimParams, IBootstrapper Bootstrapper) : ISimObjective
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


    [StrategyFamily(StrategyFamily.Growth)] sealed class FlatGrowthStrategy(SimParams SimParams, FlatBootstrapper Bootstrapper) : GrowthStrategyBase(SimParams, Bootstrapper) { }
    [StrategyFamily(StrategyFamily.Growth)] sealed class SequentialGrowthStrategy(SimParams SimParams, SequentialBootstrapper Bootstrapper) : GrowthStrategyBase(SimParams, Bootstrapper) { }
    [StrategyFamily(StrategyFamily.Growth)] sealed class RandomBlocksGrowthStrategy(SimParams SimParams, MovingBlockBootstrapper Bootstrapper) : GrowthStrategyBase(SimParams, Bootstrapper) { }
    [StrategyFamily(StrategyFamily.Growth)] sealed class ParametricGrowthStrategy(SimParams SimParams, ParametricBootstrapper Bootstrapper) : GrowthStrategyBase(SimParams, Bootstrapper) { }

}
