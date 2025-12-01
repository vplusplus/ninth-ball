
namespace NinthBall
{
    /// <summary>
    /// Configuration for rating strategies loaded from Ratings.yaml.
    /// Each rating can be enabled/disabled and configured with specific parameters.
    /// </summary>
    public record RatingsConfig
    {
        public SurvivalRateConfig? SurvivalRate { get; init; }
        public CapitalRequirementConfig? CapitalRequirement { get; init; }
        public WithdrawalRateConfig? WithdrawalRate { get; init; }
        public MedianBalanceConfig? MedianBalance { get; init; }
        public MeanBalanceConfig? MeanBalance { get; init; }
    }

    /// <summary>
    /// Configuration for survival rate rating.
    /// </summary>
    public record SurvivalRateConfig
    {
        /// <summary>
        /// Whether this rating is active.
        /// </summary>
        public bool Enabled { get; init; } = true;

        /// <summary>
        /// Optional minimum required survival rate (constraint).
        /// </summary>
        public double? MinRequired { get; init; }

        public override string ToString() =>
            Enabled
                ? MinRequired.HasValue
                    ? $"Survival Rate >= {MinRequired.Value:P1} Score"
                    : "Survival Rate Score"
                : "Survival rate rating not activated";
    }

    /// <summary>
    /// Configuration for capital requirement rating.
    /// </summary>
    public record CapitalRequirementConfig
    {
        public bool Enabled { get; init; } = true;

        public override string ToString() =>
            Enabled
                ? "Minimize Starting Capital Score (lower is better)"
                : "Capital requirement rating not activated";
    }

    /// <summary>
    /// Configuration for withdrawal rate rating.
    /// </summary>
    public record WithdrawalRateConfig
    {
        public bool Enabled { get; init; } = true;

        public override string ToString() =>
            Enabled
                ? "Maximize Withdrawal Rate Score (higher is better)"
                : "Withdrawal rate rating not activated";
    }

    /// <summary>
    /// Configuration for median balance rating.
    /// </summary>
    public record MedianBalanceConfig
    {
        public bool Enabled { get; init; } = true;
        public double? MinRequired { get; init; }

        public override string ToString() =>
            Enabled
                ? MinRequired.HasValue
                    ? $"Median Balance >= {MinRequired.Value:C0} Score"
                    : "Median Balance Score"
                : "Median balance rating not activated";
    }

    /// <summary>
    /// Configuration for mean balance rating.
    /// </summary>
    public record MeanBalanceConfig
    {
        public bool Enabled { get; init; } = true;
        public double? MinRequired { get; init; }

        public override string ToString() =>
            Enabled
                ? MinRequired.HasValue
                    ? $"Mean Balance >= {MinRequired.Value:C0} Score"
                    : "Mean Balance Score"
                : "Mean balance rating not activated";
    }
}
