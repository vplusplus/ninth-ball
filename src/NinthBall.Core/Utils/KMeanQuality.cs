
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
                centroids.CH(samples, assignments)
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// CH: “How much structure vs noise?”
        /// </summary>
        static double CH(this in XCentroids centroids, in KMean.Samples samples, ReadOnlySpan<int> assignments)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Dunn: “Is any geometry broken?”
        /// </summary>
        static double Dunn(this in XCentroids centroids, in KMean.Samples samples, ReadOnlySpan<int> assignments)
        {
            throw new NotImplementedException();
        }
    }
}
