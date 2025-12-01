
namespace NinthBall
{
    /// <summary>
    /// Configuration for what-if optimization loaded from WhatIf.yaml.
    /// Defines the optimization problem: which variables to vary and how.
    /// </summary>
    public record WhatIfConfig
    {
        /// <summary>
        /// Optimization strategy to use (currently only GridSearch supported).
        /// </summary>
        public string Strategy { get; init; } = "GridSearch";

        /// <summary>
        /// Variables to optimize and their ranges.
        /// Key is SimVariable name (e.g., "StartingBalance").
        /// </summary>
        public Dictionary<string, VariableConfig> Variables { get; init; } = new();

        /// <summary>
        /// Optional preferences for solution selection.
        /// </summary>
        public PreferencesConfig? Preferences { get; init; }
    }

    /// <summary>
    /// Configuration for a single optimization variable.
    /// </summary>
    public record VariableConfig
    {
        /// <summary>
        /// Minimum value for this variable.
        /// </summary>
        public double Min { get; init; }

        /// <summary>
        /// Maximum value for this variable.
        /// </summary>
        public double Max { get; init; }

        /// <summary>
        /// Step size for grid search.
        /// If not specified, defaults to (Max-Min)/10.
        /// </summary>
        public double? Step { get; init; }

        /// <summary>
        /// Gets the effective step size (uses default if not specified).
        /// </summary>
        public double GetEffectiveStep() => Step ?? (Max - Min) / 10.0;

        public override string ToString() =>
            $"{Min:F0} to {Max:F0}, step {GetEffectiveStep():F0}";
    }

    /// <summary>
    /// Optional preferences for optimization.
    /// </summary>
    public record PreferencesConfig
    {
        /// <summary>
        /// Priority order of ratings (most important first).
        /// Used for tie-breaking or weighted selection.
        /// </summary>
        public List<string>? Priority { get; init; }
    }
}
