
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
                // Income is active if current age >= FromAge
                bool ssActive = context.Age >= AInc.SS.FromAge;
                bool annActive = context.Age >= AInc.Ann.FromAge;

                if (context.YearIndex == 0)
                {
                    // First year initialization (accounts for elapsed years if StartAge > FromAge)
                    if (ssActive) ssAmount = AInc.SS.Amount * Math.Pow(1 + P.InflationRate, context.Age - AInc.SS.FromAge);
                    if (annActive) annAmount = AInc.Ann.Amount * Math.Pow(1 + AInc.Ann.Increment, context.Age - AInc.Ann.FromAge);
                }
                else
                {
                    // Subsequent years: Grow if active; Initialize if starting this year.
                    if (ssActive)  ssAmount  = (context.Age == AInc.SS.FromAge)  ? AInc.SS.Amount  : ssAmount * (1 + P.InflationRate);
                    if (annActive) annAmount = (context.Age == AInc.Ann.FromAge) ? AInc.Ann.Amount : annAmount * (1 + AInc.Ann.Increment);
                }

                context.Incomes = context.Incomes with
                {
                    SS  = Math.Round(ssActive ? ssAmount : 0),
                    Ann = Math.Round(annActive ? annAmount : 0),
                };
            }
        }

        public override string ToString() => $"Additional income | SS: {AInc.SS.Amount:C0} @ age {AInc.SS.FromAge} (+{P.InflationRate:P1}/yr) | Ann: {AInc.Ann.Amount:C0} @ age {AInc.Ann.FromAge} (+{AInc.Ann.Increment:P1}/yr)";
    }
}
