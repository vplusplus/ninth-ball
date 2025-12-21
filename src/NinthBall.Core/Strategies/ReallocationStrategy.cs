
namespace NinthBall
{
    [SimInput(typeof(ReallocationStrategy), typeof(Reallocate), Family = StrategyFamily.PortfolioManagement)]
    sealed class ReallocationStrategy(Reallocate Options) : ISimObjective, ISimStrategy
    {
        int ISimObjective.Order => 1;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => this;

        void ISimStrategy.Apply(ISimContext context)
        {
            var reallocateThisYear = Options.Steps.Any(x => x.AtAge == context.Age);

            if (reallocateThisYear)
            {
                var newAllocation = Options.Steps.Single(x => x.AtAge == context.Age).Allocation;
                context.PreTaxBalance.Reallocate(newAllocation, Options.MaxDrift);
                context.PostTaxBalance.Reallocate(newAllocation, Options.MaxDrift);
            }
        }

        public override string ToString() => $"Reallocation | Glide path: {CSVSteps}";

        string CSVSteps => string.Join(", ", Options.Steps.Select(x => $"{x.Allocation:P0} @ {x.AtAge}"));
    }
}
