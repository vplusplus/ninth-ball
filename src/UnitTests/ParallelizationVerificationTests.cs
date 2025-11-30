
using NinthBall;

namespace UnitTests
{
    /// <summary>
    /// Verification tests for parallelization and thread safety claims.
    /// Tests verify: (1) Deterministic RNG, (2) Simulation independence, (3) Iteration independence
    /// </summary>
    [TestClass]
    public sealed class ParallelizationVerificationTests
    {
        /// <summary>
        /// CLAIM 1: Deterministic - Same inputs produce identical results across runs
        /// </summary>
        [TestMethod]
        public void Claim1_DeterministicRNG_SameInputsProduceSameResults()
        {
            // Arrange - Create identical objectives
            ISimObjective[] CreateObjectives() =>
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 40_000),
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.01),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.07, 0.03)),
            ];

            // Act - Run simulation 5 times with SAME seed
            var results = new List<SimIteration>();
            for (int run = 0; run < 5; run++)
            {
                var objectives = CreateObjectives();
                var result = objectives.RunIteration(
                    iterationIndex: 42,  // Same iteration index
                    initialBalance: 1_000_000,
                    initialAllocation: 0.6,
                    initialMaxDrift: 0.03,
                    numYears: 10
                );
                results.Add(result);
            }

            // Assert - All runs should produce IDENTICAL results
            var first = results[0];
            foreach (var result in results.Skip(1))
            {
                Assert.AreEqual(first.Success, result.Success, "Success differs");
                Assert.AreEqual(first.EndingBalance, result.EndingBalance, 0.01, "Ending balance differs");
                Assert.AreEqual(first.SurvivedYears, result.SurvivedYears, "Survived years differs");
                
                // Verify year-by-year results are identical
                for (int y = 0; y < first.ByYear.Count; y++)
                {
                    Assert.AreEqual(first.ByYear[y].DecBalance, result.ByYear[y].DecBalance, 0.01,
                        $"Year {y} balance differs");
                }
            }
        }

        /// <summary>
        /// CLAIM 1b: Different iteration indices produce different (but deterministic) results
        /// </summary>
        [TestMethod]
        public void Claim1b_DifferentIterations_ProduceDifferentResults()
        {
            // Arrange
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 40_000),
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.01),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.07, 0.03)),
            ];

            // Act - Run different iterations
            var iteration10 = objectives.RunIteration(10, 1_000_000, 0.6, 0.03, 10);
            var iteration20 = objectives.RunIteration(20, 1_000_000, 0.6, 0.03, 10);
            var iteration10Again = objectives.RunIteration(10, 1_000_000, 0.6, 0.03, 10);

            // Assert
            // Different iterations should produce different results (different random sequences)
            Assert.AreNotEqual(iteration10.EndingBalance, iteration20.EndingBalance, 
                "Different iterations should have different outcomes");
            
            // Same iteration should be identical
            Assert.AreEqual(iteration10.EndingBalance, iteration10Again.EndingBalance, 0.01,
                "Same iteration should be deterministic");
        }

        /// <summary>
        /// CLAIM 2: Simulations are independent - Can run in parallel without interference
        /// </summary>
        [TestMethod]
        public void Claim2_SimulationIndependence_ParallelExecutionWorks()
        {
            // Arrange
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 40_000),
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.01),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.08, 0.04)),
            ];

            // Act - Run 100 simulations in PARALLEL
            var parallelResults = Enumerable.Range(0, 100)
                .AsParallel()
                .Select(i => objectives.RunIteration(i, 1_000_000, 0.6, 0.03, 10))
                .ToList();

            // Run same 100 simulations SEQUENTIALLY for comparison
            var sequentialResults = Enumerable.Range(0, 100)
                .Select(i => objectives.RunIteration(i, 1_000_000, 0.6, 0.03, 10))
                .ToList();

            // Assert - Parallel and sequential should produce identical results
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(sequentialResults[i].EndingBalance, parallelResults[i].EndingBalance, 0.01,
                    $"Iteration {i}: Parallel execution differs from sequential");
            }
        }

        /// <summary>
        /// CLAIM 3: Iterations within simulation are independent - Order doesn't matter
        /// </summary>
        [TestMethod]
        public void Claim3_IterationIndependence_OrderDoesNotMatter()
        {
            // Arrange
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 40_000),
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.01),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.07, 0.03)),
            ];

            // Act - Run iterations in different orders
            var indices = new[] { 10, 20, 30, 40, 50 };
            
            // Forward order
            var forwardResults = indices
                .Select(i => objectives.RunIteration(i, 1_000_000, 0.6, 0.03, 10))
                .ToList();
            
            // Reverse order
            var reverseResults = indices.Reverse()
                .Select(i => objectives.RunIteration(i, 1_000_000, 0.6, 0.03, 10))
                .Reverse() // Reverse back to match indices
                .ToList();
            
            // Random order then sort by index
            var randomOrder = indices.OrderBy(_ => Guid.NewGuid()).ToList();
            var randomResults = randomOrder
                .Select(i => objectives.RunIteration(i, 1_000_000, 0.6, 0.03, 10))
                .Zip(randomOrder, (result, index) => (result, index))
                .OrderBy(x => x.index)
                .Select(x => x.result)
                .ToList();

            // Assert - All orderings should produce identical results for each iteration
            for (int i = 0; i < indices.Length; i++)
            {
                Assert.AreEqual(forwardResults[i].EndingBalance, reverseResults[i].EndingBalance, 0.01,
                    $"Index {indices[i]}: Forward vs Reverse order differs");
                Assert.AreEqual(forwardResults[i].EndingBalance, randomResults[i].EndingBalance, 0.01,
                    $"Index {indices[i]}: Forward vs Random order differs");
            }
        }

        /// <summary>
        /// BONUS: Verify PredictableHashCode actually produces predictable hashes
        /// </summary>
        [TestMethod]
        public void Bonus_PredictableHashCode_IsPredictable()
        {
            // This is internal, but verifies the concept through RunIteration
            var result1 = RunIterationMultipleTimes(42);
            var result2 = RunIterationMultipleTimes(42);
            var result3 = RunIterationMultipleTimes(123);

            // Same iteration index = same results
            Assert.AreEqual(result1, result2, "Same iteration should be identical");
            
            // Different iteration index = different results
            Assert.AreNotEqual(result1, result3, "Different iteration should differ");
        }

        private static double RunIterationMultipleTimes(int iterationIndex)
        {
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 40_000),
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.01),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.08, 0.04)),
            ];

            return objectives.RunIteration(iterationIndex, 1_000_000, 0.6, 0.03, 10).EndingBalance;
        }
    }
}
