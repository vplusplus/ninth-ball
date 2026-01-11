
namespace NinthBall.Core
{
    [SimInput(typeof(AdditionalIncomeStrategy), typeof(AdditionalIncomes))]
    sealed class AdditionalIncomeStrategy(SimParams P, AdditionalIncomes AInc) : ISimObjective
    {
        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(P, AInc);

        int ISimObjective.Order => 10;

        sealed record Strategy(SimParams P, AdditionalIncomes AInc) : ISimStrategy
        {
            double ssAmount  = 0;
            double annAmount = 0;

            void ISimStrategy.Apply(ISimContext context)
            {
                context.Incomes = context.Incomes with
                {
                    SS  = Math.Round(context.Age == AInc.SS.FromAge  ? ssAmount  = AInc.SS.Amount  : ssAmount  *= 1 + P.InflationRate),
                    Ann = Math.Round(context.Age == AInc.Ann.FromAge ? annAmount = AInc.Ann.Amount : annAmount *= 1 + AInc.Ann.Increment),
                };
            }
        }

        public override string ToString() => $"Additional income | SS: {AInc.SS.Amount:C0} @ age {AInc.SS.FromAge} (+{P.InflationRate:P1}/yr) | Ann: {AInc.Ann.Amount:C0} @ age {AInc.Ann.FromAge} (+{AInc.Ann.Increment:P1}/yr)";
    }
}
