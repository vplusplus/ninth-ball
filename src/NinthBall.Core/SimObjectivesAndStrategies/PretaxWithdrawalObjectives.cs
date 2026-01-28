
namespace NinthBall.Core
{
    [StrategyFamily(StrategyFamily.Withdrawals)]
    sealed class FixedWithdrawalObjective(FixedWithdrawal FW) : ISimObjective
    {
        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(FW);

        sealed class Strategy(FixedWithdrawal FW) : ISimStrategy
        {
            double from401K = 0;

            void ISimStrategy.Apply(ISimState ctx)
            {
                if (0 == ctx.YearIndex)
                {
                    from401K = ctx.Jan.PreTax.Amount * FW.FirstYearPct;
                }
                else if (null != FW.ResetAtAge &&  FW.ResetAtAge.Count > 0 && FW.ResetAtAge.Contains(ctx.Age))
                {
                    from401K = ctx.Jan.PreTax.Amount * FW.FirstYearPct;
                }
                else
                {
                    from401K *= 1 + FW.Increment;
                }

                ctx.Withdrawals = ctx.Withdrawals with
                {
                    // Adjust to multiples of $120 i.e. $10/month
                    PreTax = from401K.RoundToMultiples(120.0)
                };
            }
        }

        public override string ToString() => $"PreTax Withdrawal | {FW.FirstYearPct:P1} of PreTax (+{FW.Increment:P1}/yr){ResetYearsToString}";
        string ResetYearsToString => (null == FW.ResetAtAge || 0 == FW.ResetAtAge.Count) ? string.Empty : $" | Reset to {FW.FirstYearPct:P1} @ age [{string.Join(',', FW.ResetAtAge)}]";
    }

    [StrategyFamily(StrategyFamily.Withdrawals)]
    sealed class VariableWithdrawalObjective(SimParams Params, VariableWithdrawal VW) : ISimObjective
    {
        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(Params, VW);

        sealed class Strategy(SimParams P, VariableWithdrawal VW) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimState ctx)
            {
                int remainingYears = P.NoOfYears - ctx.YearIndex;
                
                // Calculate the "ideal" withdrawal to hit zero at the end of the horizon.
                double amount = Stats.EquatedWithdrawal(
                    currentBalance:     ctx.Jan.PreTax.Amount, 
                    estimatedROI:       VW.FutureROI, 
                    estimatedInflation: VW.FutureInflation, 
                    remainingYears:     remainingYears
                );

                // Apply guardrails (adjusted for inflation)
                double inflationMultiplier = 0 == ctx.YearIndex ? 1.0 : ctx.PriorYear.Metrics.InflationMultiplier;

                if (VW.Floor.HasValue)
                {
                    double currentFloor = VW.Floor.Value * inflationMultiplier;
                    amount = Math.Max(amount, currentFloor);
                }

                if (VW.Ceiling.HasValue)
                {
                    double currentCeiling = VW.Ceiling.Value * inflationMultiplier;
                    amount = Math.Min(amount, currentCeiling);
                }

                ctx.Withdrawals = ctx.Withdrawals with
                {
                    // Adjust to multiples of $120 i.e. $10/month
                    PreTax = amount.RoundToMultiples(120.0)
                };
            }
        }

        public override string ToString() => $"PreTax Withdrawal | Tax-optimized amortization toward zero balance | Assumptions: {VW.FutureROI:P1} future ROI, {VW.FutureInflation:P1} future inflation{GuardrailsToString}";

        string GuardrailsToString => (VW.Floor.HasValue || VW.Ceiling.HasValue) 
            ? $" | Guardrails: [{VW.Floor?.ToString("C0") ?? "None"} - {VW.Ceiling?.ToString("C0") ?? "None"}] adjusted for inflation" 
            : string.Empty;
    }
}
