
using NinthBall.Core;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NinthBall.Core
{
    /// <summary>
    /// Historical regimes and their macroeconomic behaviors.
    /// BY-DESIGN: Transition matrix is NOT immutable for performance & friendly-serialization 
    /// </summary>
    public readonly record struct HRegimes(IReadOnlyList<HRegimes.R> Regimes, ReadOnlyMemory<double> TransitionMatrix)
    {
        // Describes characteristics of one Regime
        public readonly record struct R
        (   
            string RegimeLabel, 
            double StocksBondCorrelation,
            double StocksInflationCorrelation,
            double BondsInflationCorrelation,
            M Stocks,
            M Bonds,
            M Inflation
        );

        // Moments of one flavor of asset in one regime.
        public readonly record struct M
        (
            double Mean,
            double Volatility,
            double Skewness,
            double Kurtosis,
            double AutoCorrelation
        );

        // Matrix is square, and validated elsewhere.
        public readonly ReadOnlySpan<double> TransitionProbabilities(int fromRegime) => TransitionMatrix.Slice(fromRegime * Regimes.Count, Regimes.Count).Span;
    }

    internal static class HistoricalRegimesDiscovery
    {
        private readonly record struct TwoDMatrix(int NumRows, int NumColumns)
        {
            // Mutable: Full data
            public readonly double[] Storage = new double[NumRows * NumColumns];

            // Mutable: One row
            public readonly Span<double> this[int idx] => Storage.AsSpan().Slice(idx * NumColumns, NumColumns);

            // Mutable: One cell
            public double this[int row, int col]
            {
                get => this[row][col];
                set => this[row][col] = value;
            }

            public readonly ReadOnlyMemory<double> AsReadOnly() => Storage;
        }

        public static HRegimes DiscoverRegimes(IReadOnlyList<HBlock> blocks, Random R, int NumRegimes)
        {
            ArgumentNullException.ThrowIfNull(blocks);

            // Pre-check: We depend on cronology. Verify blocks are sorted by year & sequence length.
            if (!blocks.IsSortedByYearAndBlockLength()) throw new Exception("Invalid input: Blocks are not pre-sorted by Year and sequence length.");

            // Discover K-Mean clusters. Map K-Mean clusters to regimes.
            var elapsed = Stopwatch.StartNew();
            var regimes = blocks.DiscoverClusters(R, numClusters: NumRegimes).ToRegimeSet(blocks);
            elapsed.Stop();

            Console.WriteLine($" Discovered {regimes.Regimes.Count} regimes | {elapsed.Elapsed.TotalMilliseconds:#,0} milliSec");
            return regimes;
        }

        //......................................................................
        #region Map K-Mean clusters to regimes and transition matrix
        //......................................................................
        public static HRegimes ToRegimeSet(this KMean.Result clusters, IReadOnlyList<HBlock> blocks) => new 
        (
            Regimes:          Enumerable.Range(0, clusters.NumClusters).Select(r => blocks.ComputeRegimeProfile(clusters, r)).ToArray(),
            TransitionMatrix: clusters.ComputeRegimeTransitionMatrix()
        );

        static HRegimes.R ComputeRegimeProfile(this IReadOnlyList<HBlock> blocks, KMean.Result clusters, int regimeId)
        {
            var clusterAssignments = clusters.Assignments.Span;

            // How many members are there in this regime?
            var memberCount = clusterAssignments.Count(regimeId);

            // TODO: Revisit what to do if the regime has no members?
            if (0 == memberCount) throw new FatalWarning($"Regime {regimeId} has no members.");

            // Storage to collect regime members' features
            int idx = 0;
            double[] stocks = new double[memberCount];
            double[] bonds = new double[memberCount];
            double[] inflation = new double[memberCount];

            for (int i = 0; i < blocks.Count; i++)
            {
                // Skip blocks that are not part of current regime.
                if (clusterAssignments[i] != regimeId) continue;

                // This is my blocks. Collect features we care.
                stocks[idx]     = blocks[i].Features.NominalCAGRStocks;
                bonds[idx]      = blocks[i].Features.NominalCAGRBonds;
                inflation[idx]  = blocks[i].Features.GMeanInflationRate;

                // Advance the sample index
                idx++;
            }

            // Calculate higher-order Moments for the parametric generator
            var mStocks    = ToMoment(stocks);
            var mBonds     = ToMoment(bonds);
            var mInflation = ToMoment(inflation);

            // Calculate Triangular Correlations (The "Financial DNA" of the regime)
            double sbCorr  = stocks.Correlation(bonds);
            double siCorr  = stocks.Correlation(inflation);
            double biCorr  = bonds.Correlation(inflation);

            // Optional label to the regime (for display only)
            var regimeLabel = GuessRegimeLabel(regimeId, sbCorr, siCorr, biCorr, mStocks, mBonds, mInflation);

            return new HRegimes.R
            (
                regimeLabel,

                StocksBondCorrelation:      sbCorr,
                StocksInflationCorrelation: siCorr,
                BondsInflationCorrelation:  biCorr,

                Stocks:     mStocks,
                Bonds:      mBonds,
                Inflation:  mInflation
            );

            static HRegimes.M ToMoment(double[] values) => new HRegimes.M
            (
                Mean:            values.Mean(),
                Volatility:      values.StdDev(),
                Skewness:        values.Skewness(),
                Kurtosis:        values.Kurtosis(),
                AutoCorrelation: values.AutoCorrelation()
            );
        }

        static ReadOnlyMemory<double> ComputeRegimeTransitionMatrix(this KMean.Result clusters)
        {
            // Ideally, we would follow the chronological order of the blocks.
            // Instead, we rely on the assignment index.
            // The assignment index corresponds directly to the index of the historical blocks.
            // This is a safe assumption because features input to K-Means is immutable and
            // K-Means returns an array aligned to the original sample order, not tuples or reordered results.
            // Also, we are arranging the 2D-square-transition-matrix as a flat array.

            var assignments = clusters.Assignments.Span;
            int numRegimes  = clusters.NumClusters;

            // Square matrix
            var matrix = new TwoDMatrix(numRegimes, numRegimes);

            // Step1: Count the regime transitions.
            // One row per 'fromRegime'
            for (int i = 0; i < assignments.Length - 1; i++)
            {
                int fromRegime = assignments[i];
                int toRegime   = assignments[i + 1];
                matrix[fromRegime, toRegime]++;
            }

            // Step2: Normalize regime transitions counts to probabilities.
            // Normalize each row (local normalization)
            for (int r = 0; r < numRegimes; r++)
            {
                matrix[r].ToProbabilityDistribution();
            }

            return matrix.AsReadOnly();
        }

        static string GuessRegimeLabel(int regimeId, double sbCorrelation, double siCorrelation, double biCorrelation, HRegimes.M mStocks, HRegimes.M mBonds, HRegimes.M mInflation)
        {
            // Priority 1: Crisis (Severe pain or extreme uncertainty)
            if (mStocks.Mean < -0.10 || mStocks.Volatility > 0.20) return "Crisis";

            // Priority 2: Stagflation (High cost of living + poor growth)
            if (mInflation.Mean > 0.05 && mStocks.Mean < 0.02) return "Stagflation";

            // Priority 3: Balanced Growth (Modern ideal, diversification works)
            if (mStocks.Mean > 0.06 && sbCorrelation < 0.0) return "Balanced Growth";

            // Priority 4: Bull Market (General vigor)
            if (mStocks.Mean > 0.10) return "Bull Market";

            // Priority 5: Stagnation (The "Lost Decade")
            if (mStocks.Mean < 0.02 && mStocks.Mean > -0.05) return "Stagnation";

            // Fallback: Statistical Label
            return $"Regime{regimeId}";
        }

        #endregion

        //......................................................................
        #region DiscoverClusters()
        //......................................................................
        public static KMean.Result DiscoverClusters(this IReadOnlyList<HBlock> blocks, Random R, int numClusters)
        {
            // Prepare input for K-Mean clustering, extract the features and normalize.
            var normalizedFeatureMatrix = blocks
                .ToFeaturesMatrix()
                .NormalizeFeatureMatrix();

            // Discover clusters (a.k.a. regimes)
            var elapsed = Stopwatch.StartNew();
            var (converged, iterations, kResult) = KMean.Cluster(normalizedFeatureMatrix.Storage, normalizedFeatureMatrix.NumColumns, R, numClusters);
            elapsed.Stop();

            // Some diag.
            if (converged)
            {
                Console.WriteLine($" K-Mean: Discovered {kResult.NumClusters} clusters | {iterations} iterations | {elapsed.Elapsed.TotalMilliseconds:#,0} milliSec");
                Console.WriteLine($" K-Mean: TotalInertia: {kResult.Quality.TotalInertia:F2} | SilhouetteScore: {kResult.Quality.SilhouetteScore:F2}");
                Console.WriteLine($" K-Mean: Inertia     : [{kResult.Quality.ClusterInertia.CSVMetrics8F2()}]");
                Console.WriteLine($" K-Mean: Silhouette  : [{kResult.Quality.ClusterSilhouette.CSVMetrics8F2()}]");
            }

            return converged
                ? kResult
                : throw new FatalWarning($"K-Mean failed to converge even after {iterations} iterations");
        }

        static TwoDMatrix ToFeaturesMatrix(this IReadOnlyList<HBlock> blocks)
        {
            // One row per block, and five features per block.
            var matrix = new TwoDMatrix(blocks.Count, 5);

            var idx = 0;
            foreach (var block in blocks)
            {
                matrix.Storage[idx++] = block.Features.NominalCAGRStocks;
                matrix.Storage[idx++] = block.Features.NominalCAGRBonds;
                matrix.Storage[idx++] = block.Features.MaxDrawdownStocks;
                matrix.Storage[idx++] = block.Features.MaxDrawdownBonds;
                matrix.Storage[idx++] = block.Features.GMeanInflationRate;
            }

            return matrix;
        }

        static TwoDMatrix NormalizeFeatureMatrix(this in TwoDMatrix featureMatrix)
        {
            var numSamples  = featureMatrix.NumRows;
            var numFeatures = featureMatrix.NumColumns;

            // Calculate Mean value of each feature 
            var means = new double[numFeatures].AsSpan();
            for (int s = 0; s < numSamples; s++)
            {
                var features = featureMatrix[s];
                means.Add(features);
            }
            means.Divide(numSamples);

            // Calculate StdDev of each feature.
            var stdDevs = new double[numFeatures].AsSpan();
            for (int s = 0; s < numSamples; s++)
            {
                var features = featureMatrix[s];
                stdDevs.SumSquaredDiff( sourceRow: features, meanVector: means);
            }
            stdDevs.Divide(numSamples);
            stdDevs.Sqrt();

            // Prepare a target matrix, same shape as the source.
            TwoDMatrix normalizedFeatureMatrix = new TwoDMatrix(featureMatrix.NumRows, featureMatrix.NumColumns);

            // Perform Z-Score normalization of each row.
            for (int s = 0; s < numSamples; s++)
            {
                // Use named arguments, too many params of same kind.
                featureMatrix[s].ZNormalize(meanVector: means, stdDevVector: stdDevs, targetRow: normalizedFeatureMatrix[s]);
            }

            return normalizedFeatureMatrix;
        }

        #endregion

        //......................................................................
        #region utils
        //......................................................................
        static bool IsSortedByYearAndBlockLength(this IReadOnlyList<HBlock> blocks)
        {
            for (int i = 1; i < blocks.Count; i++)
            {
                var prev = blocks[i - 1];
                var curr = blocks[i];

                // Years must be non-descending
                if (curr.StartYear < prev.StartYear) return false;

                // If years are same, length must be non-descending
                if (curr.StartYear == prev.StartYear && curr.Slice.Length < prev.Slice.Length) return false;
            }
            return true;
        }

        private static string CSVMetrics8F2(this ReadOnlyMemory<double> numbers) => string.Join(", ", MemoryMarshal.ToEnumerable(numbers).Select(x => $"{x,8:F2}"));

        #endregion

    }
}
