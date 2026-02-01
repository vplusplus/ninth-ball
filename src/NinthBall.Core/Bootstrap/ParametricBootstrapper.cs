
using System.ComponentModel.DataAnnotations;
using NinthBall.Utils;

//..............................................................
#region Some data points:
//..............................................................
// S&P 500
// 1931         : -43.34%   (our clamp -60%)
// 1933         : +53.99%   (our clamp +60%)
// Treasury bonds:
// 2022         : -13.10%   (our clamp -15%)
// 2008         : +20.10%   (our clamp +25%) 
// Inflation rate
// 1917         :  17.8%    (World War I)
// March 1980   :  14.8%    (Modern Era High: Post-1950)
// 1949         :  -2.1%    (Modern Era Low Post-1950)
// 1032         : -10.3%    (Great Depression)
//..............................................................

#endregion

namespace NinthBall.Core
{
    public sealed record ParametricProfiles
    (
        [property: ValidateNested] ParametricProfiles.Parameters Original,
        [property: ValidateNested] ParametricProfiles.Parameters Expected,
        [property: ValidateNested] ParametricProfiles.Parameters Conservative,
        [property: ValidateNested] ParametricProfiles.Parameters HighRisk
    )
    {
        public readonly record struct Parameters
        (
            [property: Range(-1.0, 1.0)] double StocksBondCorrelation,
            [property: Range(-1.0, 1.0)] double StocksInflationCorrelation,
            [property: Range(-1.0, 1.0)] double BondsInflationCorrelation,
            [property: ValidateNested] Dist Stocks,
            [property: ValidateNested] Dist Bonds,
            [property: ValidateNested] Dist Inflation
        );

        public readonly record struct Dist
        (
            [property: Range(  0.01,   0.50)] double Mean,
            [property: Range(  0.01,   0.50)] double Volatility,
            [property: Range(-10.00,  10.00)] double Skewness,
            [property: Range(  0.00,  10.00)] double Kurtosis,
            [property: Range( -1.00,   1.00)] double AutoCorrelation
        );
    }

    /// <summary>
    /// Generates repeatable synthetic sequence of returns using statistical parameters.
    /// </summary>
    sealed class ParametricBootstrapper(SimulationSeed SimSeed, ParametricProfiles.Parameters Options) : IBootstrapper
    {
        // We can produce theoretically unlimited possible combinations.
        int IBootstrapper.GetMaxIterations(int numYears) => int.MaxValue;

        /// <summary>
        /// Generates repeatable synthetic sequence of returns using statistical parameters.
        /// </summary>
        IROISequence IBootstrapper.GetROISequence(int iterationIndex, int numYears)
        {
            // Use a deterministic seed based on iterationIndex and the global seed hint
            var random = new Random(PredictableHashCode.Combine(SimSeed.Value, iterationIndex));
            var sequence = new HROI[numYears];

            // State for autocorrelation
            double prevZStocks = 0;
            double prevZBonds = 0;
            double prevZInflation = 0;

            for (int i = 0; i < numYears; i++)
            {
                // Generate independent normal deviates, uniform distribution (0 to 1)
                double u1 = NextSafeDouble(random);
                double u2 = NextSafeDouble(random);
                double u3 = NextSafeDouble(random);

                // Convert into bell-curve centered at zero, using InverseNormalCDF
                double z1 = Statistics.InverseNormalCDF(u1);
                double z2 = Statistics.InverseNormalCDF(u2);
                double z3 = Statistics.InverseNormalCDF(u3);

                // Apply Correlation (Cholesky)
                // Introduces dependency between stocks, bonds and inflation
                var (c1, c2, c3) = Statistics.Correlate3(z1, z2, z3, 
                    Options.StocksBondCorrelation, 
                    Options.StocksInflationCorrelation, 
                    Options.BondsInflationCorrelation);

                // Apply AutoCorrelation (AR1)
                // Introduces momentum or persistence over time.
                // Current year depends partly on previous year
                // Z_t = rho * Z_{t-1} + sqrt(1 - rho^2) * epsilon_t
                // Formula ensures the process remains standard normal (mean 0, var 1) if rho < 1.
                double arStocks    = ApplyAR1(c1, prevZStocks, Options.Stocks.AutoCorrelation);
                double arBonds     = ApplyAR1(c2, prevZBonds, Options.Bonds.AutoCorrelation);
                double arInflation = ApplyAR1(c3, prevZInflation, Options.Inflation.AutoCorrelation);
                prevZStocks        = arStocks;
                prevZBonds         = arBonds;
                prevZInflation     = arInflation;

                // Apply Cornish-Fisher (Skewness/Kurtosis)
                // Warps the bell curve.
                // Negative skew ~> deeper crashes.
                // High kurtosis ~> rare but extreme events.
                double cfStocks    = Statistics.CornishFisher(arStocks, Options.Stocks.Skewness, Options.Stocks.Kurtosis);
                double cfBonds     = Statistics.CornishFisher(arBonds, Options.Bonds.Skewness, Options.Bonds.Kurtosis);
                double cfInflation = Statistics.CornishFisher(arInflation, Options.Inflation.Skewness, Options.Inflation.Kurtosis);

                // Scale by Mean and Volatility.
                // Converts abstract scaling factor into percentage returns.
                // Stats has no limits; but market does; clamp the extremes.
                // The hard-coded clamps define the behavioral contract (i.e., “personality”) of the parametric bootstrapper.
                // These values are not configuration knobs and must not be treated as tunable parameters.
                double rStocks    = Math.Clamp(Options.Stocks.Mean    + cfStocks    * Options.Stocks.Volatility,    -0.60, +0.60);
                double rBonds     = Math.Clamp(Options.Bonds.Mean     + cfBonds     * Options.Bonds.Volatility,     -0.15, +0.25);
                double rInflation = Math.Clamp(Options.Inflation.Mean + cfInflation * Options.Inflation.Volatility, -0.10, +0.30);

                sequence[i] = new HROI(0, rStocks, rBonds, rInflation);
            }

            return new ROISequence(sequence.AsMemory());
        }   

        private static double NextSafeDouble(Random random)
        {
            double u = random.NextDouble();
            if (u <= 0) u = 1e-10;
            if (u >= 1) u = 1 - 1e-10;
            return u;
        }

        private static double ApplyAR1(double epsilon, double prevZ, double rho)
        {
            if (Math.Abs(rho) < 1e-9) return epsilon;
            return rho * prevZ + Math.Sqrt(1 - rho * rho) * epsilon;
        }

        private readonly record struct ROISequence(ReadOnlyMemory<HROI> MemoryBlock) : IROISequence
        {
            readonly HROI IROISequence.this[int yearIndex] => MemoryBlock.Span[yearIndex];
        }

        public override string ToString() => $"[S,B,I] = {StrMean} {StrVolatility} {StrSkewness} {StrKurtosis} {StrAutoCorr} | {StrCorr} | Cap: ±60%";
        string StrMean       => $"μ[{Options.Stocks.Mean:P1}, {Options.Bonds.Mean:P1}, {Options.Inflation.Mean:P1}]";
        string StrVolatility => $"σ[{Options.Stocks.Volatility:P1}, {Options.Bonds.Volatility:P1}, {Options.Inflation.Volatility:P1}]";
        string StrSkewness   => $"γ1[{Options.Stocks.Skewness:F1}, {Options.Bonds.Skewness:F1}, {Options.Inflation.Skewness:F1}]";
        string StrKurtosis   => $"γ2[{Options.Stocks.Kurtosis:F1}, {Options.Bonds.Kurtosis:F1}, {Options.Inflation.Kurtosis:F1}]";
        string StrAutoCorr   => $"ρ1[{Options.Stocks.AutoCorrelation:F2}, {Options.Bonds.AutoCorrelation:F2}, {Options.Inflation.AutoCorrelation:F2}]";
        string StrCorr       => $"ρ[SB, IS, IB] = [{Options.StocksBondCorrelation:F1}, {Options.StocksInflationCorrelation:F1}, {Options.BondsInflationCorrelation:F1}]";

    }

}

// Sample output messages:
// Inflation & growth | Expected: [S,B,I] = μ[9.5%, 4.5%, 3.0%] σ[18.0%, 8.0%, 2.2%] γ1[-0.3, -0.1, 0.4] γ2[3.5, 3.2, 3.2] ρ1[0.1, 0.1, 0.7] | ρ[SB, IS, IB] = [-0.1, -0.2, -0.1] | Cap: ±60%
// Inflation & growth | Conservative: [S,B,I] = μ[8.5%, 3.5%, 3.5%] σ[21.0%, 9.5%, 2.8%] γ1[-0.6, -0.3, 0.6] γ2[4.5, 3.8, 3.6] ρ1[0.1, 0.1, 0.8] | ρ[SB, IS, IB] = [-0.1, -0.3, -0.2] | Cap: ±60%
// Inflation & growth | HighRisk: [S,B,I] = μ[10.0%, 4.5%, 3.2%] σ[25.0%, 12.0%, 3.5%] γ1[-0.2, -0.2, 0.5] γ2[4.2, 3.6, 3.5] ρ1[0.1, 0.1, 0.8] | ρ[SB, IS, IB] = [-0.1, -0.2, -0.2] | Cap: ±60%