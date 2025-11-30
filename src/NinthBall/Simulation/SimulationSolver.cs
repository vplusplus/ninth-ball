using System.Collections.Concurrent;

namespace NinthBall
{
    /// <summary>
    /// DEPRECATED: This class contains legacy solver methods.
    /// Use UnifiedSolver in the Optimization namespace for new code.
    /// 
    /// Utility methods (FindMinInput, FindMaxInput) are kept for potential use in refinement phases.
    /// </summary>
    [Obsolete("Use UnifiedSolver for optimization. This class is kept only for utility methods.")]
    public static class SimulationSolver
    {
        #region Core Search Utilities

        /// <summary>
        /// Binary search to find the smallest input value that satisfies a predicate.
        /// </summary>
        /// <param name="predicate">Returns true if input satisfies condition.</param>
        /// <param name="min">Minimum search bound.</param>
        /// <param name="max">Maximum search bound.</param>
        /// <param name="tolerance">Tolerance for convergence.</param>
        /// <returns>Smallest input value satisfying the predicate.</returns>
        public static double FindMinInput(
            Func<double, bool> predicate,
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

        /// <summary>
        /// Binary search to find the largest input value that satisfies a predicate.
        /// </summary>
        /// <param name="predicate">Returns true if input satisfies condition.</param>
        /// <param name="min">Minimum search bound.</param>
        /// <param name="max">Maximum search bound.</param>
        /// <param name="tolerance">Tolerance for convergence.</param>
        /// <returns>Largest input value satisfying the predicate.</returns>
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
        /// Note: Consider using UnifiedSolver for multi-variable optimization.
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
}
