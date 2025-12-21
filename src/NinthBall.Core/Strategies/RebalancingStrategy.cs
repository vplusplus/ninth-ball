
namespace NinthBall
{
    [SimInput(typeof(RebalancingStrategy), typeof(Rebalance))]
    sealed class RebalancingStrategy(Rebalance Options) : ISimObjective, ISimStrategy
    {
        int ISimObjective.Order => 2;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => this;

        void ISimStrategy.Apply(ISimContext context)
        {
            context.PreTaxBalance.Rebalance(Options.MaxDrift);
            context.PostTaxBalance.Rebalance(Options.MaxDrift);
            context.CashBalance.Rebalance(Options.MaxDrift);
        }

        public override string ToString() => $"Rebalancing | Yearly if drift > {Options.MaxDrift:P0}";
    }
}
