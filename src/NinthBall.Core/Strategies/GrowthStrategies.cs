

using Microsoft.Extensions.DependencyInjection;

namespace NinthBall.Core
{
    [SimInput(typeof(GrowthStrategy), typeof(Growth))]
    sealed class GrowthStrategy(IServiceProvider Services, SimParams SimParams, Growth Options) : ISimObjective
    {
        readonly IBootstrapper MyBootstrapper = Options.Bootstrapper switch
        {
            BootstrapKind.Flat        => Services.GetRequiredService<FlatBootstrapper>(),
            BootstrapKind.Sequential  => Services.GetRequiredService<SequentialBootstrapper>(),
            BootstrapKind.MovingBlock => Services.GetRequiredService<MovingBlockBootstrapper>(),
            BootstrapKind.Parametric  => Services.GetRequiredService<ParametricBootstrapper>(),
            _                         => throw new Exception($"Unknown bootstrapper: {Options.Bootstrapper}")
        };

        int ISimObjective.Order => 40;

        int ISimObjective.MaxIterations => MyBootstrapper.GetMaxIterations(SimParams.NoOfYears);

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) 
        {
            var roiSequence = MyBootstrapper.GetROISequence(iterationIndex, SimParams.NoOfYears);

            return new Strategy(roiSequence, Options.CashROI);
        }

        sealed class Strategy(IReadOnlyList<HROI> MyROISequence, double CashGrowth) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimContext context)
            {
                // Apply ROI as suggested by chosen bootstrapper.
                var roi = context.YearIndex >= 0 && context.YearIndex < MyROISequence.Count
                    ? MyROISequence[context.YearIndex]
                    : throw new IndexOutOfRangeException($"Iteration #{context.IterationIndex} year #{context.YearIndex} is outside the range of this growth strategy");

                // Modelling high-yield-savings account (for example)
                context.ROI = new ROI(roi.Year, roi.StocksROI, roi.BondROI, CashGrowth);
            }
        }

        public override string ToString() => $"Growth | {MyBootstrapper.ToString()}";
    }
}
