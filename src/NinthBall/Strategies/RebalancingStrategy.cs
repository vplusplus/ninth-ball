
namespace NinthBall
{
    internal class RebalancingObjective(SimConfig simConfig) : ISimObjective
    {
        readonly YearlyRebalance RB = simConfig.YearlyRebalance;

        readonly Strategy MyStrategy = new(simConfig.YearlyRebalance);

        // Reblance happens early (after init)
        int ISimObjective.Order => 1;

        // Stateless. We need only one instance.
        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => MyStrategy;
        
        // Initialize portfolio balance on year #0
        sealed record Strategy(YearlyRebalance rb) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimContext context)
            {
                context.PreTaxBalance.RebalanceIf(rb.MaxDrift);
                context.PostTaxBalance.RebalanceIf(rb.MaxDrift);
                context.CashBalance.RebalanceIf(rb.MaxDrift);
            }
        }

        public override string ToString() => $"Rebalance yearly - MaxDrift: {RB.MaxDrift:P0}";
    }
}
