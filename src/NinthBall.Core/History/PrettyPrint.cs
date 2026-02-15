

namespace NinthBall.Core
{
    internal static partial class PrettyPrintExtensions
    {
        public static void PrettyPrint(this TextWriter writer, KMean.Result kResult)
        {
            var Q = kResult.Quality;

            var byCluster = Enumerable.Range(0, kResult.NumClusters).Select(i => new
            {
                Cluster = $"#{i}",
                Members = Q.ClusterMembersCount.Span[i],
                Silhouette = Math.Round( Q.ClusterSilhouette.Span[i], 2),
                Inertia = Math.Round( Q.ClusterInertia.Span[i] ),
            })
            .ToList();

            byCluster.Add(new
            {
                Cluster     = "Total",
                Members     = kResult.Assignments.Length,
                Silhouette  = Math.Round(Q.Silhouette, 2),
                Inertia     = Math.Round(Q.Inertia, 0),
            });

            writer.WriteLine($"Clusters: {kResult.NumClusters} | Features: {kResult.NumFeatures} | DBI: {Q.DBI:F2} | CH: {Q.CH:F2} | Dunn: {Q.Dunn:F2}");
            writer.PrintTextTable(byCluster, minColWidth: 10);
            
        }
    }
}

