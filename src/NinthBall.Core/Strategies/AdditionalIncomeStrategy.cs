
namespace NinthBall.Core
{
    [SimInput(typeof(AdditionalIncomeStrategy), typeof(AdditionalIncomes))]
    sealed class AdditionalIncomeStrategy(AdditionalIncomes AInc) : ISimObjective
    {
        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(AInc);

        int ISimObjective.Order => 10;

        sealed record Strategy(AdditionalIncomes AddInc) : ISimStrategy
        {

            void ISimStrategy.Apply(ISimState context)
            {
                // Income is active if current age >= FromAge
                bool ssActive  = context.Age >= AddInc.SS.FromAge;
                bool annActive = context.Age >= AddInc.Ann.FromAge;

                double ssAmount = 0;
                double annAmount = 0;

                if (ssActive)
                {
                    // Social Security is baseline Amount inflated up to today
                    var inflationMultiplier = 0 == context.YearIndex ? 1.0 : context.PriorYear.Metrics.InflationMultiplier;
                    ssAmount = AddInc.SS.Amount * inflationMultiplier;
                }

                if (annActive)
                {
                    // Annuity has its own fixed compound increment, independent of CPI.
                    // StartAge is our reference point (Year 0).
                    annAmount = AddInc.Ann.Amount * Math.Pow(1 + AddInc.Ann.Increment, context.Age - AddInc.Ann.FromAge);
                }

                context.Incomes = context.Incomes with
                {
                    SS  = Math.Round(ssAmount),
                    Ann = Math.Round(annAmount),
                };
            }
        }

        public override string ToString() => $"Additional income | SS: {AInc.SS.Amount:C0} @ age {AInc.SS.FromAge} (COLA/inflation adjusted) | Ann: {AInc.Ann.Amount:C0} @ age {AInc.Ann.FromAge} (+{AInc.Ann.Increment:P1}/yr)";
    }
}
