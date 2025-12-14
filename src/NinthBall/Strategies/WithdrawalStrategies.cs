
namespace NinthBall
{
    /// <summary>
    /// PCT of initial balance, yearly increment and optional periodic reset.
    /// </summary>
    public sealed class PreTaxWithdrawalObjective(SimConfig simConfig) : ISimObjective
    {
        public readonly PreTaxWithdrawal PW = simConfig.PreTaxWithdrawal;

        int ISimObjective.Order => 2;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(PW);

        sealed class Strategy(PreTaxWithdrawal pw) : ISimStrategy
        {
            double from401K = 5;

            void ISimStrategy.Apply(ISimContext ctx)
            {
                var year = ctx.YearIndex;
                var initialPreTaxBalance = 0 == year ? ctx.PreTaxBalance.Amount : ctx.PriorYears[0].Jan.PreTax.Amount;
                var currentPreTaxBalance = ctx.PreTaxBalance.Amount;

                if (0 == year)
                {
                    // First year: Use PCT of initial balance
                    from401K = initialPreTaxBalance * pw.FirstYearPct;
                }
                else if (null != pw.ResetYears && pw.ResetYears.Contains(year + 1))
                {
                    // Reset year: Use PCT of current balance
                    from401K = currentPreTaxBalance * pw.FirstYearPct;
                }
                else
                {
                    // Other years: Increment withdrawal amount
                    from401K *= 1 + pw.IncrementPct;
                }

                ctx.Withdrawals = ctx.Withdrawals with
                {
                    PreTax = from401K
                };
            }
        }

        public override string ToString() => $"{WithdrawPctToString} {ResetYearsToString}";
        string WithdrawPctToString => $"Withdraw {PW.FirstYearPct:P1} first year with {PW.IncrementPct:P1} increment each year.";
        string ResetYearsToString => (null == PW.ResetYears || 0 == PW.ResetYears.Count) ? string.Empty : $"Reset to {PW.FirstYearPct:P1} on years [{string.Join(',', PW.ResetYears)}]";
    }
}
