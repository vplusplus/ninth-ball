
using NinthBall;
using NinthBall.Core;

namespace UnitTests
{
    [TestClass]
    public class ParametricBootstrapperTests
    {
        [TestMethod]
        public void GetROISequence_Basic_Check()
        {
            var options = new ParametricBootstrap(
                DistributionType: "LogNormal",
                StocksBondCorrelation: 0,
                Stocks: new ParametricBootstrap.Dist(0.07, 0.15, 0, 3, 0),
                Bonds: new ParametricBootstrap.Dist(0.03, 0.05, 0, 3, 0)
            );
            var seed = new SimulationSeed("test");
            var bootstrapper = new ParametricBootstrapper(seed, options);

            var sequence = bootstrapper.GetROISequence(0, 1000);
            
            Assert.AreEqual(1000, sequence.Count);
            
            double meanStocks = sequence.Average(s => s.StocksROI);
            double volStocks = sequence.Select(s => s.StocksROI).Volatility();

            // With 1000 samples, it should be reasonably close
            Assert.AreEqual(0.07, meanStocks, 0.02);
            Assert.AreEqual(0.15, volStocks, 0.02);
        }
    }
}
