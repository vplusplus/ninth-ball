
using NinthBall;

namespace UnitTests
{
    /// <summary>
    /// Whitebox tests for simulation iteration execution.
    /// Tests single and multi-iteration scenarios, result aggregation, and ordering.
    /// </summary>
    [TestClass]
    public sealed class SimulationIterationTests
    {
        #region Single Iteration Tests

        [TestMethod]
        public void RunIteration_StandardScenario_Success()
        {
            // Arrange
            const double Initial = 1_000_000;
            const double Alloc = 0.6;
            const double MaxDrift = 0.03;
            const int NumYears = 10;

            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 40_000),
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.01),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.07, 0.03)),
            ];

            // Act
            var result = objectives.RunIteration(0, Initial, Alloc, MaxDrift, NumYears);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, result.Index);
            Assert.AreEqual(NumYears, result.ByYear.Count);
            Assert.AreEqual(Initial, result.StartingBalance, 0.01);
            Assert.IsTrue(result.EndingBalance > 0);
            Assert.AreEqual(NumYears, result.SurvivedYears);
        }

        [TestMethod]
        public void RunIteration_HighWithdrawal_Fails()
        {
            // Arrange - High withdrawal will deplete balance
            const double Initial = 100_000;
            const double Alloc = 0.6;
            const double MaxDrift = 0.03;
            const int NumYears = 30;

            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 15_000), // 15% withdrawal
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.02),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.04, 0.02)), // Moderate growth
            ];

            // Act
            var result = objectives.RunIteration(0, Initial, Alloc, MaxDrift, NumYears);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsTrue(result.ByYear.Count < NumYears);
            Assert.AreEqual(result.ByYear.Count - 1, result.SurvivedYears);
        }

        [TestMethod]
        public void RunIteration_SingleYear_CompletesSuccessfully()
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
            Assert.AreEqual(1, result.SurvivedYears);
        }

        [TestMethod]
        public void RunIteration_AllYearsRecorded_CorrectProgression()
        {
            // Arrange
            const int NumYears = 5;
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 2000),
                FxSimObjective.Create(ctx => ctx.Fees = 100),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020 + ctx.YearIndex, 0.06, 0.03)),
            ];

            // Act
            var result = objectives.RunIteration(0, 100_000, 0.6, 0.03, NumYears);

            // Assert
            Assert.AreEqual(NumYears, result.ByYear.Count);
            for (int i = 0; i < NumYears; i++)
            {
                Assert.AreEqual(i, result.ByYear[i].Year);
                Assert.AreEqual(2020 + i, result.ByYear[i].LikeYear);
            }
        }

        #endregion

        #region Multi-Iteration Tests

        [TestMethod]
        public void RunSimulation_MultipleIterations_CorrectCount()
        {
            // Arrange
            const int NumIterations = 100;
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 40_000),
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.01),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.07, 0.03)),
            ];

            // Act
            var result = objectives.RunSimulation(1_000_000, 0.6, 0.03, numYears: 10, NumIterations);

            // Assert
            Assert.AreEqual(NumIterations, result.Results.Count);
        }

        [TestMethod]
        public void RunSimulation_OrdersWorstToBest_ByReturns()
        {
            // Arrange - Create scenario with varying outcomes
            int iterationCounter = 0;
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 40_000),
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.01),
                FxSimObjective.Create(ctx => 
                {
                    // Vary ROI by iteration - some iterations do better than others
                    var roi = (iterationCounter % 3) switch
                    {
                        0 => new YROI(2020, 0.03, 0.02), // Mediocre
                        1 => new YROI(2020, 0.12, 0.05), // Great
                        _ => new YROI(2020, 0.08, 0.04), // Good
                    };
                    iterationCounter++;
                    ctx.ROI = roi;
                }),
            ];

            // Act
            var result = objectives.RunSimulation(1_000_000, 0.6, 0.03, numYears: 10, numIterations: 30);

            // Assert - Results should be ordered worst-to-best (by survived years, then ending balance)
            for (int i = 1; i < result.Results.Count; i++)
            {
                var prev = result.Results[i - 1];
                var curr = result.Results[i];

                // Either current survived more years, or same years but higher ending balance
                Assert.IsTrue(
                    curr.SurvivedYears > prev.SurvivedYears ||
                    (curr.SurvivedYears == prev.SurvivedYears && curr.EndingBalance >= prev.EndingBalance),
                    "Results should be ordered worst-to-best"
                );
            }
        }

        [TestMethod]
        public void RunSimulation_SurvivalRate_CalculatedCorrectly()
        {
            // Arrange - Scenario where some fail
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 60_000), // High withdrawal
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.01),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.06, 0.03)),
            ];

            // Act
            var result = objectives.RunSimulation(500_000, 0.6, 0.03, numYears: 30, numIterations: 10);

            // Assert
            var successCount = result.Results.Count(x => x.Success);
            var expectedRate = (double)successCount / result.Results.Count;
            
            Assert.AreEqual(expectedRate, result.SurvivalRate, 0.001);
            Assert.IsTrue(result.SurvivalRate >= 0.0 && result.SurvivalRate <= 1.0);
        }

        #endregion

        #region Strategy Composition Tests

        [TestMethod]
        public void StrategyComposition_MultipleObjectives_AppliedInOrder()
        {
            // Arrange - Track execution order
            var executionOrder = new List<string>();

            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => 
                {
                    executionOrder.Add("Withdrawal");
                    ctx.PlannedWithdrawalAmount = 1000;
                }),
                FxSimObjective.Create(ctx => 
                {
                    executionOrder.Add("Fees");
                    ctx.Fees = 100;
                }),
                FxSimObjective.Create(ctx => 
                {
                    executionOrder.Add("ROI");
                    ctx.ROI = new YROI(2020, 0.05, 0.02);
                }),
            ];

            // Act
            var result = objectives.RunIteration(0, 100_000, 0.6, 0.03, numYears: 1);

            // Assert
            Assert.AreEqual(3, executionOrder.Count);
            Assert.AreEqual("Withdrawal", executionOrder[0]);
            Assert.AreEqual("Fees", executionOrder[1]);
            Assert.AreEqual("ROI", executionOrder[2]);
        }

        [TestMethod]
        public void StrategyComposition_DependentStrategies_WorkCorrectly()
        {
            // Arrange - Second strategy depends on first
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 4000),
                FxSimObjective.Create(ctx => 
                {
                    // Adjust withdrawal based on available balance
                    if (ctx.AvailableBalance < ctx.WithdrawalAmount)
                        ctx.WithdrawalAmount = Math.Max(0, ctx.AvailableBalance);
                }),
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.01),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.05, 0.02)),
            ];

            // Act - Low balance scenario
            var result = objectives.RunIteration(0, 3000, 0.6, 0.03, numYears: 1);

            // Assert
            Assert.IsTrue(result.Success);
            var actualWithdrawal = result.ByYear[0].ActualWithdrawal;
            Assert.IsTrue(actualWithdrawal < 4000); // Should be adjusted
        }

        #endregion

        #region Result Structure Validation Tests

        [TestMethod]
        public void SimIteration_Properties_CalculatedCorrectly()
        {
            // Arrange
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 40_000),
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.01),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.07, 0.03)),
            ];

            // Act
            var result = objectives.RunIteration(5, 1_000_000, 0.6, 0.03, numYears: 10);

            // Assert
            Assert.AreEqual(5, result.Index);
            Assert.AreEqual(1_000_000, result.StartingBalance, 0.01);
            Assert.AreEqual(result.ByYear[^1].DecBalance, result.EndingBalance, 0.01);
            
            if (result.Success)
                Assert.AreEqual(result.ByYear.Count, result.SurvivedYears);
            else
                Assert.AreEqual(result.ByYear.Count - 1, result.SurvivedYears);
        }

        [TestMethod]
        public void SimYear_Properties_AllPopulated()
        {
            // Arrange
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 40_000),
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.01),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2023, 0.10, 0.04)),
            ];

            // Act
            var result = objectives.RunIteration(0, 1_000_000, 0.6, 0.03, numYears: 1);

            // Assert
            var year = result.ByYear[0];
            Assert.AreEqual(0, year.Year);
            Assert.AreEqual(1_000_000, year.JanBalance, 0.01);
            Assert.AreEqual(0.6, year.JanStockPct, 0.001);
            Assert.AreEqual(0.4, year.JanBondPct, 0.001);
            Assert.AreEqual(10_000, year.Fees, 0.01);
            Assert.AreEqual(40_000, year.PlannedWithdrawal, 0.01);
            Assert.AreEqual(40_000, year.ActualWithdrawal, 0.01);
            Assert.IsTrue(year.DecBalance > 0);
            Assert.AreEqual(0.10, year.StockROI, 0.001);
            Assert.AreEqual(0.04, year.BondROI, 0.001);
            Assert.AreEqual(2023, year.LikeYear);
        }

        #endregion

        #region Deterministic Behavior Tests

        [TestMethod]
        public void RunIteration_SameInputs_ProducesSameResults()
        {
            // Arrange
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 40_000),
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.01),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.07, 0.03)),
            ];

            // Act - Run twice with same inputs
            var result1 = objectives.RunIteration(0, 1_000_000, 0.6, 0.03, numYears: 10);
            var result2 = objectives.RunIteration(0, 1_000_000, 0.6, 0.03, numYears: 10);

            // Assert
            Assert.AreEqual(result1.Success, result2.Success);
            Assert.AreEqual(result1.EndingBalance, result2.EndingBalance, 0.01);
            Assert.AreEqual(result1.SurvivedYears, result2.SurvivedYears);
            Assert.AreEqual(result1.ByYear.Count, result2.ByYear.Count);
        }

        #endregion
    }
}
