
using System.ComponentModel.DataAnnotations;
using NinthBall.Utils;

namespace NinthBall.Core
{
    public sealed record ParametricBootstrapOptions
    (
        [property: Range(-1.0, 1.0)]        double StocksBondCorrelation,
        [property: Range(-1.0, 1.0)]        double StocksInflationCorrelation,
        [property: Range(-1.0, 1.0)]        double BondsInflationCorrelation,
        [property: ValidateNested]          ParametricBootstrapOptions.Dist Stocks,
        [property: ValidateNested]          ParametricBootstrapOptions.Dist Bonds,
        [property: ValidateNested]          ParametricBootstrapOptions.Dist Inflation)
    {
        public readonly record struct Dist
        (
            [property: Range( -1.0,  1.0 )] double MeanReturn,
            [property: Range(  0.0,  1.0 )] double Volatility,
            [property: Range(-10.0, 10.0 )] double Skewness,
            [property: Range(  0.0, 10.0 )] double Kurtosis,
            [property: Range( -1.0,  1.0 )] double AutoCorrelation
        );
    }

    /// <summary>
    /// Generates repeatable synthetic sequence of returns using statistical parameters.
    /// </summary>
    internal sealed class ParametricBootstrapper(SimulationSeed SimSeed, ParametricBootstrapOptions Options) : IBootstrapper
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
                double rStocks    = Math.Clamp(Options.Stocks.MeanReturn    + cfStocks    * Options.Stocks.Volatility,    -0.60, +0.60);
                double rBonds     = Math.Clamp(Options.Bonds.MeanReturn     + cfBonds     * Options.Bonds.Volatility,     -0.15, +0.25);
                double rInflation = Math.Clamp(Options.Inflation.MeanReturn + cfInflation * Options.Inflation.Volatility, -0.10, +0.30);

                sequence[i] = new HROI(0, rStocks, rBonds, rInflation);

                //..............................................................
                // Some data points:
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

        // Describe...
        public override string ToString() => $"Parametric Bootstrap | Stocks - Mean: {Options.Stocks.MeanReturn:P1} Volatility: {Options.Stocks.Volatility:P1} | Bonds - Mean: {Options.Bonds.MeanReturn:P1} Volatility: {Options.Bonds.Volatility:P1} | Cap: +/- 60%";

    }
}
