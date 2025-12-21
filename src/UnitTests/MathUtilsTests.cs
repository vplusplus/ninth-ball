
using NinthBall;
using NinthBall.Core;

namespace UnitTests
{
    [TestClass]
    public class MathUtilsTests
    {
        [TestMethod]
        public void InverseNormalCDF_Values()
        {
            Assert.AreEqual(0.0, MathUtils.InverseNormalCDF(0.5), 1e-7);
            Assert.AreEqual(1.95996398, MathUtils.InverseNormalCDF(0.975), 1e-7);
            Assert.AreEqual(-1.95996398, MathUtils.InverseNormalCDF(0.025), 1e-7);
        }

        [TestMethod]
        public void Correlate_Check()
        {
            // If rho = 1, X1 == X2
            var (x1, x2) = MathUtils.Correlate(1.0, 2.0, 1.0);
            Assert.AreEqual(1.0, x1);
            Assert.AreEqual(1.0, x2);

            // If rho = 0, X1 = Z1, X2 = Z2
            var (y1, y2) = MathUtils.Correlate(1.0, 2.0, 0.0);
            Assert.AreEqual(1.0, y1);
            Assert.AreEqual(2.0, y2);
        }

        [TestMethod]
        public void CornishFisher_No_Adjustment()
        {
            // If skew = 0 and kurtosis = 3, no change
            Assert.AreEqual(1.0, MathUtils.CornishFisher(1.0, 0, 3), 1e-9);
        }
    }
}
