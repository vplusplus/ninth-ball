
namespace NinthBall.Core
{
    [SimInput(typeof(AnnualFeesStrategy), typeof(FeesPCT))]
    sealed class AnnualFeesStrategy(FeesPCT Options) : ISimObjective, ISimStrategy
    {
        int ISimObjective.Order => 30;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => this;

        void ISimStrategy.Apply(ISimContext context)
        {
            // Calculate fees
            context.Fees = new(
                context.PreTaxBalance.Amount * Options.PreTax,
                context.PostTaxBalance.Amount * Options.PostTax,
                context.CashBalance.Amount * Options.Cash
            );
        }

        public override string ToString() => $"Annual fees | PreTax: {Options.PreTax:P1} | PostTax: {Options.PostTax:P1} | Cash: {Options.Cash:P1}";
    }
}
