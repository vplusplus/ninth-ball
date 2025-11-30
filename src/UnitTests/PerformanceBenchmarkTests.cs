
using System.Diagnostics;
using NinthBall;

namespace UnitTests
{
    [TestClass]
    public sealed class PerformanceBenchmarkTests
    {
        [TestMethod]
        public void Benchmark_10000_Iterations_Speed()
        {
            // Arrange
            const int Iterations = 10_000;
            const int Years = 30;
            
            // Use FxSimObjective to avoid disk I/O (isolate calculation speed)
            ISimObjective[] objectives = 
            [
                FxSimObjective.Create(ctx => ctx.PlannedWithdrawalAmount = 40_000),
                FxSimObjective.Create(ctx => ctx.Fees = ctx.JanBalance * 0.01),
                FxSimObjective.Create(ctx => ctx.ROI = new YROI(2020, 0.07, 0.03)),
            ];

            // Warmup (JIT)
            objectives.RunIteration(0, 1_000_000, 0.6, 0.03, Years);

            // Act
            var sw = Stopwatch.StartNew();
            
            // Run the actual simulation method which is now parallelized
            var result = objectives.RunSimulation(
                initialBalance: 1_000_000, 
                initialAllocation: 0.6, 
                initialMaxDrift: 0.03, 
                numYears: Years, 
                numIterations: Iterations
            );
            
            sw.Stop();
            
            // Log result
            Console.WriteLine($"Time for {Iterations:N0} iterations: {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"Avg per iteration: {sw.Elapsed.TotalMilliseconds / Iterations:F4}ms");
            
            // Assert - Verify it's within expected range (e.g., < 1 second)
            // Note: This might fail on slow CI machines, but gives us the data we need
            Assert.IsTrue(sw.ElapsedMilliseconds < 1000, 
                $"Performance too slow: {sw.ElapsedMilliseconds}ms (Expected < 1000ms)");
        }
    }
}
