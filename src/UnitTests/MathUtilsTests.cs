
using NinthBall;
using NinthBall.Utils;
using NinthBall.Core;

namespace UnitTests
{
    [TestClass]
    public class MathUtilsTests
    {
        [TestMethod]
        public void MetricsDefaultValuesAreValid()
        {
            var metrics = new Metrics();

            Console.WriteLine(metrics);

            Assert.AreEqual(1.0, metrics.InflationMultiplier);
            Assert.AreEqual(1.0, metrics.InflationMultiplier);

            Assert.AreEqual(0.0, metrics.PortfolioReturn);
            Assert.AreEqual(0.0, metrics.AnnualizedReturn);
            Assert.AreEqual(0.0, metrics.RealAnnualizedReturn);
        }

        [TestMethod]
        public void InverseNormalCDF_Values()
        {
            Assert.AreEqual(0.0, Statistics.InverseNormalCDF(0.5), 1e-7);
            Assert.AreEqual(1.95996398, Statistics.InverseNormalCDF(0.975), 1e-7);
            Assert.AreEqual(-1.95996398, Statistics.InverseNormalCDF(0.025), 1e-7);
        }

        [TestMethod]
        public void CornishFisher_No_Adjustment()
        {
            // If skew = 0 and kurtosis = 3, no change
            Assert.AreEqual(1.0, Statistics.CornishFisher(1.0, 0, 3), 1e-9);
        }
    }
}
