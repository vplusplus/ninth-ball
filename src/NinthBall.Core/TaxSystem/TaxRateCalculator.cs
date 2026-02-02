
namespace NinthBall.Core
{
    /// <summary>
    /// Can calculate marginal tax rate and the tax amount using tax rate schedule.
    /// </summary>
    public static class TaxRateCalculator
    {
        /// <summary>
        /// Returns the marginal tax rate, indexed marginal tax rate threshold and the tax amount.
        /// </summary>
        public static (double MarginalTaxRate, double MarginalTaxThreshold, double TaxAmount) CalculateStackedEffectiveTax(this TaxRateSchedule TS, double incrementalIncome, double baseIncome = 0.0)
        {
            // Safety first...
            incrementalIncome = Math.Max(0, incrementalIncome);
            baseIncome = Math.Max(0, baseIncome);

            double tax = 0;
            double lower = baseIncome;
            double upper = baseIncome + incrementalIncome;

            double marginalRate = 0.0;
            double marginalRateThreshold = 0.0;

            for (int i = 0; i < TS.Brackets.Count; i++)
            {
                double currentThreshold = marginalRateThreshold = TS.Brackets[i].Threshold;
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
                MarginalTaxRate:        marginalRate,
                MarginalTaxThreshold:   marginalRateThreshold,
                TaxAmount:              tax
            );
        }

        /// <summary>
        /// Can index the tax schedule using inflation rate multiplier.
        /// </summary>
        public static TaxRateSchedule Inflate(this TaxRateSchedule TS, double inflationMultiplier, double jitterGuard)
        {
            if (inflationMultiplier <= 0) throw new ArgumentOutOfRangeException(nameof(inflationMultiplier), "Multiplier must be positive.");
            if (inflationMultiplier > 20.0) throw new ArgumentOutOfRangeException($"Inflation multiplier too large | Check logic and math | Expecting < 20.0 | Received {inflationMultiplier:F2}");
            if (jitterGuard < 10.0 || jitterGuard > 100.0) throw new ArgumentOutOfRangeException($"Invalid jitterGuard | Expecting  between $10.0 and $100.0 | Received {jitterGuard:C2}");

            // Optimization 1: InflationMultiplier 1.0 means no indexing.
            if (1.0 == inflationMultiplier) return TS;

            // Optimization 2: Nothing to index.
            if (TS.DoNotIndexTaxBrackets && TS.DoNotIndexTaxDeductions) return TS;

            // Adjust thresholds for inflation (if allowed).
            var inflatedBrackets = TS.DoNotIndexTaxBrackets
                ? TS.Brackets  // Keep original brackets
                : InflateBracketsAndReduceJitter(TS.Brackets, inflationMultiplier, jitterGuard);

            // Adjust tax deductions for inflation (if allowed).
            var inflatedTaxDeductions = TS.DoNotIndexTaxDeductions
                ? TS.TaxDeductions  // Keep original deductions
                : InflateAmountAndReduceJitter(TS.TaxDeductions, multiplier: inflationMultiplier, jitterGuard);

            // Return the inflated tax rate schedule.
            return TS with
            {
                TaxDeductions = inflatedTaxDeductions,
                Brackets      = inflatedBrackets,
            };

            // Inflate the tax-bracket thresholds.
            // Retain the marginal tax rate.
            static IReadOnlyList<TaxRateSchedule.TaxBracket> InflateBracketsAndReduceJitter(IReadOnlyList<TaxRateSchedule.TaxBracket> brackets, double multiplier, double jitterGuard)
            {
                var inflatedBrackets = new List<TaxRateSchedule.TaxBracket>(brackets.Count);
                for (int i = 0; i < brackets.Count; i++)
                {
                    var b = brackets[i];
                    inflatedBrackets.Add(new TaxRateSchedule.TaxBracket
                    (
                        Threshold: InflateAmountAndReduceJitter(amount: b.Threshold, multiplier: multiplier, jitterGuard: jitterGuard),
                        MTR: b.MTR
                    ));
                }
                return inflatedBrackets;
            }

            // WHY?
            // We do not want 30,000 schedules (30 years x 10,000 iteration-paths)
            // Just so you know, IRS also rounds up the thresholds.
            // Faithfully reproducing IRS behavior is NOT our intention.
            // Our objective is to reduce jitter across iteration paths. 
            static double InflateAmountAndReduceJitter(double amount, double multiplier, double jitterGuard)
            {
                var inflatedAmount = amount * multiplier;
                var inflatedAmountLowJitter = Math.Round(inflatedAmount / jitterGuard) * jitterGuard;
                return inflatedAmountLowJitter;
            }
        }
    }
}
