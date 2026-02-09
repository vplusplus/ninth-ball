

namespace NinthBall.Core
{
    public static partial class KMean
    {
        private const int MaxIterations = 100;
        private const double ZeroShiftThreshold = 1e-6;

        public readonly record struct Quality
        (
            double TotalInertia,
            ReadOnlyMemory<double> ClusterInertia,
            double SilhouetteScore,
            ReadOnlyMemory<double> ClusterSilhouette,
            double DBI,
            double CH,
            double Dunn
        );

        // Cluster results 2d-matrix of centroids, NumFeatures and the cluster assignments of the samples
        public readonly record struct Result
        (
            int NumClusters,
            int NumFeatures,
            ReadOnlyMemory<double> Centroids, 
            ReadOnlyMemory<int> Assignments,
            Quality Quality
        );

        // Immutable 2d-matrix [numSamples, numFeatures]
        private readonly record struct Samples(ReadOnlyMemory<double> Storage, int NumFeatures)
        {
            public readonly int Count =
                0 == NumFeatures ? throw new Exception("Zero features? What are you doing?") :
                0 != Storage.Length % NumFeatures ? throw new Exception($"Invalid feature-matrix | Storage.Length is not a multiple of num-features") :
                Storage.Length / NumFeatures;

            public ReadOnlySpan<double> this[int idx] => Storage.Slice(idx * NumFeatures, NumFeatures).Span;
        }

        // Mutable 2d-matrix [K, numFeatures]
        private readonly record struct XCentroids(int K, int NumFeatures)
        {
            public readonly Memory<double> Storage = new double[K * NumFeatures];
            public readonly int Count = K;

            public Span<double> this[int ids] => Storage.Slice(ids * NumFeatures, NumFeatures).Span;
        }

        public static (bool converged, int iterations, Result result) Cluster(ReadOnlyMemory<double> normalizedFeatureMatrix, int NumFeatures, Random R, int K)
        {
            Samples samples = new Samples(normalizedFeatureMatrix, NumFeatures);
            return Cluster(samples, R, K);
        }

        static (bool converged, int iterations, Result result) Cluster(in Samples samples, Random R, int K)
        {
            // Prepare initial locations of the centroids.
            XCentroids newCentroids = samples.InitialCentroids(R, K);
            XCentroids oldCentroids = new(K, samples.NumFeatures);

            // Assignments
            int[] oldAssignments = new int[samples.Count];
            int[] newAssignments = new int[samples.Count];
            oldAssignments.AsSpan().Fill(-1);
            newAssignments.AsSpan().Fill(-1);

            // Temp buffer to track counts, and avoid per iteration allocation
            var tempCountBuffer = new int[K];

            var converged = false;
            var iteration = 0;
            for(iteration = 0; iteration < MaxIterations && !converged; iteration++)
            {
                // Assign/Reassign samples to nearest centroid.
                // Check for convergence (no assignment change)
                newAssignments.AsSpan().CopyTo(oldAssignments);
                newAssignments.Reassign(samples, newCentroids);
                if (converged = oldAssignments.SequenceEqual(newAssignments)) break;

                // Recenter the centroid to the midpoint of members
                // Check for convergence (centroids had not shifted)
                oldCentroids.CopyFrom(newCentroids);
                newCentroids.Recenter(samples, newAssignments, R, tempCountBuffer);
                if (converged = newCentroids.MaxShift(oldCentroids) < ZeroShiftThreshold) break;
            }

            return converged
                ? (true, iteration, new Result(NumClusters: K, NumFeatures: newCentroids.NumFeatures, newCentroids.Storage, newAssignments, newCentroids.ComputeQualityMetrics(samples, newAssignments)))
                : (false, iteration, default);
        }

        static XCentroids InitialCentroids(this in Samples samples, Random R, int K)
        {
            // An empty working memory of centroids
            XCentroids centroids = new(K, samples.NumFeatures);

            // Pick a spot for the first centroid.
            // Features of a random observation is the spot for first centroid.
            var idx = R.Next(samples.Count);
            centroids[0].CopyFrom(samples[idx]);

            // Reusable buffer to hold min-squared-distances
            double[] tempDistances = new double[samples.Count];

            // Now we pick a spot for "remaining" centroids
            for (int c = 1; c < centroids.Count; c++)
            {
                // For each observation
                for (int i = 0; i < samples.Count; i++)
                {
                    var features = samples[i];

                    // Calculate squared straight-line distance from the nearest EXISTING centroid.
                    double minSqDistance = Double.MaxValue;
                    for (int existing = 0; existing < c; existing++) minSqDistance = Math.Min(minSqDistance, features.EuclideanDistanceSquared(centroids[existing]));
                    tempDistances[i] = minSqDistance;
                }

                // Choose a random observation, with probability proportional to distance-sqrd
                // Next centroid is positioned at the spot of th random sample.
                idx = R.NextDoubleWeighted(tempDistances);
                centroids[c].CopyFrom(samples[idx]);
            }

            return centroids;
        }

        static void Recenter(this in XCentroids centroids, in Samples samples, ReadOnlySpan<int> assignments, Random R, int[] tempBuffer)
        {
            // Recet all centroids. 
            centroids.Fill(0.0);

            // Check and reset tempBuffer used to trak membership count.
            var membershipCounts = null == tempBuffer || tempBuffer.Length != centroids.K ? throw new Exception("Invlaid temp buffer.") : tempBuffer;
            membershipCounts.AsSpan().Fill(0);

            // Count members and sum their features
            for (int i=0; i<assignments.Length; i++)
            {
                int clusterId = assignments[i];
                membershipCounts[clusterId]++;

                centroids[clusterId].Add(samples[i]);
            }

            // Divide by count to arrive at mean value
            for(int c=0; c<centroids.K; c++)
                if (membershipCounts[c] > 0) 
                    centroids[c].Divide(membershipCounts[c]);
                else
                    centroids[c].CopyFrom(samples[R.Next(samples.Count)]);
        }       

        static void Reassign(this int[] assignments, in Samples samples, in XCentroids centroids)
        {
            for (int i = 0; i < assignments.Length; i++) assignments[i] = samples[i].FindNearestCentroid(centroids);
        }

        static int FindNearestCentroid(this ReadOnlySpan<double> features, in XCentroids centroids)
        {
            int nearestIndex = 0;
            double minDistance = features.EuclideanDistanceSquared(centroids[0]);

            for (int c = 1; c < centroids.K; c++)
            {
                double distance = features.EuclideanDistanceSquared(centroids[c]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestIndex = c;
                }
            }

            return nearestIndex;
        }

        static double MaxShift(this in XCentroids oldCentroids, in XCentroids newCentroids)
        {
            double maxShift = 0.0;

            for (int c = 0; c < oldCentroids.K; c++)
            {
                maxShift = Math.Max(
                    maxShift,
                    oldCentroids[c].EuclideanDistanceSquared(newCentroids[c])
                );
            }

            return maxShift;
        }

        // Bulk initialize centroids with a sentinelValue
        static void Fill(this in XCentroids centroids, double sentinelValue) => centroids.Storage.Span.Fill(sentinelValue);

        // Bulk copy centroids.
        static void CopyFrom(this in XCentroids target, in XCentroids source)
        {
            if (target.Storage.Length != source.Storage.Length) throw new InvalidOperationException($"Vector lengths are not same | [{target.Storage.Length}] and [{source.Storage.Length}]");
            source.Storage.Span.CopyTo(target.Storage.Span);
        }

        // Squared straight-line distance (Euclidean) in a multi-dimensional-space.
        static double EuclideanDistanceSquared(this ReadOnlySpan<double> a, ReadOnlySpan<double> b)
        {
            if (a.Length != b.Length) throw new InvalidOperationException($"Vector lengths are not same | [{a.Length}] and [{b.Length}]");

            double sum = 0.0;
            for (int i = 0; i < a.Length; i++)
            {
                double diff = a[i] - b[i];
                sum += diff * diff;
            }
            return sum;
        }

        // Similar to Random.NextDouble() but weighted 
        static int NextDoubleWeighted(this Random R, double[] weights)
        {
            if (null == weights || 0 == weights.Length) throw new ArgumentException("Empty or NULl weights.");

            double totalWeights = weights.Sum();
            double randomValue  = R.NextDouble() * totalWeights;

            double cumulative = 0.0;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (randomValue <= cumulative) return i;
            }

            return weights.Length - 1;
        }

    }
}
