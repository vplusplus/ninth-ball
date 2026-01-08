
namespace NinthBall.Core
{
    internal sealed class FlatBootstrapper(FlatBootstrap Options) : IBootstrapper
    {
        // We need only one sequence since it's flat growth.
        readonly IROISequence FlatSequence = new ROISequence(
            new(Year: 0, Options.Stocks, Options.Bonds)
        );

        // Growth is flat, meaningless to perform more than one iteration.
        public int GetMaxIterations(int numYears) => 1;

        // Returns same sequence for all iterations
        IROISequence IBootstrapper.GetROISequence(int iterationIndex, int numYears) => FlatSequence;

        public override string ToString() => $"Flat growth | Stocks: {Options.Stocks:P1} Bonds: {Options.Bonds:P1}";

        private readonly record struct ROISequence(HROI SameROI) : IROISequence
        {
            readonly HROI IROISequence.this[int index] => SameROI;
        }
    }
}

