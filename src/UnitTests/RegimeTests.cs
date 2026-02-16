using NinthBall.Core;
using NinthBall.Core.PrettyPrint;
using NinthBall.Utils;
using System.Data;
using System.Text.Json;

namespace UnitTests
{
    [TestClass]
    public class RegimeTests
    {
        const string ReportsFolder = @"D:\Source\ninth-ball\src\UnitTests\Reports\";

        static int[] BlockSizes => [3];

        const int MyRegimeDiscoverySeed = 12345;

        static JsonSerializerOptions PrettyJson =>  new() { WriteIndented = true };


        [TestMethod]
        public void KMeanClusterSizeSelection()
        {
            int[] ThreeYearBlocks = [3];
            int NumTrainings = 50;

            // Prepare 3 year blocks
            var hReturns  = new HistoricalReturns().Returns;
            var hBlocks3Y = hReturns.ReadBlocks(ThreeYearBlocks).ToList();

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
                writer.WriteLine("## K-Mean clustering results using 3-year blocks");

                var qSummary = clusteringResults.Select(r => new
                {
                    K = r.NumClusters,
                    r.Quality.Inertia,
                    r.Quality.Silhouette,
                    r.Quality.DBI,
                    r.Quality.CH,
                    r.Quality.Dunn,
                });
                writer.PrintMarkdownTitle3("Clusters Quality metrics:");
                writer.PrintMarkdownTable(qSummary);

                writer.PrintMarkdownTitle3($"Membership and quality by cluster");

                writer.WriteLine("Note: *Cluster indexes are random, doesn't imply any raking.");
                writer.WriteLine("For visual convenience, clusters are presented by an imaginary z-score(stocks+bonds-Inflation).");
                writer.WriteLine("Do not read too much into that.*");

                // zStocksCAGR + zBondsCAGR - zGMeanInflation
                static double ZScore(ReadOnlySpan<double> zF) => zF[0] + zF[1] - zF[4];

                foreach (var R in clusteringResults)
                {
                    // We are pesenting the clusters rearranged by an abstract score for convenience.
                    var k2 = Enumerable.Range(0, R.NumClusters).OrderByDescending(x => ZScore(R.Centroids[x])).ToArray();

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

                    foreach(var k in k2)
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
                    writer.PrintMarkdownTable(dt);
                    writer.WriteLine();
                    writer.WriteLine();
                }
            }
        }

        [TestMethod]
        public void KMeanClusterStability()
        {
            var mbbOptions = new MovingBlockBootstrapOptions(BlockSizes, NoBackToBackOverlaps: false);
            var history = new HistoricalReturns();
            var blocks = new HistoricalBlocks(history, mbbOptions).Blocks;

            var featureMatrix = blocks.ToFeatureMatrix();
            var zScale = featureMatrix.DiscoverStandardizationParameters();
            featureMatrix = featureMatrix.StandardizeFeatureMatrix(zScale);

            for (int t = 0; t < 10; t++)
            {
                int trainingSeed = PredictableHashCode.Combine(12345, t);
                
                var clusters = featureMatrix.DiscoverBestClusters(trainingSeed, K: 5, numTrainings: 50);
                Console.WriteLine($"Training seed: {trainingSeed}");
                //Console.Out.PrettyPrint(clusters);
                throw new NotImplementedException("Switch to Markdown style PrettyPrintExtensions");
                
            }
        }

        [TestMethod]
        public void HelloRegimes()
        {
            var mbbOptions = new MovingBlockBootstrapOptions(BlockSizes, NoBackToBackOverlaps: false);
            var history = new HistoricalReturns();
            var blocks = new HistoricalBlocks(history, mbbOptions).Blocks;
            
            // Now this calls the robust implementation with 50 restarts and MinClusterSize=5
            var destPath = @"D:\Junk";
            Directory.CreateDirectory(destPath);

            for (int K = 3; K <= 5; K++)
            {
                var hRegimes = blocks.DiscoverRegimes(MyRegimeDiscoverySeed, K);
                Console.WriteLine($"Exporting Report for K={K}...");
                
                var reportName = Path.Combine(destPath, $"HRegime-K{K}.md");
                ExportRegimeReport(hRegimes, reportName);
            }
        }

        private static void ExportRegimeReport(HRegimes hRegimes, string path)
        {
            using var sw = new StreamWriter(path);
            var K = hRegimes.Regimes.Count;

            sw.PrintMarkdownTitle3($"Regime Discovery Analysis (K={K})");
            sw.WriteLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sw.WriteLine();

            sw.PrintMarkdownTitle4("Discovery Configuration")
              .PrintMarkdownRecordTall(new { TargetClusters = K, DiscoverySeed = MyRegimeDiscoverySeed });

            sw.PrintMarkdownTitle4("Regime Transition Probabilities")
              .PrintMarkdownTable(RegimeTransitionsAsDataTable(hRegimes));

            sw.PrintMarkdownTitle4("Market Personalities (The Big Picture)")
              .PrintMarkdownTable(ToMarketPersonalityTable(hRegimes));

            sw.PrintMarkdownTitle4("Asset Behavioral Comparisons");
            
            sw.PrintMarkdownTitle4("Stocks Moment Comparison")
              .PrintMarkdownTable(ToAssetMomentTable(hRegimes, "Stocks", r => r.Stocks));

            sw.PrintMarkdownTitle4("Bonds Moment Comparison")
              .PrintMarkdownTable(ToAssetMomentTable(hRegimes, "Bonds", r => r.Bonds));

            sw.PrintMarkdownTitle4("Inflation Moment Comparison")
              .PrintMarkdownTable(ToAssetMomentTable(hRegimes, "Inflation", r => r.Inflation));

            sw.PrintMarkdownTitle4("Detailed Regime Profiles");
            foreach (var r in hRegimes.Regimes)
            {
                sw.PrintMarkdownTitle4($"Detailed Profile: {r.RegimeLabel}")
                  .PrintMarkdownRecordTall(r);
            }

            sw.PrintMarkdownTitle4("Standardization Context (Z-Scale)")
              .PrintMarkdownTable(ToStandardizationTable(hRegimes));

            sw.Flush();
        }

        static DataTable RegimeTransitionsAsDataTable(HRegimes regimes)
        {
            var dt = new DataTable();
            dt.WithColumn("Regime");
            foreach (var r in regimes.Regimes)
                dt.WithColumn(r.RegimeLabel, typeof(double), format: "P0", alignRight: true);

            foreach (var r in regimes.Regimes)
            {
                var values = new object[dt.Columns.Count];
                values[0] = r.RegimeLabel;
                var tx = r.NextRegimeProbabilities.Span;
                for (int i = 0; i < tx.Length; i++) values[i + 1] = tx[i];
                dt.Rows.Add(values);
            }

            return dt;
        }


        private static DataTable ToMarketPersonalityTable(HRegimes hRegimes)
        {
            var dt = new DataTable();
            dt.WithColumn("Regime")
              .WithColumn("Stocks (μ/σ)")
              .WithColumn("Bonds (μ/σ)")
              .WithColumn("Infl (μ/σ)")
              .WithColumn("S&B Corr", typeof(double), format: "N2")
              .WithColumn("S&I Corr", typeof(double), format: "N2")
              .WithColumn("B&I Corr", typeof(double), format: "N2");

            foreach (var r in hRegimes.Regimes)
            {
                var row = dt.NewRow();
                row["Regime"] = r.RegimeLabel;
                row["Stocks (μ/σ)"] = $"{r.Stocks.Mean:P1} / {r.Stocks.Volatility:P1}";
                row["Bonds (μ/σ)"]  = $"{r.Bonds.Mean:P1} / {r.Bonds.Volatility:P1}";
                row["Infl (μ/σ)"]   = $"{r.Inflation.Mean:P1} / {r.Inflation.Volatility:P1}";
                row["S&B Corr"]     = r.StocksBondCorrelation;
                row["S&I Corr"]     = r.StocksInflationCorrelation;
                row["B&I Corr"]     = r.BondsInflationCorrelation;
                dt.Rows.Add(row);
            }
            return dt;
        }

        private static DataTable ToAssetMomentTable(HRegimes hRegimes, string assetName, Func<HRegimes.RP, HRegimes.M> selector)
        {
            var dt = new DataTable();
            dt.WithColumn("Regime")
              .WithColumn("Mean", typeof(double), format: "P2", alignRight: true)
              .WithColumn("Vol",  typeof(double), format: "P2", alignRight: true)
              .WithColumn("Skew", typeof(double), format: "P2", alignRight: true)
              .WithColumn("Kurt", typeof(double), format: "P2", alignRight: true)
              .WithColumn("Auto", typeof(double), format: "P2", alignRight: true);

            foreach (var r in hRegimes.Regimes)
            {
                var m = selector(r);
                var row = dt.NewRow();
                row["Regime"] = r.RegimeLabel;
                row["Mean"] = m.Mean;
                row["Vol"]  = m.Volatility;
                row["Skew"] = m.Skewness;
                row["Kurt"] = m.Kurtosis;
                row["Auto"] = m.AutoCorrelation;
                dt.Rows.Add(row);
            }
            return dt;
        }

        private static DataTable ToStandardizationTable(HRegimes hRegimes)
        {
            var features = new[] { "Stocks CAGR", "Bonds CAGR", "Stocks Drawdown", "Bonds Drawdown", "Inflation" };
            var dt = new DataTable();
            dt
                .WithColumn("Feature")
                .WithColumn("Mean", typeof(double), format: "N4")
                .WithColumn("StdDev", typeof(double), format: "N4")
                ;

            var m = hRegimes.StandardizationParams.Mean.Span;
            var s = hRegimes.StandardizationParams.StdDev.Span;

            for (int i = 0; i < features.Length; i++)
            {
                dt.Rows.Add(features[i], m[i], s[i]);
            }
            return dt;
        }

        [TestMethod]
        public void HashTest()
        {
            var R = new Random(12345);

            var baseSeed = R.Next(9, int.MaxValue);
            Console.WriteLine($"BaseSeed: {baseSeed}");

            Console.WriteLine($"Iteration Seeds");
            for (int i=0; i<10; i++)
            {
                int iterationSeed = PredictableHashCode.Combine(baseSeed, i);
                Console.Write(iterationSeed);
                Console.Write(", ");
            }

            Console.WriteLine();
        }

    }
}

/* TRANSCRIBE to markdown table print
    //        public static void PrettyPrint(this TextWriter writer, KMean.Result kResult)
    //        {
    //            var Q = kResult.Quality;

    //            var byCluster = Enumerable.Range(0, kResult.NumClusters).Select(i => new
    //            {
    //                Cluster = $"#{i}",
    //                Members = Q.ClusterMembersCount.Span[i],
    //                Silhouette = Math.Round( Q.ClusterSilhouette.Span[i], 2),
    //                Inertia = Math.Round( Q.ClusterInertia.Span[i] ),
    //            })
    //            .ToList();

    //            byCluster.Add(new
    //            {
    //                Cluster     = "Total",
    //                Members     = kResult.Assignments.Length,
    //                Silhouette  = Math.Round(Q.Silhouette, 2),
    //                Inertia     = Math.Round(Q.Inertia, 0),
    //            });

    //            writer.WriteLine($"Clusters: {kResult.NumClusters} | Features: {kResult.NumFeatures} | DBI: {Q.DBI:F2} | CH: {Q.CH:F2} | Dunn: {Q.Dunn:F2}");
    //            writer.PrintTextTable(byCluster, minColWidth: 10);
            
    //        }
*/