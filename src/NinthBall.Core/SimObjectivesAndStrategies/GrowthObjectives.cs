
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

        public override string ToString() => $"{MyShortName} | {Bootstrapper}";

        string MyShortName => this.GetType().Name.Replace("Objective", string.Empty);
    }

    [StrategyFamily(StrategyFamily.Growth)] 
    sealed class FlatGrowthObjective(SimParams SimParams, FlatGrowth Options) : GrowthObjective( SimParams, new FlatBootstrapper(Options) )
    {
        public override string ToString() => $"Inflation & growth | {Bootstrapper}";
    }

    [StrategyFamily(StrategyFamily.Growth)] 
    sealed class HistoricalGrowthObjective(SimParams SimParams, HistoricalReturns History) : GrowthObjective( SimParams, new SequentialBootstrapper(History) ) 
    {
        public override string ToString() => $"Inflation & growth | {Bootstrapper}";
    }

    [StrategyFamily(StrategyFamily.Growth)] 
    sealed class RandomHistoricalGrowthObjective(SimParams SimParams, SimulationSeed SimSeed, HistoricalBlocks HBlocks, MovingBlockBootstrapOptions Options) 
        : GrowthObjective( SimParams, new MovingBlockBootstrapper(SimSeed, HBlocks, Options) ) 
    {
        public override string ToString() => $"Inflation & growth | {Bootstrapper}";
    }

    [StrategyFamily(StrategyFamily.Growth)]
    sealed class RegimeAwareHistoricalGrowthObjective(SimParams SimParams, MovingBlockBootstrapOptions Options, SimulationSeed SimSeed, HistoricalBlocks HBlocks, HistoricalRegimes Regimes)
        : GrowthObjective(SimParams, new RegimeAwareMovingBlockBootstrapper(SimSeed, Options, HBlocks, Regimes))
    {
        public override string ToString() => $"Inflation & growth | {Bootstrapper}";
    }


    [StrategyFamily(StrategyFamily.Growth)] 
    sealed class ExpectedGrowth(SimParams SimParams, SimulationSeed SimSeed, ParametricProfiles Profiles) : GrowthObjective( SimParams, new ParametricBootstrapper(SimSeed, Profiles.Expected) )
    {
        public override string ToString() => $"Inflation & growth | Expected: {Bootstrapper}";
    }

    [StrategyFamily(StrategyFamily.Growth)]
    sealed class ConservativeGrowth(SimParams SimParams, SimulationSeed SimSeed, ParametricProfiles Profiles) : GrowthObjective( SimParams, new ParametricBootstrapper(SimSeed, Profiles.Conservative) )
    {
        public override string ToString() => $"Inflation & growth | Conservative: {Bootstrapper}";
    }

    [StrategyFamily(StrategyFamily.Growth)]
    sealed class HighRiskGrowth(SimParams SimParams, SimulationSeed SimSeed, ParametricProfiles Profiles) : GrowthObjective( SimParams, new ParametricBootstrapper(SimSeed, Profiles.HighRisk) )
    {
        public override string ToString() => $"Inflation & growth | HighRisk: {Bootstrapper}";
    }

}
