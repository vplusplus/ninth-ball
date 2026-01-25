
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
        NIIT    – Net Investment Income Tax 
    
        TaxPCT  = Total Taxes Paid ÷ Every $ of cash inflow (not a standard term; not a statutory tax rate)

     */

    // TODO: Check out Pension exclusion and Pension exclusion cliff.
    // TODO: Consider early design provision for alternate state (PA) tax calculator

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

        // Taxable SS thresholds are statutory and not inflation-adjusted.
        // TODO: Move to external optional configuration.
        const double SSNonTaxableThresholdMFJ = 32000.0;
        const double SS50PctTaxableThresholdMFJ = 44000.0;

        // How much a company pays out in dividends relative to its current stock price, expressed as a percentage.
        // TODO: See if historical data is available, include to Bootstrapper.
        const double TypicalStocksDividendYield = 2.0 / 100.0;   // 2%

        // Interest payment made by the bond issuer, expressed as a percentage of the bond's face value 
        const double TypicalBondCouponYield = 2.5 / 100.0;          // 2.5%


        // NIIT thresholds are not indexed for inflation under current law.
        // For planning realism, consider inflating or policy-adjusting this value
        const double NIITThreshold = 250000.0;          // TODO: MFJ - Move to optional configuration
        const double NIITRate = 0.038;                  // TODO: Move to optional configuration

        public static Taxes ComputePriorYearTaxes(this SimYear priorYear, TaxRateSchedule taxRatesFederal, TaxRateSchedule taxRatesLTCG, TaxRateSchedule taxRatesState)
        {
            // We do not want 30,000 tax schedules (30 years x 10000 iterations paths)
            // Quantize the inflation rate multipliers to avoid 30,000 jitters
            double inflationMultiplierFederal = Math.Round(priorYear.Metrics.FedTaxInflationMultiplier,   4);
            double inflationMultiplierState   = Math.Round(priorYear.Metrics.StateTaxInflationMultiplier, 4);

            // Adjust tax brackets, Federal standard deductions and state exemptions for inflation
            // Use $10.0 jitterGuard to avoid false-precision.
            taxRatesFederal   = taxRatesFederal.Inflate(inflationMultiplierFederal, jitterGuard: TenDollars);
            taxRatesLTCG      = taxRatesLTCG.Inflate(inflationMultiplierFederal,    jitterGuard: TenDollars);
            taxRatesState     = taxRatesState.Inflate(inflationMultiplierState,     jitterGuard: TenDollars);

            // Collect gross incomes, arranged by taxable buckets
            var unadjustedIncomes = priorYear.RawIncomes().MinZero().RoundToCents();
            var agi = unadjustedIncomes.AdjustedGrossIncomes().MinZero().RoundToCents();

            return new Taxes
            (
                GrossIncome:         unadjustedIncomes,
                AdjustedGrossIncome: agi,
                FederalTax:          agi.ComputeFederalTaxes(taxRatesFederal.TaxDeductions, taxRatesFederal, taxRatesLTCG),
                StateTax:            agi.ComputeStateTaxes(taxRatesState.TaxDeductions, taxRatesState)
            );
        }

        static Taxes.GI RawIncomes(this SimYear priorYear) => new Taxes.GI
        (
            PreTaxWDraw:    priorYear.Withdrawals.PreTax,
            SS:             priorYear.Incomes.SS,
            Ann:            priorYear.Incomes.Ann,
            BondsYield:     priorYear.Jan.PostTax.BondsAmount * TypicalBondCouponYield,
            Dividends:      priorYear.Jan.PostTax.StocksAmount * TypicalStocksDividendYield,
            CapGains:       priorYear.Withdrawals.PostTax
        )
        .MinZero();


        static Taxes.AGI AdjustedGrossIncomes(this Taxes.GI inc)
        {

            // Interim math to guess taxable portion of social security income.
            // Social Security taxation modeled using provisional income.
            // Thresholds are statutory and not inflation-adjusted.
            double nonSSOrdinary = inc.PreTaxWDraw + inc.Ann;
            double provisionalInvestmentIncome = inc.BondsYield + inc.Dividends + inc.CapGains;
            double provisionalIncome = nonSSOrdinary + provisionalInvestmentIncome + (0.5 * inc.SS);
            double taxableSS = TaxableSocialSecurity(inc.SS, provisionalIncome, base1: SSNonTaxableThresholdMFJ, base2: SS50PctTaxableThresholdMFJ);

            return new Taxes.AGI
            (
                // 100% of 401K withdrawal is ordinary income (which is true).
                // Non-taxable portion of SS excluded from AGI
                // Conservative: 100% of Ann is taxed - Is overstated during first 7 years.
                OrdInc: (inc.PreTaxWDraw * HundredPCT) + taxableSS + (inc.Ann * HundredPCT),

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
        }

        static double TaxableSocialSecurity(double ss, double provisionalIncome, double base1, double base2)
        {
            if (provisionalIncome <= base1)
                return 0.0;

            if (provisionalIncome <= base2)
                return Math.Min(
                    0.5 * (provisionalIncome - base1),
                    0.5 * ss
                );

            return Math.Min(
                0.85 * ss,
                0.85 * (provisionalIncome - base2) + 0.5 * (base2 - base1)
            );
        }

        static Taxes.Fed ComputeFederalTaxes(this Taxes.AGI adjustedGrossIncome, double standardDeductions, TaxRateSchedule fedTaxRates, TaxRateSchedule longTermCapGainsTaxRates)
        {
            // Take standard deductions...
            double remainingDeduction = standardDeductions;
            double taxableOrdInc  = TryReduce(ref remainingDeduction, adjustedGrossIncome.OrdInc);
            double taxableINT     = TryReduce(ref remainingDeduction, adjustedGrossIncome.INT);
            double taxableDIV     = TryReduce(ref remainingDeduction, adjustedGrossIncome.QDI);
            double taxableCapGain = TryReduce(ref remainingDeduction, adjustedGrossIncome.LTCG);

            // Consult tax brackets. Compute marginal tax rate and the tax amount.
            var taxOnOrdInc   = fedTaxRates.CalculateStackedEffectiveTax(taxableOrdInc + taxableINT);
            var taxOnCapGain = longTermCapGainsTaxRates.CalculateStackedEffectiveTax(taxableDIV + taxableCapGain, baseIncome: taxableOrdInc + taxableINT);

            // Net Investment Income Tax
            // MAGI ~= AGI in this model (simplification)
            // NIIT base uses post-deduction investment income (simplification)
            double magi = adjustedGrossIncome.Total;
            double netInvestmentIncome = taxableINT + taxableDIV + taxableCapGain;
            double niitBase = Math.Min( netInvestmentIncome, Math.Max(0, magi - NIITThreshold));
            double niitTax = Math.Max(0,  niitBase * NIITRate);

            return new Taxes.Fed
            (
                StdDeduction:   standardDeductions,
                Taxable:        taxableOrdInc + taxableINT + taxableDIV + taxableCapGain, 
                MTR:            taxOnOrdInc.MarginalTaxRate,
                MTRCapGain:     taxOnCapGain.MarginalTaxRate,
                Tax:            taxOnOrdInc.TaxAmount + taxOnCapGain.TaxAmount + niitTax
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

