
using NinthBall;

namespace UnitTests
{
    /// <summary>
    /// Independent verification tests - designed to catch potential defects
    /// that the other 90 tests might have missed by being "too nice".
    /// </summary>
    [TestClass]
    public sealed class IndependentVerificationTests
    {
        /// <summary>
        /// Stress test: Rapid allocation changes should not break invariants
        /// </summary>
        [TestMethod]
        public void StressTest_RapidReallocation_MaintainsInvariants()
        {
            var balance = new SimBalance(100_000, 0.6, 0.03);
            
            // Stress with rapid changes
            for (int i = 0; i < 100; i++)
            {
                double newAlloc = (i % 10) / 10.0; // 0%, 10%, 20%, ... 90%, repeat
                balance.Reallocate(newAlloc, 0.05);
                
                // Verify invariant ALWAYS holds
                var total = balance.StockBalance + balance.BondBalance;
                Assert.AreEqual(balance.CurrentBalance, total, 0.01, 
                    $"Invariant violated at iteration {i}");
            }
        }

        /// <summary>
        /// Edge case: Sequence of operations that could expose hidden bugs
        /// </summary>
        [TestMethod]
        public void EdgeCase_ComplexSequence_NoUnexpectedBehavior()
        {
            var balance = new SimBalance(100_000, 0.6, 0.03);
            
            // Complex sequence
            balance.Reduce(10_000);                          // 90k remaining
            balance.Grow(0.50, -0.30);                       // Asymmetric growth
            balance.Rebalance();                             // Force rebalance
            balance.Reallocate(0.8, 0.10);                   // Aggressive allocation
            balance.Grow(-0.40, -0.20);                      // Market crash
            balance.Reduce(balance.CurrentBalance * 0.5);    // 50% withdrawal
            
            // Should not throw, invariants should hold
            Assert.IsTrue(balance.CurrentBalance > 0);
            Assert.AreEqual(balance.CurrentBalance, 
                balance.StockBalance + balance.BondBalance, 0.01);
        }

        /// <summary>
        /// Adversarial test: Try to break the withdrawal system
        /// </summary>
        [TestMethod]
        public void AdversarialTest_ExtremeWithdrawals_HandlesGracefully()
        {
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => 
                {
                    // Try to withdraw more than available
                    ctx.PlannedWithdrawalAmount = ctx.JanBalance * 1.5;
                }),
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.5),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.0, 0.0)),
            ];

            // Should fail gracefully, not crash
            var result = objectives.RunIteration(0, 10_000, 0.6, 0.03, numYears: 5);
            
            Assert.IsFalse(result.Success); // Should fail
            Assert.IsNotNull(result.ByYear); // Should have partial results
            Assert.IsTrue(result.ByYear.Count > 0); // Should record at least one year
        }

        /// <summary>
        /// Boundary test: Zero balance edge case
        /// </summary>
        [TestMethod]
        public void BoundaryTest_ZeroBalance_AfterFullWithdrawal()
        {
            var balance = new SimBalance(1000, 0.6, 0.03);
            balance.Reduce(1000); // Complete depletion
            
            // Should handle zero gracefully
            Assert.AreEqual(0, balance.CurrentBalance, 0.01);
            Assert.AreEqual(0, balance.StockBalance, 0.01);
            Assert.AreEqual(0, balance.BondBalance, 0.01);
            
            // Should not allow further reduction
            Assert.ThrowsException<InvalidOperationException>(() => 
                balance.Reduce(1)
            );
        }

        /// <summary>
        /// Consistency test: Same inputs should ALWAYS produce same outputs
        /// </summary>
        [TestMethod]
        public void ConsistencyTest_Deterministic_Behavior()
        {
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 40_000),
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.01),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.08, 0.04)),
            ];

            // Run multiple times
            var results = new List<double>();
            for (int i = 0; i < 10; i++)
            {
                var result = objectives.RunIteration(0, 1_000_000, 0.6, 0.03, numYears: 10);
                results.Add(result.EndingBalance);
            }

            // All should be identical
            var first = results[0];
            Assert.IsTrue(results.All(r => Math.Abs(r - first) < 0.01),
                "Non-deterministic behavior detected!");
        }
    }
}
