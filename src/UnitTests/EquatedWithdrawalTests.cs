
using NinthBall.Core;

namespace UnitTests
{
    [TestClass]
    public class EquatedWithdrawalTests
    {
        [TestMethod]
        public void EquatedWithdrawal_SimpleCase()
        {
            // 100 / 10 years = 10 per year if ROI == Inflation
            double FivePCT = 0.05;
            double firstYearWithdrawal = FinMath.EquatedWithdrawal(currentBalance: 100, estimatedROI: FivePCT, estimatedInflation: FivePCT, remainingYears: 10);
            Assert.AreEqual(10.0, firstYearWithdrawal, 1e-9);
        }

        [TestMethod]
        public void EquatedWithdrawal_ZeroEndingBalanceTest()
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

            const double StartingBalance = 1000000;
            const int NumYears = 30;
            const double ROI = 0.07;
            const double Inflation = 0.03;
            double firstYearWithdrawal = FinMath.EquatedWithdrawal(currentBalance: StartingBalance, estimatedROI: ROI, estimatedInflation: Inflation, remainingYears: NumYears);

            Console.WriteLine($"Initial Balance: {StartingBalance:C0}");
            Console.WriteLine($"First year: {firstYearWithdrawal:C2}");
            Console.WriteLine($"Increment (inflation): {Inflation:P1}");
            Console.WriteLine($"ROI (Growth): {ROI:P1}");
            Console.WriteLine($"No of years: {NumYears}");

            // Verification:
            // If we withdraw 'firstYearWithdrawal' amount and grow it at 'Inflation' each year, 
            // while the balance grows at 'ROI%',
            // it should hit zero in 30 years.
            double balance = StartingBalance;
            double withdrawal = firstYearWithdrawal;
            for (int i = 0; i < 30; i++)
            {
                balance -= withdrawal;
                balance *= (1 + ROI);
                withdrawal *= (1 + Inflation);
            }
            
            Assert.AreEqual(0.0, balance, 1e-5);
        }

        [TestMethod]
        public void EquatedWithdrawal_EdgeCase_ZeroInitialBalance()
        {
            double result = FinMath.EquatedWithdrawal(currentBalance: 0, estimatedROI: 0.07, estimatedInflation: 0.03, remainingYears: 30);
            Assert.AreEqual(0.0, result);
        }

        [TestMethod]
        public void EquatedWithdrawal_EdgeCase_ZeroYears()
        {
            double result = FinMath.EquatedWithdrawal(currentBalance: 1_000_000, estimatedROI: 0.07, estimatedInflation: 0.03, remainingYears: 0);
            Assert.AreEqual(0.0, result);
        }
    }
}
