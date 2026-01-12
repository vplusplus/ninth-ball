
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
            void ISimStrategy.Apply(ISimContext context)
            {
                context.Expenses = context.Expenses with
                {
                    PYTax = 0 == context.YearIndex
                        ? new() { TaxOnOrdInc = TaxOptions.YearZeroTaxAmount }
                        : TaxMath.ComputePriorYearTaxes(TaxOptions, context.RunningInflationMultiplier, context.PriorYears.Span[^1])
                };
            }
        }

        static void ThrowIfZero(double d, string name) { if (d <= 0) throw new Exception($"{name} cannot be zero. Use some very low value, but not zero, to model zero-tax scenario"); }

        public override string ToString() => $"Taxes | Ordinary: {TaxOptions.TaxRates.OrdinaryIncome:P1} | CapGains: {TaxOptions.TaxRates.CapitalGains:P1}";
    }

    /// <summary>
    /// Tax calculation on prior year income.
    /// Exposed for better unit testing.
    /// </summary>
    public static class TaxMath
    {
        // Mutable working memory for tax calculations. 
        sealed class DDDD 
        {
            public double OrdIncome = 0.0;
            public double Interest  = 0.0;
            public double Dividends = 0.0;
            public double CapGain   = 0.0;
        };

        public static Tax ComputePriorYearTaxes(TaxConfig TaxConfig, double inflationMultiplier, SimYear priorYear)
        {
            // Collect taxable income from various sources.
            var incomes = new DDDD()
                .AddIncomeFromPreTaxAsset(priorYear)
                .AddIncomeFromPostTaxAsset(priorYear)
                .AddIncomeFromCashAsset(priorYear)
                .AddIncomeFromAdditionalIncomeSources(priorYear)
                ;

            // Standard deduction adjusted for inflation.
            var stdDeduction = TaxConfig.StandardDeduction > 0 ? TaxConfig.StandardDeduction : 31500.0;
            var inflationAdjustedStdDeduction = stdDeduction * inflationMultiplier;

            // Adjust the taxable ordinary income and taxable interest income to reflect std deduction.
            incomes.ApplyStandardDeduction(inflationAdjustedStdDeduction);

            var taxes = new Tax()
            {
                // Ordinary income and interest income are taxed at ordinary income tax rates.
                TaxOnOrdInc = Math.Round(Math.Max(0, incomes.OrdIncome * TaxConfig.TaxRates.OrdinaryIncome)),
                TaxOnInt = Math.Round(Math.Max(0, incomes.Interest * TaxConfig.TaxRates.OrdinaryIncome)),

                // All dividends are assumed to be qualified.
                // All capital gains are assumed to be long-term gains.
                // Both are taxed at preferential tax rates.
                // For simplicity, state taxes are not factored-in here.
                // Net Investment Income Tax (NIIT) is not considered here.
                TaxOnDiv = Math.Round(Math.Max(0, incomes.Dividends * TaxConfig.TaxRates.CapitalGains)),
                TaxOnCapGain   = Math.Round(Math.Max(0, incomes.CapGain * TaxConfig.TaxRates.CapitalGains))
            };

            return taxes;
        }

        static DDDD AddIncomeFromPreTaxAsset(this DDDD incomes, SimYear priorYear)
        {
            // We are looking at tax deferred asset (PreTax).
            // Only withdrawals are taxed as ordinary income.
            // Dividends, interest, and capital gains are not applicable.
            incomes.OrdIncome += priorYear.Withdrawals.PreTax;
            return incomes;
        }

        static DDDD AddIncomeFromPostTaxAsset(this DDDD incomes, SimYear priorYear)
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

            incomes.Dividends   += qualifiedDividendAmount;
            incomes.Interest    += interestAmount;
            incomes.CapGain     += longTermCapitalGain;
            return incomes;
        }

        static DDDD AddIncomeFromCashAsset(this DDDD incomes, SimYear priorYear)
        {
            // We are looking at cash asset such as high-yield savings.
            // Only interest income is applicable.
            var janCashBalance = priorYear.Jan.Cash.Amount;
            var interestAmount = Math.Max(0, janCashBalance * priorYear.ROI.CashROI);

            incomes.Interest += interestAmount;
            return incomes;
        }

        static DDDD AddIncomeFromAdditionalIncomeSources(this DDDD incomes, SimYear priorYear)
        {
            // Conservative assumption - Maximum (85%) taxable portion of Social security regardless of income bracket.
            double ssIncome = priorYear.Incomes.SS * 0.85;

            // Conservative assumption - Annuity income is fully taxable, ignores non-taxed principal portion
            double annIncome = priorYear.Incomes.Ann * 1.0;

            // Social security and annuity are taxed as ordinary income.            
            incomes.OrdIncome += ssIncome;
            incomes.OrdIncome += annIncome;
            return incomes;
        }

        static void ApplyStandardDeduction(this DDDD incomes, double stdDeduction)
        {
            double remainingDeduction = stdDeduction;

            // Apply deduction to ordinary income first
            if (remainingDeduction > 0)
            {
                double reduction = Math.Min(incomes.OrdIncome, remainingDeduction);
                incomes.OrdIncome  -= reduction;
                remainingDeduction -= reduction;
            }

            // Apply deduction to interest income next
            if (remainingDeduction > 0)
            {
                double reduction = Math.Min(incomes.Interest, remainingDeduction);
                incomes.Interest -= reduction;
                remainingDeduction -= reduction;
            }

            // Note:
            // Dividends and capital gains are NOT reduced by standard deduction
            // per IRS rules. They remain untouched.
        }
    }

}

