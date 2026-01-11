
namespace NinthBall.Core
{
    [SimInput(typeof(LivingExpensesStrategy), typeof(LivingExpenses), Family = StrategyFamily.LifestyleExpenses)]
    sealed class LivingExpensesStrategy(SimParams P, LivingExpenses LExp) : ISimObjective
    {
        int ISimObjective.Order => 32;

        // Living expenses increase by the configured inflation rate.
        readonly double YearlyIncrement = P.InflationRate;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(LExp, YearlyIncrement);
        
        sealed record Strategy(LivingExpenses LExp, double yearlyIncrement) : ISimStrategy
        {
            double livingExpense = 0;

            void ISimStrategy.Apply(ISimContext context)
            {
                if (0 == context.YearIndex)
                {
                    // Year #0 - Use firt year livingExpense
                    livingExpense = LExp.FirstYearAmount;

                    context.Expenses = context.Expenses with
                    {
                        // Do not round to multiples of $120. Doing so may be confusing for the reviewer.
                        // Just drop the fractions (if any)
                        CYExp = Math.Round(livingExpense)
                    };
                }
                else
                {
                    // On StepDown years (if specified), find the step-down amount for the current age (if specified)
                    // Assume zero if neither of the condition is true
                    var stepDown = null != LExp.StepDown ? LExp.StepDown.Where(x => x.AtAge == context.Age).DefaultIfEmpty().Max(x => x.Reduction) : 0;

                    // Reduce prior year livingExpense by suggested amount
                    if (stepDown > 0)
                    {
                        // Can't go negative
                        livingExpense = Math.Max(0, livingExpense - stepDown);
                    }

                    // Increment prior year livingExpense by suggested increment
                    livingExpense *= 1 + yearlyIncrement;

                    context.Expenses = context.Expenses with
                    {
                        // We do not spend in fractions.
                        // Round to multiples of $120 i.e $10/month
                        CYExp = livingExpense.RoundToMultiples(120.0)
                    };
                }
            }
        }

        public override string ToString() => $"Living expenses | {LExp.FirstYearAmount:C0} first year (+{YearlyIncrement:P1}/yr){CSVStepDown}";

        string CSVStepDown => null != LExp.StepDown && LExp.StepDown.Count > 0
            ? $" | Stepdown: {string.Join(", ", LExp.StepDown.Select(x => $"[-{x.Reduction:C0} @ {x.AtAge}]"))}"
            : string.Empty;
    }
}

