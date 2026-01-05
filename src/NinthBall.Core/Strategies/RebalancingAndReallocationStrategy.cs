
namespace NinthBall.Core
{
    [SimInput(typeof(RebalancingAndReallocationStrategy), typeof(Rebalance), Family = StrategyFamily.PortfolioManagement)]
    sealed class RebalancingAndReallocationStrategy(Rebalance Options) : ISimObjective, ISimStrategy
    {
        int ISimObjective.Order => 1;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => this;

        void ISimStrategy.Apply(ISimContext context)
        {
            var reallocateThisYear = null != Options.Reallocate && Options.Reallocate.Count > 0 && Options.Reallocate.Any(x => x.AtAge == context.Age);

            if (reallocateThisYear)
            {
                // Reallocate asstes on targetted years (will also trigger rebalance)
                var newAllocation = Options.Reallocate!.Single(x => x.AtAge == context.Age).Allocation;
                context.PreTaxBalance.Reallocate(newAllocation, Options.MaxDrift);
                context.PostTaxBalance.Reallocate(newAllocation, Options.MaxDrift);
            }
            else
            {
                // Yearly rebalance
                context.PreTaxBalance.Rebalance(Options.MaxDrift);
                context.PostTaxBalance.Rebalance(Options.MaxDrift);
            }
        }

        public override string ToString() => $"{TxtRB}{TxtRA}";

        string TxtRB => $"Rebalance yearly if drift > {Options.MaxDrift:P0}";
        string TxtRA => null != Options.Reallocate && Options.Reallocate.Count > 0 ? $" | Reallocate to {CSVSteps}" : string.Empty;
        string CSVSteps => string.Join(", ", Options.Reallocate.Select(x => $"[{x.Allocation:P0} @ {x.AtAge}]"));
    }
}
