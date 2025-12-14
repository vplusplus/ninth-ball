
namespace NinthBall
{
    internal class AdditionalIncomeObjective(SimConfig simConfig) : ISimObjective
    {
        readonly AdditionalIncomes AI = simConfig.AdditionalIncomes;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(simConfig.AdditionalIncomes);

        sealed record Strategy(AdditionalIncomes AI) : ISimStrategy
        {
            double ssAmount  = 0;
            double annAmount = 0;


            void ISimStrategy.Apply(ISimContext context)
            {
                context.Incomes = context.Incomes with
                {
                    SS  = context.YearIndex == AI.SS.FromYear  ? ssAmount  = AI.SS.Amount  : ssAmount  *= 1 + AI.SS.YearlyIncrement,
                    Ann = context.YearIndex == AI.Ann.FromYear ? annAmount = AI.Ann.Amount : annAmount *= 1 + AI.Ann.YearlyIncrement,
                };
            }
        }

        public override string ToString() => $"Income | SS: {AI.SS.Amount:C0} from year {AI.SS.FromYear} with {AI.SS.YearlyIncrement:P1} increment | Ammuity: {AI.Ann.Amount:C0} from year {AI.Ann.FromYear} with {AI.Ann.YearlyIncrement:P1} increment ";
    }
}
