

namespace NinthBall.Core
{
    internal static class SimConfig
    {


        /// <summary>
        /// How much a company pays out in dividends relative to its current stock price, expressed as a percentage.
        /// TODO: See if historical data is available, include to Bootstrapper.
        /// </summary>
        public static double TypicalStocksDividendYield => Config.GetPct("TypicalStocksDividendYield", 2.0/100.0);   // 2%

        /// <summary>
        /// Interest payment made by the bond issuer, expressed as a percentage of the bond's face value 
        /// </summary>
        public static double TypicalBondCouponYield => Config.GetPct("TypicalBondCouponYield", 2.5/100.0);          // 2.5%

    }
}


// TODO: TaxCalculator.cs | Line: 12 | Value: 32200.0 | Default 2026 Federal Standard Deduction (MFJ)

// TODO: TaxCalculator.cs | Line: 13 | Value: 1500.0 | NJ Personal Exemption amount per person

// TODO: TaxCalculator.cs | Line: 21-27 | Value: Multiple | 2026 Federal Ordinary Income Tax Brackets and Rates

// TODO: TaxCalculator.cs | Line: 36-38 | Value: Multiple | 2026 Federal Long-Term Capital Gains Brackets and Rates

// TODO: TaxCalculator.cs | Line: 46-53 | Value: Multiple | 2026 New Jersey Gross Income Tax Brackets and Rates

// TODO: TaxCalculator.cs | Line: 93 | Value: 2 | Standard multiplier for NJ exemptions (Married Filing Jointly)

// TODO: TaxStrategies.cs | Line: 50 | Value: 31500.0 | Fallback Federal Standard Deduction if configuration is missing

// TODO: TaxStrategies.cs | Line: 156 | Value: 0.85 | Maximum taxable portion of Social Security income (statutory)

// TODO: TaxStrategies.cs | Line: 159 | Value: 1.0 | Taxable portion of Annuity income (conservative assumption)

// TODO: RMDStrategy.cs | Line: 6 | Value: 73 | Statutory age for beginning Required Minimum Distributions

// TODO: RMDStrategy.cs | Line: 41-45 | Value: Multiple | IRS Uniform Lifetime Table factors for RMD calculation

// TODO: LivingExpensesStrategies.cs | Line: 59 | Value: 120.0 | Rounding step for living expenses (corresponds to $10/month)

// TODO: PretaxDrawdownStrategies.cs | Line: 25 | Value: 120.0 | Rounding step for Fixed Withdrawal strategy

// TODO: PretaxDrawdownStrategies.cs | Line: 62 | Value: 120.0 | Rounding step for Percentage Withdrawal strategy

// TODO: PretaxDrawdownStrategies.cs | Line: 110 | Value: 120.0 | Rounding step for Variable Percentage Withdrawal strategy

// TODO: ParametricBootstrapper.cs | Line: 69 | Value: +/- 0.60 | Synthetic return cap for Stocks

// TODO: ParametricBootstrapper.cs | Line: 70 | Value: +/- 0.60 | Synthetic return cap for Bonds

// TODO: ParametricBootstrapper.cs | Line: 73 | Value: +0.30 | Hyperinflation cap (30%)

// TODO: ParametricBootstrapper.cs | Line: 73 | Value: -0.10 | Maximum deflation floor (10%)

// TODO: SimInput.cs | Line: 52 | Value: 10000 | Default number of iterations for the Monte Carlo simulation