
namespace NinthBall.Core
{
    /// <summary>
    /// New Jersey state tax guesstimator - applies NJ-specific rules.
    /// </summary>
    public sealed class NJTaxGuesstimator() : ITaxAuthority
    {
        const double HundredPCT = 1.00;
        const double TenDollars = 10.00;

        public Taxes.Tx GuesstimateTaxes(PYEarnings pyEarnings, InflationIndex inflationIndex, TaxRateSchedules Year0TaxRates)
        {
            //......................................................
            // NEW JERSEY STATE TAX LOGIC
            //......................................................
            // 1. Social Security is 100% exempt from NJ Gross Income.
            // 2. Pension and Retirement Income Exclusion (For Age 62+)
            //......................................................

            // NJ Gross Income for eligibility check (Ignore SS)
            // Determine exclusion based on age and income tiers
            double njGrossIncome = pyEarnings.PreTaxWDraw + pyEarnings.Ann + pyEarnings.BondsYield + pyEarnings.Dividends + pyEarnings.CapGains;
            double pensionExclusion = GetNJPensionExclusionWithTheCliff(njGrossIncome, pyEarnings.Age);

            // Apply pension exclusion specifically to retirement income (401k/IRA/Annuity/Pension)
            double retirementIncomes = pyEarnings.PreTaxWDraw + pyEarnings.Ann;
            double taxableRetirement = Math.Max(0, retirementIncomes - pensionExclusion);

            // NJ Taxable Income = Taxable Retirement + Investment Income (INT/DIV/LTCG)
            // Note: Simplification - Treats all BondsYield as taxable interest and Dividends/CapGains as taxable.
            double njTaxableSum = taxableRetirement + pyEarnings.BondsYield + pyEarnings.Dividends + pyEarnings.CapGains;

            // Quantize the inflation rate multipliers to avoid 30,000 jitters
            // Inflate tax rates. For NJ, exemptions are not to be indexed (see configuration)
            var inflationMultiplier = Math.Round(inflationIndex.State, 4);
            var taxRates = Year0TaxRates.State.Inflate(inflationMultiplier, jitterGuard: TenDollars);

            // Apply Individual Exemptions and Property Tax deductions (configured as StateDeductions)
            var taxableTerminal = Math.Max(0, njTaxableSum - taxRates.TaxDeductions);

            // Apply progressive brackets
            var stateTaxes = taxRates.CalculateStackedEffectiveTax(taxableTerminal);

            return new Taxes.Tx
            (
                Gross:          njGrossIncome,
                Deductions:     pensionExclusion + taxRates.TaxDeductions,
                Taxable:        taxableTerminal,
                MTR:            stateTaxes.MarginalTaxRate,
                MTT:            stateTaxes.MarginalTaxThreshold,
                MTRCapGain:     stateTaxes.MarginalTaxRate,         // Rates are the same for OrdInc and LTCG
                MTTCapGain:     stateTaxes.MarginalTaxThreshold,    // Thesholds are the same for OrdInc and LTCG
                Tax:            stateTaxes.TaxAmount
            );
        }

        static double GetNJPensionExclusionWithTheCliff(double njGrossIncome, int age)
        {
            // NJ STATUTORY POLICY (Pinned to NJ-1040 Law)
            // These are not hard-coded constants, rather a representation of NJ-1040 Law.
            // If the law changes the application of these numbers are also likely to change.
            // Consider these numbers as code, hence are not externalized.

            // Age trigger for Pension Exclusion
            const int NJRetirementAge = 62;

            // Staggered Exclusion Tiers (Phased-out by Gross Income)
            const double NJPensionExclusionFullLimit    = 100000.0; // Income <= $100k 
            const double NJPensionExclusionHalfLimit    = 125000.0; // Income <= $125k
            const double NJPensionExclusionQuarterLimit = 150000.0; // Income <= $150k

            // Allowed exclusion by tier limits.
            const double NJTier1PensionExclusion = 100000.0; // Full Exclusion (MFJ)
            const double NJTier2PensionExclusion =  50000.0; // 50% Exclusion
            const double NJTier3PensionExclusion =  25000.0; // 25% Exclusion
            const double NJNoPensionExclusion    =      0.0; // The CLIFF

            if (age < NJRetirementAge) return 0;

            return njGrossIncome switch
            {
                <= NJPensionExclusionFullLimit => NJTier1PensionExclusion,
                <= NJPensionExclusionHalfLimit => NJTier2PensionExclusion,
                <= NJPensionExclusionQuarterLimit => NJTier3PensionExclusion,
                _ => NJNoPensionExclusion
            };
        }
    }
}
