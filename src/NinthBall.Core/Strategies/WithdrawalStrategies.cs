
namespace NinthBall.Core
{

    [SimInput(typeof(FixedWithdrawalStrategy), typeof(FixedWithdrawal), Family = StrategyFamily.WithdrawalVelocity)]
    sealed class FixedWithdrawalStrategy(FixedWithdrawal Options) : ISimObjective
    {
        int ISimObjective.Order => 20;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(Options);

        sealed class Strategy(FixedWithdrawal FW) : ISimStrategy
        {
            double from401K = 0;

            void ISimStrategy.Apply(ISimContext context)
            {
                context.Withdrawals = context.Withdrawals with
                {
                    PreTax = 0 == context.YearIndex
                        ? from401K = FW.FirstYearAmount
                        : from401K *= 1 + FW.Increment
                };
            }
        }

        public override string ToString() => $"Pre-Tax Drawdown | Fixed {Options.FirstYearAmount:C0} (+{Options.Increment:P1}/yr)";
    }

    [SimInput(typeof(PercentageWithdrawalStrategy), typeof(PercentageWithdrawal), Family = StrategyFamily.WithdrawalVelocity)]
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

        public override string ToString() => $"Pre-Tax Drawdown | {Options.FirstYearPct:P1} of PreTax (+{Options.Increment:P1}/yr){ResetYearsToString}";
        string ResetYearsToString => (null == Options.ResetAtAge || 0 == Options.ResetAtAge.Count) ? string.Empty : $" | Reset to {Options.FirstYearPct:P1} @ age [{string.Join(',', Options.ResetAtAge)}]";
    }

    [SimInput(typeof(VariablePercentageWithdrawalStrategy), typeof(VariablePercentageWithdrawal), Family = StrategyFamily.WithdrawalVelocity)]
    sealed class VariablePercentageWithdrawalStrategy(VariablePercentageWithdrawal Options, SimParams Params) : ISimObjective
    {
        int ISimObjective.Order => 20;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(Options, Params);

        sealed class Strategy(VariablePercentageWithdrawal VPW, SimParams P) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimContext ctx)
            {
                int remainingYears = P.NoOfYears - ctx.YearIndex;
                
                // Calculate the "ideal" withdrawal to hit zero at the end of the horizon.
                double amount = Stats.EquatedWithdrawal(
                    currentBalance:     ctx.PreTaxBalance.Amount, 
                    estimatedROI:       VPW.ROI, 
                    estimatedInflation: VPW.Inflation, 
                    remainingYears:     remainingYears
                );

                // Apply guardrails (adjusted for inflation)
                double inflationFactor = Math.Pow(1 + VPW.Inflation, ctx.YearIndex);

                if (VPW.Floor.HasValue)
                {
                    double currentFloor = VPW.Floor.Value * inflationFactor;
                    amount = Math.Max(amount, currentFloor);
                }

                if (VPW.Ceiling.HasValue)
                {
                    double currentCeiling = VPW.Ceiling.Value * inflationFactor;
                    amount = Math.Min(amount, currentCeiling);
                }

                ctx.Withdrawals = ctx.Withdrawals with
                {
                    PreTax = amount
                };
            }
        }

        public override string ToString() => $"Pre-Tax Drawdown | Tax-optimized amortization toward zero balance | Assumptions: {Options.ROI:P1} ROI, {Options.Inflation:P1} Inflation{GuardrailsToString}";

        string GuardrailsToString => (Options.Floor.HasValue || Options.Ceiling.HasValue) 
            ? $" | Guardrails: [{Options.Floor?.ToString("C0") ?? "None"} - {Options.Ceiling?.ToString("C0") ?? "None"}] adjusted for inflation" 
            : string.Empty;
    }
}
