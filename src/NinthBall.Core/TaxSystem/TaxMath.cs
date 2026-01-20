
namespace NinthBall.Core
{
    /// <summary>
    /// Pure functional engine for tax calculations.
    /// Coordinates between Federal and State jurisdictions.
    /// </summary>
    public static class TaxMath
    {
        public static Taxes Calculate(SimYear py, TaxRateSchedule fedSched, TaxRateSchedule fedLtcgSched, TaxRateSchedule stateSched, double fedMult, double stateMult, double baseFedDeduction, double baseStateExemption)
        {
            // 1. Collect Gross Income
            var gross = new Taxes.Inc(
                OrdInc: py.Withdrawals.PreTax + py.Incomes.SS * 0.85 + py.Incomes.Ann,
                INT:    py.Jan.PostTax.Amount * (1 - py.Jan.PostTax.Allocation) * SimConfig.TypicalBondCouponYield,
                DIV:    py.Jan.PostTax.Amount * py.Jan.PostTax.Allocation * SimConfig.TypicalStocksDividendYield,
                LTCG:   py.Withdrawals.PostTax
            );

            // 2. Federal Jurisdiction
            var fedDeduction = baseFedDeduction * fedMult; 
            var fedTaxableOrd = Math.Max(0, gross.OrdInc + gross.INT - fedDeduction);
            
            var resFedOrd = fedSched.Inflate(fedMult).CalculateStackedEffectiveTaxRate(fedTaxableOrd);
            var resFedLtcg = fedLtcgSched.Inflate(fedMult).CalculateStackedEffectiveTaxRate(gross.DIV + gross.LTCG, baseIncome: fedTaxableOrd);

            var federal = new Taxes.TD(
                Deduction:  fedDeduction,
                Taxable:    fedTaxableOrd + gross.DIV + gross.LTCG,
                MarginalRate: new Taxes.TR(resFedOrd.MarginalTaxRate, resFedLtcg.MarginalTaxRate),
                Tax:        resFedOrd.TaxAmount + resFedLtcg.TaxAmount,
                TaxBreakdown: new Taxes.Inc(resFedOrd.TaxAmount, 0, 0, resFedLtcg.TaxAmount) // Simplification: Fed INT/DIV taxes often bundled with Ord
            );

            // 3. State Jurisdiction (NJ Fallback)
            var stateExemption = baseStateExemption; 
            var stateTaxable = Math.Max(0, gross.Total - stateExemption);
            var resState = stateSched.Inflate(stateMult).CalculateStackedEffectiveTaxRate(stateTaxable);

            var state = new Taxes.TD(
                Deduction:  stateExemption,
                Taxable:    stateTaxable,
                MarginalRate: new Taxes.TR(resState.MarginalTaxRate, 0.0),
                Tax:        resState.TaxAmount,
                TaxBreakdown: new Taxes.Inc(resState.TaxAmount, 0, 0, 0)
            );

            // 4. Return Composed Taxes
            return new Taxes(gross, federal, state).RoundToCents();
        }
    }
}
