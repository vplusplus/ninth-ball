namespace NinthBall.Core
{
    //..........................................................................
    #region Models - HRegimes, Regime, Moments and ZParams
    //..........................................................................
    // Historical regimes and their macro-economic characteristics.
    public readonly record struct HRegimes
    (
        ZParams                 ZParams,                // Standardization parameters, discovered during training, required for inference.
        TwoDMatrix              ZCentroids,             // Mid-point of each regime in z-normalized feature space.
        ReadOnlyMemory<double>  RegimeDistribution,     // Cluster membership (counts) represented as pct.
        TwoDMatrix              RegimeTransitions,      // Regime transitions discovered from training samples
        IReadOnlyList<Regime>   Regimes                 // The regimes and their macro-economic characteristics
    );

    // Macro-economic characteristics of one Cluster/Regime
    public readonly record struct Regime
    (
        int     RegimeIdx,                              // Stable Idx of the regime, indexed into other related structures.
        string  RegimeLabel,                            // Friendly label, an approximation, has no relevance on model behavior.
        double  StocksBondsCorrelation,                 // Macro-economic indicators
        double  InflationStocksCorrelation,
        double  InflationBondsCorrelation,
        Moments Stocks,
        Moments Bonds,
        Moments Inflation
    );

    // Market dynamics of one flavor of asset in one clueter/regime.
    public readonly record struct Moments(double Mean, double Volatility, double Skewness, double Kurtosis, double AutoCorrelation);

    // Features standardization parameters (discovered during training, required for inference)
    public readonly record struct ZParams(ReadOnlyMemory<double> Mean, ReadOnlyMemory<double> StdDev);

    #endregion

    internal sealed class HistoricalRegimes(SimulationSeed SimSeed, HistoricalReturns History)
    {
        public HRegimes Regimes => ThreeYearRegimes.Value; 
        
        readonly Lazy<HRegimes> ThreeYearRegimes = new( () =>
        {
            // Why: Five regimes gave balanced clusters and less-diagonal matrix (This is not a tuneable configuration)
            const int FiveRegimes = 5;

            // Why: Feeding 3/4/5 will result redundant info and strong diagonal. (This is not a tuneable configuration)
            int[] ThreeYearBlocksOnly = [ 3 ];    

            // Using 3-year blocks, discover 5-regimes and their characteristics.
            return History.Returns
                .ReadBlocks(ThreeYearBlocksOnly)
                .DiscoverRegimes(regimeDiscoverySeed: SimSeed.RegimeDiscoverySeed, numRegimes: FiveRegimes)
                ;
        });
    }

    internal static class HistoricalRegimesDiscovery
    {
        public static HRegimes DiscoverRegimes(this IReadOnlyList<HBlock> trainingBlocks, int regimeDiscoverySeed, int numRegimes)
        {
            ArgumentNullException.ThrowIfNull(trainingBlocks);

            return trainingBlocks
                .EnsureSortedByYearAndBlockLength()                    
                .ExtractFeatures()
                .DiscoverStandardizationParameters(out var standardizationParams)
                .StandardizeFeatureMatrix(standardizationParams)
                .DiscoverBestClusters(trainingSeed: regimeDiscoverySeed, K: numRegimes, numTrainings: 50)
                .ToHistoricalRegimes(trainingBlocks, standardizationParams);
        }

        // Extract features
        public static TwoDMatrix ExtractFeatures(this IReadOnlyList<HBlock> blocks)
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

        // Extract the mean and stddev of the feature-matrix
        public static TwoDMatrix DiscoverStandardizationParameters(this TwoDMatrix featureMatrix, out ZParams standardizationParameters)
        {
            var numSamples  = featureMatrix.NumRows;
            var numFeatures = featureMatrix.NumColumns;

            // Working memory to track mean and stddev of each feature.
            var means       = new double[numFeatures];
            var stdDevs     = new double[numFeatures];

            // Calculate mean of each feature.
            for (int s = 0; s < numSamples; s++) means.Add(featureMatrix[s]);
            means.Divide(numSamples);

            // Calculate StdDev of each feature.
            for (int s = 0; s < numSamples; s++) stdDevs.SumSquaredDiff(sourceRow: featureMatrix[s], meanVector: means);
            stdDevs.Divide(numSamples);
            stdDevs.Sqrt();

            standardizationParameters = new(Mean: means, StdDev: stdDevs);
            return featureMatrix;
        }

        // Standardize the feature-matrix
        public static TwoDMatrix StandardizeFeatureMatrix(this in TwoDMatrix featureMatrix, ZParams standardizationParams)
        {
            var means       = standardizationParams.Mean.Span;
            var stdDevs     = standardizationParams.StdDev.Span;
            
            // Prepare a target matrix, same shape as the source.
            XTwoDMatrix standardizedFeatureMatrix = new(featureMatrix.NumRows, featureMatrix.NumColumns);

            // Perform Z-Score normalization of each row.
            for (int i = 0; i < featureMatrix.NumRows; i++)
                featureMatrix[i].ZNormalize(means, stdDevs, targetRow: standardizedFeatureMatrix[i]);

            // Returns the z-normalized feature matrix.
            return standardizedFeatureMatrix.ReadOnly;
        }

        //......................................................................
        #region KMean.Result -> HRegimes
        //......................................................................
        static HRegimes ToHistoricalRegimes(this KMean.Result clusters, IReadOnlyList<HBlock> trainingBlocks, ZParams standardizationParams)
        {
            return new HRegimes
            (
                ZParams:            standardizationParams,
                ZCentroids:         clusters.Centroids,
                RegimeDistribution: ComputeRegimeDistribution(clusters),
                RegimeTransitions:  ComputeRegimeTransitionMatrix(clusters),
                Regimes:            ComputeRegimeProfiles(trainingBlocks, clusters).AdjustRegimeLabels()
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

            // Count the regime transitions.
            for (int i = 0; i < assignments.Length - 1; i++)
            {
                int fromRegime = assignments[i];
                int toRegime   = assignments[i + 1];
                matrix[fromRegime, toRegime]++;
            }

            // Normalize each row (local normalization) from counts to probabilities.
            for (int r = 0; r < numRegimes; r++)
            {
                matrix[r].ToProbabilityDistribution();
            }

            return matrix.ReadOnly;
        }

        static ReadOnlyMemory<double> ComputeRegimeDistribution(this KMean.Result clusters)
        {
            var assignments  = clusters.Assignments.Span;

            // Count the members
            // Translate counts to probabilities
            var distribution = new double[clusters.NumClusters];
            for (int rIdx = 0; rIdx < clusters.NumClusters; rIdx++) distribution[rIdx] = assignments.Count(rIdx);
            distribution.ToProbabilityDistribution();

            return distribution;
        }

        static Regime[] ComputeRegimeProfiles(this IReadOnlyList<HBlock> blocks, KMean.Result clusters)
        {
            var assignments = clusters.Assignments.Span;
            var regimes     = new Regime[clusters.NumClusters];

            for(int regimeIdx = 0; regimeIdx < clusters.NumClusters; regimeIdx++)
            {
                // How many members are there in this regime?
                // Clusters will never be empty since we are rejecting degenerate clusters.
                var memberCount = assignments.Count(regimeIdx);
                if (0 == memberCount) throw new FatalWarning($"Regime {regimeIdx} has no members.");

                // Storage to collect current regime members' features
                int idx = 0;
                double[] stocks    = new double[memberCount];
                double[] bonds     = new double[memberCount];
                double[] inflation = new double[memberCount];

                for (int i = 0; i < blocks.Count; i++)
                {
                    // Skip blocks that are not part of current regime.
                    if (assignments[i] == regimeIdx)
                    {
                        // This is my blocks. Collect features we care.
                        stocks[idx]    = blocks[i].Features.NominalCAGRStocks;
                        bonds[idx]     = blocks[i].Features.NominalCAGRBonds;
                        inflation[idx] = blocks[i].Features.GMeanInflationRate;

                        // Advance the sample index
                        idx++;
                    }
                }

                regimes[regimeIdx] = new Regime
                (
                    RegimeIdx:   regimeIdx,
                    RegimeLabel: $"TBD{regimeIdx}",

                    StocksBondsCorrelation:     stocks.Correlation(bonds),
                    InflationStocksCorrelation: inflation.Correlation(stocks),
                    InflationBondsCorrelation:  inflation.Correlation(bonds),
                    
                    Stocks:     ToMoments(stocks),
                    Bonds:      ToMoments(bonds),
                    Inflation:  ToMoments(inflation)
                );
            }

            return regimes;

            static Moments ToMoments(double[] values) => new Moments
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
        public static int FindNearestRegime(this HRegimes histroricalRegimes, ReadOnlySpan<double> zBlockFeatures)
        {
            var regimeCentroids = histroricalRegimes.ZCentroids;

            // Pick a regime, pretend that is nearest (we picked first regime here)
            var nearestRegimeIdx = histroricalRegimes.Regimes[0].RegimeIdx;
            var minDistance      = zBlockFeatures.EuclideanDistanceSquared(regimeCentroids[nearestRegimeIdx]);

            foreach(var nextRegime in histroricalRegimes.Regimes)
            {
                var distance = zBlockFeatures.EuclideanDistanceSquared(regimeCentroids[nextRegime.RegimeIdx]);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestRegimeIdx = nextRegime.RegimeIdx;
                }
            }

            return nearestRegimeIdx;
        }

        public static TwoDMatrix ApplySmoothing(this TwoDMatrix regimeTransitionMatrix, ReadOnlySpan<double> targetDistribution, double regimeAwareness)
        {
            if (regimeAwareness < 0.0 || regimeAwareness > 1.0) throw new ArgumentException("RegimeAwareness must range from 0.0 to 1.0");
            if (regimeTransitionMatrix.NumColumns != regimeTransitionMatrix.NumRows) throw new ArgumentException("RegimeTransitions must be a square matrix.");
            if (targetDistribution.Length != regimeTransitionMatrix.NumColumns) throw new ArgumentException($"Regime count mismatch | RegimeTransitionMatrix says {regimeTransitionMatrix.NumColumns} | RegimeDistribution says {targetDistribution.Length}");

            int numRegmes = regimeTransitionMatrix.NumRows;
            var adjustedTransitionMatrix = new XTwoDMatrix(numRegmes, numRegmes);

            double lambda = regimeAwareness;
            double complement = 1 - lambda;

            for (int row = 0; row < numRegmes; row++)
            {
                for (int col = 0; col < numRegmes; col++)
                {
                    // As regime awareness approach zero, transition probability approach the target.
                    double pCurrent = regimeTransitionMatrix[row, col];
                    double pTarget = targetDistribution[col];
                    adjustedTransitionMatrix[row, col] = (lambda * pCurrent) + (complement * pTarget);
                }
            }

            return adjustedTransitionMatrix.ReadOnly;
        }

        #endregion

        //......................................................................
        #region utils
        //......................................................................
        static IReadOnlyList<HBlock> EnsureSortedByYearAndBlockLength(this IReadOnlyList<HBlock> blocks)
        {
            for (int i = 1; i < blocks.Count; i++)
            {
                var prev = blocks[i - 1];
                var curr = blocks[i];

                var notGood = 
                    (curr.StartYear < prev.StartYear) ||
                    (curr.StartYear == prev.StartYear && curr.Slice.Length < prev.Slice.Length);

                if (notGood) throw new Exception($"Blocks are not pre-sorted by year and sequence length | {curr.StartYear}#{curr.Slice.Length} | {prev.StartYear}#{prev.Slice.Length}");
            }

            return blocks;
        }

        static Regime[] AdjustRegimeLabels(this Regime[] profiles)
        {
            ArgumentNullException.ThrowIfNull(profiles);
            if (0 == profiles.Length) return profiles;

            double BullScore(Regime p) =>
                + p.Stocks.Mean
                - p.Stocks.Volatility
                + p.Stocks.Skewness
                - p.Stocks.Kurtosis
                - p.Bonds.Mean;

            double CrisisScore(Regime p) =>
                - p.Stocks.Mean
                + p.Stocks.Volatility
                - p.Stocks.Skewness
                + p.Stocks.Kurtosis
                + p.Bonds.Mean;

            double InflationScore(Regime p) =>
                + p.Inflation.Mean
                + p.Inflation.Volatility
                - p.Bonds.Mean;

            double RecoveryScore(Regime p) =>
                + (p.Stocks.Mean > 0 ? p.Stocks.Mean : -1.0)    // Penalize if negative
                + p.Stocks.Skewness                             // Look for the "Bounce"
                + (p.Stocks.Volatility * 0.5)                   // Volatility is "Excitement" here
                - p.Bonds.Mean;                                 // Rates are usually stabilizing

            double StagnationScore(Regime p) =>
                - Math.Abs(p.Stocks.Mean)                       // Reward being closest to 0
                - p.Stocks.Volatility                           // Reward low volatility (boringness)
                - Math.Abs(p.Inflation.Mean)                    // Reward low/stable inflation
                - p.Stocks.Kurtosis;                            // Reward absence of extreme events

            // We MUST preserve the order. Say it again: "We MUST preserve the order"
            // Remember the index of each profile, and also add the default label.
            var unnamed = Enumerable.Range(0, profiles.Length).Select(x => new { Idx = x, Profile = profiles[x] with { RegimeLabel = $"Regime #{x}" }}).ToList();

            // DRY Helper: Consult FxScore, apply suggested tag. Note: Max() wins.
            void TagByScore(string tag, Func<Regime, double> fxScore)
            {
                if (unnamed.Count > 0)
                {
                    var next = unnamed.OrderByDescending(p => fxScore(p.Profile)).First();
                    unnamed.Remove(next);
                    profiles[next.Idx] = next.Profile with { RegimeLabel = tag };
                }
            }

            // Tag by extremes
            TagByScore("Bull",          BullScore);
            TagByScore("Crisis",        CrisisScore);
            TagByScore("Infl",          InflationScore);
            TagByScore("Recovery",      RecoveryScore);
            TagByScore("Stagnation",    StagnationScore);

            // By now we should have tagged them all.
            // Carry forward anything left behind
            if (unnamed.Count > 0) foreach (var p in unnamed) profiles[p.Idx] = p.Profile;
            return profiles;
        }

        #endregion
    }
}

/*

TODO: Verify

Plot regime over time and verify:

1930s               → Crisis
1940s inflation     → Infl
1950s–60s           → Bull
1970s               → Infl/Stagnation
2008                → Crisis
2009–2013           → Recovery

If those align historically, your model is validated.

*/