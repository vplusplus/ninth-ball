
namespace NinthBall.Core
{
    [StrategyFamily(StrategyFamily.Fees)]
    sealed class AnnualFeesObjective(AnnualFees F) : ISimObjective, ISimStrategy
    {
        int ISimObjective.Order => 30;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => this;

        void ISimStrategy.Apply(ISimState context)
        {
            // Calculate fees
            context.Fees = new(
                Math.Round(context.Jan.PreTax.Amount  * F.PreTax),
                Math.Round(context.Jan.PostTax.Amount * F.PostTax)
            );
        }

        public override string ToString() => $"Annual fees | PreTax: {F.PreTax:P1} | PostTax: {F.PostTax:P1} | Cash: None";
    }
}
