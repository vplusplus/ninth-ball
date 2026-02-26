using NinthBall.Utils;

namespace NinthBall.Core
{
    /// <summary>
    /// Generates repeatable synthetic sequence of returns using statistical parameters.
    /// </summary>
    sealed class RegimeAwareParametricBootstrapper(SimulationSeed SimSeed, BootstrapOptions Options, HistoricalRegimes HistoricalRegimes) : IBootstrapper
    {
        // We can produce theoretically unlimited possible combinations.
        int IBootstrapper.GetMaxIterations(int numYears) => int.MaxValue;

        /// <summary>
        /// Generates repeatable synthetic sequence of returns using statistical parameters.
        /// </summary>
        IROISequence IBootstrapper.GetROISequence(int iterationIndex, int numYears)
        {
            var regimes = HistoricalRegimes.Regimes;

            // Smooth the regime transition matrix.
            var adjustedRegimeTransitions = regimes.RegimeTransitions.ApplySmoothing(regimes.RegimeDistribution.Span, Options.RegimeAwareness);

            // Use a deterministic seed based on iterationIndex and the global seed hint
            var random   = new Random(PredictableHashCode.Combine(SimSeed.Value, iterationIndex));
            var sequence = new HROI[numYears];

            // Autocorrelation state maintained throughout the simulation sequence
            double prevZStocks = 0, prevZBonds = 0, prevZInflation = 0;

            // Start with a weighted random regime based on historical distribution
            var currentRegimeIdx = random.NextWeightedIndex(regimes.RegimeDistribution.Span);

            int year = 0;
            while (year < numYears)
            {
                var currentRegime = regimes.Regimes[currentRegimeIdx];

                // Randomly choose duration for this regime (3, 4, or 5 years) as requested
                int duration = Options.BlockSizes[random.Next(0, Options.BlockSizes.Count)];
                int periodEndYear = Math.Min(year + duration, numYears);

                for (; year < periodEndYear; year++)
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
                        currentRegime.StocksBondsCorrelation,
                        currentRegime.InflationStocksCorrelation,
                        currentRegime.InflationBondsCorrelation);

                    // Apply AutoCorrelation (AR1)
                    // Introduces momentum or persistence over time.
                    // Current year depends partly on previous year
                    // Z_t = rho * Z_{t-1} + sqrt(1 - rho^2) * epsilon_t
                    // Formula ensures the process remains standard normal (mean 0, var 1) if rho < 1.
                    double arStocks     = ApplyAR1(c1, prevZStocks,     currentRegime.Stocks.AutoCorrelation);
                    double arBonds      = ApplyAR1(c2, prevZBonds,      currentRegime.Bonds.AutoCorrelation);
                    double arInflation  = ApplyAR1(c3, prevZInflation,  currentRegime.Inflation.AutoCorrelation);

                    // Carry forward the optionally-mean-reversed values
                    prevZStocks     = arStocks;
                    prevZBonds      = arBonds;
                    prevZInflation  = arInflation;

                    // Apply Cornish-Fisher (Skewness/Kurtosis)
                    // Warps the bell curve.
                    // Negative skew ~> deeper crashes.
                    // High kurtosis ~> rare but extreme events.
                    double cfStocks     = Statistics.CornishFisher(arStocks,    currentRegime.Stocks.Skewness,    currentRegime.Stocks.Kurtosis);
                    double cfBonds      = Statistics.CornishFisher(arBonds,     currentRegime.Bonds.Skewness,     currentRegime.Bonds.Kurtosis);
                    double cfInflation  = Statistics.CornishFisher(arInflation, currentRegime.Inflation.Skewness, currentRegime.Inflation.Kurtosis);

                    // Scale by Mean and Volatility.
                    // Converts abstract scaling factor into percentage returns.
                    // Stats has no limits; but market does; clamp the extremes.
                    // The hard-coded clamps define the behavioral contract (i.e., “personality”) of the parametric bootstrapper.
                    // These values are not configuration knobs and must not be treated as tunable parameters.
                    double rStocks    = Math.Clamp(currentRegime.Stocks.Mean + cfStocks * currentRegime.Stocks.Volatility, -0.60, +0.60);
                    double rBonds     = Math.Clamp(currentRegime.Bonds.Mean + cfBonds * currentRegime.Bonds.Volatility, -0.15, +0.25);
                    double rInflation = Math.Clamp(currentRegime.Inflation.Mean + cfInflation * currentRegime.Inflation.Volatility, -0.10, +0.30);

                    sequence[year] = new HROI(0, rStocks, rBonds, rInflation);
                }

                // Transition to the next regime using the transition matrix
                currentRegimeIdx = random.NextWeightedIndex(adjustedRegimeTransitions[currentRegimeIdx]);
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

        public override string ToString() => $"Random sequence of {CSVBlockSizes}-year ROI and Inflation | Regime awareness: {Options.RegimeAwareness:P0}";
        string CSVBlockSizes => string.Join("/", Options.BlockSizes);

    }
}
