using NinthBall.Core;
using NinthBall.Utils;
using System.Collections;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text.Json;
using UnitTests.PrettyTables;

namespace UnitTests
{
    [TestClass]
    public class RegimeTests
    {
        static int[] BlockSizes => [3];

        const int MyRegimeDiscoverySeed = 12345;

        static JsonSerializerOptions PrettyJson =>  new() { WriteIndented = true };


        [TestMethod]
        public void KMeanClusterSizeSelection()
        {
            var mbbOptions = new MovingBlockBootstrapOptions(BlockSizes, NoBackToBackOverlaps: false );
            var history = new HistoricalReturns();
            var blocks = new HistoricalBlocks(history, mbbOptions).Blocks;

            var featureMatrix = blocks.ToFeatureMatrix();
            var zScale = featureMatrix.DiscoverStandardizationParameters();
            featureMatrix = featureMatrix.StandardizeFeatureMatrix(zScale);

            for(int K=3; K<=5; K++)
            {
                Console.WriteLine();

                var clusters = featureMatrix.DiscoverBestClusters(MyRegimeDiscoverySeed, K: K, numTrainings: 50);
                Console.Out.PrettyPrint(clusters);
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
                Console.Out.PrettyPrint(clusters);
                Console.WriteLine();
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

            sw.PrintMarkdownPageTitle($"Regime Discovery Analysis (K={K})");
            sw.WriteLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sw.WriteLine();

            sw.PrintMarkdownSectionTitle("Discovery Configuration")
              .PrintMarkdownRecordTall(new { TargetClusters = K, DiscoverySeed = MyRegimeDiscoverySeed });

            sw.PrintMarkdownSectionTitle("Regime Transition Probabilities")
              .PrintMarkdownTable(hRegimes.RegimeTransitionsAsDataTable());

            sw.PrintMarkdownSectionTitle("Market Personalities (The Big Picture)")
              .PrintMarkdownTable(ToMarketPersonalityTable(hRegimes));

            sw.PrintMarkdownSectionTitle("Asset Behavioral Comparisons");
            
            sw.PrintMarkdownSectionTitle("Stocks Moment Comparison")
              .PrintMarkdownTable(ToAssetMomentTable(hRegimes, "Stocks", r => r.Stocks));

            sw.PrintMarkdownSectionTitle("Bonds Moment Comparison")
              .PrintMarkdownTable(ToAssetMomentTable(hRegimes, "Bonds", r => r.Bonds));

            sw.PrintMarkdownSectionTitle("Inflation Moment Comparison")
              .PrintMarkdownTable(ToAssetMomentTable(hRegimes, "Inflation", r => r.Inflation));

            sw.PrintMarkdownSectionTitle("Detailed Regime Profiles");
            foreach (var r in hRegimes.Regimes)
            {
                sw.PrintMarkdownSectionTitle($"Detailed Profile: {r.RegimeLabel}")
                  .PrintMarkdownRecordTall(r);
            }

            sw.PrintMarkdownSectionTitle("Standardization Context (Z-Scale)")
              .PrintMarkdownTable(ToStandardizationTable(hRegimes));

            sw.Flush();
        }

        private static DataTable ToMarketPersonalityTable(HRegimes hRegimes)
        {
            var dt = new DataTable();
            dt.WithColumn("Regime")
              .WithColumn("Stocks (μ/σ)")
              .WithColumn("Bonds (μ/σ)")
              .WithColumn("Infl (μ/σ)")
              .WithColumn("S&B Corr", typeof(double)).WithFormat("S&B Corr", "N2")
              .WithColumn("S&I Corr", typeof(double)).WithFormat("S&I Corr", "N2")
              .WithColumn("B&I Corr", typeof(double)).WithFormat("B&I Corr", "N2");

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
              .WithColumn("Mean", typeof(double)).WithFormat("Mean", "P2")
              .WithColumn("Vol", typeof(double)).WithFormat("Vol", "P2")
              .WithColumn("Skew", typeof(double)).WithFormat("Skew", "N2")
              .WithColumn("Kurt", typeof(double)).WithFormat("Kurt", "N2")
              .WithColumn("Auto", typeof(double)).WithFormat("Auto", "N2");

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
            dt.WithColumn("Feature").WithColumn("Mean", typeof(double)).WithColumn("StdDev", typeof(double));
            dt.WithFormat("Mean", "N4").WithFormat("StdDev", "N4");

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
