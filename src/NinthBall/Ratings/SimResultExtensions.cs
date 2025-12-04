
namespace NinthBall
{
    /// <summary>
    /// Extension methods for scoring SimResult with ratings.
    /// </summary>
    public static partial class SimResultExtensions
    {
        /// <summary>
        /// Scores the simulation simResult with the specified ratings.
        /// </summary>
        public static SimResult WithScores(this SimResult simResult, IEnumerable<ISimRating> ratings)
        {
            ArgumentNullException.ThrowIfNull(simResult);
            ArgumentNullException.ThrowIfNull(ratings);

            // Start with existing scores (merge strategy)
            var merged = new Dictionary<string, SimScore10>(simResult.Scores);

            // Rate the simulation simResult.
            // Overwrite if exists (idempotent)
            foreach (var rating in ratings) merged[rating.Name] = rating.Score(simResult);  

            // The rated simResult.
            return simResult with { Scores = merged };
        }
    }
}
