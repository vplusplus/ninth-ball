
namespace NinthBall.Core
{
    [SimInput(typeof(RebalancingAndReallocationStrategy), typeof(Rebalance), Family = StrategyFamily.PortfolioManagement)]
    sealed class RebalancingAndReallocationStrategy(InitialBalance Initial, Rebalance RBL) : ISimObjective
    {
        int ISimObjective.Order => 1;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(Initial, RBL);

        private sealed class Strategy(InitialBalance Initial, Rebalance RBL) : ISimStrategy
        {
            // Start with initial allocation.
            double preTaxAllocation = Initial.PreTax.Allocation;
            double postTaxAllocation = Initial.PostTax.Allocation;
            double maxDrift = RBL.MaxDrift;

            void ISimStrategy.Apply(ISimState context)
            {
                // Check if reallocation-steps is specified for this year.
                var reallocateThisYear = null != RBL.Reallocate && RBL.Reallocate.Count > 0 && RBL.Reallocate.Any(x => x.AtAge == context.Age);

                if (reallocateThisYear)
                {
                    var alloc = RBL.Reallocate!.Single(x => x.AtAge == context.Age);

                    // Ensure its not an empty-configuration with all default values.
                    if (alloc != default)
                    {
                        // From this year on, use the new allocation.
                        preTaxAllocation  = alloc.PreTaxStocksAllocation;
                        postTaxAllocation = alloc.PostTaxStocksAllocation;
                    }
                }

                // Yearly rebalancing (or reallocation).
                context.Rebalance(preTaxAllocation, postTaxAllocation, maxDrift);
            }
        }

        public override string ToString() => $"{TxtRB}{TxtRA}";

        string TxtRB => $"Rebalance yearly if drift > {RBL.MaxDrift:P0}";
        string TxtRA => null != RBL.Reallocate && RBL.Reallocate.Count > 0 ? $" | Reallocate to {CSVSteps}" : string.Empty;
        string CSVSteps => string.Join(", ", RBL.Reallocate.Select(x => $"[Pre:{x.PreTaxStocksAllocation:P0}, Post:{x.PostTaxStocksAllocation:P0} @ {x.AtAge}]"));
    }
}
