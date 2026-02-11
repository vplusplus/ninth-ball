

namespace NinthBall.Core
{
    public static class KMean
    {
        private const double ZeroShiftThreshold = 1e-6;

        // Cluster results 2d-matrix of centroids, NumFeatures and the cluster assignments of the samples
        public readonly record struct Result
        (
            TwoDMatrix          Centroids, 
            ReadOnlyMemory<int> Assignments,
            Quality             Quality
        )
        {
            public readonly int NumClusters => Centroids.NumRows;
            public readonly int NumFeatures => Centroids.NumColumns;
        }

        public readonly record struct Quality
        (
            // Overall metrics
            double TotalInertia,
            double SilhouetteScore,
            double DBI,
            double CH,
            double Dunn,

            // Per cluster metrics
            ReadOnlyMemory<double> ClusterInertia,
            ReadOnlyMemory<double> ClusterSilhouette
        );


        public static (bool converged, int iterations, Result result) Cluster(in TwoDMatrix samples, Random R, int K, int maxIterations = 100)
        {
            // Prepare initial locations of the centroids.
            XTwoDMatrix newCentroids = samples.InitialCentroids(R, K);
            XTwoDMatrix oldCentroids = new(K, samples.NumColumns);

            // Assignments
            int[] oldAssignments = new int[samples.NumRows];
            int[] newAssignments = new int[samples.NumRows];
            oldAssignments.AsSpan().Fill(-1);
            newAssignments.AsSpan().Fill(-1);

            // Temp buffer to track counts, and avoid per iteration allocation
            var tempCountBuffer = new int[K];

            var converged = false;
            var iteration = 0;
            for(iteration = 0; iteration < maxIterations && !converged; iteration++)
            {
                // Assign/Reassign samples to nearest centroid.
                // Check for convergence (no assignment change)
                newAssignments.AsSpan().CopyTo(oldAssignments);
                newAssignments.Reassign(samples, newCentroids.ReadOnly);
                if (converged = oldAssignments.SequenceEqual(newAssignments)) break;

                // Recenter the centroid to the midpoint of members
                // Check for convergence (centroids had not shifted)
                oldCentroids.CopyFrom(newCentroids);
                newCentroids.Recenter(samples, newAssignments, R, tempCountBuffer);
                if (converged = newCentroids.ReadOnly.MaxShift(oldCentroids) < ZeroShiftThreshold) break;
            }

            if (converged)
            {
                TwoDMatrix centroids = newCentroids.ReadOnly;

                var trainingResult = new KMean.Result
                (
                    //Samples: samples,
                    Centroids: centroids,
                    newAssignments,
                    Quality: centroids.ComputeQualityMetrics(samples: samples, assignments: newAssignments)
                );

                return new(true, iteration, trainingResult);
            }
            else
            {
                return (false, iteration, default);
            }
        }

        static XTwoDMatrix InitialCentroids(this in TwoDMatrix samples, Random R, int K)
        {
            // An empty working memory of centroids
            XTwoDMatrix centroids = new(K, samples.NumColumns);

            // Pick a spot for the first centroid.
            // Features of a random observation is the spot for first centroid.
            var idx = R.Next(samples.NumRows);
            centroids[0].CopyFrom(samples[idx]);

            // Reusable buffer to hold min-squared-distances
            double[] tempDistances = new double[samples.NumRows];

            // Now we pick a spot for "remaining" centroids
            for (int c = 1; c < centroids.NumRows; c++)
            {
                // For each observation
                for (int i = 0; i < samples.NumRows; i++)
                {
                    var features = samples[i];

                    // Calculate squared straight-line distance from the nearest EXISTING centroid.
                    double minSqDistance = Double.MaxValue;
                    for (int existing = 0; existing < c; existing++) minSqDistance = Math.Min(minSqDistance, features.EuclideanDistanceSquared(centroids[existing]));
                    tempDistances[i] = minSqDistance;
                }

                // Choose a random observation, with probability proportional to distance-sqrd
                // Next centroid is positioned at the spot of th random sample.
                idx = R.NextWeightedIndex(tempDistances);
                centroids[c].CopyFrom(samples[idx]);
            }

            return centroids;
        }

        static void Recenter(this in XTwoDMatrix centroids, in TwoDMatrix samples, ReadOnlySpan<int> assignments, Random R, int[] tempBuffer)
        {
            // No of clusters
            int K = centroids.NumRows;

            // Recet all centroids. 
            centroids.Fill(0.0);

            // Check and reset tempBuffer used to trak membership count.
            var membershipCounts = null == tempBuffer || tempBuffer.Length != K ? throw new Exception("Invlaid temp buffer.") : tempBuffer;
            membershipCounts.AsSpan().Fill(0);

            // Count members and sum their features
            for (int i=0; i<assignments.Length; i++)
            {
                int clusterId = assignments[i];
                membershipCounts[clusterId]++;

                centroids[clusterId].Add(samples[i]);
            }

            // Divide by count to arrive at mean value
            for(int c=0; c < K; c++)
                if (membershipCounts[c] > 0) 
                    centroids[c].Divide(membershipCounts[c]);
                else
                    centroids[c].CopyFrom(samples[R.Next(samples.NumRows)]);
        }       

        static void Reassign(this int[] assignments, in TwoDMatrix samples, in TwoDMatrix centroids)
        {
            for (int i = 0; i < assignments.Length; i++) assignments[i] = samples[i].FindNearestCentroid(centroids);
        }

        public static int FindNearestCentroid(this ReadOnlySpan<double> features, in TwoDMatrix centroids)
        {
            // Public signature. Validate params.
            if (0 == centroids.NumRows || 0 == centroids.NumColumns) throw new Exception("Invalid centroids.");
            if (features.Length != centroids.NumColumns) throw new Exception("Incompatible vector");

            int nearestIndex = 0;
            double minDistance = features.EuclideanDistanceSquared(centroids[0]);

            for (int c = 1; c < centroids.NumRows; c++)
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

        static double MaxShift(this in TwoDMatrix oldCentroids, in TwoDMatrix newCentroids)
        {
            double maxShift = 0.0;

            for (int c = 0; c < oldCentroids.NumRows; c++)
            {
                maxShift = Math.Max(
                    maxShift,
                    oldCentroids[c].EuclideanDistanceSquared(newCentroids[c])
                );
            }

            return maxShift;
        }

        // Bulk initialize centroids with a sentinelValue
        static void Fill(this in XTwoDMatrix centroids, double sentinelValue) => Array.Fill(centroids.Storage, sentinelValue);

        // Bulk copy centroids.
        static void CopyFrom(this in XTwoDMatrix target, in XTwoDMatrix source)
        {
            if (target.Storage.Length != source.Storage.Length) throw new InvalidOperationException($"Vector lengths are not same | [{target.Storage.Length}] and [{source.Storage.Length}]");
            source.Storage.CopyTo(target.Storage);
        }
    }
}
