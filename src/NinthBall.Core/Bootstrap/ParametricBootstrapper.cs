
namespace NinthBall.Core
{
    internal sealed class ParametricBootstrapper(ParametricBootstrap Options, SimulationSeed SimSeed) : IBootstrapper
    {
        // We can produce theoretically unlimited possible combinations.
        public int GetMaxIterations(int numYears) => int.MaxValue;

        public IReadOnlyList<HROI> GetROISequence(int iterationIndex, int numYears)
        {
            // Use a deterministic seed based on iterationIndex and the global seed hint
            var random = new Random(PredictableHashCode.Combine(SimSeed.Value, iterationIndex));

            var sequence = new List<HROI>(numYears);

            // State for autocorrelation
            double prevZStocks = 0;
            double prevZBonds = 0;

            for (int i = 0; i < numYears; i++)
            {
                // 1. Generate independent normal deviates using InverseNormalCDF
                double u1 = NextSafeDouble(random);
                double u2 = NextSafeDouble(random);

                double z1 = MathUtils.InverseNormalCDF(u1);
                double z2 = MathUtils.InverseNormalCDF(u2);

                // 2. Apply Correlation (Cholesky)
                var (c1, c2) = MathUtils.Correlate(z1, z2, Options.StocksBondCorrelation);

                // 3. Apply AutoCorrelation (AR1)
                // Z_t = rho * Z_{t-1} + sqrt(1 - rho^2) * epsilon_t
                // This ensures the process remains standard normal (mean 0, var 1) if rho < 1.
                double arStocks = ApplyAR1(c1, prevZStocks, Options.Stocks.AutoCorrelation);
                double arBonds = ApplyAR1(c2, prevZBonds, Options.Bonds.AutoCorrelation);

                prevZStocks = arStocks;
                prevZBonds = arBonds;

                // 4. Apply Cornish-Fisher (Skewness/Kurtosis)
                double cfStocks = MathUtils.CornishFisher(arStocks, Options.Stocks.Skewness, Options.Stocks.Kurtosis);
                double cfBonds = MathUtils.CornishFisher(arBonds, Options.Bonds.Skewness, Options.Bonds.Kurtosis);

                // 5. Scale by Mean and Volatility
                // Cap returns at -100% (total loss) as you can't lose more than you have.
                double rStocks = Math.Max(-1.0, Options.Stocks.MeanReturn + cfStocks * Options.Stocks.Volatility);
                double rBonds = Math.Max(-1.0, Options.Bonds.MeanReturn + cfBonds * Options.Bonds.Volatility);

                sequence.Add(new HROI(i, rStocks, rBonds));
            }

            return sequence;
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
    }
}
