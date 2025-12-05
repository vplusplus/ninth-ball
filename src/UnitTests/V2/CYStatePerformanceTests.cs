using System.Diagnostics;

namespace UnitTests.V2
{
    [TestClass]
    public class CYStatePerformanceTests
    {
        /// <summary>
        /// Simulates 10,000 Monte Carlo iterations, each with 30 years of transactions.
        /// This represents the actual workload expected in retirement simulation.
        /// </summary>
        [TestMethod]
        public void MonteCarloSimulationPerformance()
        {
            const int iterations = 10_000;
            const int years = 30;
            const int transactionsPerYear = 8; // SS, Ann, 401K, CYExp, PYTax, Fees, Inv, Sav

            var sw = Stopwatch.StartNew();
            
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
                    
                    // Verify correctness (minimal overhead, but ensures code isn't optimized away)
                    state.EnsureZeroSum();
                }
            }
            
            sw.Stop();
            
            long totalOperations = iterations * years * transactionsPerYear;
            double msPerIteration = sw.Elapsed.TotalMilliseconds / iterations;
            double nsPerOperation = sw.Elapsed.TotalMilliseconds * 1_000_000 / totalOperations;
            
            Console.WriteLine($"Monte Carlo Simulation Performance:");
            Console.WriteLine($"  Iterations: {iterations:N0}");
            Console.WriteLine($"  Years per iteration: {years}");
            Console.WriteLine($"  Total operations: {totalOperations:N0}");
            Console.WriteLine($"  Total time: {sw.Elapsed.TotalMilliseconds:F2} ms");
            Console.WriteLine($"  Time per iteration: {msPerIteration:F4} ms");
            Console.WriteLine($"  Time per operation: {nsPerOperation:F2} ns");
            Console.WriteLine($"  Operations per second: {totalOperations / sw.Elapsed.TotalSeconds:N0}");
            
            // Assert performance is acceptable (should complete in < 5 seconds)
            Assert.IsTrue(sw.Elapsed.TotalSeconds < 5.0, 
                $"Performance regression: took {sw.Elapsed.TotalSeconds:F2}s (expected < 5s)");
        }

        /// <summary>
        /// Microbenchmark: Direct dictionary access vs method overhead
        /// </summary>
        [TestMethod]
        public void DictionaryAccessMicrobenchmark()
        {
            const int iterations = 10_000_000;
            var state = new CYState(Jan4K: 100000, JanInv: 50000, JanSav: 10000);
            
            // Warm up
            for (int i = 0; i < 1000; i++)
            {
                state.AddIncome(In.SS, 1);
                _ = state.SS;
            }
            
            // Benchmark: Write operations
            var sw1 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                state.AddIncome(In.SS, 1);
                state.AddIncome(In.Ann, 1);
                state.AddIncome(In.FourK, 1);
            }
            sw1.Stop();
            
            // Benchmark: Read operations
            var sw2 = Stopwatch.StartNew();
            double sum = 0;
            for (int i = 0; i < iterations; i++)
            {
                sum += state.SS;
                sum += state.ANN;
                sum += state.FourK;
            }
            sw2.Stop();
            
            double writeNsPerOp = sw1.Elapsed.TotalMilliseconds * 1_000_000 / (iterations * 3);
            double readNsPerOp = sw2.Elapsed.TotalMilliseconds * 1_000_000 / (iterations * 3);
            
            Console.WriteLine($"Dictionary Access Microbenchmark:");
            Console.WriteLine($"  Write time: {sw1.Elapsed.TotalMilliseconds:F2} ms ({writeNsPerOp:F2} ns/op)");
            Console.WriteLine($"  Read time: {sw2.Elapsed.TotalMilliseconds:F2} ms ({readNsPerOp:F2} ns/op)");
            Console.WriteLine($"  Dummy sum (prevent optimization): {sum}");
        }

        /// <summary>
        /// Comparison: Dictionary vs hypothetical direct field access
        /// This provides a theoretical performance ceiling
        /// </summary>
        [TestMethod]
        public void CompareWithDirectFieldAccess()
        {
            const int iterations = 1_000_000;
            
            // Test current dictionary-based approach
            var stateCurrent = new CYState(0, 0, 0);
            var sw1 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                stateCurrent.AddIncome(In.SS, 100);
                stateCurrent.AddExpense(Exp.CYExp, 50);
                _ = stateCurrent.SS + stateCurrent.CYExp;
            }
            sw1.Stop();
            
            // Test hypothetical direct field approach (simulated with simple class)
            var stateDirect = new DirectFieldState();
            var sw2 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                stateDirect.AddIncome(100);
                stateDirect.AddExpense(50);
                _ = stateDirect.SS + stateDirect.CYExp;
            }
            sw2.Stop();
            
            double currentMs = sw1.Elapsed.TotalMilliseconds;
            double directMs = sw2.Elapsed.TotalMilliseconds;
            double overhead = ((currentMs - directMs) / directMs) * 100;
            
            Console.WriteLine($"Dictionary vs Direct Field Comparison:");
            Console.WriteLine($"  Dictionary approach: {currentMs:F2} ms");
            Console.WriteLine($"  Direct field approach: {directMs:F2} ms");
            Console.WriteLine($"  Overhead: {overhead:F1}%");
            Console.WriteLine($"  Absolute difference: {currentMs - directMs:F2} ms for {iterations:N0} operations");
        }
        
        // Simple direct-field implementation for comparison
        private class DirectFieldState
        {
            public double SS { get; private set; }
            public double CYExp { get; private set; }
            public double CashInHand { get; private set; }
            
            public void AddIncome(double amount)
            {
                SS += amount;
                CashInHand += amount;
            }
            
            public void AddExpense(double amount)
            {
                CYExp -= amount;
                CashInHand -= amount;
            }
        }
    }
}
