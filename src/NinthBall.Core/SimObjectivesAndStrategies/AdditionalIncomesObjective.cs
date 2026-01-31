
namespace NinthBall.Core
{
    [StrategyFamily(StrategyFamily.Income)]
    sealed class AdditionalIncomesObjective(SimParams P, AdditionalIncomes AInc) : ISimObjective
    {
        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) 
        {
            return new Strategy(P, AInc);
        }

        sealed record Strategy(SimParams P, AdditionalIncomes AddInc) : ISimStrategy
        {
            // If the Simulation start age is past the additional income start age:
            // Pre-initialize to the exact specified value.
            // We do not adjust for inflation or increment for the gap (if any)
            // BY-DESIGN: User sees exactly the specified amount on first year.
            double ssAmount  = AddInc.SS.FromAge < P.StartAge ? AddInc.SS.Amount : 0.0;
            double annAmount = AddInc.Ann.FromAge < P.StartAge ? AddInc.Ann.Amount : 0.0;

            void ISimStrategy.Apply(ISimState context)
            {
                // On income start year, use the exact amount specified.
                // Do not make any adjustment (User sees exactly the specified amount).
                // Other years, increment by prior year inflation.

                ssAmount = context.Age == AddInc.SS.FromAge ? AddInc.SS.Amount 
                    : 0 == context.YearIndex ? ssAmount 
                    : ssAmount * (1 + context.PriorYear.ROI.InflationRate);

                annAmount = context.Age == AddInc.Ann.FromAge ? AddInc.Ann.Amount
                    : 0 == context.YearIndex ? annAmount
                    : annAmount * (1 + AddInc.Ann.Increment);

                context.Incomes = context.Incomes with
                {
                    SS  = Math.Round(ssAmount),
                    Ann = Math.Round(annAmount),
                };
            }
        }

        public override string ToString() => $"Additional income | SS: {AInc.SS.Amount:C0} @ age {AInc.SS.FromAge} (+COLA) | Ann: {AInc.Ann.Amount:C0} @ age {AInc.Ann.FromAge} (+{AInc.Ann.Increment:P1}/yr)";
    }
}
