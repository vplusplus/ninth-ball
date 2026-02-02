using NinthBall.Core;

namespace UnitTests
{
    [TestClass]
    public class NJStateTaxUnitTests
    {
        private static TaxRateSchedules GetTestSchedules()
        {
            // Simple NJ-like progressive brackets for testing
            var njBrackets = new List<TaxRateSchedule.TaxBracket>
            {
                new(0, 0.014),
                new(20000, 0.0175),
                new(35000, 0.035),
                new(40000, 0.05525),
                new(75000, 0.0637)
            };
            var njSchedule = new TaxRateSchedule(1000, njBrackets); // $1000 exemption

            return new TaxRateSchedules(
                Federal: new TaxRateSchedule(0, []), // Not used
                LTCG: new TaxRateSchedule(0, []),    // Not used
                State: njSchedule
            );
        }

        private static TaxAndMarketAssumptions GetDefaultTAMA() => new(
            SSFederalNonTaxableThreshold:   32000,
            SSFederal50PctTaxableThreshold: 44000,
            NIITThreshold: 250000,
            NIITRate: 0.038,
            TypicalStocksDividendYield: 0.02,
            TypicalBondCouponYield: 0.025,
            FedTaxInflationLagHaircut: 0.0025,
            StateTaxInflationLagHaircut: 0.0075
        );

        //[TestMethod]
        //public void TestNJExclusionUnder62()
        //{
        //    var schedules = GetTestSchedules();
        //    var tama = GetDefaultTAMA();
            
        //    // Setup SimYear with 100k Pre-tax and 30k SS
        //    var priorYear = new SimYear
        //    {
        //        Age = 61,
        //        Withdrawals = new Withdrawals(PreTax: 100000, PostTax: 0, Cash: 0),
        //        Incomes = new Incomes(SS: 30000, Ann: 0),
        //        Metrics = new Metrics() // No inflation
        //    };
            
        //    // Age 61: No exclusion
        //    var result = priorYear.ComputePriorYearTaxes(schedules, tama);
            
        //    // Expected: 100k - 1k(exemption) = 99k taxable. (SS ignored)
        //    Assert.AreEqual(99000, result.State.Taxable, "Age 61 should have no pension exclusion.");
        //}

        //[TestMethod]
        //public void TestNJExclusionAt62Full()
        //{
        //    var schedules = GetTestSchedules();
        //    var tama = GetDefaultTAMA();

        //    var priorYear = new SimYear
        //    {
        //        Age = 62,
        //        Withdrawals = new Withdrawals(PreTax: 90000, PostTax: 0, Cash: 0),
        //        Incomes = new Incomes(SS: 30000, Ann: 0),
        //        Metrics = new Metrics()
        //    };
            
        //    // Age 62, Income < 100k: Full 100k exclusion
        //    var result = priorYear.ComputePriorYearTaxes(schedules, tama);
            
        //    // Expected: 90k - 90k(exclusion) - 1k(exemption) = 0 taxable. (SS ignored)
        //    Assert.AreEqual(0, result.State.Taxable, "Age 62 with <100k should have full exclusion.");
        //}

        //[TestMethod]
        //public void TestNJExclusionCliff()
        //{
        //    var schedules = GetTestSchedules();
        //    var tama = GetDefaultTAMA();

        //    // Scenario A: Income 150,000 (Last step before cliff)
        //    var priorYearBelow = new SimYear
        //    {
        //        Age = 62,
        //        Withdrawals = new Withdrawals(PreTax: 150000, PostTax: 0, Cash: 0),
        //        Incomes = new Incomes(SS: 30000, Ann: 0),
        //        Metrics = new Metrics()
        //    };
        //    var resultBelow = priorYearBelow.ComputePriorYearTaxes(schedules, tama);
        //    // $150k - $25k exclusion - $1k exemption = $124k taxable.
        //    Assert.AreEqual(124000, resultBelow.State.Taxable, "Income at 150k should have 25k exclusion.");

        //    // Scenario B: Income 150,001 (THE CLIFF)
        //    var priorYearAbove = new SimYear
        //    {
        //        Age = 62,
        //        Withdrawals = new Withdrawals(PreTax: 150001, PostTax: 0, Cash: 0),
        //        Incomes = new Incomes(SS: 30000, Ann: 0),
        //        Metrics = new Metrics()
        //    };
        //    var resultAbove = priorYearAbove.ComputePriorYearTaxes(schedules, tama);
        //    // $150,001 - $0 exclusion - $1k exemption = $149,001 taxable.
        //    Assert.AreEqual(149001, resultAbove.State.Taxable, "Income at 150,001 should hit the CLIFF (zero exclusion).");

        //    // Verify the jump
        //    var taxDiff = resultAbove.State.Tax - resultBelow.State.Tax;
        //    Console.WriteLine($"Tax Jump at $150,001: {taxDiff:C}");
        //    Assert.IsTrue(taxDiff > 1000, "The Cliff should cause a significant tax jump.");
        //}

        //[TestMethod]
        //public void TestNJSocialSecurityExemption()
        //{
        //    var schedules = GetTestSchedules();
        //    var tama = GetDefaultTAMA();

        //    // Zero ordinary income, only Social Security
        //    var priorYear = new SimYear
        //    {
        //        Age = 70,
        //        Withdrawals = new Withdrawals(PreTax: 0, PostTax: 0, Cash: 0),
        //        Incomes = new Incomes(SS: 100000, Ann: 0),
        //        Metrics = new Metrics()
        //    };
            
        //    var result = priorYear.ComputePriorYearTaxes(schedules, tama);
            
        //    // Expected: SS is 100% exempt. Taxable should be 0.
        //    Assert.AreEqual(0, result.State.Taxable, "Social Security should be 100% exempt from NJ tax.");
        //    Assert.AreEqual(0, result.State.Tax, "Tax on 100k Social Security should be Zero.");
        //}
    }
}
