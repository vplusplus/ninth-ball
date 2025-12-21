
namespace NinthBall
{

    [SimInput(typeof(FixedWithdrawalStrategy), typeof(FixedWithdrawal))]
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

        public override string ToString() => $"Withdrawal | Fixed {Options.FirstYearAmount:C0} (+{Options.Increment:P1}/yr)";
    }

    [SimInput(typeof(PercentageWithdrawalStrategy), typeof(PercentageWithdrawal))]
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

        public override string ToString() => $"Withdrawal | {Options.FirstYearPct:P1} of PreTax (+{Options.Increment:P1}/yr){ResetYearsToString}";
        string ResetYearsToString => (null == Options.ResetAtAge || 0 == Options.ResetAtAge.Count) ? string.Empty : $" | Reset to {Options.FirstYearPct:P1} @ age [{string.Join(',', Options.ResetAtAge)}]";
    }

    [SimInput(typeof(VariablePercentageWithdrawalStrategy), typeof(VariablePercentageWithdrawal))]
    sealed class VariablePercentageWithdrawalStrategy(VariablePercentageWithdrawal Options) : ISimObjective
    {
        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => throw new NotImplementedException($"{nameof(VariablePercentageWithdrawalStrategy)} not yet implemented.");
        public override string ToString() => $"Withdrawal | Variable percentage ({Options.ROI:P1} ROI, {Options.Escalation:P1} Escalation) - (not implemented)";
    }
}
