using NinthBall.Utils;
using System.Diagnostics;

namespace NinthBall.Core
{
    public static class KMean
    {
        private const double ZeroShiftThreshold = 1e-6;

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
            double Inertia,
            double Silhouette,
            double DBI,
            double CH,
            double Dunn,

            // Per cluster metrics
            ReadOnlyMemory<int>    ClusterMembersCount,
            ReadOnlyMemory<double> ClusterInertia,
            ReadOnlyMemory<double> ClusterSilhouette
        );

        //......................................................................
        // KMean training loop
        //......................................................................
        public static KMean.Result DiscoverBestClusters(this in TwoDMatrix standardizedFeatureMatrix, in int trainingSeed, in int K, in int numTrainings = 50)
        {
            const int    MaxIterationsPerTraining = 100;    // We typically converge in less than 10 iterations (BY-DESIGN: Sensitive; Not configurable)
            const double MinClusterSizePCT = 0.05;          // Min 5% of the sample size (BY-DESIGN: Sensitive; Not configurable)

            // Compute the minimum allowed cluster size.
            int minAllowedClusterSize = Math.Max(1, (int)(standardizedFeatureMatrix.NumRows * MinClusterSizePCT));

            // Best result so far. What is best? See rejection logic below.
            KMean.Result? bestResult = default;

            var elapsed = Stopwatch.StartNew();
            for (int attempt = 0; attempt < numTrainings; attempt++)
            {
                // Training specific pseudo random generator
                var R = new Random(PredictableHashCode.Combine(trainingSeed, attempt));

                // Train
                var (converged, nextResult) = KMean.Cluster
                (
                    standardizedFeatureMatrix,
                    R: R,
                    K: K,
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
                : throw new FatalWarning($"K-Means failed to find any valid cluster(s) | {numTrainings} trainings | {MaxIterationsPerTraining} iter/training | MinClusterSize: {minAllowedClusterSize}");
        }

        static bool HasDegenerateClusters(this KMean.Result kResult, int minAcceptableClusterSize)
        {
            var assignments = kResult.Assignments.Span;

            for (int c = 0; c < kResult.NumClusters; c++)
            {
                var memberCount = assignments.Count(c);
                if (memberCount < minAcceptableClusterSize) return true;
            }

            return false;
        }

        //......................................................................
        // KMean - Single training
        //......................................................................
        static (bool converged, Result result) Cluster(in TwoDMatrix samples, Random R, int K, int maxIterations)
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
            for(var i = 0; i < maxIterations && !converged; i++)
            {
                // Assign/Reassign samples to nearest centroid.
                // Check for convergence (no assignment change)
                newAssignments.AsSpan().CopyTo(oldAssignments);
                newAssignments.Reassign(samples, newCentroids.ReadOnly);
                if (converged = oldAssignments.SequenceEqual(newAssignments)) break;

                // Recenter the centroid to the midpoint of members
                // Check for convergence (centroids had not shifted)
                newCentroids.Storage.CopyTo(oldCentroids.Storage);
                newCentroids.Recenter(samples, newAssignments, R, tempCountBuffer);
                if (converged = newCentroids.ReadOnly.MaxShift(oldCentroids) < ZeroShiftThreshold) break;
            }

            if (converged)
            {
                TwoDMatrix centroids = newCentroids.ReadOnly;

                var trainingResult = new KMean.Result
                (
                    Centroids: centroids,
                    newAssignments,
                    Quality: centroids.ComputeQualityMetrics(samples: samples, assignments: newAssignments)
                );
                
                return new(true, trainingResult);
            }
            else
            {
                return (false, default);
            }
        }

        static XTwoDMatrix InitialCentroids(this in TwoDMatrix samples, Random R, int K)
        {
            // An empty working memory of centroids (numClusters x numFeatures)
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
                // Next centroid is positioned at the spot of the random sample.
                idx = R.NextWeightedIndex(tempDistances);
                centroids[c].CopyFrom(samples[idx]);
            }

            return centroids;
        }

        static void Recenter(this in XTwoDMatrix centroids, in TwoDMatrix samples, ReadOnlySpan<int> assignments, Random R, int[] tempBuffer)
        {
            // No of clusters
            int K = centroids.NumRows;

            // Reset all centroids. 
            Array.Fill(centroids.Storage, 0.0);

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

        static int FindNearestCentroid(this ReadOnlySpan<double> features, in TwoDMatrix centroids)
        {
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
    }
}
