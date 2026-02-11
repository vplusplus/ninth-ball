
namespace NinthBall.Core
{
    internal static class KMeanQuality
    {
        public static KMean.Quality ComputeQualityMetrics(this in TwoDMatrix centroids, in TwoDMatrix samples, ReadOnlySpan<int> assignments)
        {
            // Public signature. Validate inputs.
            // Also add validation that assigned indexes are with-in the range of centroids cols.
            var good = true
                && centroids.NumRows > 0
                && samples.NumRows > 0
                && assignments.Length > 0
                && centroids.NumRows == samples.NumColumns
                && samples.NumRows == assignments.Length;

            if (!good) throw new ArgumentException("You should never see this | One or more parameters to ComputeQualityMetrics() was invalid.");

            var (totalInertia, clusterInertia)  = centroids.Inertia(samples, assignments);
            var (silhouette, clustersilhouette) = centroids.Silhouette(samples, assignments);

            return new
            (
                TotalInertia:       totalInertia,
                SilhouetteScore:    silhouette,
                DBI:                centroids.DBI(samples, assignments),
                CH:                 centroids.CH(samples, assignments),
                Dunn:               centroids.Dunn(samples, assignments),
                ClusterInertia:     clusterInertia,
                ClusterSilhouette:  clustersilhouette
            );
        }

        /// <summary>
        /// Inertia: “How tight are the blobs?” 
        /// </summary>
        static (double totalInertia, ReadOnlyMemory<double> clusterInertia) Inertia(this in TwoDMatrix centroids, in TwoDMatrix samples, ReadOnlySpan<int> assignments)
        {
            var inertiaPerCluster = new double[centroids.NumRows];
            double totalInertia = 0.0;

            for (int i = 0; i < samples.NumRows; i++)
            {
                var distanceSq = samples[i].EuclideanDistanceSquared(centroids[assignments[i]]);
                inertiaPerCluster[assignments[i]] += distanceSq;
                totalInertia += distanceSq;
            }

            return (totalInertia, inertiaPerCluster);
        }

        /// <summary>
        /// Silhouette: “Does each point belong to the assigned cluster?”
        /// </summary>
        static (double silhouette, ReadOnlyMemory<double> clusterSilhouette) Silhouette(this in TwoDMatrix centroids, in TwoDMatrix samples, ReadOnlySpan<int> assignments)
        {
            if (centroids.NumRows <= 1 || samples.NumRows <= 1) return (0.0, new double[centroids.NumRows]);

            // No of clusters
            int K = centroids.NumRows;
            
            double[] s = new double[samples.NumRows];
            double[] clusterSilhouettes = new double[K];
            int[] clusterCounts = new int[K];

            for (int i = 0; i < samples.NumRows; i++)
            {
                int ownCluster = assignments[i];
                double a_i = 0;
                int countA = 0;

                double[] b_distances = new double[K];
                int[] b_counts = new int[K];

                for (int j = 0; j < samples.NumRows; j++)
                {
                    if (i == j) continue;
                    double dist = Math.Sqrt(samples[i].EuclideanDistanceSquared(samples[j]));
                    
                    if (assignments[j] == ownCluster)
                    {
                        a_i += dist;
                        countA++;
                    }
                    else
                    {
                        b_distances[assignments[j]] += dist;
                        b_counts[assignments[j]]++;
                    }
                }

                if (countA > 0) a_i /= countA;

                double b_i = double.MaxValue;
                bool foundOther = false;
                for (int c = 0; c < K; c++)
                {
                    if (c == ownCluster || b_counts[c] == 0) continue;
                    b_i = Math.Min(b_i, b_distances[c] / b_counts[c]);
                    foundOther = true;
                }

                if (!foundOther) s[i] = 0;
                else s[i] = (b_i - a_i) / Math.Max(a_i, b_i);

                clusterSilhouettes[ownCluster] += s[i];
                clusterCounts[ownCluster]++;
            }

            double totalSum = 0;
            for (int c = 0; c < K; c++)
            {
                totalSum += clusterSilhouettes[c];
                if (clusterCounts[c] > 0) clusterSilhouettes[c] /= clusterCounts[c];
            }

            return (totalSum / samples.NumRows, clusterSilhouettes);
        }

        /// <summary>
        /// DBI: “How much do blobs overlap?”
        /// </summary>
        static double DBI(this in TwoDMatrix centroids, in TwoDMatrix samples, ReadOnlySpan<int> assignments)
        {
            // No of clusters
            int K = centroids.NumRows;

            if (K <= 1) return 0.0;

            // 1. Calculate average distance (scatter) for each cluster
            var scatter = new double[K];
            var counts = new int[K];

            for (int i = 0; i < samples.NumRows; i++)
            {
                int c = assignments[i];
                scatter[c] += Math.Sqrt(samples[i].EuclideanDistanceSquared(centroids[c]));
                counts[c]++;
            }

            for (int c = 0; c < K; c++)
            {
                if (counts[c] > 0) scatter[c] /= counts[c];
            }

            // 2. Calculate the DBI: average of max similarities (scatter_i + scatter_j) / dist_ij
            double sumMaxR = 0.0;
            for (int i = 0; i < K; i++)
            {
                double maxR = 0.0;
                for (int j = 0; j < K; j++)
                {
                    if (i == j) continue;

                    double dist = Math.Sqrt(centroids[i].EuclideanDistanceSquared(centroids[j]));
                    if (dist > 0)
                    {
                        double R = (scatter[i] + scatter[j]) / dist;
                        if (R > maxR) maxR = R;
                    }
                }
                sumMaxR += maxR;
            }

            return sumMaxR / K;
        }

        /// <summary>
        /// CH: “How much structure vs noise?”
        /// </summary>
        static double CH(this in TwoDMatrix centroids, in TwoDMatrix samples, ReadOnlySpan<int> assignments)
        {
            // No of clusters
            int K = centroids.NumRows;

            if (K <= 1 || samples.NumRows <= K) return 0.0;

            // 1. Calculate global centroid (mean of all samples)
            var globalCentroid = new double[samples.NumColumns];
            for (int i = 0; i < samples.NumRows; i++)
            {
                globalCentroid.AsSpan().Add(samples[i]);
            }
            globalCentroid.AsSpan().Divide(samples.NumRows);

            // 2. SS_W (Within-cluster sum of squares) is Total Inertia
            var (ssW, _) = centroids.Inertia(samples, assignments);
            if (ssW == 0) return 0.0; // Avoid division by zero if all points are at centroids

            // 3. SS_B (Between-cluster sum of squares)
            // SS_B = sum( n_k * dist(centroid_k, global_centroid)^2 )
            var counts = new int[K];
            for (int i = 0; i < assignments.Length; i++) counts[assignments[i]]++;

            double ssB = 0.0;
            for (int x = 0; x < K; x++)
            {
                if (counts[x] > 0)
                {
                    ssB += counts[x] * centroids[x].EuclideanDistanceSquared(globalCentroid);
                }
            }

            // 4. CH = (ssB / (k - 1)) / (ssW / (n - k))
            double n = samples.NumRows;
            return (ssB / (K - 1.0)) / (ssW / (n - K));
        }

        /// <summary>
        /// Dunn: “Is any geometry broken?”
        /// </summary>
        static double Dunn(this in TwoDMatrix centroids, in TwoDMatrix samples, ReadOnlySpan<int> assignments)
        {
            // No of clusters
            int K = centroids.NumRows;

            if (K <= 1 || samples.NumRows <= 1) return 0.0;

            // 1. Min Inter-cluster distance (minimum distance between any two points in different clusters)
            // Strict version: O(N^2)
            double minInterDist = double.MaxValue;
            for (int i = 0; i < samples.NumRows; i++)
            {
                for (int j = i + 1; j < samples.NumRows; j++)
                {
                    if (assignments[i] == assignments[j]) continue;

                    double dist = Math.Sqrt(samples[i].EuclideanDistanceSquared(samples[j]));
                    if (dist < minInterDist) minInterDist = dist;
                }
            }

            // 2. Max Intra-cluster distance (Maximum diameter within any cluster)
            // Strict version: O(N^2)
            double maxIntraDist = 0.0;
            for (int i = 0; i < samples.NumRows; i++)
            {
                for (int j = i + 1; j < samples.NumRows; j++)
                {
                    if (assignments[i] != assignments[j]) continue;

                    double dist = Math.Sqrt(samples[i].EuclideanDistanceSquared(samples[j]));
                    if (dist > maxIntraDist) maxIntraDist = dist;
                }
            }

            return maxIntraDist > 0 ? minInterDist / maxIntraDist : 0.0;
        }
    }
}
