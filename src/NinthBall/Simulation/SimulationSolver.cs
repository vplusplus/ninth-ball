using System.Collections.Concurrent;

namespace NinthBall
{
    /// <summary>
    /// Inverse optimization: Find inputs that achieve target survival rates.
    /// Uses simple Grid Search algorithm for comprehensibility and robustness.
    /// </summary>
    public static class SimulationSolver
    {
        #region Single Variable Solvers

        /// <summary>
        /// Find initial balance needed for target survival rate.
        /// </summary>
        public static SolverResult FindInitialBalance(
            IReadOnlyList<ISimObjective> objectives,
            double targetSurvivalRate,
            double stockAllocationPct,
            double maxDrift,
            int years,
            int iterations,
            double minBalance = 100_000,
            double maxBalance = 10_000_000,
            double tolerance = 1_000)
        {
            // Predicate: Does this balance achieve the target survival rate?
            bool IsSufficient(double balance)
            {
                var result = objectives.RunSimulation(
                    balance, stockAllocationPct, maxDrift, years, iterations);
                return result.SurvivalRate >= targetSurvivalRate;
            }

            // We want the MINIMUM balance that is sufficient
            double optimal = FindMinInput(
                IsSufficient, minBalance, maxBalance, tolerance);

            // Run final verification
            var finalResult = objectives.RunSimulation(
                optimal, stockAllocationPct, maxDrift, years, iterations);

            return new SolverResult(
                FoundValue: optimal,
                AchievedRate: finalResult.SurvivalRate,
                TargetRate: targetSurvivalRate,
                SimulationsRun: (int)Math.Log2((maxBalance - minBalance) / tolerance) + 1
            );
        }

        /// <summary>
        /// Find withdrawal percentage for target survival rate.
        /// </summary>
        public static SolverResult FindWithdrawalRate(
            Func<double, IReadOnlyList<ISimObjective>> createObjectives,
            double initialBalance,
            double targetSurvivalRate,
            double stockAllocationPct,
            double maxDrift,
            int years,
            int iterations,
            double minRate = 0.01,
            double maxRate = 0.10,
            double tolerance = 0.001)
        {
            // Predicate: Does this rate achieve the target survival rate?
            bool IsSafe(double rate)
            {
                var objectives = createObjectives(rate);
                var result = objectives.RunSimulation(
                    initialBalance, stockAllocationPct, maxDrift, years, iterations);
                return result.SurvivalRate >= targetSurvivalRate;
            }

            // We want the MAXIMUM rate that is safe
            double optimal = FindMaxInput(
                IsSafe, minRate, maxRate, tolerance);

            // Run final verification
            var finalObjectives = createObjectives(optimal);
            var finalResult = finalObjectives.RunSimulation(
                initialBalance, stockAllocationPct, maxDrift, years, iterations);

            return new SolverResult(
                FoundValue: optimal,
                AchievedRate: finalResult.SurvivalRate,
                TargetRate: targetSurvivalRate,
                SimulationsRun: (int)Math.Log2((maxRate - minRate) / tolerance) + 1
            );
        }

        #endregion

        #region Dual Variable Solvers

        /// <summary>
        /// Find optimal (initial balance, buffer cash) combination.
        /// Minimizes total capital while achieving target survival.
        /// </summary>
        public static DualSolverResult FindOptimalBalanceAndBuffer(
            Func<double, double, IReadOnlyList<ISimObjective>> createObjectives,
            double targetSurvivalRate,
            double stockAllocationPct,
            double maxDrift,
            int years,
            int iterations,
            (double min, double max) balanceRange,
            (double min, double max) bufferRange,
            int gridSize = 10)
        {
            // Cache for deterministic simulations
            var cache = new ConcurrentDictionary<string, double>();

            double Evaluate(double balance, double buffer)
            {
                string key = $"{balance:F2}_{buffer:F2}";
                return cache.GetOrAdd(key, _ =>
                {
                    var objectives = createObjectives(balance, buffer);
                    var result = objectives.RunSimulation(
                        balance, stockAllocationPct, maxDrift, years, iterations);
                    return result.SurvivalRate;
                });
            }

            // Cost function: Minimize total capital (balance + buffer)
            double CostFunction(double balance, double buffer)
            {
                double rate = Evaluate(balance, buffer);
                return rate >= targetSurvivalRate 
                    ? balance + buffer 
                    : double.MaxValue;
            }

            var (balance, buffer) = GridSearch(
                CostFunction, balanceRange, bufferRange, gridSize);

            return new DualSolverResult(
                Variable1: balance,
                Variable2: buffer,
                AchievedRate: Evaluate(balance, buffer),
                TargetRate: targetSurvivalRate,
                TotalCost: balance + buffer
            );
        }

        #endregion

        #region Core Algorithms

        // "Find Smallest Input satisfying Predicate"
        public static double FindMinInput(
            Func<double, bool> predicate, // Returns true if input satisfies condition
            double min,
            double max,
            double tolerance)
        {
            double best = max;
            if (!predicate(max)) return max; // Even max doesn't work? Return max (best effort)

            // Check if min works (optimization)
            if (predicate(min)) return min;

            while (max - min > tolerance)
            {
                double mid = (min + max) / 2;
                if (predicate(mid))
                {
                    best = mid;
                    max = mid; // Try smaller
                }
                else
                {
                    min = mid; // Need larger
                }
            }
            return best;
        }

        // "Find Largest Input satisfying Predicate"
        public static double FindMaxInput(
            Func<double, bool> predicate,
            double min,
            double max,
            double tolerance)
        {
            double best = min;
            if (!predicate(min)) return min; // Even min doesn't work? Return min

            // Check if max works (optimization)
            if (predicate(max)) return max;

            while (max - min > tolerance)
            {
                double mid = (min + max) / 2;
                if (predicate(mid))
                {
                    best = mid;
                    min = mid; // Try larger
                }
                else
                {
                    max = mid; // Need smaller
                }
            }
            return best;
        }

        /// <summary>
        /// Grid search for 2 variables.
        /// </summary>
        public static (double v1, double v2) GridSearch(
            Func<double, double, double> costFunction,
            (double min, double max) range1,
            (double min, double max) range2,
            int gridSize)
        {
            var candidates = new ConcurrentBag<(double v1, double v2, double cost)>();
            
            double step1 = (range1.max - range1.min) / gridSize;
            double step2 = (range2.max - range2.min) / gridSize;
            
            // Parallelize the outer loop for speed
            Parallel.For(0, gridSize + 1, i =>
            {
                double v1 = range1.min + i * step1;
                for (int j = 0; j <= gridSize; j++)
                {
                    double v2 = range2.min + j * step2;
                    double cost = costFunction(v1, v2);
                    
                    if (cost < double.MaxValue)
                    {
                        candidates.Add((v1, v2, cost));
                    }
                }
            });
            
            if (candidates.IsEmpty)
                throw new InvalidOperationException(
                    "No combination achieves target. Expand search range.");
            
            // Find minimum cost
            var best = candidates.OrderBy(c => c.cost).First();
            
            // Optional: Refine around best (Phase 2)
            // For now, return grid best
            return (best.v1, best.v2);
        }
        
        #endregion
    }
    
    /// <summary>Result of single variable optimization.</summary>
    public record SolverResult(
        double FoundValue,
        double AchievedRate,
        double TargetRate,
        int SimulationsRun);
    
    /// <summary>Result of dual variable optimization.</summary>
    public record DualSolverResult(
        double Variable1,
        double Variable2,
        double AchievedRate,
        double TargetRate,
        double TotalCost);
}
