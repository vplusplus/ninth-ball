

namespace NinthBall.Core
{
    [SimInput(typeof(RebalancingAndReallocationStrategy), typeof(Rebalance), Family = StrategyFamily.PortfolioManagement)]
    sealed class RebalancingAndReallocationStrategy(InitialBalance Initial, Rebalance Options) : ISimObjective
    {
        int ISimObjective.Order => 1;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(Initial, Options);

        private sealed class Strategy(InitialBalance Initial, Rebalance Options) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimState context)
            {
                var preTaxAllocation = Initial.PreTax.Allocation;
                var postTaxAllocation = Initial.PostTax.Allocation;
                var maxDrift = Options.MaxDrift;

                var reallocateThisYear = null != Options.Reallocate && Options.Reallocate.Count > 0 && Options.Reallocate.Any(x => x.AtAge == context.Age);

                if (reallocateThisYear)
                {
                    preTaxAllocation = postTaxAllocation = Options.Reallocate!.Single(x => x.AtAge == context.Age).Allocation;
                }

                context.TargetAllocation = new(
                    new(preTaxAllocation, maxDrift),
                    new(postTaxAllocation, maxDrift)
                );
            }
        }


        public override string ToString() => $"{TxtRB}{TxtRA}";

        string TxtRB => $"Rebalance yearly if drift > {Options.MaxDrift:P0}";
        string TxtRA => null != Options.Reallocate && Options.Reallocate.Count > 0 ? $" | Reallocate to {CSVSteps}" : string.Empty;
        string CSVSteps => string.Join(", ", Options.Reallocate.Select(x => $"[{x.Allocation:P0} @ {x.AtAge}]"));
    }
}
