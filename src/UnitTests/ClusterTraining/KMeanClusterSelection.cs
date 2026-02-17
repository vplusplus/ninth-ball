using NinthBall.Core;
using NinthBall.Core.PrettyPrint;
using NinthBall.Utils;
using System.Data;

namespace UnitTests.ClusterTraining
{
    [TestClass]
    public class KMeanClusterSelection
    {
        const string ReportsFolder = @"D:\Source\ninth-ball\src\UnitTests\Reports\";

        const int NumTrainings = 50;
        const int MyRegimeDiscoverySeed = 12345;
        static readonly int[] ThreeYearBlocks = [3];

        [TestMethod]
        public void KMeanClusterSizeSelection()
        {
            // Prepare 3 year blocks
            var hReturns  = new HistoricalReturns().Returns;
            var hBlocks3Y = hReturns.ReadBlocks(ThreeYearBlocks).ToList().AsReadOnly();

            // Extract features, standardize
            var features  = hBlocks3Y.ToFeatureMatrix();
            var zScale    = features.DiscoverStandardizationParameters();
            var zFeatures = features.StandardizeFeatureMatrix(zScale);

            // Try cluster sizes
            var clusteringResults = new List<KMean.Result>();
            for(int numClusters = 3; numClusters <= 6; numClusters++)
            {
                var clusters = zFeatures.DiscoverBestClusters(MyRegimeDiscoverySeed, K: numClusters, numTrainings: NumTrainings);
                clusteringResults.Add(clusters);
            }

            using(var writer = File.CreateText(Path.Combine(ReportsFolder, "KMean-Results.md")))
            {
                writer
                    .AppendLine("## K-Mean - K Selection")
                    .AppendLine("Train using different K. Compare cluster quality metrics. Choose best K.");

                var qSummary = clusteringResults.Select(r => new
                {
                    K = r.NumClusters,
                    r.Quality.Inertia,
                    r.Quality.Silhouette,
                    r.Quality.DBI,
                    r.Quality.CH,
                    r.Quality.Dunn,
                });

                writer
                    .PrintMarkdownTitle3("Clusters Quality metrics:")
                    .PrintMarkdownTable(qSummary)

                    .PrintMarkdownTitle3($"Membership and quality by cluster")
                    .AppendLine("Note: *Cluster indexes are random.")
                    .AppendLine("For visual convenience, clusters are presented by an imaginary z-score (stocks + bonds - Inflation).")
                    .AppendLine("Do not read too much into that.*");

                foreach (var R in clusteringResults)
                {
                    var dt = FeaturesByCluster(R);
                    writer
                        .PrintMarkdownTable(dt)
                        .AppendLine()
                        .AppendLine();
                }
            }
        }


        [TestMethod]
        public void KMeanClusterStability()
        {
            // Prepare 3 year blocks
            var hReturns = new HistoricalReturns().Returns;
            var hBlocks3Y = hReturns.ReadBlocks(ThreeYearBlocks).ToList().AsReadOnly();

            // Extract features, standardize
            var features  = hBlocks3Y.ToFeatureMatrix();
            var zScale    = features.DiscoverStandardizationParameters();
            var zFeatures = features.StandardizeFeatureMatrix(zScale);

            int[] SEEDs = Enumerable.Range(0, 10).Select(x => PredictableHashCode.Combine(12345, x)).ToArray();

            // Try a different seeds
            var clusteringResults = new List<KMean.Result>();

            foreach(var trainingSeed in SEEDs)
            {
                var clusters = zFeatures.DiscoverBestClusters(trainingSeed, K: 5, numTrainings: NumTrainings);
                clusteringResults.Add(clusters);
            }

            using (var writer = File.CreateText(Path.Combine(ReportsFolder, "KMean-Stability.md")))
            {
                writer
                    .AppendLine("## K-Mean clustering stability")
                    .AppendLine("Train using same K but different training seeds. Look for stability of the resulting clusters ");

                var qSummary = clusteringResults.Select((r, idx) => new
                {
                    Seed = SEEDs[idx],
                    r.Quality.Inertia,
                    r.Quality.Silhouette,
                    r.Quality.DBI,
                    r.Quality.CH,
                    r.Quality.Dunn,
                });

                writer
                    .PrintMarkdownTitle3("Clusters Quality metrics:")
                    .PrintMarkdownTable(qSummary)

                    .PrintMarkdownTitle3($"Membership and quality by cluster")
                    .AppendLine("Note: *Cluster indexes are random.")
                    .AppendLine("For visual convenience, clusters are presented by an imaginary z-score (stocks + bonds - Inflation).")
                    .AppendLine("Do not read too much into that.*");

                int seedIdx = 0;
                foreach (var R in clusteringResults)
                {
                    var dt = FeaturesByCluster(R);

                    writer
                        .PrintMarkdownTitle4($"Seed: {SEEDs[seedIdx++]}")
                        .PrintMarkdownTable(dt)
                        .AppendLine()
                        .AppendLine();
                }
            }

        }


        static DataTable FeaturesByCluster(KMean.Result R)
        {
            // zStocksCAGR + zBondsCAGR - zGMeanInflation
            static double ZScore(ReadOnlySpan<double> zF) => zF[0] + zF[1] - zF[4];

            // We are pesenting the clusters rearranged by an abstract score for convenience.
            var rankedClusterIndexes = Enumerable.Range(0, R.NumClusters).OrderByDescending(x => ZScore(R.Centroids[x])).ToArray();

            var dt = new DataTable();
            dt
                .WithColumn($"K={R.NumClusters}")
                .WithColumn<int>("Members")
                .WithColumn<double>("Inertia")
                .WithColumn<double>("Silhouette")

                .WithColumn<string>("zF >")

                .WithColumn<double>("zStockCAGR")
                .WithColumn<double>("zBondCAGR")
                .WithColumn<double>("zStockMaxDD")
                .WithColumn<double>("zBondMaxDD")
                .WithColumn<double>("zInflation")
                ;

            foreach (var k in rankedClusterIndexes)
            {
                var centroid = R.Centroids[k];

                var row = dt.Rows.Add(
                [
                    $"Cluster {k}",
                    R.Quality.ClusterMembersCount.Span[k],
                    R.Quality.ClusterInertia.Span[k],
                    R.Quality.ClusterSilhouette.Span[k],

                    string.Empty,

                    centroid[0],
                    centroid[1],
                    centroid[2],
                    centroid[3],
                    centroid[4],
                ]);
            }

            return dt;
        }


    }
}
