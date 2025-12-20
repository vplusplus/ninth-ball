
using System.Security.Cryptography;

namespace NinthBall
{
    sealed class LivingExpensesStrategy(LivingExpenses Options) : ISimObjective
    {
        int ISimObjective.Order => 32;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(Options);
        
        sealed record Strategy(LivingExpenses exp) : ISimStrategy
        {
            double amount = 0;

            void ISimStrategy.Apply(ISimContext context)
            {
                context.Expenses = context.Expenses with
                {
                    CYExp = 0 == context.YearIndex
                        ? amount = exp.FirstYearAmount
                        : amount *= 1 + exp.Increment
                };
            }
        }

        public override string ToString() => $"Living expenses - First year: {Options.FirstYearAmount:C0} | Yearly increment: {Options.Increment:P1}";
    }


    sealed class PrecalculatedLivingExpensesStrategy(PrecalculatedLivingExpenses Options) : ISimObjective
    {
        private readonly IReadOnlyList<double> ExpenseSequence = PrecalculatedLivingExpenseReader.ReadPrecalculatedLivingExpenses(Options.FileName, Options.SheetName);

        int ISimObjective.Order => 32;

        public ISimStrategy CreateStrategy(int iterationIndex) => new Strategy(ExpenseSequence);

        sealed class Strategy(IReadOnlyList<double> sequence) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimContext context)
            {
                context.Expenses = context.Expenses with
                {
                    CYExp = context.YearIndex >= 0 && context.YearIndex < sequence.Count
                        ? sequence[context.YearIndex]
                        : throw new IndexOutOfRangeException($"Year index #{context.YearIndex} is outside the range of the predefined expense list.")
                };
            }
        }

        public override string ToString() => $"Living expenses - From {Path.GetFileName(Options.FileName)} [sheet: {Options.SheetName}]";
    }

}
