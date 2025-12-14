
using System.Security.Cryptography;

namespace NinthBall
{

    // TODO: Support first year PCT model. Q: Does it PCT of whole, PCT of Pre/PostTax assets?

    internal class LivingExpensesObjective(SimConfig simConfig) : ISimObjective
    {
        readonly LivingExpenses E = simConfig.LivingExpenses;

        int ISimObjective.Order => 10;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(simConfig.LivingExpenses);
        
        sealed record Strategy(LivingExpenses exp) : ISimStrategy
        {
            double amount = 0;

            void ISimStrategy.Apply(ISimContext context)
            {
                context.Expenses = context.Expenses with
                {
                    CYExp = 0 == context.YearIndex
                        ? amount = exp.FirstYearAmount
                        : amount *= 1 + exp.IncrementPct
                };
            }
        }

        public override string ToString() => $"Living expensed - First year: {E.FirstYearAmount:C0} | Yearly increment: {E.IncrementPct:P1}";
    }


    /// <summary>
    /// Pre-calculated withdrawal sequence from an external file.
    /// </summary>
    public sealed class PrecalculatedLivingExpensesObjective(SimConfig simConfig) : ISimObjective
    {
        readonly SimConfig C = simConfig;
        readonly PrecalculatedLivingExpenses Exp = simConfig.PrecalculatedLivingExpenses;

        public ISimStrategy CreateStrategy(int iterationIndex) => new Strategy(Exp.LivingExpensesSequence);

        sealed class Strategy(IReadOnlyList<double> sequence) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimContext context)
            {
                context.Expenses = context.Expenses with
                {
                    CYExp = context.YearIndex >= 0 && context.YearIndex < sequence.Count
                        ? sequence[context.YearIndex]
                        : throw new IndexOutOfRangeException($"Year index #{context.YearIndex} is outside the range of the predfined expense list.")
                };
            }
        }

        public override string ToString() => $"Living expenses - From {Path.GetFileName(Exp.FileName)} [sheet: {Exp.SheetName}]";
    }

}
