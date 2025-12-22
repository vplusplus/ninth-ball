
namespace NinthBall.Core
{
    [SimInput(typeof(AdditionalIncomeStrategy), typeof(AdditionalIncomes))]
    sealed class AdditionalIncomeStrategy(AdditionalIncomes options) : ISimObjective
    {
        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(options);

        int ISimObjective.Order => 10;

        sealed record Strategy(AdditionalIncomes options) : ISimStrategy
        {
            double ssAmount  = 0;
            double annAmount = 0;

            void ISimStrategy.Apply(ISimContext context)
            {
                context.Incomes = context.Incomes with
                {
                    SS  = context.Age == options.SS.FromAge  ? ssAmount  = options.SS.Amount  : ssAmount  *= 1 + options.SS.Increment,
                    Ann = context.Age == options.Ann.FromAge ? annAmount = options.Ann.Amount : annAmount *= 1 + options.Ann.Increment,
                };
            }
        }

        public override string ToString() => $"Additional income | SS: {options.SS.Amount:C0} @ age {options.SS.FromAge} (+{options.SS.Increment:P1}/yr) | Ann: {options.Ann.Amount:C0} @ age {options.Ann.FromAge} (+{options.Ann.Increment:P1}/yr)";
    }
}
