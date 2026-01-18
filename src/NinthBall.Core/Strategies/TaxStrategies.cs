
namespace NinthBall.Core
{
    [SimInput(typeof(TaxStrategy), typeof(TaxConfig))]
    sealed class TaxStrategy(SimParams P, TaxConfig TaxOptions) : ISimObjective
    {
        int ISimObjective.Order => 31;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex)
        {
            ThrowIfZero(TaxOptions.YearZeroTaxAmount, "YearZeroTaxAmount");
            ThrowIfZero(TaxOptions.TaxRates.OrdinaryIncome, "TaxRates.OrdinaryIncome");
            ThrowIfZero(TaxOptions.TaxRates.CapitalGains, "TaxRates.CapitalGains");

            return new Strategy(P, TaxOptions);
        } 
        
        sealed record Strategy(SimParams P, TaxConfig TaxOptions) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimState context)
            {
                context.Taxes = 0 == context.YearIndex
                    ? FirstYearTaxes(TaxOptions.YearZeroTaxAmount) 
                    : context.PriorYear.ComputePriorYearTaxes(TaxOptions);
            }
        }

        static Taxes FirstYearTaxes(double firstYearTaxAmount) => new Taxes
        (
            StandardDeduction: 0.0,
            Taxable: new(),
            TaxRates: new(),
            Tax: new(OrdInc: firstYearTaxAmount, 0.0, 0.0, 0.0)
        );

        static void ThrowIfZero(double d, string name) { if (d <= 0) throw new Exception($"{name} cannot be zero. To model zero-tax scenario use some very low value, but not zero."); }

        public override string ToString() => $"Taxes | Ordinary: {TaxOptions.TaxRates.OrdinaryIncome:P1} | CapGains: {TaxOptions.TaxRates.CapitalGains:P1}";
    }

    /// <summary>
    /// Tax calculation on prior year income.
    /// Exposed for better unit testing.
    /// </summary>
    public static class TaxMath
    {
        public static Taxes ComputePriorYearTaxes(this SimYear priorYear, TaxConfig TaxConfig)
        {
            // Standard deduction adjusted for inflation.
            var stdDeduction = TaxConfig.StandardDeduction > 0 ? TaxConfig.StandardDeduction : 31500.0;
            var inflationMultiplier = Math.Max(1.0, priorYear.Metrics.InflationMultiplier);
            var inflationAdjustedStdDeduction = stdDeduction * inflationMultiplier;

            // Collect taxable income from various sources.
            // Adjust to reflect std deduction.
            var taxable = new Taxable()
                .AddIncomeFromPreTaxAsset(priorYear)
                .AddIncomeFromPostTaxAsset(priorYear)
                .AddIncomeFromAdditionalIncomeSources(priorYear)
                .ApplyStandardDeduction(inflationAdjustedStdDeduction)
                ;

            var taxRates = new TaxRate(
                OrdInc: TaxConfig.TaxRates.OrdinaryIncome,
                LTCG: TaxConfig.TaxRates.CapitalGains
            );

            var taxes = new Taxes
            (
                // Capture standard deduction
                StandardDeduction: inflationAdjustedStdDeduction,
                Taxable: taxable,
                TaxRates: taxRates,
                Tax: taxable.ComputeTaxes(taxRates)
            );

            return taxes;
        }

        static TaxAmt ComputeTaxes(this Taxable taxable, TaxRate taxRate)
        {
            return new TaxAmt
            (
                // Ordinary income and interest income are taxed at ordinary income tax rates.
                OrdInc: Math.Max(0, Math.Round(taxable.OrdInc * taxRate.OrdInc)),
                INT: Math.Max(0, Math.Round(taxable.INT * taxRate.OrdInc)),

                // All dividends are assumed to be qualified.
                // All capital gains are assumed to be long-term gains.
                // Both are taxed at preferential tax rates.
                // For simplicity, state taxes are not factored-in here.
                // Net Investment Income Tax (NIIT) is not considered here.
                DIV: Math.Max(0, Math.Round(taxable.DIV * taxRate.LTCG)),
                LTCG: Math.Max(0, Math.Round(taxable.LTCG * taxRate.LTCG))
            );
        }

        static Taxable AddIncomeFromPreTaxAsset(this Taxable taxable, SimYear priorYear)
        {
            // We are looking at tax deferred asset (PreTax).
            // Only withdrawals are taxed as ordinary income.
            // Dividends, interest, and capital gains are not applicable.
            return taxable.Add(ordInc: priorYear.Withdrawals.PreTax);
        }

        static Taxable AddIncomeFromPostTaxAsset(this Taxable taxable, SimYear priorYear)
        {
            // We are looking at taxable asset (PostTax)
            var asset = priorYear.Jan.PostTax;
            var janStockBalance = asset.Amount * asset.Allocation;
            var janBondBalance = asset.Amount - janStockBalance;

            // Dividends on Stocks are taxable.
            // Dividends are taxable even if ROI is negative.
            // We do not have historical data on dividends.
            // Using a reasonable assumption of 2% of the stock balance.
            // This is a standard assumption used by: Vanguard, Fidelity and Schwab
            // BY-DESIGN: For simplicity, we assume all dividends are qualified dividends.
            const double TypicalStocksDividendYield = 0.02;
            var qualifiedDividendAmount = janStockBalance * TypicalStocksDividendYield;

            // Bond ROI is treated as interest income, and is taxable.
            // Bond interest is approximated using a fixed long‑term Treasury coupon yield (≈2–3%), 
            // since yield‑to‑maturity includes price changes that are not taxable as interest.
            const double TypicalBondCouponYield = 0.025;    // 2.5%
            var interestAmount = Math.Max(0, janBondBalance * TypicalBondCouponYield);

            // We do not track cash basis, too complex.
            // BY-DESIGN: 100% of the Withdrawal is treated as long-term capital gain.
            // This is a reasonable simplification for Monte Carlo simulation.
            // However, avoid frequent rebalancing or reallocations that may trigger short-term capital gains.
            var longTermCapitalGain = priorYear.Withdrawals.PostTax;

            return taxable.Add(DIV: qualifiedDividendAmount, INT: interestAmount, LTCG: longTermCapitalGain);
        }

        static Taxable Add(this Taxable taxable, double ordInc = 0, double DIV = 0, double INT = 0, double LTCG = 0) => new
        (
            OrdInc: taxable.OrdInc + ordInc,
            DIV: taxable.DIV + DIV,
            INT: taxable.INT + INT,
            LTCG: taxable.LTCG + LTCG
        );

        static Taxable AddIncomeFromAdditionalIncomeSources(this Taxable taxable, SimYear priorYear)
        {
            // Conservative assumption - Maximum (85%) taxable portion of Social security regardless of income bracket.
            double ssIncome = priorYear.Incomes.SS * 0.85;

            // Conservative assumption - Annuity income is fully taxable, ignores non-taxed principal portion
            double annIncome = priorYear.Incomes.Ann * 1.0;

            // Social security and annuity are taxed as ordinary income.
            return taxable.Add(
                ordInc: ssIncome + annIncome
            );
        }

        static Taxable ApplyStandardDeduction(this Taxable taxable, double stdDeduction)
        {
            double remainingDeduction = stdDeduction;

            // Apply deduction to ordinary income first
            if (remainingDeduction > 0)
            {
                double reduction = Math.Min(taxable.OrdInc, remainingDeduction);
                taxable = taxable.Add(ordInc: -reduction);
                remainingDeduction -= reduction;
            }

            // Apply deduction to interest income next
            if (remainingDeduction > 0)
            {
                double reduction = Math.Min(taxable.INT, remainingDeduction);
                taxable = taxable.Add(INT: -reduction);
                remainingDeduction -= reduction;
            }

            // Note:
            // Left over standard deduction can be applied to Dividends and capital gains to reduce tax-bracket but not the taxable amount.
            // Currently we are using a fixed tax bracket.
            // Also, we are treating all DIV as qualified and all capital gains as long-term.
            // Together, do not touch DIV and CGAIN.

            return taxable;
        }
    }

}

