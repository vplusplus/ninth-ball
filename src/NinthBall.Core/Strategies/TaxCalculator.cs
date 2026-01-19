

namespace NinthBall.Core.Strategies
{

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


    }
}
