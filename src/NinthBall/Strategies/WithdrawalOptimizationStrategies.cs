

namespace NinthBall
{
    /// <summary>
    /// Utilize buffer cash to cover withdrawals in bad years (prior year growth below threshold).
    /// </summary>
    sealed class UseBufferCashAfterBadYears(SimConfig simConfig) : ISimObjective
    {
        readonly SimConfig C = simConfig;
        readonly UseBufferCash P = simConfig.UseBufferCash;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(P.Amount, P.GrowthThreshold);

        sealed class Strategy(double initialBufferCash, double growthThreshold) : ISimStrategy
        {
            double availableBufferCash = initialBufferCash;

            void ISimStrategy.Apply(ISimContext context)
            {
                // First year, nothing to adjust.
                if (context.PriorYears.Count == 0) return;
                if (availableBufferCash <= 0) return;

                // If prior year growth is below suggested threshold.
                var badPriorYear = context.PriorYears[^1].IsBadYear(growthThreshold);

                // If prior year was bad, draw from buffer cash.
                if (badPriorYear)
                {
                    // In real life, we will drain buffer slower, since it is probably tax-free.
                    // Do not adjust for tax, let it be a cushion.
                    var fromBuffer = Math.Min(availableBufferCash, context.WithdrawalAmount);

                    // Take from buffer, reduce planned withdrawal.
                    availableBufferCash -= fromBuffer;
                    context.WithdrawalAmount -= fromBuffer;
                }
            }
        }

        public override string ToString() => P.ToString();
    }

    /// <summary>
    /// Reduce withdrawal by a percentage if prior year was bad, only up to maxSkips times before cutOffYear.
    /// </summary>
    class ReduceWithdrawalAfterBadYears(SimConfig simConfig) : ISimObjective
    {
        readonly SimConfig C = simConfig;
        readonly ReduceWithdrawal P = simConfig.ReduceWithdrawal;

        ISimStrategy ISimObjective.CreateStrategy(int _) => new Strategy(
            maxSkips: P.MaxSkips,
            cutOffYear: P.CutOffYear,
            minGrowthPct: P.GrowthThreshold,
            reductionPct: P.ReductionPct
        );

        sealed class Strategy(int maxSkips, int cutOffYear, double minGrowthPct, double reductionPct) : ISimStrategy
        {
            int skipCount = 0;

            void ISimStrategy.Apply(ISimContext context)
            {
                // On first year, nothing to adjust.
                if (context.PriorYears.Count == 0) return;

                // If we have no skips left, stop skipping.
                if (skipCount >= maxSkips) return;

                // If we have reached the cutoff year, stop skipping.
                if (context.YearIndex >= cutOffYear) return;

                // If prior year growth is below suggested threshold.
                var badPriorYear = context.PriorYears[^1].IsBadYear(minGrowthPct);

                // If prior year was bad, reduce withdrawal by reductionPct.
                if (badPriorYear)
                {
                    skipCount += 1;
                    context.WithdrawalAmount *= (1.0 - reductionPct);
                }
            }
        }

        public override string ToString() => P.ToString();
    }
}
