
namespace NinthBall.Core
{
    /*
        GI      – Gross Income
        AGI     – Adjusted Gross Income

        OrdInc  – Ordinary Income (Also OI)
        INT     – Interest
        QDI     – Qualified Dividends
        LTCG    – Long term Capital Gains

        MTR     – Marginal Tax Rate
        ETTR    – Effective Tax Rates
        NIIT    – Net Investment Income Tax  (We do not do check this yet. Check impact and include)
    
        TaxPCT  = Total Taxes Paid ÷ Every $ of cash inflow (not a standard term; not a statutory tax rate)

     */

    public readonly record struct Taxes(Taxes.GI GrossIncome, Taxes.AGI AdjustedGrossIncome, Taxes.Fed FederalTax, Taxes.State StateTax)
    {
        public readonly record struct GI
        (
            double PreTaxWDraw,     // Withdrawals from tax deferred account
            double SS,              // Social security income
            double Ann,             // Annuity incomes
            double BondsYield,      // Bonds yield from PostTax accounts
            double Dividends,       // Dividends from PostTax accounts
            double CapGains         // Capital gains from PostTax accounts
        )
        {
            public readonly double Total => PreTaxWDraw + SS + Ann + BondsYield + Dividends + CapGains;
        }

        public readonly record struct AGI(double OrdInc, double INT, double QDI, double LTCG) {
            public readonly double Total => OrdInc + INT + QDI + LTCG;
        }

        public readonly record struct Fed(double StdDeduction, double Taxable, double MTR, double MTRCapGain, double Tax);
        public readonly record struct State(double StateDeduction, double Taxable, double MTR, double Tax);

        // Total taxes.
        public readonly double Total => FederalTax.Tax + StateTax.Tax;

        // PCT tax paid on adjusted gross income.
        public readonly double ETTR => AdjustedGrossIncome.Total <= 0.01 ? 0.0 : Total / AdjustedGrossIncome.Total;

        // For every $ that came in, what PCT went to taxes
        public readonly double TaxPCT => GrossIncome.Total <= 0.01 ? 0.0 : (FederalTax.Tax + StateTax.Tax) / GrossIncome.Total;

        // For every $ that came in, what PCT went to Federal taxes
        public readonly double TaxPCTFed => GrossIncome.Total <= 0.01 ? 0.0 : FederalTax.Tax / GrossIncome.Total;

        // For every $ that came in, what PCT went to State taxes
        public readonly double TaxPCTState => GrossIncome.Total <= 0.01 ? 0.0 : StateTax.Tax / GrossIncome.Total;
    }

    public static class TaxMath
    {
        const double EightyFivePCT = 0.85;
        const double HundredPCT    = 1.00;
        const double TenDollars    = 10.00;

        public static Taxes ComputePriorYearTaxes(this SimYear priorYear, TaxRateSchedule taxRatesFederal, TaxRateSchedule taxRatesLTCG, TaxRateSchedule taxRatesState, double year0StdDeductions, double year0StateExemptions)
        {
            // We do not want 30,000 tax schedules (30 years x 10000 iterations paths)
            // Quantize the inflation rate multipliers to avoid 30,000 jitters
            double inflationMultiplierFederal = Math.Round(priorYear.Metrics.FedTaxInflationMultiplier,   4);
            double inflationMultiplierState   = Math.Round(priorYear.Metrics.StateTaxInflationMultiplier, 4);

            // Adjust tax brackets for inflation 
            // Use $10.0 jitterGuard to avoid false-precision.
            taxRatesFederal = taxRatesFederal.Inflate(inflationMultiplierFederal, jitterGuard: TenDollars);
            taxRatesLTCG      = taxRatesLTCG.Inflate(inflationMultiplierFederal,    jitterGuard: TenDollars);
            taxRatesState     = taxRatesState.Inflate(inflationMultiplierState,     jitterGuard: TenDollars);

            // Adjust standard deductions and state exemptions for inflation
            // Use $10.0 jitterGuard to avoid false-precision.
            var standardDeductions = (year0StdDeductions * inflationMultiplierFederal).RoundToMultiples(TenDollars);
            var stateExemptions    = (year0StateExemptions * inflationMultiplierState).RoundToMultiples(TenDollars);

            // Collect gross incomes, arranged by taxable buckets
            var unadjustedIncomes = priorYear.RawIncomes().MinZero().RoundToCents();
            var agi = unadjustedIncomes.AdjustedGrossIncomes().MinZero().RoundToCents();

            return new Taxes
            (
                GrossIncome:         unadjustedIncomes,
                AdjustedGrossIncome: agi,
                FederalTax:          agi.ComputeFederalTaxes(standardDeductions, taxRatesFederal, taxRatesLTCG),
                StateTax:            agi.ComputeStateTaxes(stateExemptions, taxRatesState)
            );
        }

        static Taxes.GI RawIncomes(this SimYear priorYear) => new Taxes.GI
        (
            PreTaxWDraw:    priorYear.Withdrawals.PreTax,
            SS:             priorYear.Incomes.SS,
            Ann:            priorYear.Incomes.Ann,
            BondsYield:     priorYear.Jan.PostTax.BondsAmount * SimConfig.TypicalBondCouponYield,
            Dividends:      priorYear.Jan.PostTax.StocksAmount * SimConfig.TypicalStocksDividendYield,
            CapGains:       priorYear.Withdrawals.PostTax
        )
        .MinZero();

        static Taxes.AGI AdjustedGrossIncomes(this Taxes.GI inc) => new Taxes.AGI
        (
            // 100% of 401K withdrawal is ordinary income
            // Non-taxable portion of SS excluded from AGI
            // Conservative: 85% of SS is taxed - May be over stated. 
            // Conservative: 100% of Ann is taxed - Is overstated during first 7 years.
            OrdInc: (inc.PreTaxWDraw * HundredPCT) + (inc.SS * EightyFivePCT) + (inc.Ann * HundredPCT),

            // Model doesn't track cash basis.
            // Conservative: 100% of bond yields is taxable. 
            INT: inc.BondsYield,

            // Model doesn't track cash basis.
            // Conservative: Not all portions are taxable - overstated
            // Simplification: 100% of dividends are treated qualified (may understate the tax if sold < 1 year)
            QDI: inc.Dividends,

            // Model doesn't track cash basis.
            // Conservative: Not all portions are taxable - overstated
            // Simplification: 100% of capital gains treated long-term (may understate the tax if sold < 1 year)
            LTCG: inc.CapGains
        )
        .MinZero();

        static Taxes.Fed ComputeFederalTaxes(this Taxes.AGI grossIncome, double standardDeductions, TaxRateSchedule fedTaxRates, TaxRateSchedule longTermCapGainsTaxRates)
        {
            // Take standard deductions...
            double remainingDeduction = standardDeductions;
            double taxableOrdInc  = TryReduce(ref remainingDeduction, grossIncome.OrdInc);
            double taxableINT     = TryReduce(ref remainingDeduction, grossIncome.INT);
            double taxableDIV     = TryReduce(ref remainingDeduction, grossIncome.QDI);
            double taxableCapGain = TryReduce(ref remainingDeduction, grossIncome.LTCG);

            // Consult tax brackets. Compute marginal tax rate and the tax amount.
            var taxOnOrdInc   = fedTaxRates.CalculateStackedEffectiveTax(taxableOrdInc + taxableINT);
            var taxcOnCapGain = longTermCapGainsTaxRates.CalculateStackedEffectiveTax(taxableDIV + taxableCapGain, baseIncome: taxableOrdInc + taxableINT);

            return new Taxes.Fed
            (
                StdDeduction:         standardDeductions,
                Taxable:    taxableOrdInc + taxableINT + taxableDIV + taxableCapGain, 
                MTR:        taxOnOrdInc.MarginalTaxRate,
                MTRCapGain: taxcOnCapGain.MarginalTaxRate,
                Tax:        taxOnOrdInc.TaxAmount + taxcOnCapGain.TaxAmount
            );

            static double TryReduce(ref double remaining, in double source)
            {
                var reduction = Math.Max(0, Math.Min(remaining, source));
                remaining -= reduction;
                return source - reduction;
            }
        }

        static Taxes.State ComputeStateTaxes(this Taxes.AGI grossIncome, double stateDeductions, TaxRateSchedule stateTaxRates)
        {
            var taxable = Math.Max(0, grossIncome.Total - stateDeductions);

            var stateTaxes = stateTaxRates.CalculateStackedEffectiveTax(taxable);

            return new Taxes.State
            (
                StateDeduction:       stateDeductions,
                Taxable:    taxable,
                MTR:        stateTaxes.MarginalTaxRate,
                Tax:        stateTaxes.TaxAmount
            );
        }

        static Taxes.GI MinZero(this Taxes.GI x) => new
        (
            PreTaxWDraw: Math.Max(0, x.PreTaxWDraw),
            SS:         Math.Max(0, x.SS),
            Ann:        Math.Max(0, x.Ann),
            BondsYield: Math.Max(0, x.BondsYield),
            Dividends:  Math.Max(0, x.Dividends),
            CapGains:   Math.Max(0, x.CapGains)
        );

        static Taxes.AGI MinZero(this Taxes.AGI x) => new
        (
            OrdInc: Math.Max(0, x.OrdInc),
            INT:    Math.Max(0, x.INT),
            QDI:    Math.Max(0, x.QDI),
            LTCG:   Math.Max(0, x.LTCG)
        );

        static Taxes.GI RoundToCents(this Taxes.GI x) => new
        (
            PreTaxWDraw: Math.Round(x.PreTaxWDraw, 2),
            SS:          Math.Round(x.SS, 2),
            Ann:         Math.Round(x.Ann, 2),
            BondsYield:  Math.Round(x.BondsYield, 2),
            Dividends:   Math.Round(x.Dividends, 2),
            CapGains:    Math.Round(x.CapGains, 2)
        );

        static Taxes.AGI RoundToCents(this Taxes.AGI x) => new
        (
            OrdInc: Math.Round(x.OrdInc, 2),
            QDI:    Math.Round(x.QDI, 2),
            INT:    Math.Round(x.INT, 2),
            LTCG:   Math.Round(x.LTCG, 2)
        );

    }

}
