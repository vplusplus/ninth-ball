
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NinthBall.Core
{
    /// <summary>
    /// Historical regimes and their macro-economic characteristics.
    /// IMPORTANT: TODO: Move individual centroids into the Regime profile. Remove the pinky-promise
    /// </summary>
    public readonly record struct HRegimes
    (
        HRegimes.Z                  StandardizationParams,
        TwoDMatrix                  Centroids, 
        IReadOnlyList<HRegimes.R>   Regimes, 
        TwoDMatrix                  TransitionMatrix
    )
    {
        // Parameters required for standardization of features during inference.
        public readonly record struct Z
        (
            ReadOnlyMemory<double> Mean, 
            ReadOnlyMemory<double> StdDev
        );

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
    }

    internal sealed class HistoricalRegimes(SimulationSeed SimSeed, HistoricalReturns History)
    {
        public HRegimes Regimes => ThreeYearRegimes.Value; 
        
        // Discover regimes using 3-year blocks, once.
        readonly Lazy<HRegimes> ThreeYearRegimes = new( () =>
        {
            // BY-DESIGN: Use only three-year-blocks for regime discovery (This is not a tuneable configuration)
            int[] ThreeYearBlocksOnlyNotTwoFourOrFive = [3];

            // BY-DESIGN: Exactly four regimes (This is not a tuneable configuration)
            const int FourRegimesNotThreeOrFive = 4;

            // Simulation seed is our repeatable pseudo random seed.
            var R = new Random(SimSeed.Value);

            // Using three-year blocks, discover regimes and their characteristics.
            return History.History
                .ReadBlocks(ThreeYearBlocksOnlyNotTwoFourOrFive)
                .ToList()
                .DiscoverRegimes(R, FourRegimesNotThreeOrFive)
                ;
        });
    }

    internal static class HistoricalRegimesDiscovery
    {
        public static HRegimes DiscoverRegimes(this IReadOnlyList<HBlock> trainingBlocks, Random R, int numRegimes)
        {
            ArgumentNullException.ThrowIfNull(trainingBlocks);

            // Pre-check: We depend on cronology. Verify blocks are sorted by year & sequence length.
            if (!trainingBlocks.IsSortedByYearAndBlockLength()) throw new Exception("Invalid input: Blocks are not pre-sorted by Year and sequence length.");

            // Extract training features
            var featureMatrix = trainingBlocks.ToFeatureMatrix();

            // Learn the standardization parameters (mean and stddev)
            var standardizationParams = featureMatrix.DiscoverStandardizationParameters();

            // Standardize the features
            var standardizedFeatureMatrix = featureMatrix.StandardizeFeatureMatrix(standardizationParams);

            // Discover K-Mean clusters
            var clusters = standardizedFeatureMatrix.DiscoverClusters(R, numRegimes);

            // Map training blocks to regimes, calculate regime profile for each.
            var regimes = Enumerable.Range(0, clusters.NumClusters).Select(r => trainingBlocks.ComputeRegimeProfile(clusters, r)).ToArray();

            // Compute the probability of regime switching
            var transitionMatrix = clusters.ComputeRegimeTransitionMatrix();

            return new
            (
                StandardizationParams: standardizationParams,
                Centroids: clusters.Centroids,
                Regimes: regimes,
                TransitionMatrix: transitionMatrix
            );
        }

        // Extract features
        // TODO: CRITICAL: Features like MaxDrawdown are length-dependent.
        // There is no such thing as annualized MaxDrawDown.
        // Coin a new feature such that MaxDrawDown is comparable across 3/4/5-years
        public static TwoDMatrix ToFeatureMatrix(this IReadOnlyList<HBlock> blocks)
        {
            // One row per block, and five features per block.
            var matrix = new XTwoDMatrix(blocks.Count, 5);

            var idx = 0;
            foreach (var block in blocks)
            {
                matrix.Storage[idx++] = block.Features.NominalCAGRStocks;
                matrix.Storage[idx++] = block.Features.NominalCAGRBonds;
                matrix.Storage[idx++] = block.Features.MaxDrawdownStocks;
                matrix.Storage[idx++] = block.Features.MaxDrawdownBonds;
                matrix.Storage[idx++] = block.Features.GMeanInflationRate;
            }

            return matrix.ReadOnly;
        }

        // Extract the mean and stddev of the featureset
        public static HRegimes.Z DiscoverStandardizationParameters(this TwoDMatrix featureMatrix)
        {
            var numSamples  = featureMatrix.NumRows;
            var numFeatures = featureMatrix.NumColumns;
            var means       = new double[featureMatrix.NumColumns];
            var stdDevs     = new double[featureMatrix.NumColumns];

            // Calculate mean of each feature.
            for (int s = 0; s < numSamples; s++) means.Add(featureMatrix[s]);
            means.Divide(numSamples);

            // Calculate StdDev of each feature.
            for (int s = 0; s < numSamples; s++) stdDevs.SumSquaredDiff(sourceRow: featureMatrix[s], meanVector: means);
            stdDevs.Divide(numSamples);
            stdDevs.Sqrt();

            return new(Mean: means, StdDev: stdDevs);
        }

        // Standardize the feature matrix
        public static TwoDMatrix StandardizeFeatureMatrix(this in TwoDMatrix featureMatrix, HRegimes.Z standardizationParams)
        {
            var numSamples  = featureMatrix.NumRows;
            var numFeatures = featureMatrix.NumColumns;
            var means       = standardizationParams.Mean.Span;
            var stdDevs     = standardizationParams.StdDev.Span;
            
            // Prepare a target matrix, same shape as the source.
            XTwoDMatrix standardizedFeatureMatrix = new XTwoDMatrix(numSamples, numFeatures);

            // Perform Z-Score normalization of each row.
            for (int s = 0; s < numSamples; s++)
            {
                featureMatrix[s].ZNormalize(meanVector: means, stdDevVector: stdDevs, targetRow: standardizedFeatureMatrix[s]);
            }

            // Returns the z-normalized feature matrix.
            return standardizedFeatureMatrix.ReadOnly;
        }

        // Discover K-Mean clusters
        public static KMean.Result DiscoverClusters(this TwoDMatrix standardizedFeatureMatrix, Random R, int numClusters)
        {
            // Discover clusters (a.k.a. regimes)
            var elapsed = Stopwatch.StartNew();
            var (converged, iterations, kResult) = KMean.Cluster(standardizedFeatureMatrix, R, numClusters);
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

        public static HRegimes.R ComputeRegimeProfile(this IReadOnlyList<HBlock> blocks, KMean.Result clusters, int regimeId)
        {
            var clusterAssignments = clusters.Assignments.Span;

            // How many members are there in this regime?
            var memberCount = clusterAssignments.Count(regimeId);

            // TODO: Revisit what to do if the regime has no members?
            if (0 == memberCount) throw new FatalWarning($"Regime {regimeId} has no members.");

            // Storage to collect regime members' features
            int idx = 0;
            double[] stocks    = new double[memberCount];
            double[] bonds     = new double[memberCount];
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

        public static TwoDMatrix ComputeRegimeTransitionMatrix(this KMean.Result clusters)
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
            var matrix = new XTwoDMatrix(numRegimes, numRegimes);

            // Step1: Count the regime transitions.
            // One row per 'fromRegime'
            for (int i = 0; i < assignments.Length - 1; i++)
            {
                int fromRegime = assignments[i];
                int toRegime   = assignments[i + 1];
                matrix[fromRegime, toRegime]++;
            }

            // Step2: Normalize regime transitions counts to probabilities.
            // Note:  Normalize each row (local normalization)
            for (int r = 0; r < numRegimes; r++)
            {
                matrix[r].ToProbabilityDistribution();
            }

            return matrix.ReadOnly;
        }

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

        private static string CSVMetrics8F2(this ReadOnlyMemory<double> numbers) => string.Join(", ", MemoryMarshal.ToEnumerable(numbers).Select(x => $"{x,8:F2}"));

        #endregion

  
    }
}
