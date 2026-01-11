
namespace NinthBall.Core
{

    [SimInput(typeof(PreTaxFixedWithdrawalStrategy), typeof(FixedWithdrawal), Family = StrategyFamily.PreTaxWithdrawalVelocity)]
    sealed class PreTaxFixedWithdrawalStrategy(FixedWithdrawal Options) : ISimObjective
    {
        int ISimObjective.Order => 20;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(Options);

        sealed class Strategy(FixedWithdrawal FW) : ISimStrategy
        {
            double from401K = 0;

            void ISimStrategy.Apply(ISimContext context)
            {
                var take = 0 == context.YearIndex
                    ? from401K = FW.FirstYearAmount
                    : from401K *= 1 + FW.Increment;

                context.Withdrawals = context.Withdrawals with
                {
                    // Adjust to multiples of $120 i.e. $10/month
                    PreTax = take.RoundToMultiples(120.0)   
                };
            }
        }

        public override string ToString() => $"Pre-Tax Drawdown | Fixed {Options.FirstYearAmount:C0} (+{Options.Increment:P1}/yr)";
    }

    [SimInput(typeof(PreTaxPercentageWithdrawalStrategy), typeof(PercentageWithdrawal), Family = StrategyFamily.PreTaxWithdrawalVelocity)]
    sealed class PreTaxPercentageWithdrawalStrategy(PercentageWithdrawal Options) : ISimObjective
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
                    // Adjust to multiples of $120 i.e. $10/month
                    PreTax = from401K.RoundToMultiples(120.0)
                };
            }
        }

        public override string ToString() => $"Pre-Tax Drawdown | {Options.FirstYearPct:P1} of PreTax (+{Options.Increment:P1}/yr){ResetYearsToString}";
        string ResetYearsToString => (null == Options.ResetAtAge || 0 == Options.ResetAtAge.Count) ? string.Empty : $" | Reset to {Options.FirstYearPct:P1} @ age [{string.Join(',', Options.ResetAtAge)}]";
    }

    [SimInput(typeof(PreTaxVariablePercentageWithdrawalStrategy), typeof(VariablePercentageWithdrawal), Family = StrategyFamily.PreTaxWithdrawalVelocity)]
    sealed class PreTaxVariablePercentageWithdrawalStrategy(SimParams Params, VariablePercentageWithdrawal VPW) : ISimObjective
    {
        int ISimObjective.Order => 20;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(Params, VPW);

        sealed class Strategy(SimParams P, VariablePercentageWithdrawal VPW) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimContext ctx)
            {
                int remainingYears = P.NoOfYears - ctx.YearIndex;
                
                // Calculate the "ideal" withdrawal to hit zero at the end of the horizon.
                double amount = Stats.EquatedWithdrawal(
                    currentBalance:     ctx.PreTaxBalance.Amount, 
                    estimatedROI:       VPW.FutureROI, 
                    estimatedInflation: P.InflationRate, 
                    remainingYears:     remainingYears
                );

                // Apply guardrails (adjusted for inflation)
                double inflationMultiplier = Math.Pow(1 + P.InflationRate, ctx.YearIndex);

                if (VPW.Floor.HasValue)
                {
                    double currentFloor = VPW.Floor.Value * inflationMultiplier;
                    amount = Math.Max(amount, currentFloor);
                }

                if (VPW.Ceiling.HasValue)
                {
                    double currentCeiling = VPW.Ceiling.Value * inflationMultiplier;
                    amount = Math.Min(amount, currentCeiling);
                }

                ctx.Withdrawals = ctx.Withdrawals with
                {
                    // Adjust to multiples of $120 i.e. $10/month
                    PreTax = amount.RoundToMultiples(120.0)
                };
            }
        }

        public override string ToString() => $"Pre-Tax Drawdown | Tax-optimized amortization toward zero balance | Assumptions: {VPW.FutureROI:P1} future ROI, {Params.InflationRate:P1} future inflation{GuardrailsToString}";

        string GuardrailsToString => (VPW.Floor.HasValue || VPW.Ceiling.HasValue) 
            ? $" | Guardrails: [{VPW.Floor?.ToString("C0") ?? "None"} - {VPW.Ceiling?.ToString("C0") ?? "None"}] adjusted for inflation" 
            : string.Empty;
    }
}
