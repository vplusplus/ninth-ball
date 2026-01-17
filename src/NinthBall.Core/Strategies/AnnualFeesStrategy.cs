
namespace NinthBall.Core
{
    [SimInput(typeof(AnnualFeesStrategy), typeof(FeesPCT))]
    sealed class AnnualFeesStrategy(FeesPCT Options) : ISimObjective, ISimStrategy
    {
        int ISimObjective.Order => 30;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => this;

        void ISimStrategy.Apply(ISimState context)
        {
            // Calculate fees
            context.Fees = new(
                Math.Ceiling(context.Jan.PreTax.Amount * Options.PreTax),
                Math.Ceiling(context.Jan.PostTax.Amount * Options.PostTax),
                Math.Ceiling(context.Jan.Cash.Amount * Options.Cash)
            );
        }

        public override string ToString() => $"Annual fees | PreTax: {Options.PreTax:P1} | PostTax: {Options.PostTax:P1} | Cash: {Options.Cash:P1}";
    }
}
