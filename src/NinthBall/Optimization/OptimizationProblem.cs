
namespace NinthBall
{
    /// <summary>
    /// Defines a complete optimization problem with search space and ratings.
    /// </summary>
    public record OptimizationProblem(
        SimulationEvaluator Evaluator,
        IReadOnlyList<SimVariable> SearchVariables,
        IReadOnlyDictionary<SimVariable, (double min, double max)> SearchRanges,
        IReadOnlyDictionary<SimVariable, double> StepSizes,
        IReadOnlyList<ISimRating> Ratings
    )
    {
        /// <summary>
        /// True if this is a single-rating optimization problem.
        /// </summary>
        public bool IsSingleRating => Ratings.Count == 1;

        /// <summary>
        /// True if this is a multi-rating optimization problem.
        /// </summary>
        public bool IsMultiRating => Ratings.Count > 1;

        /// <summary>
        /// Validates the problem definition.
        /// </summary>
        public void Validate()
        {
            if (Evaluator == null)
                throw new InvalidOperationException("Evaluator cannot be null");

            if (SearchVariables == null || SearchVariables.Count == 0)
                throw new InvalidOperationException("SearchVariables cannot be null or empty");

            if (SearchRanges == null || SearchRanges.Count == 0)
                throw new InvalidOperationException("SearchRanges cannot be null or empty");

            if (StepSizes == null || StepSizes.Count == 0)
                throw new InvalidOperationException("StepSizes cannot be null or empty");

            if (Ratings == null || Ratings.Count == 0)
                throw new InvalidOperationException("Ratings cannot be null or empty");

            // Ensure all search variables have ranges and steps
            foreach (var variable in SearchVariables)
            {
                if (!SearchRanges.ContainsKey(variable))
                    throw new InvalidOperationException($"Search range not defined for variable: {variable}");
                
                if (!StepSizes.ContainsKey(variable))
                    throw new InvalidOperationException($"Step size not defined for variable: {variable}");
            }
        }
    }
}
