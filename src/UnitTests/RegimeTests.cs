using DocumentFormat.OpenXml.Drawing.Charts;
using NinthBall.Core;
using NinthBall.Utils;
using System.Text.Json;

namespace UnitTests
{
    [TestClass]
    public class RegimeTests
    {
        static int[] BlockSizes => [3];

        const int MyRegimeDiscoverySeed = 12345;

        static JsonSerializerOptions PrettyJson =>  new() { WriteIndented = true };


        [TestMethod]
        public void HelloKMeanClusters()
        {
            var mbbOptions = new MovingBlockBootstrapOptions(BlockSizes, NoBackToBackOverlaps: false );
            var history = new HistoricalReturns();
            var blocks = new HistoricalBlocks(history, mbbOptions).Blocks;

            var featureMatrix = blocks.ToFeatureMatrix();
            var zScale = featureMatrix.DiscoverStandardizationParameters();
            var clusters = featureMatrix.StandardizeFeatureMatrix(zScale).DiscoverBestClusters(MyRegimeDiscoverySeed, 4);

            //var json = JsonSerializer.Serialize(clusters, PrettyJson);
            //File.WriteAllText(@"D:\Source\ninth-ball\src\UnitTests\KMean-Clusters-345.json", json);
        }

        [TestMethod]
        public void HelloRegimes()
        {
            var mbbOptions = new MovingBlockBootstrapOptions(BlockSizes, NoBackToBackOverlaps: false);
            var history = new HistoricalReturns();
            var blocks = new HistoricalBlocks(history, mbbOptions).Blocks;
            
            // Now this calls the robust implementation with 50 restarts and MinClusterSize=5
            var hRegimes = blocks.DiscoverRegimes(MyRegimeDiscoverySeed, 4);

            Console.WriteLine("Regimes:");
            hRegimes.Print(Console.Out);

            // Print Matrix
            Console.WriteLine();
            Console.WriteLine("Transitions:");
            Console.Out.PrettyPrintTransitionMatrix(hRegimes);
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

/*

ONE TRAINING:

 Read 97 years of historical ROI data from 1928 to 2024.
 K-Mean: Discovered 4 clusters | 5 iterations | 4 milliSec
 K-Mean: TotalInertia: 256.32 | SilhouetteScore: 0.26
 K-Mean: Inertia     : [  131.87,     3.22,    91.23,    30.00]
 K-Mean: Silhouette  : [    0.28,     0.61,     0.09,     0.28]
--------------------------------------------------------------------------------
             | Bull         | Crisis       | Stagnation   | Regime3      | 
--------------------------------------------------------------------------------
Bull         | 85.0%        | 0.0%         | 8.3%         | 6.7%         | 
Crisis       | 33.3%        | 66.7%        | 0.0%         | 0.0%         | 
Stagnation   | 20.0%        | 6.7%         | 60.0%        | 13.3%        | 
Regime3      | 31.2%        | 0.0%         | 6.2%         | 62.5%        | 
--------------------------------------------------------------------------------

Best of FIFTY TRAININGS:

 K-Mean: Discovered 4 clusters | 4 iterations | 50 trainings | 79 milliSec
 K-Mean: TotalInertia: 227.52 | SilhouetteScore: 0.30
 K-Mean: Inertia     : [  131.87,    35.08,    59.88,     0.69]
 K-Mean: Silhouette  : [    0.30,     0.27,     0.24,     0.83]
--------------------------------------------------------------------------------
           | Bull       | Regime1    | Regime2    | Balanced   | 
--------------------------------------------------------------------------------
Bull       | 85.0%      | 6.7%       | 6.7%       | 1.7%       | 
Regime1    | 29.4%      | 70.6%      | 0.0%       | 0.0%       | 
Regime2    | 26.7%      | 6.7%       | 66.7%      | 0.0%       | 
Balanced   | 0.0%       | 0.0%       | 0.0%       | 100.0%     | 
--------------------------------------------------------------------------------



*/