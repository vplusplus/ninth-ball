
namespace NinthBall
{
    /// <summary>
    /// Unified optimization solver supporting both single and multi-rating optimization.
    /// Uses Pareto dominance logic and grid search with absolute scoring.
    /// </summary>
    public static class UnifiedSolver
    {
        /// <summary>
        /// Solves the optimization problem using grid search and Pareto dominance.
        /// </summary>
        /// <param name="problem">Optimization problem to solve.</param>
        /// <param name="gridPointsPerVariable">Number of grid points per variable (default: 10).</param>
        /// <returns>Optimization result with Pareto-optimal solutions.</returns>
        public static OptimizationResult Solve(OptimizationProblem problem, int gridPointsPerVariable = 10)
        {
            problem.Validate();

            if (gridPointsPerVariable < 2)
                throw new ArgumentException("Grid size must be at least 2", nameof(gridPointsPerVariable));

            // 1. Generate all grid points
            var allGridPoints = GenerateGridPoints(problem, gridPointsPerVariable).ToList();

            // 2. Evaluate each candidate in parallel
            var evaluatedCandidates = allGridPoints
                .AsParallel()
                .Select(overrides => EvaluateCandidate(overrides, problem))
                .ToList();

            // 3. Filter valid solutions (no constraint violations)
            var validSolutions = evaluatedCandidates
                .Where(c => !c.HasConstraintViolation)
                .ToList();

            if (!validSolutions.Any())
                throw new InvalidOperationException(
                    "No valid solutions found. All candidates violate constraints. " +
                    "Consider expanding search ranges or relaxing constraint ratings.");

            // 4. Compute Pareto front (scores are already absolute, no normalization needed)
            var paretoFront = ComputeParetoFront(validSolutions, problem.Ratings);

            // 5. Return results
            return new OptimizationResult(
                ParetoFront: paretoFront,
                AllValidSolutions: validSolutions,
                Problem: problem,
                TotalEvaluations: allGridPoints.Count
            );
        }

        /// <summary>
        /// Generates all grid points for the search space.
        /// </summary>
        private static IEnumerable<Dictionary<SimVariable, double>> GenerateGridPoints(
            OptimizationProblem problem,
            int gridPointsPerVariable)
        {
            var variables = problem.SearchVariables.ToList();
            var ranges = problem.SearchRanges;

            // Generate grid steps for each variable
            var gridSteps = variables.Select(v =>
            {
                var (min, max) = ranges[v];
                var step = (max - min) / (gridPointsPerVariable - 1);
                return Enumerable.Range(0, gridPointsPerVariable)
                    .Select(i => min + i * step)
                    .ToList();
            }).ToList();

            // Generate all combinations using recursive approach
            return GenerateCombinations(variables, gridSteps, 0, new Dictionary<SimVariable, double>());
        }

        /// <summary>
        /// Recursively generates all combinations of variable values.
        /// </summary>
        private static IEnumerable<Dictionary<SimVariable, double>> GenerateCombinations(
            List<SimVariable> variables,
            List<List<double>> gridSteps,
            int depth,
            Dictionary<SimVariable, double> current)
        {
            if (depth == variables.Count)
            {
                yield return new Dictionary<SimVariable, double>(current);
                yield break;
            }

            var variable = variables[depth];
            foreach (var value in gridSteps[depth])
            {
                current[variable] = value;
                foreach (var combination in GenerateCombinations(variables, gridSteps, depth + 1, current))
                {
                    yield return combination;
                }
            }
        }

        /// <summary>
        /// Evaluates a single candidate solution.
        /// </summary>
        private static Solution EvaluateCandidate(
            Dictionary<SimVariable, double> overrides,
            OptimizationProblem problem)
        {
            var result = problem.Evaluator.Evaluate(overrides);

            // Compute absolute rating scores
            var scores = new Dictionary<string, double>();
            bool hasViolation = false;

            foreach (var rating in problem.Ratings)
            {
                double score = rating.Score(result);
                scores[rating.Name] = score;

                // Score of 0.0 indicates constraint violation
                if (score <= 0.0)
                    hasViolation = true;
            }

            return new Solution(
                Inputs: overrides,
                Result: result,
                Scores: scores,
                HasConstraintViolation: hasViolation
            );
        }

        /// <summary>
        /// Computes Pareto front from valid solutions.
        /// Uses absolute scores where higher is always better.
        /// </summary>
        private static List<Solution> ComputeParetoFront(
            List<Solution> candidates,
            IReadOnlyList<ISimRating> ratings)
        {
            var nonDominated = new List<Solution>();

            foreach (var candidate in candidates)
            {
                bool isDominated = candidates.Any(other =>
                    other != candidate && Dominates(other, candidate, ratings));

                if (!isDominated)
                    nonDominated.Add(candidate);
            }

            return nonDominated;
        }

        /// <summary>
        /// Checks if solution A dominates solution B (Pareto dominance).
        /// A dominates B if A is >= B on all ratings and > B on at least one.
        /// Scores are absolute [0-1] where higher is always better.
        /// </summary>
        private static bool Dominates(Solution a, Solution b, IReadOnlyList<ISimRating> ratings)
        {
            bool betterInAtLeastOne = false;

            foreach (var rating in ratings)
            {
                double aScore = a.Scores[rating.Name];
                double bScore = b.Scores[rating.Name];

                if (aScore < bScore) return false; // A is worse on this rating
                if (aScore > bScore) betterInAtLeastOne = true;
            }

            return betterInAtLeastOne;
        }
    }

    /// <summary>
    /// Represents a candidate solution with inputs, results, and absolute rating scores.
    /// </summary>
    public record Solution(
        IReadOnlyDictionary<SimVariable, double> Inputs,
        SimResult Result,
        IReadOnlyDictionary<string, double> Scores,          // Absolute scores [0-1]
        bool HasConstraintViolation
    );

    /// <summary>
    /// Result of an optimization run.
    /// </summary>
    public record OptimizationResult(
        IReadOnlyList<Solution> ParetoFront,
        IReadOnlyList<Solution> AllValidSolutions,
        OptimizationProblem Problem,
        int TotalEvaluations
    )
    {
        /// <summary>
        /// Gets the best solution for single-rating problems.
        /// Throws exception for multi-rating problems.
        /// </summary>
        public Solution BestSolution
        {
            get
            {
                if (Problem.IsMultiRating)
                    throw new InvalidOperationException(
                        $"Multiple Pareto-optimal solutions ({ParetoFront.Count}). Use ParetoFront property for multi-rating problems.");

                if (ParetoFront.Count == 0)
                    throw new InvalidOperationException("No solutions found.");

                return ParetoFront[0];
            }
        }
    }
}
