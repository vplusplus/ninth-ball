
namespace NinthBall.Core
{
    /// <summary>
    /// Can calculate effective tax rate using tax rate schedule.
    /// </summary>
    public static class TaxRateCalculator
    {
        /// <summary>
        /// Returns effective tax rate (NOT the tax amount).
        /// Multiply with income to get the tax-amount.
        /// </summary>
        public static (double MarginalTaxRate, double TaxAmount) CalculateStackedEffectiveTax(this TaxRateSchedule TS, double incrementalIncome, double baseIncome = 0.0)
        {
            // if (incrementalIncome <= 0) return (0.0, 0.0, 0.0);
            incrementalIncome = Math.Max(0, incrementalIncome);

            double tax = 0;
            double lower = baseIncome;
            double upper = baseIncome + incrementalIncome;
            double marginalRate = 0.0;

            for (int i = 0; i < TS.Brackets.Count; i++)
            {
                double currentThreshold = TS.Brackets[i].Threshold;
                double currentRate = marginalRate = TS.Brackets[i].MTR;

                // Next threshold defines the boundary; last bracket goes to infinity
                double nextThreshold = (i + 1 < TS.Brackets.Count)
                    ? TS.Brackets[i + 1].Threshold
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
                TaxAmount:       tax
            );
        }

        /// <summary>
        /// Can index the tax schedule using inflation rate multiplier.
        /// </summary>
        public static TaxRateSchedule Inflate(this TaxRateSchedule TS, double inflationMultiplier, double jitterGuard, bool inflateDeductions = true)
        {
            if (inflationMultiplier <= 0) throw new ArgumentOutOfRangeException(nameof(inflationMultiplier), "Multiplier must be positive.");
            if (inflationMultiplier > 10.0) throw new ArgumentOutOfRangeException($"Inflation multiplier too large | Check logic and math | Expecting < 10.0 | Received {inflationMultiplier:F2}");
            if (jitterGuard < 10.0 || jitterGuard > 100.0) throw new ArgumentOutOfRangeException($"Invalid jitterGuard | Expecting  between $10.0 and $100.0 | Received {jitterGuard:C2}");

            // Optimization 1: InflationMultiplier 1.0 means no indexing.
            if (1.0 == inflationMultiplier) return TS;

            // Optimization 2: Flat tax rate schedule. Zero to Infinite range. Nothing to index.
            if (1 == TS.Brackets.Count && 0 == TS.Brackets[0].Threshold) return TS;

            // Adjust thresholds for inflation.
            int n = TS.Brackets.Count;
            var inflatedBrackets = new List<TaxRateSchedule.TaxBracket>(n);

            for (int i = 0; i < n; i++)
            {
                var b = TS.Brackets[i];

                inflatedBrackets.Add(new TaxRateSchedule.TaxBracket
                (
                    Threshold: InflateAndReduceJitter(amount: b.Threshold, multiplier: inflationMultiplier, jitterGuard: jitterGuard),
                    MTR: b.MTR
                ));
            }

            // Adjust tax deductions for inflation.
            var inflatedTaxDeductions = inflateDeductions
                ? InflateAndReduceJitter(TS.TaxDeductions, multiplier: inflationMultiplier, jitterGuard)
                : TS.TaxDeductions;

            // Return the inflated tax rate schedule.
            return new TaxRateSchedule(inflatedTaxDeductions, inflatedBrackets);

            // WHY?
            // We do not want 30,000 schedules (30 years x 10,000 iteration-paths)
            // IRS also rounds up the thresholds.
            // Our objective is not to faithfully reproduce IRS behavior.
            // Our objective is to reduce jitter across iteration paths. 
            static double InflateAndReduceJitter(double amount, double multiplier, double jitterGuard)
            {
                var newThreshold = amount * multiplier;
                var newThresholdLowJitter = Math.Round(newThreshold / jitterGuard) * jitterGuard;
                return newThresholdLowJitter;
            }
        }
    }
}
