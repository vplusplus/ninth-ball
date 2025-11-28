
namespace NinthBall
{
    internal class AnnualFeesObjective(SimConfig simConfig) : ISimObjective
    {
        readonly SimConfig C = simConfig;
        readonly Fees P = simConfig.Fees;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex)
        {
            return new Strategy(P.AnnualFeesPct);
        }

        sealed class Strategy(double annualFeesPct) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimContext context)
            {
                // Be considerate with multiple possible fFee strategies/
                // Don't replace the Fees. Use +=
                context.Fees += context.JanBalance * annualFeesPct;
            }
        }

        public override string ToString() => P.ToString();
    }
}
