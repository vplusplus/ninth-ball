
namespace NinthBall.Core
{
    /// <summary>
    /// Can calculate effective tax rate using tax rate schedule.
    /// </summary>
    public static class TaxRateCalculator
    {
        extension(TaxRateSchedule TS)
        {
            /// <summary>
            /// Returns effective tax rate (NOT the tax amount).
            /// Multiply with income to get the tax-amount.
            /// </summary>
            public (double MarginalTaxRate, double TaxRate, double TaxAmount) CalculateStackedEffectiveTaxRate(double incrementalIncome, double baseIncome = 0.0)
            {
                // if (incrementalIncome <= 0) return (0.0, 0.0, 0.0);
                incrementalIncome = Math.Max(0, incrementalIncome);

                double tax = 0;
                double lower = baseIncome;
                double upper = baseIncome + incrementalIncome;
                double marginalRate = 0.0;

                for (int i = 0; i < TS.Brackets.Count; i++)
                {
                    double currentThreshold = TS.Brackets[i].IncomeThreshold;
                    double currentRate = marginalRate = TS.Brackets[i].MarginalRate;

                    // Next threshold defines the boundary; last bracket goes to infinity
                    double nextThreshold = (i + 1 < TS.Brackets.Count)
                        ? TS.Brackets[i + 1].IncomeThreshold
                        : double.PositiveInfinity;

                    // Calculate overlap of [lower, upper] with [currentThreshold, nextThreshold]
                    double bracketStart = Math.Max(lower, currentThreshold);
                    double bracketEnd = Math.Min(upper, nextThreshold);

                    if (bracketEnd > bracketStart)
                    {
                        tax += (bracketEnd - bracketStart) * currentRate;
                    }

                    // Optimization: if we've covered the entire 'upper' range, we can stop
                    if (upper <= nextThreshold) break;
                }

                return 
                (
                    MarginalTaxRate: marginalRate,
                    TaxRate:         incrementalIncome < 0.01 ? 0.0 : tax / incrementalIncome,
                    TaxAmount:       tax
                );
            }

            /// <summary>
            /// Can index the tax schedule using inflation rate multiplier.
            /// </summary>
            public TaxRateSchedule Inflate(double inflationMultiplier)
            {
                if (inflationMultiplier <= 0) throw new ArgumentOutOfRangeException(nameof(inflationMultiplier), "Multiplier must be positive.");

                int n = TS.Brackets.Count;
                var inflatedBrackets = new List<TaxRateSchedule.TaxBracket>(n);

                for (int i = 0; i < n; i++)
                {
                    var b = TS.Brackets[i];

                    inflatedBrackets.Add(new TaxRateSchedule.TaxBracket
                    (
                        IncomeThreshold: b.IncomeThreshold * inflationMultiplier,
                        MarginalRate: b.MarginalRate
                    ));
                }

                return new TaxRateSchedule(inflatedBrackets);
            }
        }
    }
}
