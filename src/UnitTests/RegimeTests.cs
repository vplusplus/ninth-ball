using DocumentFormat.OpenXml.Office2010.CustomUI;
using NinthBall.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace UnitTests
{
    [TestClass]
    public class RegimeTests
    {

        [TestMethod]
        public void HelloRegimes()
        {
            var prettyJson = new JsonSerializerOptions { WriteIndented = true };

            var mbbOptions = new MovingBlockBootstrapOptions(BlockSizes: [3, 4, 5], NoBackToBackOverlaps: false );
            var history = new HistoricalReturns();
            var blocks = new HistoricalBlocks(history, mbbOptions).Blocks;

            Random R = new Random(12345);

            var clusters = HistoricalRegimesDiscovery.DiscoverClusters(blocks, R, 4);
            var json = JsonSerializer.Serialize(clusters, prettyJson);
            Console.WriteLine(json);


            var regimes = HistoricalRegimesDiscovery.DiscoverRegimes(blocks, R, 4);
            json = JsonSerializer.Serialize(regimes, prettyJson);
            File.WriteAllText(@"D:\Source\ninth-ball\src\UnitTests\Regimes.json", json);


        }


    }
}
