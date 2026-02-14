
using System.Diagnostics;
using System.Runtime.InteropServices;
using NinthBall.Utils;

namespace NinthBall.Core
{
    /// <summary>
    /// Historical regimes and their macro-economic characteristics.
    /// </summary>
    public readonly record struct HRegimes( HRegimes.Z StandardizationParams, IReadOnlyList<HRegimes.RP> Regimes)
    {
        // Parameters required for standardization of features during inference.
        public readonly record struct Z
        (
            ReadOnlyMemory<double> Mean, 
            ReadOnlyMemory<double> StdDev
        );

        // Regime profile: Describes characteristics of one Regime
        public readonly record struct RP
        (
            string                  RegimeLabel,
            ReadOnlyMemory<double>  Centroid,
            ReadOnlyMemory<double>  NextRegimeProbabilities,
            
            double StocksBondCorrelation,
            double StocksInflationCorrelation,
            double BondsInflationCorrelation,

            M Stocks,
            M Bonds,
            M Inflation
        );

        // Moments: Market dynamics of one flavor of asset in one regime.
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
        
        readonly Lazy<HRegimes> ThreeYearRegimes = new( () =>
        {
           const int FiveRegimes = 5;   
            int[] ThreeYearBlocksOnlyNotTwoFourOrFive = [3];    // BY-DESIGN: Use only three-year-blocks for regime discovery (This is not a tuneable configuration)

            // Using three-year blocks, discover regimes and their characteristics.
            return History.Returns
                .ReadBlocks(ThreeYearBlocksOnlyNotTwoFourOrFive)
                .ToList()
                .DiscoverRegimes(regimeDiscoverySeed: SimSeed.RegimeDiscoverySeed, numRegimes: FiveRegimes)
                ;
        });
    }

    internal static class HistoricalRegimesDiscovery
    {
        public static HRegimes DiscoverRegimes(this IReadOnlyList<HBlock> trainingBlocks, int regimeDiscoverySeed, int numRegimes)
        {
            ArgumentNullException.ThrowIfNull(trainingBlocks);

            // Pre-check: We depend on Chronology. Verify blocks are sorted by year & sequence length.
            if (!trainingBlocks.IsSortedByYearAndBlockLength()) throw new Exception("Invalid input: Blocks are not pre-sorted by year and sequence length.");

            // Extract training features
            var featureMatrix = trainingBlocks.ToFeatureMatrix();

            // Learn the standardization parameters (mean and stddev)
            var standardizationParams = featureMatrix.DiscoverStandardizationParameters();

            // Standardize the features
            var standardizedFeatureMatrix = featureMatrix.StandardizeFeatureMatrix(standardizationParams);

            // Discover K-Mean clusters
            var clusters = standardizedFeatureMatrix.DiscoverBestClusters(regimeDiscoverySeed, numRegimes);

            // Best K-Mean result -> HRegimes
            return clusters.ToHistoricalRegimes(trainingBlocks, standardizationParams);
        }

        // Extract features
        // TODO: IMPORTANT:
        // MaxDrawdown is length-dependent, not directly comparable across 3/4/5-year blocks
        // Technically, MaxDrawdown can't be annualized. Alternates are not effective.
        // This is a nagging issue, still open.
        public static TwoDMatrix ToFeatureMatrix(this IReadOnlyList<HBlock> blocks)
        {
            // One row per block, and five features per block.
            var matrix = new XTwoDMatrix(NumRows: blocks.Count, NumColumns: 5);

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

        //......................................................................
        #region KMean training loop - Pick best result
        //......................................................................
        public static KMean.Result DiscoverBestClusters(this in TwoDMatrix standardizedFeatureMatrix, in int regimeDiscoverySeed, in int numClusters)
        {
            const int    NumTrainings               = 50;       // Train 50 times, find the best (BY-DESIGN: Sensitive; Not configurable)
            const int    MaxIterationsPerTraining   = 100;      // We typically converge in less than 10 iterations (BY-DESIGN: Sensitive; Not configurable)
            const double MinClusterSizePCT          = 0.05;     // Min 5% of the sample size (BY-DESIGN: Sensitive; Not configurable)

            // Compute the minimum allowed cluster size.
            int minAllowedClusterSize = Math.Max(1, (int)(standardizedFeatureMatrix.NumRows * MinClusterSizePCT));

            // Best result so far. What is best? See rejection logic below.
            KMean.Result? bestResult = default;

            var elapsed = Stopwatch.StartNew();
            for (int attempt = 0; attempt < NumTrainings; attempt++)
            {
                // Training specific pseudo random generator
                var R = new Random(PredictableHashCode.Combine(regimeDiscoverySeed, attempt));

                // Train
                var (converged, iter, nextResult) = KMean.Cluster
                (
                    standardizedFeatureMatrix, 
                    R: R,
                    K: numClusters, 
                    maxIterations: MaxIterationsPerTraining
                );

                // Reject clusters that didn't converge.
                if (!converged) continue;

                // Reject degenerate clusters (This also eliminates zero-member-clusters)
                if (nextResult.HasDegenerateClusters(minAllowedClusterSize)) continue;

                // Ignore if the SilhouetteScore is inferior.
                if (bestResult.HasValue && nextResult.Quality.Silhouette < bestResult.Value.Quality.Silhouette) continue;

                // Converged, no degenerate clusters and better SilhouetteScore. Keep it.
                bestResult = nextResult;
            }
            elapsed.Stop();

            return bestResult.HasValue
                ? bestResult.Value
                : throw new FatalWarning($"K-Means failed to find any valid clustering | {NumTrainings} trainings | {MaxIterationsPerTraining} iter/training | MinClusterSize: {minAllowedClusterSize}");
        }

        public static bool HasDegenerateClusters(this KMean.Result kResult, int minAcceptableClusterSize)
        {
            var assignments = kResult.Assignments.Span;

            for(int c = 0; c < kResult.NumClusters; c++)
            {
                var memberCount = assignments.Count(c);
                if (memberCount < minAcceptableClusterSize) return true;
            }

            return false;
        }


        #endregion

        //......................................................................
        #region KMean.Result -> HRegimes
        //......................................................................
        static HRegimes ToHistoricalRegimes(this KMean.Result clusters, IReadOnlyList<HBlock> trainingBlocks, HRegimes.Z standardizationParams)
        {
            // Compute the probability of regime switching
            var transitionMatrix = clusters.ComputeRegimeTransitionMatrix();

            // Map training blocks to regimes, calculate regime profile for each.
            var regimes = Enumerable.Range(0, clusters.NumClusters).Select(r => trainingBlocks.ComputeRegimeProfile(clusters, transitionMatrix, r)).ToArray();

            return new
            (
                StandardizationParams: standardizationParams,
                Regimes: regimes
            );
        }

        static TwoDMatrix ComputeRegimeTransitionMatrix(this KMean.Result clusters)
        {
            // Ideally, we would follow the chronological order of the blocks.
            // Instead, we rely on the assignment index.
            // The assignment index corresponds directly to the index of the historical blocks.
            // This is a safe assumption because features input to K-Means is immutable and
            // K-Means returns an array aligned to the original sample order, not tuples or reordered results.
            // Also, we are arranging the 2D-square-transition-matrix as a flat array.

            var assignments = clusters.Assignments.Span;
            int numRegimes = clusters.NumClusters;

            // Square matrix
            var matrix = new XTwoDMatrix(numRegimes, numRegimes);

            // Step1: Count the regime transitions.
            // One row per 'fromRegime'
            for (int i = 0; i < assignments.Length - 1; i++)
            {
                int fromRegime = assignments[i];
                int toRegime = assignments[i + 1];
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

        static HRegimes.RP ComputeRegimeProfile(this IReadOnlyList<HBlock> blocks, KMean.Result clusters, TwoDMatrix transitionProbabilities, int regimeId)
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

            // Capture the centroid of this regime.
            var centroid = clusters.Centroids.Row(regimeId);

            // Capture probabilty of transition from current regime to other regimes
            var txProbabilities = transitionProbabilities.Row(regimeId);

            // Optional label to the regime (for display only)
            var regimeLabel = GuessRegimeLabel(regimeId, sbCorr, siCorr, biCorr, mStocks, mBonds, mInflation);

            return new HRegimes.RP
            (
                RegimeLabel:     regimeLabel,
                Centroid:        centroid,
                NextRegimeProbabilities: txProbabilities,

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

        #endregion

        //......................................................................
        #region Extensions to support inference
        //......................................................................
        public static int FindNearestRegime(this HRegimes regimes, ReadOnlySpan<double> standardizedFeatures)
        {
            int nearestIndex = 0;
            double minDistance = standardizedFeatures.EuclideanDistanceSquared(regimes.Regimes[0].Centroid.Span);

            for (int regimeIdx = 1; regimeIdx < regimes.Regimes.Count; regimeIdx++)
            {
                double distance = standardizedFeatures.EuclideanDistanceSquared(regimes.Regimes[regimeIdx].Centroid.Span);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestIndex = regimeIdx;
                }
            }

            return nearestIndex;
        }

        // Re-construct (copy) regime transitons as 2D matrix.
        public static TwoDMatrix GetRegimeTransitionMatrix(this HRegimes regimes)
        {
            int numRegimes = regimes.Regimes.Count;
            var matrix = new XTwoDMatrix(numRegimes, numRegimes);

            for (int i = 0; i < numRegimes; i++)
            {
                regimes.Regimes[i].NextRegimeProbabilities.Span.CopyTo(matrix[i]);
            }

            return matrix.ReadOnly;
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

        static string GuessRegimeLabel(int regimeId, double sbCorrelation, double siCorrelation, double biCorrelation, HRegimes.M mStocks, HRegimes.M mBonds, HRegimes.M mInflation)
        {
            //// Priority 1: Crisis (Severe pain or extreme uncertainty)
            //if (mStocks.Mean < -0.10 || mStocks.Volatility > 0.20) return "Crisis";

            //// Priority 2: Stagflation (High cost of living + poor growth)
            //if (mInflation.Mean > 0.05 && mStocks.Mean < 0.02) return "Stagflation";

            //// Priority 3: Balanced Growth (Modern ideal, diversification works)
            //if (mStocks.Mean > 0.06 && sbCorrelation < 0.0) return "Balanced";

            //// Priority 4: Bull Market (General vigor)
            //if (mStocks.Mean > 0.10) return "Bull";

            //// Priority 5: Stagnation (The "Lost Decade")
            //if (mStocks.Mean < 0.02 && mStocks.Mean > -0.05) return "Stagnation";

            // Fallback: Statistical Label
            return $"Regime{regimeId}";
        }

        //static void PrettyPrint(this KMean.Result bestResult, int numTraining, TimeSpan elapsed)
        //{
        //    var Q = bestResult.Quality;

        //    Console.WriteLine($" K-Mean: Discovered {bestResult.NumClusters} clusters | {numTraining} restarts | {elapsed.TotalMilliseconds:#,0} milliSec");
        //    Console.WriteLine($" K-Mean: Silhouette: {Q.Silhouette:F2} | TotalInertia: {Q.Inertia:F2})");
        //    Console.WriteLine($" K-Mean: Inertia     : [{CSVMetrics8F2(Q.ClusterInertia)}]");
        //    Console.WriteLine($" K-Mean: Silhouette  : [{CSVMetrics8F2(Q.ClusterSilhouette)}]");

        //    static string CSVMetrics8F2(ReadOnlyMemory<double> numbers) => string.Join(", ", MemoryMarshal.ToEnumerable(numbers).Select(x => $"{x,8:F2}"));
        //}
        

        #endregion

 
    }
}
