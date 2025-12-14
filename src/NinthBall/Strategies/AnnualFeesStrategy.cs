
namespace NinthBall
{
    internal class AnnualFeesObjective(SimConfig simConfig) : ISimObjective
    {
        readonly FeesPCT F = simConfig.Fees;

        readonly ISimStrategy MyStrategy = new Strategy(simConfig.Fees);

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => MyStrategy;

        sealed record Strategy(FeesPCT pctFees) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimContext context)
            {
                context.FeesPCT = pctFees;
            }
        }

        public override string ToString() => $"Fees | PreTax: {F.PreTaxFeesPCT:P1} | PostTax: {F.PostTaxFeesPCT:P1} | Cash: {F.CashFeesPCT:P1}";
    }
}
