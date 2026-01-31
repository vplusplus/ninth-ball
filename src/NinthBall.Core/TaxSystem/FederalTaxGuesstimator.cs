
namespace NinthBall.Core
{
    /// <summary>
    /// Federal tax guesstimator - applies IRS rules for income tax calculation.
    /// </summary>
    public sealed class FederalTaxGuesstimator(TaxAndMarketAssumptions TAMA) : ITaxGuesstimator
    {
        const double EightyFivePCT = 0.85;
        const double FiftyPCT = 0.50;
        const double HundredPCT = 1.00;
        const double TenDollars = 10.00;

        public Taxes.Tx GuesstimateTaxes(SimYear priorYear, TaxRateSchedules Year0TaxRates)
        {
            // Extract unadjusted gross income from SimYear
            var unadjustedGrossIncome = priorYear.DeriveGrossIncome(TAMA).MinZero().RoundToCents();

            // Compute federal adjusted gross income 
            var adjustedGrossIncome = RoundToCents(MinZero(FederalAdjustGrossIncomes(unadjustedGrossIncome, TAMA)));

            // Quantize the inflation rate multipliers to avoid 30,000 jitters
            // Inflate tax rates (and the standard deductions).
            var inflationMultiplier = Math.Round(priorYear.Metrics.FedTaxInflationMultiplier, 4);
            var taxRatesOrdInc = Year0TaxRates.Federal.Inflate(inflationMultiplier, jitterGuard: TenDollars);
            var taxRatesLTCG = Year0TaxRates.LTCG.Inflate(inflationMultiplier, jitterGuard: TenDollars);

            // Take standard deductions...
            double remainingDeduction = taxRatesOrdInc.TaxDeductions;
            double taxableOrdInc = TryReduce(ref remainingDeduction, adjustedGrossIncome.OrdInc);
            double taxableINT = TryReduce(ref remainingDeduction, adjustedGrossIncome.INT);
            double taxableDIV = TryReduce(ref remainingDeduction, adjustedGrossIncome.QDI);
            double taxableCapGain = TryReduce(ref remainingDeduction, adjustedGrossIncome.LTCG);

            // Consult tax brackets. Compute marginal tax rate and the tax amount.
            var taxOnOrdInc = taxRatesOrdInc.CalculateStackedEffectiveTax(taxableOrdInc + taxableINT);
            var taxOnCapGain = taxRatesLTCG.CalculateStackedEffectiveTax(taxableDIV + taxableCapGain, baseIncome: taxableOrdInc + taxableINT);

            // Net Investment Income Tax
            // MAGI ~= AGI in this model (simplification)
            // NIIT base uses post-deduction investment income (simplification)
            double magi = adjustedGrossIncome.Total;
            double netInvestmentIncome = taxableINT + taxableDIV + taxableCapGain;
            double niitBase = Math.Min(netInvestmentIncome, Math.Max(0, magi - TAMA.NIITThreshold));
            double niitTax = Math.Max(0, niitBase * TAMA.NIITRate);

            return new Taxes.Tx
            (
                Gross:      adjustedGrossIncome.Total,
                Deductions: taxRatesOrdInc.TaxDeductions,
                Taxable:    taxableOrdInc + taxableINT + taxableDIV + taxableCapGain,
                MTR:        taxOnOrdInc.MarginalTaxRate,
                MTRCapGain: taxOnCapGain.MarginalTaxRate,
                Tax:        taxOnOrdInc.TaxAmount + taxOnCapGain.TaxAmount + niitTax
            );

            static double TryReduce(ref double remaining, in double source)
            {
                var reduction = Math.Max(0, Math.Min(remaining, source));
                remaining -= reduction;
                return source - reduction;
            }
        }

        private readonly record struct AGI(double OrdInc, double INT, double QDI, double LTCG)
        {
            public readonly double Total => OrdInc + INT + QDI + LTCG;
        }

        static AGI FederalAdjustGrossIncomes(Taxes.GI inc, TaxAndMarketAssumptions TAMA)
        {

            // Interim math to guess taxable portion of social security income.
            // Social Security taxation modeled using provisional income.
            // Thresholds are statutory and not inflation-adjusted.
            double nonSSOrdinary = inc.PreTaxWDraw + inc.Ann;
            double provisionalInvestmentIncome = inc.BondsYield + inc.Dividends + inc.CapGains;
            double provisionalIncome = nonSSOrdinary + provisionalInvestmentIncome + (FiftyPCT * inc.SS);
            double taxableSS = TaxableSocialSecurity(inc.SS, provisionalIncome, base1: TAMA.SSNonTaxableThreshold, base2: TAMA.SS50PctTaxableThreshold);

            return MinZero(new AGI
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
            ));
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

        static AGI MinZero(AGI x) => new
        (
            OrdInc: Math.Max(0, x.OrdInc),
            INT:    Math.Max(0, x.INT),
            QDI:    Math.Max(0, x.QDI),
            LTCG:   Math.Max(0, x.LTCG)
        );

        static AGI RoundToCents(AGI x) => new
        (
            OrdInc: Math.Round(x.OrdInc, 2),
            QDI:    Math.Round(x.QDI, 2),
            INT:    Math.Round(x.INT, 2),
            LTCG:   Math.Round(x.LTCG, 2)
        );
    }

}
