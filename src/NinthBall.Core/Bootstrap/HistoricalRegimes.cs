
using NinthBall.Core;
using System.Diagnostics;

namespace NinthBall.Core
{
    public readonly record struct RegimeSet(IReadOnlyList<RegimeSet.R> Regimes, RegimeSet.RX Transitions)
    {
        // Regimes and transition probabilities
        public readonly record struct R(int RegimeId, string RegimeLabel, RP Profile);

        // Describes characteristics of one Regime
        public readonly record struct RP
        (
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

        // Probability of transition from current regime to next
        public readonly record struct RX
        {
            public readonly double[][] Matrix { get; init; }

            public RX(double[][] matrix)
            {
                if (null == matrix
                    || 0 == matrix.Length
                    || matrix.Any(x => null == x)
                    || matrix.Any(x => x.Length != matrix.Length)
                    || matrix.Any(x => Math.Abs(1.0 - x.Sum()) > 1e-6)
                ) throw new ArgumentNullException("Invalid regime transition probability matrix.");

                Matrix = matrix;
            }

            public ReadOnlySpan<double> TransitionProbabilities(int fromId) => Matrix[fromId];
        }
    }

    internal static class HistoricalRegimesDiscovery
    {
        private readonly record struct TwoDMatrix(int NumSamples, int NumFeatures)
        {
            public readonly Memory<double> Storage = new double[NumSamples * NumFeatures];

            public readonly int Count = NumSamples;
            public readonly Span<double> this[int idx] => Storage.Slice(idx * NumFeatures, NumFeatures).Span;
        }

        public static RegimeSet DiscoverRegimes(IReadOnlyList<HBlock> blocks, Random R, int NumRegimes)
        {
            // Pre-check: We depend on cronology. Pre-check blocks are sorted by year & sequence length.
            if (!blocks.IsSortedByYearAndBlockLength()) throw new Exception("Invalid input: Blocks are not pre-sorted by Year and sequence length.");

            var elapsed = Stopwatch.StartNew();
            var clusters = blocks.DiscoverClusters(R, numClusters: NumRegimes);
            var regimes = blocks.ToRegimeSet(clusters);
            elapsed.Stop();

            Console.WriteLine($" Discovered {regimes.Regimes.Count} regimes | {elapsed.Elapsed.TotalMilliseconds:#,0} milliSec");
            return regimes;
        }

        //......................................................................
        #region Map clusters to region profiles and transition matrix
        //......................................................................
        private static RegimeSet.R[] ComputeRegimes(this IReadOnlyList<HBlock> blocks, KMean.Result clusters)
        {
            var numRegimes = clusters.NumClusters;

            RegimeSet.R[] regimes = new RegimeSet.R[numRegimes];
            for (int r = 0; r < numRegimes; r++)
            {
                var profile = blocks.ComputeRegimeProfile(clusters, r);
                var label   = profile.GuessRegimeLabel(r);

                regimes[r] = new
                (
                    RegimeId: r,
                    RegimeLabel: label,
                    Profile: profile
                );
            }

            return regimes;
        }

        private static RegimeSet.RP ComputeRegimeProfile(this IReadOnlyList<HBlock> blocks, KMean.Result clusters, int regimeId)
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
                stocks[idx] = blocks[i].Features.NominalCAGRStocks;
                bonds[idx] = blocks[i].Features.NominalCAGRBonds;
                inflation[idx] = blocks[i].Features.GMeanInflationRate;

                // Advance the sample index
                idx++;
            }

            // Calculate higher-order Moments for the parametric generator
            var mStocks    = ToMoment(stocks);
            var mBonds     = ToMoment(bonds);
            var mInvaltion = ToMoment(inflation);

            // Calculate Triangular Correlations (The "Financial DNA" of the regime)
            double sbCorr  = stocks.Correlation(bonds);
            double siCorr  = stocks.Correlation(inflation);
            double biCorr  = bonds.Correlation(inflation);

            return new RegimeSet.RP
            (
                StocksBondCorrelation: sbCorr,
                StocksInflationCorrelation: siCorr,
                BondsInflationCorrelation: biCorr,
                Stocks: mStocks,
                Bonds: mBonds,
                Inflation: mInvaltion
            );

            static RegimeSet.M ToMoment(double[] values) => new RegimeSet.M
            (
                Mean: values.Mean(),
                Volatility: values.StdDev(),
                Skewness: values.Skewness(),
                Kurtosis: values.Kurtosis(),
                AutoCorrelation: values.AutoCorrelation()
            );
        }

        private static RegimeSet.RX ComputeRegimeTransitionProbabilities(this KMean.Result clusters)
        {
            var assignments = clusters.Assignments.Span;
            int numRegimes = clusters.NumClusters;

            // Prepare memory footprint
            double[][] matrix = new double[numRegimes][];
            for (int i = 0; i < numRegimes; i++) matrix[i] = new double[numRegimes];

            // We are supposed to follow the cronology of the blocks.
            // However, index of the assignment is the index of the historical blocks.
            // This is a guarenteed contract since assignmens are not tuples, its just an array indexed by original sample index.
            for (int i = 0; i < assignments.Length - 1; i++)
            {
                int fromRegime = assignments[i];
                int toRegime = assignments[i + 1];
                matrix[fromRegime][toRegime]++;
            }

            // Normalize regime transitions counts to probabilities
            for (int i = 0; i < numRegimes; i++) matrix[i].ToProbabilityDistribution();

            return new(matrix);
        }

        #endregion

        //......................................................................
        #region DiscoverClusters()
        //......................................................................
        public static KMean.Result DiscoverClusters(this IReadOnlyList<HBlock> blocks, Random R, int numClusters)
        {
            // Prepare input for K-Mean clustering, extract the features and normalize.
            var normalizedFeatureMatrix = blocks.ToRegimeFeaturesMatrix().NormalizeFeatureMatrix();

            // Discover clusters (a.k.a. regimes)
            var elapsed = Stopwatch.StartNew();
            var (converged, iterations, kResult) = KMean.Cluster(normalizedFeatureMatrix.Storage, normalizedFeatureMatrix.NumFeatures, R, numClusters);
            elapsed.Stop();

            // Some diag.
            if (converged)
            {
                Console.WriteLine($" K-Mean: Discovered {kResult.NumClusters} clusters | {iterations} iterations | {elapsed.Elapsed.TotalMilliseconds:#,0.0} milliSec");
                Console.WriteLine($" K-Mean: TotalInertia: {kResult.Quality.TotalInertia} | SilhouetteScore: {kResult.Quality.SilhouetteScore} ");
            }

            return converged
                ? kResult
                : throw new FatalWarning($"K-Mean failed to converge even after {iterations} iterations");
        }

        static TwoDMatrix ToRegimeFeaturesMatrix(this IReadOnlyList<HBlock> blocks)
        {
            var matrix = new TwoDMatrix(blocks.Count, 5);

            int index = 0;
            var storage = matrix.Storage.Span;
            foreach (var block in blocks)
            {
                storage[index++] = block.Features.NominalCAGRStocks;
                storage[index++] = block.Features.NominalCAGRBonds;
                storage[index++] = block.Features.MaxDrawdownStocks;
                storage[index++] = block.Features.MaxDrawdownBonds;
                storage[index++] = block.Features.GMeanInflationRate;
            }

            return matrix;
        }

        static TwoDMatrix NormalizeFeatureMatrix(this in TwoDMatrix rawFeatureMatrix)
        {
            var numSamples = rawFeatureMatrix.NumSamples;
            var numFeatures = rawFeatureMatrix.NumFeatures;
            var normalizedMatrix = new TwoDMatrix(numSamples, numFeatures);

            // 1. Calculate Mean (Horizontal Aggregate)
            var means = new double[numFeatures].AsSpan();
            for (int s = 0; s < numSamples; s++)
            {
                means.Add(rawFeatureMatrix[s]);
            }
            means.Divide(numSamples);

            // 2. Calculate StdDev (Horizontal Aggregate)
            var stdDevs = new double[numFeatures].AsSpan();
            for (int s = 0; s < numSamples; s++)
            {
                stdDevs.SumSquaredDiff(rawFeatureMatrix[s], means);
            }
            stdDevs.Divide(numSamples);
            stdDevs.Sqrt();

            // 3. Perform Z-Score normalization (Horizontal Aggregate)
            for (int s = 0; s < numSamples; s++)
            {
                normalizedMatrix[s].ZNormalize(rawFeatureMatrix[s], means, stdDevs);
            }

            return normalizedMatrix;
        }

        private static RegimeSet ToRegimeSet(this IReadOnlyList<HBlock> blocks, KMean.Result clusters)
        {
            return new RegimeSet
            (
                blocks.ComputeRegimes(clusters),
                clusters.ComputeRegimeTransitionProbabilities()
            );
        }

        private static string GuessRegimeLabel(this RegimeSet.RP profile, int regimeId)
        {
            return $"Regime{regimeId}";
        }

        #endregion

        //......................................................................
        #region Validation utils
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

        #endregion

    }
}
