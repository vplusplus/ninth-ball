using System.Diagnostics;

namespace UnitTests.V2
{
    [TestClass]
    public class CYStatePerformanceTests
    {
        /// <summary>
        /// Simulates 10,000 Monte Carlo iterations with Reset() optimization.
        /// Uses object reuse instead of creating new instances for each year.
        /// </summary>
        [TestMethod]
        public void MonteCarloSimulationPerformance()
        {
            const int iterations = 10_000;
            const int years = 30;
            const int transactionsPerYear = 8; // SS, Ann, 401K, CYExp, PYTax, Fees, Inv, Sav

            // Warmup
            RunSimulations(10, 5);

            var sw = Stopwatch.StartNew();
            RunSimulations(iterations, years);
            sw.Stop();

            long totalOperations = iterations * years * transactionsPerYear;
            double msPerIteration = sw.Elapsed.TotalMilliseconds / iterations;
            double nsPerOperation = sw.Elapsed.TotalMilliseconds * 1_000_000 / totalOperations;

            Console.WriteLine($"Monte Carlo with Reset() Performance:");
            Console.WriteLine($"  Iterations: {iterations:N0}");
            Console.WriteLine($"  Years per iteration: {years}");
            Console.WriteLine($"  Total operations: {totalOperations:N0}");
            Console.WriteLine($"  Total time: {sw.Elapsed.TotalMilliseconds:F2} ms");
            Console.WriteLine($"  Time per iteration: {msPerIteration:F4} ms");
            Console.WriteLine($"  Time per operation: {nsPerOperation:F2} ns");
            Console.WriteLine($"  Operations per second: {totalOperations / sw.Elapsed.TotalSeconds:N0}");
            Console.WriteLine($"  Object allocations: {iterations:N0} (vs {iterations * years:N0} without Reset)");

            // Assert performance is acceptable
            Assert.IsTrue(sw.Elapsed.TotalSeconds < 5.0,
                $"Performance regression: took {sw.Elapsed.TotalSeconds:F2}s (expected < 5s)");

            static void RunSimulations(int iterations, int years)
            {
                for (int i = 0; i < iterations; i++)
                {
                    var state = new CYState(Jan4K: 100000, JanInv: 50000, JanSav: 10000);

                    for (int year = 0; year < years; year++)
                    {
                        // Typical year simulation with all asset types
                        state.AddIncome(In.SS, 2000);
                        state.AddIncome(In.Ann, 1500);
                        state.AddIncome(In.FourK, 3000);

                        state.AddExpense(Exp.CYExp, 4000);
                        state.AddExpense(Exp.PYTax, 800);
                        state.AddExpense(Exp.Fees, 200);

                        state.Deposit(IO.Inv, 1000);
                        state.Withdraw(IO.Sav, 500);

                        // Verify correctness
                        state.EnsureZeroSum();

                        // Reset for next year (reuse same object)
                        if (year < years - 1)
                            state.Reset(jan4K: 100000, janInv: 50000, janSav: 10000);
                    }
                }
            }
        }

    }
}
