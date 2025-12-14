
namespace NinthBall
{
    internal class InitPortfolioObjective(SimConfig simConfig) : ISimObjective
    {
        readonly InitPortfolio P = simConfig.InitPortfolio;

        readonly Strategy MyStrategy = new(simConfig.InitPortfolio);

        // Init goes first
        int ISimObjective.Order => int.MinValue;

        // Stateless. We need only one instance.
        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => MyStrategy;

        
        // Initialize portfolio balance on year #0
        sealed record Strategy(InitPortfolio p) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimContext context)
            {
                if (0 == context.YearIndex)
                {
                    context.PreTaxBalance.Init(p.PreTax.Amount, p.PreTax.Allocation);
                    context.PostTaxBalance.Init(p.PostTax.Amount, p.PostTax.Allocation);
                    context.CashBalance.Init(p.Cash.Amount, 1.0);
                }
            }
        }

        public override string ToString() => $"PreTax: {P.PreTax.Amount:C0} | PostTax: {P.PostTax.Amount:C0} | Cash: {P.Cash.Amount:C0}";
    }
}
