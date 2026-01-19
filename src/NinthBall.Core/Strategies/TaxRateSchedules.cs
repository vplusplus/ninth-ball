
namespace NinthBall.Core
{
    
    /// <summary>
    /// Describes the tiered tax rate schedule.
    /// </summary>
    public readonly record struct TaxRateSchedule(IReadOnlyList<TaxRateSchedule.TaxBracket> Brackets)
    {
        public readonly record struct TaxBracket(double IncomeThreshold, double MarginalRate);
    }

    /// <summary>
    /// Can calculate effective tax rate using tax rate schedule.
    /// </summary>
    public static class TaxRateScheduleCalculator
    {
        extension(TaxRateSchedule TS)
        {
            /// <summary>
            /// Returns effective tax rate (NOT the tax amount).
            /// Multiply with income to get the tax-amount.
            /// </summary>
            public double CalculateStackedEffectiveTaxRate(double incrementalIncome, double baseIncome = 0.0)
            {
                if (incrementalIncome <= 0) return 0;

                double tax = 0;
                double lower = baseIncome;
                double upper = baseIncome + incrementalIncome;

                for (int i = 0; i < TS.Brackets.Count; i++)
                {
                    double currentThreshold = TS.Brackets[i].IncomeThreshold;
                    double currentRate = TS.Brackets[i].MarginalRate;

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

                return tax / incrementalIncome;
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
    
    /// <summary>
    /// Known tax schedules. 
    /// TODO: Use Lazy pattern, read from optional-config, fall back to hardcoded defaults
    /// </summary>
    public static class TaxRateSchedules
    {
        public const double FedStdDeduction2026 = 32200.0;          // Married Filing Jointly, 2026 example
        public const double NJPersonalExemption2026 = 1500.0;       // Simplified per person, MFJ 2 persons = 3000

        /// <summary>
        /// 2026 Federal Income Tax Brackets for Married Filing Jointly.
        /// Values updated per IRS 2026 inflation adjustments.
        /// </summary>
        public static readonly TaxRateSchedule Federal2026Joint = new
        ([
            new (0, 0.10),
            new (24800, 0.12),
            new (100800, 0.22),
            new (211400, 0.24),
            new (403550, 0.32),
            new (512450, 0.35),
            new (768700, 0.37)
        ]);

        /// <summary>
        /// 2026 Long-Term Capital Gains Brackets for Married Filing Jointly.
        /// </summary>
        public static readonly TaxRateSchedule FedLTCG2026Joint = new
        ([
            new (0, 0.0),
            new (98900, 0.15),   // Updated from 89,250
            new (613700, 0.20)   // Updated from 553,850
        ]);

        /// <summary>
        /// 2026 New Jersey Gross Income Tax Brackets for Married Filing Jointly.
        /// </summary>
        public static readonly TaxRateSchedule NJ2026Joint = new
        ([
            new (0, 0.014),
            new (20000, 0.0175),
            new (50000, 0.0245),
            new (70000, 0.035),
            new (80000, 0.05525),
            new (150000, 0.0637),
            new (500000, 0.0897),
            new (1000000, 0.1075)
        ]);
    }

}
