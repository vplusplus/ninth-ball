

namespace NinthBall.Core.Strategies
{
    public readonly record struct TaxRateSchedule(IReadOnlyList<TaxRateSchedule.TaxBracket> Brackets)
    {
        public readonly record struct TaxBracket(double IncomeThreshold, double MarginalRate);
    }

    public static class TaxRateSchedules 
    {
        public const double FedStdDeduction2026     = 32200.0;   // Married Filing Jointly, 2026 example
        public const double NJPersonalExemption2026 = 1500.0;      // Simplified per person, MFJ 2 persons = 3000

        /// <summary>
        /// 2026 Federal Income Tax Brackets for Married Filing Jointly.
        /// Values updated per IRS 2026 inflation adjustments.
        /// </summary>
        public static readonly TaxRateSchedule Federal2026Joint = new
        ([
            new (0, 0.10),
            new (24800, 0.12),   // Updated from 22,000
            new (100800, 0.22),  // Updated from 89,450
            new (211400, 0.24),  // Updated from 190,750
            new (403550, 0.32),  // Updated from 364,200
            new (512450, 0.35),  // Updated from 462,500
            new (768700, 0.37)   // Updated from 693,750
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

    public static class TaxCalculator
    {
        public static (Tax FedTax, Tax StateTax) CalculateTaxes
        (
            Taxable incomeNominal,                      // Income nuckets (nominal)
            double federalInflationMultiplier,          // A running multiplier that reflects inflation rate impact since year #0
            double adjustedNJTaxInflationMultiplier     // Same as Inflation multiplier, except, lags Inflation rate by some percentage (example 70%) since NJ do not index like Federal
        )
        {
            // Inflate standard deduction using same multiplier as brackets
            double fedStdDeductionNominal = TaxRateSchedules.FedStdDeduction2026 * federalInflationMultiplier;

            // 1. Inflate tax schedules
            var fedOrdBracketsNominal   = TaxRateSchedules.Federal2026Joint.Inflate(federalInflationMultiplier);
            var fedLTCGBracketsNominal  = TaxRateSchedules.FedLTCG2026Joint.Inflate(federalInflationMultiplier);
            var njBracketsNominal       = TaxRateSchedules.NJ2026Joint.Inflate(adjustedNJTaxInflationMultiplier);

            // 2. Calculate Federal effective tax rates
            double fedOrdIncBase = Math.Max(0, incomeNominal.OrdInc + incomeNominal.INT - fedStdDeductionNominal);
            double fedOrdIncRate = fedOrdBracketsNominal.CalculateStackedEffectiveTaxRate(fedOrdIncBase);

            double fedLTCGRate = fedLTCGBracketsNominal.CalculateStackedEffectiveTaxRate(
                incomeNominal.DIV + incomeNominal.LTCG,
                baseIncome: fedOrdIncBase
            );

            // 3. Calculate Federal taxes
            var fedTax = new Tax
            (
                OrdInc: incomeNominal.OrdInc * fedOrdIncRate,
                INT:    incomeNominal.INT * fedOrdIncRate,
                DIV:    incomeNominal.DIV * fedLTCGRate,
                LTCG:   incomeNominal.LTCG * fedLTCGRate
            );

            // 4. Calculate NJ taxable amounts
            double totalNJExemptions = 2 * TaxRateSchedules.NJPersonalExemption2026;
            double totalIncomeNominal = incomeNominal.OrdInc + incomeNominal.DIV + incomeNominal.INT + incomeNominal.LTCG;
            double taxableNJTotal = Math.Max(0, totalIncomeNominal - totalNJExemptions);

            // 5. Calculate NJ effective tax rate on combined income (NJ taxes all income at same graduated rates)
            double njEffectiveRate = njBracketsNominal.CalculateStackedEffectiveTaxRate(taxableNJTotal);

            // 6. Calculate NJ taxes (allocate based on total effective rate)
            var njTax = new Tax
            (
                OrdInc: incomeNominal.OrdInc * njEffectiveRate,
                DIV:    incomeNominal.DIV * njEffectiveRate,
                INT:    incomeNominal.INT * njEffectiveRate,
                LTCG:   incomeNominal.LTCG * njEffectiveRate
            );

            return (fedTax, njTax);
        }

        extension(TaxRateSchedule TS)
        {
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

            public TaxRateSchedule Inflate(double inflationMultiplier)
            {
                if (inflationMultiplier <= 0) throw new ArgumentOutOfRangeException(nameof(inflationMultiplier), "Multiplier must be positive.");

                int n = TS.Brackets.Count;
                var inflatedBrackets = new List<TaxRateSchedule.TaxBracket>(n);

                for (int i = 0; i < n; i++)
                {
                    var b = TS.Brackets[i];
                    inflatedBrackets.Add(new TaxRateSchedule.TaxBracket(
                        IncomeThreshold: b.IncomeThreshold * inflationMultiplier,
                        MarginalRate: b.MarginalRate
                    ));
                }

                return new TaxRateSchedule(inflatedBrackets);
            }


        }

    }
}

