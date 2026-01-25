
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

    public readonly record struct Taxes(Taxes.GI GrossIncome, Taxes.Fed FederalTax, Taxes.State StateTax)
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
        // But, AGI is different for Fed and state. We do not want to add two.
        // public readonly double ETTR => AdjustedGrossIncome.Total <= 0.01 ? 0.0 : Total / AdjustedGrossIncome.Total;

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
        const double FiftyPCT      = 0.50;
        const double HundredPCT    = 1.00;
        const double TenDollars    = 10.00;

        public static Taxes ComputePriorYearTaxes(this SimYear priorYear, TaxRateSchedules y0TaxRates, TaxAndMarketAssumptions TAMA)
        {
            // Collect gross incomes, arranged by taxable buckets
            var unadjustedIncomes = RawIncomes(priorYear, TAMA).MinZero().RoundToCents();

            // Compute Federal and State taxes
            return new Taxes
            (
                GrossIncome:    unadjustedIncomes,
                FederalTax:     unadjustedIncomes.ComputeFederalTaxes(y0TaxRates, priorYear.Metrics, TAMA),
                StateTax:       unadjustedIncomes.ComputeNJStateTaxes(y0TaxRates, priorYear.Metrics, priorYear.Age,  TAMA)
            );

            static Taxes.GI RawIncomes(SimYear priorYear, TaxAndMarketAssumptions TAMA) => new Taxes.GI
            (
                PreTaxWDraw: priorYear.Withdrawals.PreTax,
                SS: priorYear.Incomes.SS,
                Ann: priorYear.Incomes.Ann,
                BondsYield: priorYear.Jan.PostTax.BondsAmount * TAMA.TypicalBondCouponYield,
                Dividends: priorYear.Jan.PostTax.StocksAmount * TAMA.TypicalStocksDividendYield,
                CapGains: priorYear.Withdrawals.PostTax
            )
            .MinZero();
        }

        static Taxes.Fed ComputeFederalTaxes(this Taxes.GI unadjustedGrossIncome, TaxRateSchedules y0TaxRates, Metrics metrics, TaxAndMarketAssumptions TAMA)
        {
            // Compute federal adjusted gross income 
            var adjustedGrossIncome = FederalAdjustGrossIncomes(unadjustedGrossIncome, TAMA).MinZero().RoundToCents();

            // Quantize the inflation rate multipliers to avoid 30,000 jitters
            var inflationMultiplier = Math.Round(metrics.FedTaxInflationMultiplier, 4);

            // Inflate tax rates (and the standard deductions).
            var taxRatesOrdInc = y0TaxRates.Federal.Inflate(inflationMultiplier, jitterGuard: TenDollars);
            var taxRatesLTCG   = y0TaxRates.LTCG.Inflate(inflationMultiplier,    jitterGuard: TenDollars);

            // Take standard deductions...
            double remainingDeduction = taxRatesOrdInc.TaxDeductions;
            double taxableOrdInc      = TryReduce(ref remainingDeduction, adjustedGrossIncome.OrdInc);
            double taxableINT         = TryReduce(ref remainingDeduction, adjustedGrossIncome.INT);
            double taxableDIV         = TryReduce(ref remainingDeduction, adjustedGrossIncome.QDI);
            double taxableCapGain     = TryReduce(ref remainingDeduction, adjustedGrossIncome.LTCG);

            // Consult tax brackets. Compute marginal tax rate and the tax amount.
            var taxOnOrdInc  = taxRatesOrdInc.CalculateStackedEffectiveTax(taxableOrdInc + taxableINT);
            var taxOnCapGain = taxRatesLTCG.CalculateStackedEffectiveTax(taxableDIV + taxableCapGain, baseIncome: taxableOrdInc + taxableINT);

            // Net Investment Income Tax
            // MAGI ~= AGI in this model (simplification)
            // NIIT base uses post-deduction investment income (simplification)
            double magi = adjustedGrossIncome.Total;
            double netInvestmentIncome = taxableINT + taxableDIV + taxableCapGain;
            double niitBase = Math.Min(netInvestmentIncome, Math.Max(0, magi - TAMA.NIITThreshold));
            double niitTax = Math.Max(0, niitBase * TAMA.NIITRate);

            return new Taxes.Fed
            (
                StdDeduction:   taxRatesOrdInc.TaxDeductions,
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

            static Taxes.AGI FederalAdjustGrossIncomes(Taxes.GI inc, TaxAndMarketAssumptions TAMA)
            {

                // Interim math to guess taxable portion of social security income.
                // Social Security taxation modeled using provisional income.
                // Thresholds are statutory and not inflation-adjusted.
                double nonSSOrdinary = inc.PreTaxWDraw + inc.Ann;
                double provisionalInvestmentIncome = inc.BondsYield + inc.Dividends + inc.CapGains;
                double provisionalIncome = nonSSOrdinary + provisionalInvestmentIncome + (FiftyPCT * inc.SS);
                double taxableSS = TaxableSocialSecurity(inc.SS, provisionalIncome, base1: TAMA.SSNonTaxableThreshold, base2: TAMA.SS50PctTaxableThreshold);

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
                        FiftyPCT * ss,
                        FiftyPCT * (provisionalIncome - base1)
                    );

                return Math.Min(
                    EightyFivePCT * ss,
                    EightyFivePCT * (provisionalIncome - base2) + FiftyPCT * (base2 - base1)
                );
            }
        }

        static Taxes.State ComputeNJStateTaxes(this Taxes.GI inc, TaxRateSchedules y0TaxRates, Metrics metrics, int ageOnTaxYear, TaxAndMarketAssumptions TAMA)
        {
            //......................................................
            // NEW JERSEY STATE TAX LOGIC
            //......................................................
            // 1. Social Security is 100% exempt from NJ Gross Income.
            // 2. Pension and Retirement Income Exclusion (For Age 62+)
            //......................................................

            // NJ Gross Income for eligibility check (Ignore SS)
            // Determine exclusion based on age and income tiers
            double njGrossIncome = inc.PreTaxWDraw + inc.Ann + inc.BondsYield + inc.Dividends + inc.CapGains;
            double pensionExclusion = GetNJPensionExclusionWithTheCliff(njGrossIncome, ageOnTaxYear);

            // Apply pension exclusion specifically to retirement income (401k/IRA/Annuity/Pension)
            double retirementIncomes = inc.PreTaxWDraw + inc.Ann;
            double taxableRetirement = Math.Max(0, retirementIncomes - pensionExclusion);

            // NJ Taxable Income = Taxable Retirement + Investment Income (INT/DIV/LTCG)
            // Note: Simplification - Treats all BondsYield as taxable interest and Dividends/CapGains as taxable.
            double njTaxableSum = taxableRetirement + inc.BondsYield + inc.Dividends + inc.CapGains;

            // Quantize the inflation rate multipliers to avoid 30,000 jitters
            var inflationMultiplier = Math.Round(metrics.StateTaxInflationMultiplier, 4);

            // Inflate tax rates. For NJ, do not inflate standard deductions/exemptions.
            var taxRates = y0TaxRates.State.Inflate(inflationMultiplier, jitterGuard: TenDollars, inflateDeductions: false);

            // Apply Individual Exemptions and Property Tax deductions (configured as StateDeductions)
            var taxableTerminal = Math.Max(0, njTaxableSum - taxRates.TaxDeductions);

            // Apply progressive brackets
            var stateTaxes = taxRates.CalculateStackedEffectiveTax(taxableTerminal);

            return new Taxes.State
            (
                StateDeduction: taxRates.TaxDeductions,
                Taxable:        taxableTerminal,
                MTR:            stateTaxes.MarginalTaxRate,
                Tax:            stateTaxes.TaxAmount
            );

            static double GetNJPensionExclusionWithTheCliff(double njGrossIncome, int age)
            {
                // NJ STATUTORY POLICY (Pinned to NJ-1040 Law)
                // These are not hard-coded constants, rather representation of NJ-1040 Law.
                // If the law changes the application of these numbers are also likely to change.
                // Consider these numbers as code, hence are not externalized.

                // Age trigger for Pension Exclusion
                const int NJRetirementAge = 62;

                // Staggered Exclusion Tiers (Phased-out by Gross Income)
                const double NJPensionExclusionFullLimit    = 100000.0; // Income <= $100k
                const double NJPensionExclusionHalfLimit    = 125000.0; // Income <= $125k
                const double NJPensionExclusionQuarterLimit = 150000.0; // Income <= $150k

                // Allowed exclusion by tier limits.
                const double NJTier1PensionExclusion        = 100000.0; // Full Exclusion (MFJ)
                const double NJTier2PensionExclusion        = 50000.0;  // 50% Exclusion
                const double NJTier3PensionExclusion        = 25000.0;  // 25% Exclusion
                const double NJNoPensionExclusion           = 0.0;      // The CLIFF

                if (age < NJRetirementAge) return 0;

                return njGrossIncome switch
                {
                    <= NJPensionExclusionFullLimit      => NJTier1PensionExclusion,
                    <= NJPensionExclusionHalfLimit      => NJTier2PensionExclusion,
                    <= NJPensionExclusionQuarterLimit   => NJTier3PensionExclusion,
                    _                                   => NJNoPensionExclusion
                };
            }
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
