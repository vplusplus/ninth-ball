
using NinthBall;

namespace UnitTests
{
    [TestClass]
    public class StatsTests
    {
        [TestMethod]
        public void EquatedWithdrawal_Simple_Case()
        {
            // 100 / 10 years = 10 per year if ROI == Inflation
            double result = Stats.EquatedWithdrawal(currentBalance: 100, estimatedROI: 0.05, estimatedInflation: 0.05, remainingYears: 10);
            Assert.AreEqual(10.0, result, 1e-9);
        }

        [TestMethod]
        public void EquatedWithdrawal_Growing_Annuity_Due()
        {
            // PV = 1,000,000
            // r = 7%
            // g = 3%
            // n = 30
            // W = 1,000,000 * (0.07 - 0.03) / ((1 + 0.07) * (1 - Math.Pow((1.03 / 1.07), 30)))
            // W = 40,000 / (1.07 * (1 - 0.318...))
            // W = 40,000 / (1.07 * 0.681...)
            // W = 40,000 / 0.729...
            // W approx 54,845
            
            double result = Stats.EquatedWithdrawal(currentBalance: 1_000_000, estimatedROI: 0.07, estimatedInflation: 0.03, remainingYears: 30);
            
            // Verification: If we withdraw this amount and grow it at 3% each year, 
            // while the balance grows at 7%, it should hit zero in 30 years.
            double balance = 1_000_000;
            double withdrawal = result;
            for (int i = 0; i < 30; i++)
            {
                balance -= withdrawal;
                balance *= 1.07;
                withdrawal *= 1.03;
            }
            
            Assert.AreEqual(0.0, balance, 1e-5);
        }

        [TestMethod]
        public void EquatedWithdrawal_Zero_Balance()
        {
            double result = Stats.EquatedWithdrawal(currentBalance: 0, estimatedROI: 0.07, estimatedInflation: 0.03, remainingYears: 30);
            Assert.AreEqual(0.0, result);
        }

        [TestMethod]
        public void EquatedWithdrawal_Zero_Years()
        {
            double result = Stats.EquatedWithdrawal(currentBalance: 1_000_000, estimatedROI: 0.07, estimatedInflation: 0.03, remainingYears: 0);
            Assert.AreEqual(0.0, result);
        }
    }
}
