
namespace NinthBall
{

    sealed class FixedWithdrawalStrategy(FixedWithdrawal Options) : ISimObjective
    {
        int ISimObjective.Order => 20;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(Options);

        sealed class Strategy(FixedWithdrawal FW) : ISimStrategy
        {
            double from401K = 0;

            void ISimStrategy.Apply(ISimContext ctx)
            {
                if (0 == ctx.YearIndex)
                {
                    from401K = FW.FirstYearAmount;
                }
                else
                {
                    from401K *= 1 + FW.Increment;
                }

                ctx.Withdrawals = ctx.Withdrawals with
                {
                    PreTax = from401K
                };
            }
        }

        public override string ToString() => $"Withdraw {Options.FirstYearAmount:C0} first year with {Options.Increment:P1} increment each year.";
    }

    sealed class PercentageWithdrawalStrategy(PercentageWithdrawal Options) : ISimObjective
    {
        int ISimObjective.Order => 20;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(Options);

        sealed class Strategy(PercentageWithdrawal PW) : ISimStrategy
        {
            double from401K = 0;

            void ISimStrategy.Apply(ISimContext ctx)
            {
                if (0 == ctx.YearIndex)
                {
                    from401K = ctx.PreTaxBalance.Amount * PW.FirstYearPct;
                }
                else if (null != PW.ResetAtAge && PW.ResetAtAge.Contains(ctx.Age))
                {
                    from401K = ctx.PreTaxBalance.Amount * PW.FirstYearPct;
                }
                else
                {
                    from401K *= 1 + PW.Increment;
                }

                ctx.Withdrawals = ctx.Withdrawals with
                {
                    PreTax = from401K
                };
            }
        }

        public override string ToString() => $"{WithdrawPctToString} {ResetYearsToString}";
        string WithdrawPctToString => $"Withdraw {Options.FirstYearPct:P1} first year with {Options.Increment:P1} increment each year.";
        string ResetYearsToString => (null == Options.ResetAtAge || 0 == Options.ResetAtAge.Count) ? string.Empty : $"Reset to {Options.FirstYearPct:P1} @[{string.Join(',', Options.ResetAtAge)}]";
    }

    sealed class VariablePercentageWithdrawalStrategy(VariablePercentageWithdrawal Options) : ISimObjective
    {
        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => throw new NotImplementedException($"{nameof(VariablePercentageWithdrawalStrategy)} not yet implemented.");
    }
}
