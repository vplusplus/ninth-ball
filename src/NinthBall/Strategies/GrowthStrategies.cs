

using Microsoft.Extensions.DependencyInjection;

namespace NinthBall
{
    sealed class GrowthStrategy(IServiceProvider Services, SimParams SimParams, Growth Options) : ISimObjective
    {
        int ISimObjective.Order => 40;

        readonly IBootstrapper MyBootstrapper = (Options.Bootstrapper ?? string.Empty).ToLower() switch
        {
            "flat"        => Services.GetRequiredService<FlatBootstrapper>(),
            "sequential"  => Services.GetRequiredService<SequentialBootstrapper>(),
            "movingblock" => Services.GetRequiredService<MovingBlockBootstrapper>(),
            "parametric"  => Services.GetRequiredService<ParametricBootstrapper>(),
            _             => throw new Exception($"Unknown bootstrapper: {Options.Bootstrapper}")
        };

        public int MaxIterations => MyBootstrapper.GetMaxIterations(SimParams.NoOfYears);

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

        public override string ToString() => $"Growth | Using historical data and {Options.Bootstrapper} bootstrap";
    }
}
