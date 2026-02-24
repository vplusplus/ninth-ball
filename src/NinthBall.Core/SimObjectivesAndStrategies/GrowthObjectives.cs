
namespace NinthBall.Core
{
    abstract class GrowthObjective(SimParams SimParams, IBootstrapper bootstrapper) : ISimObjective
    {
        protected readonly IBootstrapper Bootstrapper = bootstrapper;

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

        public override string ToString() => $"Inflation & growth | {MyShortName} | {Bootstrapper}";

        string MyShortName => this.GetType().Name.Replace("Objective", string.Empty);
    }

    [StrategyFamily(StrategyFamily.Growth)]
    sealed class FlatGrowthObjective(SimParams SimParams, FlatGrowth Options) : GrowthObjective(
        SimParams,
        new FlatBootstrapper(Options)
    );

    [StrategyFamily(StrategyFamily.Growth)]
    sealed class HistoricalGrowthObjective(SimParams SimParams, HistoricalReturns History) : GrowthObjective(
        SimParams,
        new SequentialBootstrapper(History)
    );

    [StrategyFamily(StrategyFamily.Growth)]
    sealed class RandomHistoricalGrowthObjective(SimParams SimParams, SimulationSeed SimSeed, BootstrapOptions Options, HistoricalBlocks HBlocks, HistoricalRegimes Regimes) : GrowthObjective(
        SimParams,
        new RegimeAwareMovingBlockBootstrapper(SimSeed, Options, HBlocks, Regimes)
    );

    [StrategyFamily(StrategyFamily.Growth)]
    sealed class RandomGrowthObjective(SimParams SimParams, SimulationSeed SimSeed, BootstrapOptions Options, HistoricalRegimes Regimes) : GrowthObjective(
        SimParams,
        new RegimeAwareParametricBootstrapper(SimSeed, Options, Regimes)
    );

    [StrategyFamily(StrategyFamily.Growth)]
    sealed class ParametricGrowthObjective(SimParams SimParams, SimulationSeed SimSeed, ParametricProfiles Profiles) : GrowthObjective(
        SimParams, new ParametricBootstrapper(SimSeed, Profiles)
    );
}
