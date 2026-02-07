
namespace NinthBall.Core
{
    public static partial class KMean
    {
        static KMean.Quality ComputeQualityMetrics(this in XCentroids centroids, in KMean.Samples samples, ReadOnlySpan<int> assignments)
        {
            var (totalInertia, clusterInertia) = centroids.Inertia(samples, assignments);
            var (silhouette, clustersilhouette) = centroids.Silhouette(samples, assignments);

            return new
            (
                totalInertia,
                clusterInertia,
                silhouette,
                clustersilhouette,
                centroids.DBI(samples, assignments),
                centroids.CH(samples, assignments),
                centroids.Dunn(samples, assignments)
            );
        }

        /// <summary>
        /// Inertia: “How tight are the blobs?” 
        /// </summary>
        static (double totalInertia, ReadOnlyMemory<double> clusterInertia) Inertia(this in XCentroids centroids, in KMean.Samples samples, ReadOnlySpan<int> assignments)
        {
            var inertiaPerCluster = new double[centroids.K];
            double totalInertia = 0.0;

            for (int i = 0; i < samples.Count; i++)
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
        static (double silhouette, ReadOnlyMemory<double> clusterSilhouette) Silhouette(this in XCentroids centroids, in KMean.Samples samples, ReadOnlySpan<int> assignments)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// DBI: “How much do blobs overlap?”
        /// </summary>
        static double DBI(this in XCentroids centroids, in KMean.Samples samples, ReadOnlySpan<int> assignments)
        {
            if (centroids.K <= 1) return 0.0;

            // 1. Calculate average distance (scatter) for each cluster
            var scatter = new double[centroids.K];
            var counts = new int[centroids.K];

            for (int i = 0; i < samples.Count; i++)
            {
                int c = assignments[i];
                scatter[c] += Math.Sqrt(samples[i].EuclideanDistanceSquared(centroids[c]));
                counts[c]++;
            }

            for (int c = 0; c < centroids.K; c++)
            {
                if (counts[c] > 0) scatter[c] /= counts[c];
            }

            // 2. Calculate the DBI: average of max similarities (scatter_i + scatter_j) / dist_ij
            double sumMaxR = 0.0;
            for (int i = 0; i < centroids.K; i++)
            {
                double maxR = 0.0;
                for (int j = 0; j < centroids.K; j++)
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

            return sumMaxR / centroids.K;
        }

        /// <summary>
        /// CH: “How much structure vs noise?”
        /// </summary>
        static double CH(this in XCentroids centroids, in KMean.Samples samples, ReadOnlySpan<int> assignments)
        {
            if (centroids.K <= 1 || samples.Count <= centroids.K) return 0.0;

            // 1. Calculate global centroid (mean of all samples)
            var globalCentroid = new double[samples.NumFeatures];
            for (int i = 0; i < samples.Count; i++)
            {
                globalCentroid.AsSpan().Sum(samples[i]);
            }
            globalCentroid.AsSpan().Divide(samples.Count);

            // 2. SS_W (Within-cluster sum of squares) is Total Inertia
            var (ssW, _) = centroids.Inertia(samples, assignments);
            if (ssW == 0) return 0.0; // Avoid division by zero if all points are at centroids

            // 3. SS_B (Between-cluster sum of squares)
            // SS_B = sum( n_k * dist(centroid_k, global_centroid)^2 )
            var counts = new int[centroids.K];
            for (int i = 0; i < assignments.Length; i++) counts[assignments[i]]++;

            double ssB = 0.0;
            for (int x = 0; x < centroids.K; x++)
            {
                if (counts[x] > 0)
                {
                    ssB += counts[x] * centroids[x].EuclideanDistanceSquared(globalCentroid);
                }
            }

            // 4. CH = (ssB / (k - 1)) / (ssW / (n - k))
            double n = samples.Count;
            double k = centroids.K;
            return (ssB / (k - 1.0)) / (ssW / (n - k));
        }

        /// <summary>
        /// Dunn: “Is any geometry broken?”
        /// </summary>
        static double Dunn(this in XCentroids centroids, in KMean.Samples samples, ReadOnlySpan<int> assignments)
        {
            if (centroids.K <= 1 || samples.Count <= 1) return 0.0;

            // 1. Min Inter-cluster distance (minimum distance between any two points in different clusters)
            // Strict version: O(N^2)
            double minInterDist = double.MaxValue;
            for (int i = 0; i < samples.Count; i++)
            {
                for (int j = i + 1; j < samples.Count; j++)
                {
                    if (assignments[i] == assignments[j]) continue;

                    double dist = Math.Sqrt(samples[i].EuclideanDistanceSquared(samples[j]));
                    if (dist < minInterDist) minInterDist = dist;
                }
            }

            // 2. Max Intra-cluster distance (Maximum diameter within any cluster)
            // Strict version: O(N^2)
            double maxIntraDist = 0.0;
            for (int i = 0; i < samples.Count; i++)
            {
                for (int j = i + 1; j < samples.Count; j++)
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
