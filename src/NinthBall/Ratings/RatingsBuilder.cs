
namespace NinthBall
{
    /// <summary>
    /// Factory to create ISimRating instances from RatingsConfig.
    /// Pattern matches SimBuilder - uses config to instantiate ratings.
    /// </summary>
    public static class RatingsBuilder
    {
        /// <summary>
        /// Creates list of enabled ratings from configuration.
        /// </summary>
        public static IReadOnlyList<ISimRating> CreateRatings(RatingsConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);

            return new List<ISimRating?>
            {
                RatingOrNull(config.SurvivalRate, c => new SurvivalRateRating(c)),
                RatingOrNull(config.CapitalRequirement, c => new CapitalRequirementRating(c)),
                RatingOrNull(config.WithdrawalRate, c => new WithdrawalRateRating(c)),
                RatingOrNull(config.MedianBalance, c => new MedianBalanceRating(c)),
                RatingOrNull(config.MeanBalance, c => new MeanBalanceRating(c)),
            }
            .Where(r => r != null)
            .ToList()
            .AsReadOnly()!;
        }

        /// <summary>
        /// Returns rating instance if config is not null and enabled.
        /// Returns null otherwise.
        /// </summary>
        private static ISimRating? RatingOrNull<T>(T? config, Func<T, ISimRating> factory)
            where T : class
        {
            if (config == null) return null;

            // Check if config has Enabled property and it's false
            var enabledProp = typeof(T).GetProperty("Enabled");
            if (enabledProp != null)
            {
                var enabled = (bool?)enabledProp.GetValue(config);
                if (enabled == false) return null;
            }

            return factory(config);
        }
    }
}
