
namespace NinthBall.Core
{
    public static partial class KMean
    {
        static KMean.Quality ComputeQualityMetrics(this in XCentroids centroids, in KMean.Samples samples, ReadOnlySpan<int> assignments)
        {
            return new
            (
                centroids.Inertia(samples, assignments),
                centroids.ClusterInertia(samples, assignments),
                centroids.Silhouette(samples, assignments),
                centroids.ClusterSilhouette(samples, assignments),
                centroids.DBI(samples, assignments),
                centroids.CH(samples, assignments)
            );
        }

        /// <summary>
        /// Inertia: “How tight are the blobs?” 
        /// </summary>
        static double Inertia(this in XCentroids centroids, in KMean.Samples samples, ReadOnlySpan<int> assignments)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Inertia: “How tight are the blobs?” 
        /// </summary>
        static ReadOnlyMemory<double> ClusterInertia(this in XCentroids centroids, in KMean.Samples samples, ReadOnlySpan<int> assignments)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Silhouette: “Does each point belong to the assigned cluster?”
        /// </summary>
        static double Silhouette(this in XCentroids centroids, in KMean.Samples samples, ReadOnlySpan<int> assignments)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Silhouette: “Does each point belong to the assigned cluster?”
        /// </summary>
        static ReadOnlyMemory<double> ClusterSilhouette(this in XCentroids centroids, in KMean.Samples samples, ReadOnlySpan<int> assignments)
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
