
using System.ComponentModel.DataAnnotations;

namespace NinthBall.Core
{
    public sealed record ParametricBootstrapOptions
    (
        [property: Range(-1.0, 1.0)]
        double StocksBondCorrelation,

        [property: Range(-1.0, 1.0)]
        double StocksInflationCorrelation,

        [property: Range(-1.0, 1.0)]
        double BondsInflationCorrelation,

        [property: ValidateNested]
        ParametricBootstrapOptions.Dist Stocks,

        [property: ValidateNested]
        ParametricBootstrapOptions.Dist Bonds,

        [property: ValidateNested]
        ParametricBootstrapOptions.Dist Inflation)
    {
        public readonly record struct Dist
        (
            [property: Range( -1.0, 1.0  )] double MeanReturn,
            [property: Range(  0.0, 1.0  )] double Volatility,
            [property: Range(-10.0, 10.0 )] double Skewness,
            [property: Range(  0.0, 100.0)] double Kurtosis,
            [property: Range( -1.0, 1.0  )] double AutoCorrelation
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
                // 1.1 Generate independent normal deviates, uniform distribution (0 to 1)
                double u1 = NextSafeDouble(random);
                double u2 = NextSafeDouble(random);
                double u3 = NextSafeDouble(random);

                // 1.2 Convert into bell-curve centered at zero, using InverseNormalCDF
                double z1 = MathUtils.InverseNormalCDF(u1);
                double z2 = MathUtils.InverseNormalCDF(u2);
                double z3 = MathUtils.InverseNormalCDF(u3);

                // 2. Apply Correlation (Cholesky)
                // Introduces dependency between stocks, bonds and inflation
                var (c1, c2, c3) = MathUtils.Correlate3(z1, z2, z3, 
                    Options.StocksBondCorrelation, 
                    Options.StocksInflationCorrelation, 
                    Options.BondsInflationCorrelation);

                // 3. Apply AutoCorrelation (AR1)
                // Z_t = rho * Z_{t-1} + sqrt(1 - rho^2) * epsilon_t
                // This ensures the process remains standard normal (mean 0, var 1) if rho < 1.
                // Introduces momentum or persistence over time.
                // Current year depends partly on previous year
                double arStocks    = ApplyAR1(c1, prevZStocks, Options.Stocks.AutoCorrelation);
                double arBonds     = ApplyAR1(c2, prevZBonds, Options.Bonds.AutoCorrelation);
                double arInflation = ApplyAR1(c3, prevZInflation, Options.Inflation.AutoCorrelation);

                prevZStocks = arStocks;
                prevZBonds = arBonds;
                prevZInflation = arInflation;

                // 4. Apply Cornish-Fisher (Skewness/Kurtosis)
                // Warps the bell curve.
                // Negative skew ~> deeper crashes.
                // High kurtosis ~> rare but extreme events.
                double cfStocks    = MathUtils.CornishFisher(arStocks, Options.Stocks.Skewness, Options.Stocks.Kurtosis);
                double cfBonds     = MathUtils.CornishFisher(arBonds, Options.Bonds.Skewness, Options.Bonds.Kurtosis);
                double cfInflation = MathUtils.CornishFisher(arInflation, Options.Inflation.Skewness, Options.Inflation.Kurtosis);

                // 5. Scale by Mean and Volatility
                // Converts abstract scaling factor into percentage returns
                // Cap returns between -60% and +60%
                double rStocks    = Math.Min(+0.60, Math.Max(-0.60, Options.Stocks.MeanReturn + cfStocks * Options.Stocks.Volatility));
                double rBonds     = Math.Min(+0.60, Math.Max(-0.60, Options.Bonds.MeanReturn + cfBonds * Options.Bonds.Volatility));
                
                // Inflation capped between -10% (Deflation) and +30% (Hyperinflation)
                double rInflation = Math.Min(+0.30, Math.Max(-0.10, Options.Inflation.MeanReturn + cfInflation * Options.Inflation.Volatility));

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

        // Describe...
        public override string ToString() => $"Parametric Bootstrap | Stocks - Mean: {Options.Stocks.MeanReturn:P1} Volatility: {Options.Stocks.Volatility:P1} | Bonds - Mean: {Options.Bonds.MeanReturn:P1} Volatility: {Options.Bonds.Volatility:P1} | Cap: +/- 60%";

    }
}
