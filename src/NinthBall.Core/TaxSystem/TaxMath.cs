
namespace NinthBall.Core
{
    public readonly record struct Taxes(Taxes.Gross GrossIncome, Taxes.Fed FederalTax, Taxes.State StateTax)
    {
        public readonly record struct Gross(double OrdInc, double INT, double DIV, double CapGain) { public readonly double Total => OrdInc + INT + DIV + CapGain; }
        public readonly record struct Fed(double Deductions, double Taxable, double MarginalRateOrdInc, double MarginalRateCapGain, double Tax) { public readonly double TaxPct => Taxable < 0.01 ? 0.0 : Tax / Taxable; }
        public readonly record struct State(double Deductions, double Taxable, double MarginalRate, double Tax) { public readonly double TaxPct => Taxable < 0.01 ? 0.0 : Tax / Taxable; }
        public readonly double Total => FederalTax.Tax + StateTax.Tax;
        public readonly double EffectiveRate => GrossIncome.Total < 0.01 ? 0.0 : Total / GrossIncome.Total;

    }

    public static class TaxMath
    {
        public static Taxes ComputePriorYearTaxes(this SimYear py, TaxRateSchedule fedTaxRates, TaxRateSchedule fedCapGainTaxRates, TaxRateSchedule stateTaxRates, double fedDeductions, double stateDeduction)
        {
            // Collect Gross Income
            Taxes.Gross gross = new
            (
                // 100% of 401K (PreTax) withdrawal is OrdInc.
                //  85% of Social Security is taxable as OrdInc (Conservative)
                // 100% of Annuity is taxable as OrdInc (Conservative)
                OrdInc: py.Withdrawals.PreTax + py.Incomes.SS * 0.85 + py.Incomes.Ann,

                // 401K bonds-yield are not taxable (tax deferred)
                // 100% of PostTax bonds-yield is taxable (Conservative - Cash basis not tracked)
                INT: py.Jan.PostTax.BondsAmount * SimConfig.TypicalBondCouponYield,

                // 401K Dividends are not taxable (tax deferred)
                // 100% of PostTax Dividends are taxable (tax deferred)
                DIV: py.Jan.PostTax.StocksAmount * SimConfig.TypicalStocksDividendYield,

                // 401K Capital gains are not taxable (tax deferred)
                // 100% of PostTax Capital gains are taxable (Conservative - Cash basis not tracked)
                CapGain: py.Withdrawals.PostTax
            );

            // Each component can't be negative.
            // Losses on one bucket doesn't offset gains on others.
            gross = new
            (
                OrdInc:  Math.Max(0, gross.OrdInc),
                INT:     Math.Max(0, gross.INT),
                DIV:     Math.Max(0, gross.DIV),
                CapGain: Math.Max(0, gross.CapGain)
            );


            return new Taxes
            (
                GrossIncome: gross,
                FederalTax: ComputeFederalTaxes(gross, fedDeductions, fedTaxRates, fedCapGainTaxRates),
                StateTax: ComputeStateTaxes(gross, stateDeduction, stateTaxRates)
            );


            static Taxes.Fed ComputeFederalTaxes(Taxes.Gross grossIncome, double standardDeductions, TaxRateSchedule fedTaxRates, TaxRateSchedule longTermCapGainsTaxRates)
            {
                // Take standard deductions...
                double remainingDeduction = standardDeductions;
                double taxableOrdInc  = TryReduce(ref remainingDeduction, grossIncome.OrdInc);
                double taxableINT     = TryReduce(ref remainingDeduction, grossIncome.INT);
                double taxableDIV     = TryReduce(ref remainingDeduction, grossIncome.DIV);
                double taxableCapGain = TryReduce(ref remainingDeduction, grossIncome.CapGain);

                // Consult tax brackets. Compute tax.
                var taxOnOrdInc   = fedTaxRates.CalculateStackedEffectiveTaxRate(taxableOrdInc + taxableINT);
                var taxcOnCapGain = longTermCapGainsTaxRates.CalculateStackedEffectiveTaxRate(taxableDIV + taxableCapGain, baseIncome: taxableOrdInc + taxableINT);

                return new Taxes.Fed
                (
                    Deductions:          standardDeductions,
                    Taxable:             taxableOrdInc + taxableINT + taxableDIV + taxableINT,
                    MarginalRateOrdInc:  taxOnOrdInc.MarginalTaxRate,
                    MarginalRateCapGain: taxcOnCapGain.MarginalTaxRate,
                    Tax:                 taxOnOrdInc.TaxAmount + taxcOnCapGain.TaxAmount
                );

                static double TryReduce(ref double remaining, in double source)
                {
                    var reduction = Math.Max(0, Math.Min(remaining, source));
                    remaining -= reduction;
                    return source - reduction;
                }
            }

            static Taxes.State ComputeStateTaxes(Taxes.Gross grossIncome, double stateDeductions, TaxRateSchedule stateTaxRates)
            {
                var taxable = Math.Max(0, grossIncome.Total - stateDeductions);

                var stateTaxes = stateTaxRates.CalculateStackedEffectiveTaxRate(taxable);

                return new Taxes.State
                (
                    Deductions: stateDeductions,
                    Taxable: taxable,
                    MarginalRate: stateTaxes.MarginalTaxRate,
                    Tax: stateTaxes.TaxAmount
                );
            }

        }

    }
}


/*
         public static Taxes Calculate(SimYear py, TaxRateSchedule fedSched, TaxRateSchedule fedLtcgSched, TaxRateSchedule stateSched, double fedMult, double stateMult, double baseFedDeduction, double baseStateExemption)
        {
            // 1. Collect Gross Income
            var gross = new Taxes.GrossInc(
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
                TaxBreakdown: new Taxes.GrossInc(resFedOrd.TaxAmount, 0, 0, resFedLtcg.TaxAmount) // Simplification: Fed INT/DIV taxes often bundled with Ord
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
                TaxBreakdown: new Taxes.GrossInc(resState.TaxAmount, 0, 0, 0)
            );

            // 4. Return Composed Taxes
            return new Taxes(gross, federal, state).RoundToCents();
        }
*/