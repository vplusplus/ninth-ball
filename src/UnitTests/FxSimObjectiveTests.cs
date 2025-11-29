
using NinthBall;

namespace UnitTests
{
    /// <summary>
    /// Whitebox tests for FxSimObjective helper.
    /// Tests the pattern of creating simulation objectives from lambda expressions or static methods.
    /// </summary>
    [TestClass]
    public sealed class FxSimObjectiveTests
    {
        #region Creation Tests

        [TestMethod]
        public void Create_FromLambdaExpression_Success()
        {
            // Arrange
            Action<ISimContext> lambda = ctx => ctx.PlannedWithdrawalAmount = 4000;

            // Act
            var objective = FxSimObjective.Create(lambda);

            // Assert
            Assert.IsNotNull(objective);
            Assert.IsInstanceOfType<ISimObjective>(objective);
        }

        [TestMethod]
        public void Create_FromStaticMethod_Success()
        {
            // Act
            var objective = FxSimObjective.Create(SetFixedWithdrawal);

            // Assert
            Assert.IsNotNull(objective);
            Assert.IsInstanceOfType<ISimObjective>(objective);
        }

        [TestMethod]
        public void Create_FromLocalFunction_Success()
        {
            // Arrange
            void LocalWithdrawFunction(ISimContext ctx) => ctx.PlannedWithdrawalAmount = 3000;

            // Act
            var objective = FxSimObjective.Create(LocalWithdrawFunction);

            // Assert
            Assert.IsNotNull(objective);
        }

        [TestMethod]
        public void Create_FromInstanceMethod_Success()
        {
            // Arrange
            var helper = new TestHelper();

            // Act
            var objective = FxSimObjective.Create(helper.SetWithdrawal);

            // Assert
            Assert.IsNotNull(objective);
        }

        [TestMethod]
        public void CreateStrategy_WithNullFunction_ThrowsException()
        {
            // Arrange
            var objective = new FxSimObjective(null!);

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => 
                ((ISimObjective)objective).CreateStrategy(iterationIndex: 0)
            );
        }

        #endregion

        #region Strategy Application Tests

        [TestMethod]
        public void Strategy_AppliesFunction_ModifiesContext()
        {
            // Arrange
            var objective = FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 5000);
            var strategy = ((ISimObjective)objective).CreateStrategy(iterationIndex: 0);
            
            var ctx = new SimContext(0, 100_000, 0.6, 0.03);
            ISimContext iCtx = ctx;
            ctx.StartYear(0);

            // Act
            strategy.Apply(iCtx);

            // Assert
            Assert.AreEqual(5000, iCtx.WithdrawalAmount, 0.01);
        }

        [TestMethod]
        public void Strategy_MultipleObjectives_ExecuteInSequence()
        {
            // Arrange
            var objective1 = FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 4000);
            var objective2 = FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.01);
            var objective3 = FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.10, 0.03));

            var strategies = new[] 
            { 
                ((ISimObjective)objective1).CreateStrategy(0), 
                ((ISimObjective)objective2).CreateStrategy(0), 
                ((ISimObjective)objective3).CreateStrategy(0) 
            };

            var ctx = new SimContext(0, 100_000, 0.6, 0.03);
            ISimContext iCtx = ctx;
            ctx.StartYear(0);

            // Act
            foreach (var strategy in strategies)
            {
                strategy.Apply(iCtx);
            }

            // Assert
            Assert.AreEqual(4000, iCtx.WithdrawalAmount, 0.01);
            Assert.AreEqual(1000, iCtx.Fees, 0.01);
        }

        [TestMethod]
        public void Strategy_ModifyWithdrawalAfterPlanned_Success()
        {
            // Arrange
            var objective1 = FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 4000);
            var objective2 = FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.03); // Add fees first
            var objective3 = FxSimObjective.Create(ctx => 
            {
                // Reducewithdrawal if available balance is low
                if (ctx.AvailableBalance < ctx.WithdrawalAmount)
                    ctx.WithdrawalAmount = Math.Max(0, ctx.AvailableBalance);
            });

            var strategies = new[] { 
                ((ISimObjective)objective1).CreateStrategy(0), 
                ((ISimObjective)objective2).CreateStrategy(0),
                ((ISimObjective)objective3).CreateStrategy(0) 
            };
            
            var ctx = new SimContext(0, 5000, 0.6, 0.03); // Low balance
            ISimContext iCtx = ctx;
            ctx.StartYear(0);

            // Act
            foreach (var strategy in strategies)
            {
                strategy.Apply(iCtx);
            }

            // Assert
            // Balance: 5000, Fees: 150, Planned: 4000, Available: 4850
            // Should not adjust (enough balance)
            Assert.IsTrue(iCtx.WithdrawalAmount <= 5000);
            Assert.IsTrue(iCtx.WithdrawalAmount >= 0);
        }

        #endregion

        #region Error Handling Tests

        [TestMethod]
        public void Strategy_ThrowsException_Propagates()
        {
            // Arrange
            var objective = FxSimObjective.Create(ctx => 
                throw new InvalidOperationException("Test exception")
            );
            var strategy = ((ISimObjective)objective).CreateStrategy(0);
            
            var ctx = new SimContext(0, 100_000, 0.6, 0.03);
            ISimContext iCtx = ctx;
            ctx.StartYear(0);

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() => strategy.Apply(iCtx));
        }

        #endregion

        #region Integration with RunIteration Tests

        [TestMethod]
        public void Integration_WithRunIteration_ProducesValidResults()
        {
            // Arrange
            const double Initial = 1_000_000;
            const double Alloc = 0.6;
            const double MaxDrift = 0.03;
            const int NumYears = 5;

            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 40_000),
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.01),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.08, 0.04)),
            ];

            // Act
            var result = objectives.RunIteration(0, Initial, Alloc, MaxDrift, NumYears);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(NumYears, result.ByYear.Count);
            Assert.AreEqual(Initial, result.StartingBalance, 0.01);
            Assert.IsTrue(result.EndingBalance > 0);
        }

        [TestMethod]
        public void Integration_ComplexScenario_MatchesExpected()
        {
            // Arrange - Replicate SampleTest.cs scenario
            const double Initial = 1000.0;
            const double Alloc = 0.6;
            const double MaxDrift = 0.03;
            const int NumYears = 5;

            void SetPlannedWithdrawalAmount(ISimContext ctx) => ctx.PlannedWithdrawalAmount = 1000;
            void SetFees(ISimContext ctx) => ctx.Fees = ctx.JanBalance * 0.09;

            ISimObjective[] myObjectives = 
            [
                FxSimObjective.Create(SetPlannedWithdrawalAmount),
                FxSimObjective.Create(SetFees),
            ];

            // Act & Assert - Should complete without throwing
            var result = myObjectives.RunIteration(1, Initial, Alloc, MaxDrift, NumYears);
            
            // With high fees and withdrawals, this small balance won't last
            Assert.IsFalse(result.Success); // Should fail quickly
            Assert.IsTrue(result.ByYear.Count < NumYears);
        }

        #endregion

        #region Helper Methods and Classes

        private static void SetFixedWithdrawal(ISimContext ctx)
        {
            ctx.PlannedWithdrawalAmount = 4000;
        }

        private class TestHelper
        {
            public void SetWithdrawal(ISimContext ctx)
            {
                ctx.PlannedWithdrawalAmount = 3500;
            }
        }

        #endregion
    }
}
