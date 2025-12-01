
namespace NinthBall
{
    /// <summary>
    /// Extension methods for scoring SimResult with ratings.
    /// </summary>
    public static class SimResultExtensions
    {
        /// <summary>
        /// Scores the simulation result with the specified ratings.
        /// Returns a new SimResult instance with scores merged into existing scores.
        /// Batch operation - all ratings are evaluated and result is cloned once.
        /// Scores are idempotent - same rating always produces same score for the same result.
        /// </summary>
        /// <param name="result">Simulation result to score.</param>
        /// <param name="ratings">Ratings to apply.</param>
        /// <returns>New SimResult instance with updated scores.</returns>
        public static SimResult WithScores(
            this SimResult result,
            IEnumerable<ISimRating> ratings)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            
            if (ratings == null)
                throw new ArgumentNullException(nameof(ratings));

            // Start with existing scores (merge strategy)
            var merged = new Dictionary<string, Score>(result.Scores);

            // Apply all ratings in batch
            foreach (var rating in ratings)
            {
                merged[rating.Name] = rating.Score(result);  // Overwrite if exists (idempotent)
            }

            // Single clone with updated scores
            return result with { Scores = merged };
        }

        /// <summary>
        /// Scores the simulation result with a single rating.
        /// Convenience method for single rating application.
        /// </summary>
        public static SimResult WithScore(
            this SimResult result,
            ISimRating rating)
        {
            return result.WithScores(new[] { rating });
        }
    }
}
