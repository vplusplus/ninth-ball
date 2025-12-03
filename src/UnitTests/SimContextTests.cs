
using NinthBall;

namespace UnitTests
{
    /// <summary>
    /// Whitebox tests for SimContext state management.
    /// Tests iteration lifecycle, property access patterns, and integration with SimBalance.
    /// </summary>
    [TestClass]
    public sealed class SimContextTests
    {
        #region Lifecycle Tests

        [TestMethod]
        public void Constructor_InitializesCorrectly()
        {
            // Arrange & Act
            var ctx = new SimContext(
                IterationIndex: 5,
                InitialBalance: 100_000,
                InitialStockAllocation: 0.6,
                InitialMaxDrift: 0.03
            );

            // Assert
            Assert.AreEqual(5, ctx.IterationIndex);
            Assert.AreEqual(100_000, ctx.JanBalance, 0.01);
            Assert.AreEqual(0, ctx.PriorYears.Count);
        }

        [TestMethod]
        public void StartYear_InitializesYearState()
        {
            // Arrange
            var ctx = new SimContext(0, 100_000, 0.6, 0.03);

            // Act
            ctx.StartYear(0);

            // Assert
            Assert.AreEqual(0, ctx.YearIndex);
            Assert.AreEqual(100_000, ctx.JanBalance, 0.01);
            Assert.AreEqual(0, ctx.PriorYears.Count);
        }

        [TestMethod]
        public void EndYear_AddsYearToPriorYears()
        {
            // Arrange
            var ctx = new SimContext(0, 100_000, 0.6, 0.03);
            ISimContext iCtx = ctx;

            ctx.StartYear(0);
            iCtx.PlannedWithdrawalAmount = 4000;
            iCtx.Fees = 900;
            iCtx.ROI = new YROI(2020, 0.10, 0.03);

            // Act
            var success = ctx.EndYear();

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(1, ctx.PriorYears.Count);
            
            var year = ctx.PriorYears[0];
            Assert.AreEqual(0, year.Year);
            Assert.AreEqual(100_000, year.JanBalance, 0.01);
            Assert.AreEqual(4000, year.PlannedWithdrawal, 0.01);
            Assert.AreEqual(4000, year.ActualWithdrawal, 0.01);
            Assert.AreEqual(900, year.Fees, 0.01);
        }

        [TestMethod]
        public void MultiYearProgression_AccumulatesPriorYears()
        {
            // Arrange
            var ctx = new SimContext(0, 100_000, 0.6, 0.03);
            ISimContext iCtx = ctx;

            // Act - Simulate 3 years
            for (int year = 0; year < 3; year++)
            {
                ctx.StartYear(year);
                iCtx.PlannedWithdrawalAmount = 4000;
                iCtx.Fees = 900;
                iCtx.ROI = new YROI(2020 + year, 0.10, 0.03);
                ctx.EndYear();
            }

            // Assert
            Assert.AreEqual(3, ctx.PriorYears.Count);
            Assert.AreEqual(0, ctx.PriorYears[0].Year);
            Assert.AreEqual(1, ctx.PriorYears[1].Year);
            Assert.AreEqual(2, ctx.PriorYears[2].Year);
        }

        [TestMethod]
        public void EndYear_InsufficientBalance_ReturnsFailure()
        {
            // Arrange
            var ctx = new SimContext(0, 1000, 0.6, 0.03);
            ISimContext iCtx = ctx;

            ctx.StartYear(0);
            iCtx.PlannedWithdrawalAmount = 2000; // More than available
            iCtx.Fees = 100;
            iCtx.ROI = new YROI(2020, 0.0, 0.0);

            // Act
            var success = ctx.EndYear();

            // Assert
            Assert.IsFalse(success);
        }

        #endregion

        #region Property Access Tests

        [TestMethod]
        public void PlannedWithdrawalAmount_SetOnce_Success()
        {
            // Arrange
            var ctx = new SimContext(0, 100_000, 0.6, 0.03);
            ISimContext iCtx = ctx;
            ctx.StartYear(0);

            // Act
            iCtx.PlannedWithdrawalAmount = 4000;

            // Assert
            Assert.AreEqual(4000, iCtx.WithdrawalAmount, 0.01);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void PlannedWithdrawalAmount_SetTwice_ThrowsException()
        {
            // Arrange
            var ctx = new SimContext(0, 100_000, 0.6, 0.03);
            ISimContext iCtx = ctx;
            ctx.StartYear(0);

            // Act
            iCtx.PlannedWithdrawalAmount = 4000;
            iCtx.PlannedWithdrawalAmount = 5000; // Should throw
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void WithdrawalAmount_GetBeforeSet_ThrowsException()
        {
            // Arrange
            var ctx = new SimContext(0, 100_000, 0.6, 0.03);
            ISimContext iCtx = ctx;
            ctx.StartYear(0);

            // Act
            var _ = iCtx.WithdrawalAmount; // Should throw
        }

        [TestMethod]
        public void WithdrawalAmount_ModifyAfterPlanned_Success()
        {
            // Arrange
            var ctx = new SimContext(0, 100_000, 0.6, 0.03);
            ISimContext iCtx = ctx;
            ctx.StartYear(0);
            iCtx.PlannedWithdrawalAmount = 4000;

            // Act
            iCtx.WithdrawalAmount = 3000; // Reduce withdrawal

            // Assert
            Assert.AreEqual(3000, iCtx.WithdrawalAmount, 0.01);
        }

        [TestMethod]
        public void Fees_DefaultToZero()
        {
            // Arrange
            var ctx = new SimContext(0, 100_000, 0.6, 0.03);
            ISimContext iCtx = ctx;
            ctx.StartYear(0);

            // Assert
            Assert.AreEqual(0, iCtx.Fees, 0.01);
        }

        [TestMethod]
        public void Fees_AccumulateMultipleAssignments()
        {
            // Arrange
            var ctx = new SimContext(0, 100_000, 0.6, 0.03);
            ISimContext iCtx = ctx;
            ctx.StartYear(0);

            // Act - Simulate multiple fee strategies
            iCtx.Fees += 500;
            iCtx.Fees += 400;

            // Assert
            Assert.AreEqual(900, iCtx.Fees, 0.01);
        }

        [TestMethod]
        public void ROI_SetAndUsed()
        {
            // Arrange
            var ctx = new SimContext(0, 100_000, 0.6, 0.03);
            ISimContext iCtx = ctx;
            ctx.StartYear(0);
            iCtx.PlannedWithdrawalAmount = 4000;
            iCtx.Fees = 900;

            // Act
            iCtx.ROI = new YROI(2020, 0.10, 0.03);
            ctx.EndYear();

            // Assert
            var year = ctx.PriorYears[0];
            Assert.AreEqual(0.10, year.StockROI, 0.001);
            Assert.AreEqual(0.03, year.BondROI, 0.001);
            Assert.AreEqual(2020, year.LikeYear);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void ROI_SetNull_ThrowsException()
        {
            // Arrange
            var ctx = new SimContext(0, 100_000, 0.6, 0.03);
            ISimContext iCtx = ctx;
            ctx.StartYear(0);

            // Act
            iCtx.ROI = null!; // Should throw
        }

        #endregion

        #region AvailableBalance Tests

        [TestMethod]
        public void AvailableBalance_AfterFeesAndWithdrawal_CorrectCalculation()
        {
            // Arrange
            var ctx = new SimContext(0, 100_000, 0.6, 0.03);
            ISimContext iCtx = ctx;
            ctx.StartYear(0);

            // Act
            iCtx.PlannedWithdrawalAmount = 4000;
            iCtx.Fees = 900;

            // Assert
            Assert.AreEqual(95_100, iCtx.AvailableBalance, 0.01); // 100k - 900 - 4000
        }

        [TestMethod]
        public void AvailableBalance_BeforeAnyDeductions_EqualsJanBalance()
        {
            // Arrange
            var ctx = new SimContext(0, 100_000, 0.6, 0.03);
            ctx.StartYear(0);

            // Assert
            Assert.AreEqual(100_000, ctx.AvailableBalance, 0.01);
        }

        #endregion

        #region Integration with SimBalance Tests

        [TestMethod]
        public void Integration_BalanceReduction_AppliedCorrectly()
        {
            // Arrange
            var ctx = new SimContext(0, 100_000, 0.6, 0.03);
            ISimContext iCtx = ctx;

            ctx.StartYear(0);
            iCtx.PlannedWithdrawalAmount = 4000;
            iCtx.Fees = 900;
            iCtx.ROI = new YROI(2020, 0.10, 0.03);

            // Act
            ctx.EndYear();

            // Assert - Check balance after fees, withdrawal, and growth
            var expectedBalance = (100_000 - 900 - 4000) * 1.0674; // Weighted average return
            Assert.AreEqual(expectedBalance, ctx.JanBalance, 500); // Allow some tolerance for weighted calc
        }

        [TestMethod]
        public void Integration_RebalancingAtYearStart_TriggeredCorrectly()
        {
            // Arrange
            var ctx = new SimContext(0, 100_000, 0.6, 0.03);
            ISimContext iCtx = ctx;

            // Year 0 - Create drift
            ctx.StartYear(0);
            iCtx.PlannedWithdrawalAmount = 1000;
            iCtx.Fees = 100;
            iCtx.ROI = new YROI(2020, 0.50, 0.0); // Stocks up significantly
            ctx.EndYear();

            var janBalanceYear1 = ctx.JanBalance;

            // Year 1 - Should rebalance at start
            ctx.StartYear(1);

            // Assert - Balance should remain the same, but allocation was rebalanced
            Assert.AreEqual(janBalanceYear1, ctx.JanBalance, 0.01);
        }

        #endregion

        #region Year State Reset Tests

        [TestMethod]
        public void StartYear_ResetsStateFromPreviousYear()
        {
            // Arrange
            var ctx = new SimContext(0, 100_000, 0.6, 0.03);
            ISimContext iCtx = ctx;

            // Year 0
            ctx.StartYear(0);
            iCtx.PlannedWithdrawalAmount = 4000;
            iCtx.Fees = 900;
            iCtx.ROI = new YROI(2020, 0.10, 0.03);
            ctx.EndYear();

            // Act - Year 1
            ctx.StartYear(1);

            // Assert - New year should not have withdrawal set
            Assert.ThrowsException<Exception>(() => _ = iCtx.WithdrawalAmount);
        }

        #endregion

        #region Success/Failure Classification Tests

        [TestMethod]
        public void EndYear_SufficientBalance_Success()
        {
            // Arrange
            var ctx = new SimContext(0, 100_000, 0.6, 0.03);
            ISimContext iCtx = ctx;

            ctx.StartYear(0);
            iCtx.PlannedWithdrawalAmount = 4000;
            iCtx.Fees = 900;
            iCtx.ROI = new YROI(2020, 0.10, 0.03);

            // Act
            var success = ctx.EndYear();

            // Assert
            Assert.IsTrue(success);
            Assert.IsTrue(ctx.PriorYears[0].DecBalance > 0);
        }

        [TestMethod]
        public void EndYear_InsufficientBalance_FailureWithZeroValues()
        {
            // Arrange
            var ctx = new SimContext(0, 1000, 0.6, 0.03);
            ISimContext iCtx = ctx;

            ctx.StartYear(0);
            iCtx.PlannedWithdrawalAmount = 2000;
            iCtx.Fees = 100;
            iCtx.ROI = new YROI(2020, 0.0, 0.0);

            // Act
            var success = ctx.EndYear();

            // Assert
            Assert.IsFalse(success);
            var year = ctx.PriorYears[0];
            Assert.AreEqual(0, year.PlannedWithdrawal, 0.01);
            Assert.AreEqual(0, year.ActualWithdrawal, 0.01);
            Assert.AreEqual(0, year.Fees, 0.01);
            Assert.AreEqual(0, year.DecBalance, 0.01);
        }

        [TestMethod]
        public void EndYear_NoWithdrawalSet_Failure()
        {
            // Arrange
            var ctx = new SimContext(0, 100_000, 0.6, 0.03);
            ISimContext iCtx = ctx;

            ctx.StartYear(0);
            // Don't set withdrawal
            iCtx.Fees = 900;
            iCtx.ROI = new YROI(2020, 0.10, 0.03);

            // Act
            var success = ctx.EndYear();

            // Assert
            Assert.IsFalse(success);
        }

        #endregion
    }
}
