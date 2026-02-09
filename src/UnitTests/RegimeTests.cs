using NinthBall.Core;
using System.Text.Json;

namespace UnitTests
{
    [TestClass]
    public class RegimeTests
    {

        [TestMethod]
        public void HelloRegimes()
        {
            var prettyJson = new JsonSerializerOptions { WriteIndented = true };

            var mbbOptions = new MovingBlockBootstrapOptions(BlockSizes: [3], NoBackToBackOverlaps: false );
            var history = new HistoricalReturns();
            var blocks = new HistoricalBlocks(history, mbbOptions).Blocks;

            Random R = new Random(12345);

            var clusters = HistoricalRegimesDiscovery.DiscoverClusters(blocks, R, 4);
            var json = JsonSerializer.Serialize(clusters, prettyJson);
            File.WriteAllText(@"D:\Source\ninth-ball\src\UnitTests\KMean-Clusters-3.json", json);

            var regimes = clusters.ToRegimeSet(blocks);
            json = JsonSerializer.Serialize(regimes, prettyJson);
            File.WriteAllText(@"D:\Source\ninth-ball\src\UnitTests\KMean-Regimes-3.json", json);
        }
    }
}
