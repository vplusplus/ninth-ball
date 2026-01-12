
namespace NinthBall.Core
{
    [SimInput(typeof(LivingExpensesStrategy), typeof(LivingExpenses), Family = StrategyFamily.LifestyleExpenses)]
    sealed class LivingExpensesStrategy(LivingExpenses LExp) : ISimObjective
    {
        int ISimObjective.Order => 32;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(LExp);
        
        sealed record Strategy(LivingExpenses LExp) : ISimStrategy
        {

            void ISimStrategy.Apply(ISimContext context)
            {
                // Calculate nominal expense based on baseline (Year 0) amount and cumulative inflation
                // This correctly uses a "Look-Back" since context.RunningInflationMultiplier 
                // only reflects inflation up to Jan 1st of the current year.
                double nominalExpense = LExp.FirstYearAmount * context.RunningInflationMultiplier;

                // Apply StepDown reductions if any
                if (context.YearIndex > 0 && null != LExp.StepDown)
                {
                    // Sum all step-downs that have occurred up to the current age
                    // Note: This logic assumes step-downs are in "Year 0" dollars and should also be inflated,
                    // OR they are absolute nominal reductions. 
                    // To keep it simple and consistent with the previous implementation:
                    // We will subtract the StepDown reduction from the baseline first-year amount 
                    // (if the reduction is intended to be in real terms)
                    // ACTUALLY, the previous implementation tracked a running 'livingExpense' variable.
                    // If we want to support multiple step-downs over time, we need to sum them up.

                    var totalReduction = LExp.StepDown
                        .Where(x => context.Age >= x.AtAge)
                        .Sum(x => x.Reduction);

                    nominalExpense = Math.Max(0, (LExp.FirstYearAmount - totalReduction) * context.RunningInflationMultiplier);
                }

                context.Expenses = context.Expenses with
                {
                    // Round to multiples of $120 i.e $10/month
                    // Year 0 uses raw Rounding (as per previous logic), others use RoundToMultiples
                    LivExp = context.YearIndex == 0 
                        ? Math.Round(nominalExpense) 
                        : nominalExpense.RoundToMultiples(120.0)
                };
            }
        }

        public override string ToString() => $"Living expenses | {LExp.FirstYearAmount:C0} first year (COLA/inflation adjusted){CSVStepDown}";

        string CSVStepDown => null != LExp.StepDown && LExp.StepDown.Count > 0
            ? $" | Stepdown: {string.Join(", ", LExp.StepDown.Select(x => $"[-{x.Reduction:C0} @ {x.AtAge}]"))}"
            : string.Empty;
    }
}

