using NinthBall.Core;
using System.Text.Json;

namespace UnitTests
{
    [TestClass]
    public class RegimeTests
    {
        static int[] BlockSizes => [5];

        static Random RND() => new Random(12345);

        static JsonSerializerOptions PrettyJson =>  new() { WriteIndented = true };


        [TestMethod]
        public void HelloKMeanClusters()
        {
            var R = RND();

            var mbbOptions = new MovingBlockBootstrapOptions(BlockSizes, NoBackToBackOverlaps: false );
            var history = new HistoricalReturns();
            var blocks = new HistoricalBlocks(history, mbbOptions).Blocks;

            var featureMatrix = blocks.ToFeatureMatrix();
            var zScale = featureMatrix.DiscoverStandardizationParameters();
            var clusters = featureMatrix.ZNormalizeFeatureMatrix(zScale).DiscoverClusters(R, 4);

            var json = JsonSerializer.Serialize(clusters, PrettyJson);
            File.WriteAllText(@"D:\Source\ninth-ball\src\UnitTests\KMean-Clusters-345.json", json);
        }

        [TestMethod]
        public void HelloRegimes()
        {
            var R = RND();

            var mbbOptions = new MovingBlockBootstrapOptions(BlockSizes, NoBackToBackOverlaps: false);
            var history = new HistoricalReturns();
            var blocks = new HistoricalBlocks(history, mbbOptions).Blocks;
            var hRegimes = blocks.DiscoverRegimes(R, 4);

            var json = JsonSerializer.Serialize(hRegimes, PrettyJson);
            File.WriteAllText(@"D:\Source\ninth-ball\src\UnitTests\KMean-Regimes-5.json", json);
        }
    }
}
