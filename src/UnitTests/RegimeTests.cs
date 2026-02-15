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
            var hRegimes = blocks.DiscoverRegimes(MyRegimeDiscoverySeed, 3);
            Console.WriteLine($"Num regimes : {hRegimes.Regimes.Count}");
            hRegimes.RegimeTransitionsAsDataTable().PrettyPrint(Console.Out);
            Console.WriteLine();

            hRegimes = blocks.DiscoverRegimes(MyRegimeDiscoverySeed, 4);
            Console.WriteLine($"Num regimes : {hRegimes.Regimes.Count}");
            hRegimes.RegimeTransitionsAsDataTable().PrettyPrint(Console.Out);
            Console.WriteLine();

            hRegimes = blocks.DiscoverRegimes(MyRegimeDiscoverySeed, 5);
            Console.WriteLine($"Num regimes : {hRegimes.Regimes.Count}");
            hRegimes.RegimeTransitionsAsDataTable().PrettyPrint(Console.Out);
            Console.WriteLine();


            //Console.Out.PrettyPrintTransitionMatrix(hRegimes);
            //Console.WriteLine();

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
