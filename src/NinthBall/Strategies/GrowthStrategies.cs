

using Microsoft.Extensions.DependencyInjection;

namespace NinthBall
{
    [SimInput(typeof(GrowthStrategy), typeof(Growth))]
    sealed class GrowthStrategy(IServiceProvider Services, SimParams SimParams, Growth Options) : ISimObjective
    {
        int ISimObjective.Order => 40;

        readonly IBootstrapper MyBootstrapper = Options.Bootstrapper switch
        {
            BootstrapKind.Flat        => Services.GetRequiredService<FlatBootstrapper>(),
            BootstrapKind.Sequential  => Services.GetRequiredService<SequentialBootstrapper>(),
            BootstrapKind.MovingBlock => Services.GetRequiredService<MovingBlockBootstrapper>(),
            BootstrapKind.Parametric  => Services.GetRequiredService<ParametricBootstrapper>(),
            _                         => throw new Exception($"Unknown bootstrapper: {Options.Bootstrapper}")
        };

        int ISimObjective.MaxIterations => MyBootstrapper.GetMaxIterations(SimParams.NoOfYears);

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) 
        {
            var roiSequence = MyBootstrapper.GetROISequence(iterationIndex, SimParams.NoOfYears);

            return new Strategy(roiSequence, Options.CashROI);
        }

        sealed class Strategy(IReadOnlyList<HROI> MyROISequence, double cashGrowth) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimContext context)
            {
                var roi = context.YearIndex >= 0 && context.YearIndex < MyROISequence.Count
                    ? MyROISequence[context.YearIndex]
                    : throw new IndexOutOfRangeException($"Iteration #{context.IterationIndex} year #{context.YearIndex} is outside the range of this growth strategy");

                context.ROI = new ROI(roi.Year, roi.StocksROI, roi.BondROI, cashGrowth);
            }
        }

        public override string ToString() => $"Growth | {Options.Bootstrapper} bootstrap using historical data";
    }
}
