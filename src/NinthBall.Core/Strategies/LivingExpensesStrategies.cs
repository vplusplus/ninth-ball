
namespace NinthBall.Core
{
    [SimInput(typeof(LivingExpensesStrategy), typeof(LivingExpenses), Family = StrategyFamily.LifestyleExpenses)]
    sealed class LivingExpensesStrategy(LivingExpenses Options) : ISimObjective
    {
        int ISimObjective.Order => 32;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(Options);
        
        sealed record Strategy(LivingExpenses LExp) : ISimStrategy
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
                    livingExpense *= 1 + LExp.Increment;

                    context.Expenses = context.Expenses with
                    {
                        // We do not spend in fractions.
                        // Round to multiples of $120 i.e $10/month
                        CYExp = livingExpense.RoundToMultiples(120.0)
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
        private readonly IReadOnlyList<double> ExpenseSequence = ReadPrecalculatedLivingExpenses(Options.FileName, Options.SheetName);

        int ISimObjective.Order => 32;

        public ISimStrategy CreateStrategy(int iterationIndex) => new Strategy(ExpenseSequence);

        sealed class Strategy(IReadOnlyList<double> ExpSeq) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimContext context)
            {
                // Use the precalculated input for current year.
                var cyExp = context.YearIndex >= 0 && context.YearIndex < ExpSeq.Count
                    ? ExpSeq[context.YearIndex]
                    : throw new IndexOutOfRangeException($"Year index #{context.YearIndex} is outside the range of the predefined expense list.");

                context.Expenses = context.Expenses with
                {
                    // Amount is pre-calculated. Do not adjust.
                    // Just drop fractions (if any).
                    CYExp = Math.Round(cyExp)
                };
            }
        }

        public override string ToString() => $"Living expenses | Pre-calculated from {Path.GetFileName(Options.FileName)} [{Options.SheetName}]";

        public static IReadOnlyList<double> ReadPrecalculatedLivingExpenses(string xlFileName, string sheetName)
        {
            var sequence = new List<double>();

            using (var xlReader = new ExcelReader(xlFileName))
            {
                var sheet = xlReader.GetSheets().Where(s => sheetName.Equals(s.SheetName, StringComparison.OrdinalIgnoreCase)).SingleOrDefault()
                    ?? throw new FatalWarning($"Sheet not found | File: {Path.GetFileName(xlFileName)} | Sheet: '{sheetName}'");

                foreach (var row in sheet.GetRows())
                {
                    if (null == row) continue;

                    // Skip first (header) row.  Do not use IEnumerable.Skip(1) option; Use Rowindex.
                    var isFirstRow = null != row.RowIndex && 1 == row.RowIndex.Value;
                    if (isFirstRow) continue;

                    var cells = row.GetCellValues().ToArray();

                    if (
                        null != cells
                        && cells.Length >= 1
                        && double.TryParse(cells[0], out var amount)
                    )
                    {
                        sequence.Add(amount);
                    }
                }
            }

            return sequence.AsReadOnly();
        }
    }
}

