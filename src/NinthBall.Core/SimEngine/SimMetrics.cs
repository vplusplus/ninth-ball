
namespace NinthBall.Core
{
    /*
        Multiplier                  | Reasonable 30-year range |
        --------------------------- | ------------------------ |
        InflationMultiplier         | 2.0 – 2.5                |
        FedTaxInflationMultiplier   | 1.7 – 2.3                |
        StateTaxInflationMultiplier | 1.4 – 1.9                |
    */

    /// <summary>
    /// Running inflation rate(s) and cummulative metrics.
    /// </summary>
    public readonly record struct Metrics
    (
        // Multiplication factors
        double InflationMultiplier,         // Cumulative inflation multiplier since year #0
        double FedTaxInflationMultiplier,   // Cumulative Federal tax indexing (with lag)
        double StateTaxInflationMultiplier, // Cumulative State tax indexing (with lag)
        double GrowthMultiplier,            // Nominal growth multiplier since year #0

        // Percentage values
        double PortfolioReturn,             // Portfolio-weighted nominal return for the current year
        double AnnualizedReturn,            // Annualized nominal return (CAGR) since year #0
        double RealAnnualizedReturn         // Inflation-adjusted annualized return since year #0
    )
    {
        // CRITICAL: Multipliers start with 1.0, rest are zero
        public Metrics() : this(1.0, 1.0, 1.0, 1.0, 0.0, 0.0, 0.0) { }
    }

    internal static class SimMetrics
    {
        /// <summary>
        /// Small haircut on current year inflation rate to represent Federal C-CPI lag.
        /// Optionally configurable.
        /// </summary>
        static double FedTaxInflationLagHairCut => Config.GetPct("FedTaxInflationLagHairCut", 0.0025);

        /// <summary>
        /// Larger haircut on current year inflation rate to represent State's delayed adjustments and lag.
        /// Optionally configurable.
        /// </summary>
        static double NJStateTaxInflationLagHaircut => Config.GetPct("NJStateTaxInflationLagHaircut", 0.0075);

        /// <summary>
        /// Tracks running inflation multipliers and cummulative metrics.
        /// </summary>
        public static Metrics UpdateRunningMetrics(this Metrics pyMetrics, int yearIndex, double portfolioReturn, double currentYearInflationRate)
        {
            // Not trusting external year #0 initialization...
            Metrics prior = yearIndex > 0 ? pyMetrics : new
            (
                InflationMultiplier:            1.0,
                FedTaxInflationMultiplier:      1.0,
                StateTaxInflationMultiplier:    1.0,
                GrowthMultiplier:               1.0,
                PortfolioReturn:                0.0,
                AnnualizedReturn:               0.0,
                RealAnnualizedReturn:           0.0
            );

            // Running multiplier that represents cumulative inflation impact since year #0
            // Tracked at full precision, not quantized.
            // In theory, this can go-back.
            var inflationMultiplier = Math.Max(
                0.01,
                prior.InflationMultiplier * (1 + currentYearInflationRate)
            );

            // The IRS uses the Chained CPI (C-CPI-U) to measure inflation.
            // C-CPI generally rises slower than the standard CPI (Apply small haitcut)
            // And, it never goes back (Apply floor)
            // Tracked at full precision, not quantized.
            var fedTaxInflationMultiplier = Math.Max(
                prior.FedTaxInflationMultiplier,
                prior.FedTaxInflationMultiplier * (1 + currentYearInflationRate - FedTaxInflationLagHairCut)
            );

            // NJ, for example, doesn't index based on CPI or C-CPI each year.
            // Eventually they adjust, still fall behind (Apply larger haircut)
            // And, it never goes back (Apply floor)
            // Tracked at full precision, not quantized.
            var stateTaxInflationRateMultiplier = Math.Max(
                prior.StateTaxInflationMultiplier,
                prior.StateTaxInflationMultiplier * (1 + currentYearInflationRate - NJStateTaxInflationLagHaircut)
            );

            // Running multipler that represents cumulative portfolio growth since year #0
            double cumulativeGrowthMultiplier = prior.GrowthMultiplier * (1 + portfolioReturn);

            // Annualized nominal return since year #0
            double annualizedReturn = Math.Pow(cumulativeGrowthMultiplier, 1.0 / (yearIndex + 1)) - 1.0;

            // Real annualized return since year #0
            double realAnnualizedReturn = Math.Pow(cumulativeGrowthMultiplier / inflationMultiplier, 1.0 / (yearIndex + 1)) - 1.0;

            // The updated metrics...
            return new
            (
                InflationMultiplier:         inflationMultiplier,
                FedTaxInflationMultiplier:   fedTaxInflationMultiplier,
                StateTaxInflationMultiplier: stateTaxInflationRateMultiplier,
                GrowthMultiplier:            cumulativeGrowthMultiplier,

                PortfolioReturn:      portfolioReturn,
                AnnualizedReturn:     annualizedReturn,
                RealAnnualizedReturn: realAnnualizedReturn
            );
        }
    }
}
