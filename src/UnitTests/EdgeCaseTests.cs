
using NinthBall;

namespace UnitTests
{
    /// <summary>
    /// Edge case and boundary condition tests for the simulation framework.
    /// Tests numerical extremes, temporal boundaries, and unusual scenarios.
    /// </summary>
    [TestClass]
    public sealed class EdgeCaseTests
    {
        #region Numerical Edge Cases

        [TestMethod]
        public void SimBalance_VerySmallBalance_HandlesCorrectly()
        {
            // Arrange & Act
            var balance = new SimBalance(InitialBalance: 0.01, InitialStockPct: 0.6, InitialMaxDrift: 0.03);

            // Assert
            Assert.AreEqual(0.01, balance.CurrentBalance, 0.001);
            Assert.IsTrue(balance.StockBalance >= 0);
            Assert.IsTrue(balance.BondBalance >= 0);
        }

        [TestMethod]
        public void SimBalance_VeryLargeBalance_HandlesCorrectly()
        {
            // Arrange & Act
            var balance = new SimBalance(InitialBalance: 1_000_000_000, InitialStockPct: 0.6, InitialMaxDrift: 0.03);

            // Assert
            Assert.AreEqual(1_000_000_000, balance.CurrentBalance, 1.0);
            Assert.AreEqual(600_000_000, balance.StockBalance, 1.0);
            Assert.AreEqual(400_000_000, balance.BondBalance, 1.0);
        }

        [TestMethod]
        public void Simulation_ZeroInitialBalance_FailsImmediately()
        {
            // Arrange
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 1000),
                FxSimObjective.Create(ctx => ctx.Fees = 0),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.05, 0.02)),
            ];

            // Act
            var result = objectives.RunIteration(0, initialBalance: 0.0, 0.6, 0.03, numYears: 10);

            // Assert
            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public void SimBalance_ExtremeAllocation_100PercentStocks()
        {
            // Arrange
            var balance = new SimBalance(InitialBalance: 100_000, InitialStockPct: 1.0, InitialMaxDrift: 0.0);

            // Act
            balance.Grow(stocksROI: 0.20, bondsROI: 0.05);

            // Assert
            Assert.AreEqual(120_000, balance.CurrentBalance, 0.01);
            Assert.AreEqual(120_000, balance.StockBalance, 0.01);
            Assert.AreEqual(0, balance.BondBalance, 0.01);
        }

        [TestMethod]
        public void SimBalance_ExtremeAllocation_100PercentBonds()
        {
            // Arrange
            var balance = new SimBalance(InitialBalance: 100_000, InitialStockPct: 0.0, InitialMaxDrift: 0.0);

            // Act
            balance.Grow(stocksROI: 0.20, bondsROI: 0.05);

            // Assert
            Assert.AreEqual(105_000, balance.CurrentBalance, 0.01);
            Assert.AreEqual(0, balance.StockBalance, 0.01);
            Assert.AreEqual(105_000, balance.BondBalance, 0.01);
        }

        [TestMethod]
        public void SimBalance_ExtremeDrift_100Percent()
        {
            // Arrange & Act
            var balance = new SimBalance(InitialBalance: 100_000, InitialStockPct: 0.6, InitialMaxDrift: 1.0);

            // Create massive drift
            balance.Grow(stocksROI: 5.0, bondsROI: 0.0); // 500% stock growth!

            var rebalanced = balance.Rebalance();

            // Assert - Even with huge drift, should not rebalance (drift threshold is 100%)
            Assert.IsFalse(rebalanced);
        }

        [TestMethod]
        public void SimBalance_NoDriftTolerance_AlwaysRebalances()
        {
            // Arrange
            var balance = new SimBalance(InitialBalance: 100_000, InitialStockPct: 0.6, InitialMaxDrift: 0.0);

            // Act - Any asymmetric growth should trigger rebalance
            balance.Grow(stocksROI: 0.01, bondsROI: 0.0);
            var rebalanced = balance.Rebalance();

            // Assert - Should rebalance even with tiny drift
            Assert.IsTrue(rebalanced);
        }

        #endregion

        #region ROI Edge Cases

        [TestMethod]
        public void Simulation_ZeroROI_NoGrowth()
        {
            // Arrange
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 1000),
                FxSimObjective.Create(ctx => ctx.Fees = 100),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.0, 0.0)),
            ];

            // Act
            var result = objectives.RunIteration(0, 100_000, 0.6, 0.03, numYears: 5);

            // Assert - Balance should decrease by withdrawals + fees each year
            Assert.IsTrue(result.Success);
            Assert.AreEqual(100_000 - (1000 + 100) * 5, result.EndingBalance, 0.01);
        }

        [TestMethod]
        public void Simulation_100PercentLoss_DramaticDecrease()
        {
            // Arrange
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 0),
                FxSimObjective.Create(ctx => ctx.Fees = 0),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2008, -1.0, -1.0)), // Total loss
            ];

            // Act
            var result = objectives.RunIteration(0, 100_000, 0.6, 0.03, numYears: 1);

            // Assert
            Assert.AreEqual(0, result.EndingBalance, 0.01);
        }

        [TestMethod]
        public void Simulation_MassiveGain_100PercentPlus()
        {
            // Arrange
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 0),
                FxSimObjective.Create(ctx => ctx.Fees = 0),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 1.0, 0.5)), // 100% stocks, 50% bonds
            ];

            // Act
            var result = objectives.RunIteration(0, 100_000, 0.6, 0.03, numYears: 1);

            // Assert - 60k * 2.0 + 40k * 1.5 = 120k + 60k = 180k
            Assert.AreEqual(180_000, result.EndingBalance, 0.01);
        }

        [TestMethod]
        public void Simulation_AsymmetricROI_StocksUpBondsDown()
        {
            // Arrange
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 0),
                FxSimObjective.Create(ctx => ctx.Fees = 0),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.50, -0.20)),
            ];

            // Act
            var result = objectives.RunIteration(0, 100_000, 0.6, 0.03, numYears: 1);

            // Assert - 60k * 1.5 + 40k * 0.8 = 90k + 32k = 122k
            Assert.AreEqual(122_000, result.EndingBalance, 0.01);
        }

        #endregion

        #region Withdrawal Edge Cases

        [TestMethod]
        public void Simulation_ZeroWithdrawal_BalanceGrowsOnly()
        {
            // Arrange
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 0),
                FxSimObjective.Create(ctx => ctx.Fees = 0),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.10, 0.05)),
            ];

            // Act
            var result = objectives.RunIteration(0, 100_000, 0.6, 0.03, numYears: 10);

            // Assert - Should grow without withdrawals
            Assert.IsTrue(result.EndingBalance > 100_000);
        }

        [TestMethod]
        public void Simulation_WithdrawalEqualsBalance_FailsOnFirstYear()
        {
            // Arrange
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = ctx.JanBalance),
                FxSimObjective.Create(ctx => ctx.Fees = 0),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.10, 0.05)),
            ];

            // Act
            var result = objectives.RunIteration(0, 100_000, 0.6, 0.03, numYears: 10);

            // Assert - Can't withdraw entire balance and survive
            Assert.AreEqual(0, result.EndingBalance, 0.01);
        }

        [TestMethod]
        public void Simulation_WithdrawalSlightlyLessThanBalance_Survives()
        {
            // Arrange
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = ctx.JanBalance * 0.99),
                FxSimObjective.Create(ctx => ctx.Fees = 0),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 2.0, 1.0)), // Massive growth compensates
            ];

            // Act
            var result = objectives.RunIteration(0, 100_000, 0.6, 0.03, numYears: 5);

            // Assert - With huge growth, might survive
            Assert.IsTrue(result.SurvivedYears > 0);
        }

        #endregion

        #region Fee Edge Cases

        [TestMethod]
        public void Simulation_ZeroFees_NoFeeImpact()
        {
            // Arrange
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 10_000),
                FxSimObjective.Create(ctx => ctx.Fees = 0),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.07, 0.03)),
            ];

            ISimObjective[] objectivesWithFees = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 10_000),
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.0), // Explicitly zero
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.07, 0.03)),
            ];

            // Act
            var result1 = objectives.RunIteration(0, 100_000, 0.6, 0.03, numYears: 10);
            var result2 = objectivesWithFees.RunIteration(0, 100_000, 0.6, 0.03, numYears: 10);

            // Assert - Should be identical
            Assert.AreEqual(result1.EndingBalance, result2.EndingBalance, 0.01);
        }

        [TestMethod]
        public void Simulation_VeryHighFees_RapidDepletion()
        {
            // Arrange
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 0),
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.50), // 50% annual fees!
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.10, 0.05)),
            ];

            // Act
            var result = objectives.RunIteration(0, 100_000, 0.6, 0.03, numYears: 10);

            // Assert - Even with growth, high fees will deplete
            Assert.IsTrue(result.EndingBalance < 100_000);
        }

        #endregion

        #region Temporal Edge Cases

        [TestMethod]
        public void Simulation_SingleYearOnly_CompletesSuccessfully()
        {
            // Arrange
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 1000),
                FxSimObjective.Create(ctx => ctx.Fees = 100),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.05, 0.02)),
            ];

            // Act
            var result = objectives.RunIteration(0, 100_000, 0.6, 0.03, numYears: 1);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.ByYear.Count);
        }

        [TestMethod]
        public void Simulation_VeryLongSimulation_100Years()
        {
            // Arrange
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 10_000),
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.01),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.08, 0.04)),
            ];

            // Act
            var result = objectives.RunIteration(0, 1_000_000, 0.6, 0.03, numYears: 100);

            // Assert - Should complete (may or may not succeed based on parameters)
            Assert.IsTrue(result.ByYear.Count > 0);
            Assert.IsTrue(result.ByYear.Count <= 100);
        }

        #endregion

        #region Allocation Pattern Edge Cases

        [TestMethod]
        public void SimBalance_50_50_Allocation_BalancedGrowth()
        {
            // Arrange
            var balance = new SimBalance(InitialBalance: 100_000, InitialStockPct: 0.5, InitialMaxDrift: 0.03);

            // Act
            balance.Grow(stocksROI: 0.10, bondsROI: 0.04);

            // Assert
            Assert.AreEqual(107_000, balance.CurrentBalance, 0.01); // 50k*1.1 + 50k*1.04
            Assert.AreEqual(55_000, balance.StockBalance, 0.01);
            Assert.AreEqual(52_000, balance.BondBalance, 0.01);
        }

        [TestMethod]
        public void SimBalance_80_20_AggressiveAllocation()
        {
            // Arrange
            var balance = new SimBalance(InitialBalance: 100_000, InitialStockPct: 0.8, InitialMaxDrift: 0.05);

            // Act
            balance.Grow(stocksROI: 0.12, bondsROI: 0.03);

            // Assert
            Assert.AreEqual(110_200, balance.CurrentBalance, 0.01); // 80k*1.12 + 20k*1.03
        }

        [TestMethod]
        public void SimBalance_20_80_ConservativeAllocation()
        {
            // Arrange
            var balance = new SimBalance(InitialBalance: 100_000, InitialStockPct: 0.2, InitialMaxDrift: 0.05);

            // Act
            balance.Grow(stocksROI: 0.12, bondsROI: 0.03);

            // Assert
            Assert.AreEqual(104_800, balance.CurrentBalance, 0.01); // 20k*1.12 + 80k*1.03
        }

        #endregion

        #region Multiple Strategy Interactions

        [TestMethod]
        public void Simulation_CompoundingFeesAndWithdrawals_CorrectSequencing()
        {
            // Arrange
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 5000),
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.01),
                FxSimObjective.Create(ctx => ctx.Fees += 500), // Additional fixed fee
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.06, 0.03)),
            ];

            // Act
            var result = objectives.RunIteration(0, 100_000, 0.6, 0.03, numYears: 1);

            // Assert
            var year = result.ByYear[0];
            Assert.AreEqual(1500, year.Fees, 0.01); // 1000 + 500
            Assert.AreEqual(5000, year.ActualWithdrawal, 0.01);
        }

        #endregion

        #region Precision and Rounding Tests

        [TestMethod]
        public void SimBalance_TinyAmounts_MaintainsPrecision()
        {
            // Arrange
            var balance = new SimBalance(InitialBalance: 1.00, InitialStockPct: 0.6, InitialMaxDrift: 0.03);

            // Act
            balance.Reduce(0.50);
            balance.Grow(stocksROI: 0.10, bondsROI: 0.05);

            // Assert - Should handle tiny amounts without losing to rounding
            Assert.IsTrue(balance.CurrentBalance > 0.50);
            Assert.IsTrue(balance.CurrentBalance < 0.60);
        }

        #endregion
    }
}
