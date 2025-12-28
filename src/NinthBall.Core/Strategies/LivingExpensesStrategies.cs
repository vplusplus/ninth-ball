
namespace NinthBall.Core
{
    [SimInput(typeof(LivingExpensesStrategy), typeof(LivingExpenses), Family = StrategyFamily.LifestyleExpenses)]
    sealed class LivingExpensesStrategy(LivingExpenses Options) : ISimObjective
    {
        int ISimObjective.Order => 32;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(Options);
        
        sealed record Strategy(LivingExpenses LExp) : ISimStrategy
        {
            double amount = 0;

            void ISimStrategy.Apply(ISimContext context)
            {
                if (0 == context.YearIndex)
                {
                    // Year #0 - Use firt year amount
                    context.Expenses = context.Expenses with
                    {
                        CYExp = (amount = LExp.FirstYearAmount)
                    };
                }
                else
                {
                    // On StepDown years (if specified), reduce prior year amount by suggested number
                    if (null != LExp.StepDown && LExp.StepDown.Any(x => x.AtAge == context.Age))
                    {
                        amount -= LExp.StepDown.Single(x => x.AtAge == context.Age).Reduction;
                    }

                    // Increment prior year amount by suggested increment
                    context.Expenses = context.Expenses with
                    {
                        CYExp = (amount *= 1 + LExp.Increment)
                    };
                }
            }
        }

        public override string ToString() => $"Living expenses | {Options.FirstYearAmount:C0} first year (+{Options.Increment:P1}/yr){CSVStepDown}";

        string CSVStepDown => null != Options.StepDown && Options.StepDown.Count > 0
            ? $" | Stepdown: {string.Join(", ", Options.StepDown.Select(x => $"[-{x.Reduction:C0} @ {x.AtAge}]"))}"
            : string.Empty;
    }


    [SimInput(typeof(PrecalculatedLivingExpensesStrategy), typeof(PrecalculatedLivingExpenses), Family = StrategyFamily.LifestyleExpenses)]
    sealed class PrecalculatedLivingExpensesStrategy(PrecalculatedLivingExpenses Options) : ISimObjective
    {
        private readonly IReadOnlyList<double> ExpenseSequence = PrecalculatedLivingExpenseReader.ReadPrecalculatedLivingExpenses(Options.FileName, Options.SheetName);

        int ISimObjective.Order => 32;

        public ISimStrategy CreateStrategy(int iterationIndex) => new Strategy(ExpenseSequence);

        sealed class Strategy(IReadOnlyList<double> ExpSeq) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimContext context)
            {
                context.Expenses = context.Expenses with
                {
                    CYExp = context.YearIndex >= 0 && context.YearIndex < ExpSeq.Count
                        ? ExpSeq[context.YearIndex]
                        : throw new IndexOutOfRangeException($"Year index #{context.YearIndex} is outside the range of the predefined expense list.")
                };
            }
        }

        public override string ToString() => $"Living expenses | Pre-calculated from {Path.GetFileName(Options.FileName)} [{Options.SheetName}]";
    }
}
