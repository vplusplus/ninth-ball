using NinthBall.Core;
using static NinthBall.Core.AdditionalIncomes;

namespace UnitTests
{
    [TestClass]
    public class TaxRateScheduleUnitTests
    {

        [TestMethod]
        public void HelloTaxSchedule()
        {

            (double Income, double ExpectedTax)[] TestData =
            {
                // Negarive / Zero / minimal
                (-0.01,       0.00),
                (0.00,        0.00),
                (0.01,        0.001),

                // 10% bracket
                (1000.00,     100.00),
                (24799.99,    2479.999),
                (24800.00,    2480.00),
                (24800.01,    2480.0012),

                // 12% bracket boundary
                (100799.99,   11599.9988),
                (100800.00,   11600.00),
                (100800.01,   11600.0022),

                // 22% bracket boundary
                (211399.99,   35931.9978),
                (211400.00,   35932.00),
                (211400.01,   35932.0024),

                // 24% bracket boundary
                (403549.99,   82047.9976),
                (403550.00,   82048.00),
                (403550.01,   82048.0032),

                // 32% bracket boundary
                (512449.99,   116895.9968),
                (512450.00,   116896.00),
                (512450.01,   116896.0035),

                // 35% bracket boundary
                (768699.99,   206583.4965),
                (768700.00,   206583.50),
                (768700.01,   206583.5037)
            };


            foreach (var (income, expectedTax) in TestData)
            {
                var (marginalTaxRate, taxAmount) = TaxRateSchedules.FallbackFed2026.CalculateStackedEffectiveTax(income);

                Console.WriteLine($"{income,12:C0} | {marginalTaxRate,8:P2} | Tax: {taxAmount,10:C0}");
                Assert.AreEqual(Math.Round(expectedTax, 2), Math.Round(taxAmount, 2), $"Input: {income:C0} | Expected Tax: {expectedTax:C2} | Actual Tax: {taxAmount:C2} ");
            }
        }

        [TestMethod]
        public void HelloStackedTaxSchedule()
        {
            (double Base, double Income, double ExpectedTax)[] TestData =
            {
                // Negative / Zero / Minimal
                (0,       -0.01,       0.00),
                (100.0,    0.00,       0.00),
                (200.0,    0.001,      0.0001), // 10% of 0.001

                // Simple stacks (all-within-bracket)
                (0,        24800,      2480.00),   // Full 10%
                (24800,    10000,      1200.00),   // All in 12% (Base at threshold)
                (100800,   10000,      2200.00),   // All in 22% (Base at threshold)
                (1000000,  100000,     37000.00),  // All in 37% (Base deep in top bracket)

                // Boundary crossings
                (20000,    10000,      1104.00),   // Cross 10% -> 12% (4800@10% + 5200@12%)
                (100000,   1000,       140.00),    // Cross 12% -> 22% (800@12% + 200@22%)
                (500000,   20000,      6626.50),   // Cross 32% -> 35% (12450@32% + 7550@35%)

                // Multi-bracket jump
                (10000,    211400,     37332.00),  // Start in 10%, jump through 12%, 22% into 24%
                                                   // 14800@10% + 76000@12% + 110600@22% + 10000@24%
                                                   // 1480 + 9120 + 24332 + 2400 = 37332
            };

            foreach (var (baseIncome, incrementalIncome, expectedTax) in TestData)
            {
                var (marginalTaxRate, taxAmount) = TaxRateSchedules.FallbackFed2026.CalculateStackedEffectiveTax(incrementalIncome, baseIncome: baseIncome);

                Console.WriteLine($"{baseIncome,12:C0} + {incrementalIncome,-12:C0} | {marginalTaxRate,8:P2} | Tax: {taxAmount,10:C0}");

                Assert.AreEqual(Math.Round(expectedTax, 2), Math.Round(taxAmount, 2), $" {baseIncome:C0} + {incrementalIncome:C0} | Expected Tax: {expectedTax:C2} | Actual Tax: {taxAmount:C2} ");
            }
        }

    }
}
