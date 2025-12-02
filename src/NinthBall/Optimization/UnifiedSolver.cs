
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
        /// <param name="progress">Optional progress reporter for tracking evaluation progress.</param>
        /// <returns>Optimization result with Pareto-optimal solutions.</returns>
        public static OptimizationResult Solve(OptimizationProblem problem, IProgress<(int current, int total)>? progress = null)
        {
            problem.Validate();

            // 1. Generate all grid points from step sizes
            var allGridPoints = GenerateGridPointsFromSteps(problem).ToList();
            int total = allGridPoints.Count;
            int current = 0;

            // 2. Evaluate each candidate in parallel with progress tracking
            var evaluatedCandidates = allGridPoints
                .AsParallel()
                .Select(overrides =>
                {
                    var solution = EvaluateCandidate(overrides, problem);
                    
                    Interlocked.Increment(ref current);
                    progress?.Report((current, total));
                    
                    return solution;
                })
                .ToList();

            progress?.Report((current, total));

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
        /// Generates all grid points for the search space using step sizes.
        /// </summary>
        private static IEnumerable<Dictionary<SimVariable, double>> GenerateGridPointsFromSteps(OptimizationProblem problem)
        {
            var variables = problem.SearchVariables.ToList();
            var ranges = problem.SearchRanges;
            var steps = problem.StepSizes;

            // Generate grid values for each variable based on step size
            var gridValues = variables.Select(v =>
            {
                var (min, max) = ranges[v];
                var step = steps[v];
                var values = new List<double>();
                
                for (double value = min; value <= max; value += step)
                {
                    values.Add(value);
                }
                
                // Ensure max is included even if not exactly on step boundary
                if (values.Count == 0 || Math.Abs(values[^1] - max) > 1e-10)
                {
                    values.Add(max);
                }
                
                return values;
            }).ToList();

            // Generate all combinations using recursive approach
            return GenerateCombinations(variables, gridValues, 0, new Dictionary<SimVariable, double>());
        }

        /// <summary>
        /// Recursively generates all combinations of variable values.
        /// </summary>
        private static IEnumerable<Dictionary<SimVariable, double>> GenerateCombinations(List<SimVariable> variables, List<List<double>> gridValues, int depth, Dictionary<SimVariable, double> current)
        {
            if (depth == variables.Count)
            {
                yield return new Dictionary<SimVariable, double>(current);
                yield break;
            }

            var variable = variables[depth];
            foreach (var value in gridValues[depth])
            {
                current[variable] = value;
                foreach (var combination in GenerateCombinations(variables, gridValues, depth + 1, current))
                {
                    yield return combination;
                }
            }
        }

        /// <summary>
        /// Evaluates a single candidate solution.
        /// </summary>
        private static Solution EvaluateCandidate(Dictionary<SimVariable, double> overrides, OptimizationProblem problem)
        {
            var result = problem.Evaluator.Evaluate(overrides);

            // Score the result with all ratings (batch operation)
            var scoredResult = result.WithScores(problem.Ratings);

            // Check for constraint violations (Score.Zero or Score.Unknown)
            bool hasViolation = scoredResult.Scores.Values.Any(score => score <= Score.Zero || score.IsUnknown);

            return new Solution(
                Inputs: overrides,
                Result: scoredResult,  // Result now contains scores
                HasConstraintViolation: hasViolation
            );
        }

        /// <summary>
        /// Computes Pareto front from valid solutions.
        /// Uses absolute scores where higher is always better.
        /// </summary>
        private static List<Solution> ComputeParetoFront(List<Solution> candidates, IReadOnlyList<ISimRating> ratings)
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
                Score aScore = a.Result.Scores[rating.Name];
                Score bScore = b.Result.Scores[rating.Name];

                if (aScore < bScore) return false; // A is worse on this rating
                if (aScore > bScore) betterInAtLeastOne = true;
            }

            return betterInAtLeastOne;
        }
    }

    /// <summary>
    /// Represents a candidate solution with inputs and scored simulation result.
    /// </summary>
    public record Solution(
        IReadOnlyDictionary<SimVariable, double> Inputs,
        SimResult Result,
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
