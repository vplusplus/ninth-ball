
namespace NinthBall.Core
{
    [SimInput(typeof(LivingExpensesStrategy), typeof(LivingExpenses), Family = StrategyFamily.LifestyleExpenses)]
    sealed class LivingExpensesStrategy(SimParams P, LivingExpenses LExp) : ISimObjective
    {
        int ISimObjective.Order => 32;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex)
        {
            // Fail fast: Year #0 can't use step-down. Adjust Initial amount instead.
            if (null != LExp.StepDown && LExp.StepDown.Any(x => x.AtAge == P.StartAge)) throw new FatalWarning("Avoid step-down at start age. Adjust FirstYearAmount if required and retry.");

            return new Strategy(P, LExp);
        }
        
        sealed record Strategy(SimParams P, LivingExpenses LExp) : ISimStrategy
        {
            private double runningLivingExpenseNominal = double.NaN;

            void ISimStrategy.Apply(ISimContext context)
            {
                if (0 == context.YearIndex)
                {
                    // Year #0: Use configured first-year-amount as-is.
                    runningLivingExpenseNominal = LExp.FirstYearAmount;
                }
                else
                {
                    // Subsequent years: Inflate prior year amount.
                    // On Jan 1st, we do not know current year CPI. Use prior year CPI.
                    var priorYear = context.PriorYears.Span[^1];
                    var priorYearInflationRate = priorYear.ROI.InflationRate;
                    runningLivingExpenseNominal *= (1 +  priorYearInflationRate);
                }

                // On step-down years (if defined)
                if (null != LExp.StepDown && LExp.StepDown.Count > 0)
                {
                    // Though not intended, nothing stops user from specifying multiple step-downs on same year.
                    var reductionNominal = LExp.StepDown.Where(x => x.AtAge == context.Age).Sum(x => x.Reduction);

                    // BY-DESIGN: Step-down amount is treated as nominal amount on that year/age.
                    // Step down amount is not infation-adjusted
                    // Reduce living expenses by specified amount.
                    if (reductionNominal > 0) runningLivingExpenseNominal -= reductionNominal;
                }

                // Guard: Living expenses can't become ghost income.
                runningLivingExpenseNominal = Math.Max(0, runningLivingExpenseNominal);

                context.Expenses = context.Expenses with
                {
                    // When we plan expenses, we do not plan in fractions.
                    // Round to multiples of $120 i.e $10/month
                    // Do not touch first year amount; user sees what they said.
                    LivExp = 0 == context.YearIndex
                        ? Math.Round(runningLivingExpenseNominal)
                        : runningLivingExpenseNominal.RoundToMultiples(120.0)
                };
            }
        }

        public override string ToString() => $"Living expenses | {LExp.FirstYearAmount:C0} first year (COLA/inflation adjusted){CSVStepDown}";

        string CSVStepDown => null != LExp.StepDown && LExp.StepDown.Count > 0
            ? $" | Stepdown: {string.Join(", ", LExp.StepDown.Select(x => $"[-{x.Reduction:C0} @ {x.AtAge}]"))}"
            : string.Empty;
    }
}

