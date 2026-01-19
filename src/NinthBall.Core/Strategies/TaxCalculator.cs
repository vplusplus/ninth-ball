

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

            // 4. Calculate NJ taxable amounts (all ordinary for NJ)
            var njTaxable = new Taxable
            (
                OrdInc: Math.Max(0, incomeNominal.OrdInc - 2 * TaxRateSchedules.NJPersonalExemption2026),
                DIV:    incomeNominal.DIV,
                INT:    incomeNominal.INT,
                LTCG:   incomeNominal.LTCG
            );

            // 5. Calculate NJ effective tax rate on combined income
            double njEffectiveRate = njBracketsNominal.CalculateStackedEffectiveTaxRate(
                njTaxable.OrdInc + njTaxable.DIV + njTaxable.INT + njTaxable.LTCG
            );

            // 6. Calculate NJ taxes
            var njTax = new Tax
            (
                OrdInc: njTaxable.OrdInc * njEffectiveRate,
                DIV: njTaxable.DIV * njEffectiveRate,
                INT: njTaxable.INT * njEffectiveRate,
                LTCG: njTaxable.LTCG * njEffectiveRate
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

                foreach (var (threshold, rate) in TS.Brackets)
                {
                    if (upper <= threshold)
                    {
                        tax += Math.Max(0, upper - lower) * rate;
                        break;
                    }

                    if (lower < threshold)
                    {
                        tax += Math.Max(0, threshold - lower) * rate;
                        lower = threshold;
                    }
                }

                if (upper > TS.Brackets[^1].IncomeThreshold)
                {
                    tax += (upper - Math.Max(lower, TS.Brackets[^1].IncomeThreshold))
                           * TS.Brackets[^1].MarginalRate;
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

