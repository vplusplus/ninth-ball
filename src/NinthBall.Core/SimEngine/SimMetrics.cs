
namespace NinthBall.Core
{
    /*  Typical range for the multipliers:
        ---------------------------------------------------------
        Multiplier                  | Reasonable 30-year range
        ---------------------------------------------------------
        InflationMultiplier         | 2.0 – 2.5
        FedTaxInflationMultiplier   | 1.7 – 2.3
        StateTaxInflationMultiplier | 1.4 – 1.9
        ---------------------------------------------------------
    */

    /// <summary>
    /// Iteration path specific running inflation multipliers and cumulative metrics.
    /// </summary>
    public readonly record struct Metrics
    (
        // Multiplication factors
        double InflationMultiplier,         // Cumulative inflation multiplier since year #0
        double FedTaxInflationMultiplier,   // Cumulative Federal tax indexing (with lag)
        double StateTaxInflationMultiplier, // Cumulative State tax indexing (with lag)
        double GrowthMultiplier,            // Nominal growth multiplier since year #0

        // Cumulative percentage values
        double PortfolioReturn,             // Portfolio-weighted nominal return for the current year
        double AnnualizedReturn,            // Annualized nominal return (CAGR) since year #0
        double RealAnnualizedReturn         // Inflation-adjusted annualized return since year #0
    )
    {
        /// <summary>
        /// CRITICAL: Metrics initialization for correct year #0 values.
        /// </summary>
        public Metrics() : this
        (
            // Multipliers start with 1.0
            InflationMultiplier:            1.0,
            FedTaxInflationMultiplier:      1.0,
            StateTaxInflationMultiplier:    1.0,
            GrowthMultiplier:               1.0,

            // Rest start with zero 
            PortfolioReturn:                0.0,
            AnnualizedReturn:               0.0,
            RealAnnualizedReturn:           0.0

        ){}
    }

    internal static class SimMetrics
    {
        public static Metrics UpdateRunningMetrics(this Metrics pyMetrics, int yearIndex, double portfolioReturn, double currentYearInflationRate, TaxAndMarketAssumptions TAMA)
        {
            // Not trusting external year #0 initialization...
            Metrics prior = yearIndex > 0 ? pyMetrics : new();

            // Running multiplier that represents cumulative inflation impact since year #0
            // Tracked at full precision, not quantized.
            // In theory, this can go-back.
            var inflationMultiplier = Math.Max(
                0.01,
                prior.InflationMultiplier * (1 + currentYearInflationRate)
            );

            // The IRS uses the Chained CPI (C-CPI-U) to measure inflation.
            // C-CPI generally rises slower than the standard CPI (Apply small haircut)
            // And, it never goes back (Apply floor)
            // Tracked at full precision (not quantized)
            var fedTaxInflationMultiplier = Math.Max(
                prior.FedTaxInflationMultiplier,
                prior.FedTaxInflationMultiplier * (1 + currentYearInflationRate - TAMA.FedTaxInflationLagHaircut)
            );

            // NJ, for example, doesn't index based on CPI or C-CPI each year.
            // Eventually they adjust, still fall behind (Apply larger haircut)
            // And, it never goes back (Apply floor)
            // Tracked at full precision (not quantized)
            var stateTaxInflationRateMultiplier = Math.Max(
                prior.StateTaxInflationMultiplier,
                prior.StateTaxInflationMultiplier * (1 + currentYearInflationRate - TAMA.NJStateTaxInflationLagHaircut)
            );

            // Running multiplier that represents cumulative portfolio growth since year #0
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

                PortfolioReturn:             portfolioReturn,
                AnnualizedReturn:            annualizedReturn,
                RealAnnualizedReturn:        realAnnualizedReturn
            );
        }
    }
}
