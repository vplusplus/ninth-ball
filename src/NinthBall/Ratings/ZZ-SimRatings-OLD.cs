

/*

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

    /// <summary>
    /// Rates total capital required (starting balance + buffer).
    /// Domain bounds: $100k (minimum viable) to $5M (wastefully high).
    /// </summary>
    public class CapitalRequirementRating : ISimRating
    {
        private const double MIN_VIABLE = 100_000;      // Can't sustain on less
        private const double MAX_REASONABLE = 5_000_000; // Beyond this is wasteful
        private readonly CapitalRequirementConfig _config;
        
        public CapitalRequirementRating(CapitalRequirementConfig config)
        {
            _config = config ?? new CapitalRequirementConfig();
        }
        
        public string Name => "CapitalRequirementScore";
        
        public override string ToString() => _config.ToString();
        
        public SimScore10 Score(SimResult result)
        {
            double capital = result.InitialBalance;
            
            // Add buffer if used
            var bufferObj = result.Objectives.OfType<UseBufferCashAfterBadYears>().SingleOrDefault();
            if (bufferObj != null)
                capital += bufferObj.Amount;
            
            // Outside reasonable bounds = unacceptable
            if (capital < MIN_VIABLE || capital > MAX_REASONABLE)
                return NinthBall.SimScore10.Zero;
            
            // Linear scale: lower capital = higher score (1-10)
            // $100k = score 10, $5M = score 1
            double normalized = 1.0 - (capital - MIN_VIABLE) / (MAX_REASONABLE - MIN_VIABLE);
            return new SimScore10(1.0 + (normalized * 9.0));
        }
    }

    /// <summary>
    /// Rates survival rate (or enforces minimum as constraint).
    /// Natural absolute scale: 0% survival = 0.0, 100% survival = 1.0.
    /// </summary>
    public class SurvivalRateRating : ISimRating
    {
        private readonly double? _minRequired;
        private readonly SurvivalRateConfig _config;

        /// <summary>
        /// Creates rating for survival rate.
        /// </summary>
        /// <param name="config">Configuration with minRequired constraint.</param>
        public SurvivalRateRating(SurvivalRateConfig config)
        {
            _config = config ?? new SurvivalRateConfig();
            _minRequired = config?.MinRequired;
        }

        public string Name => "SurvivalRateScore";
        
        public override string ToString() => _config.ToString();
        
        public SimScore10 Score(SimResult result)
        {
            double survivalRate = result.SurvivalRate;
            
            // Constraint violation
            if (_minRequired.HasValue && survivalRate < _minRequired.Value)
                return NinthBall.SimScore10.Zero;
            
            // Map [0.0-1.0] survival rate to [1-10] score
            return new SimScore10(1.0 + (survivalRate * 9.0));
        }
    }

    /// <summary>
    /// Rates median ending balance (or enforces minimum as constraint).
    /// Domain bounds: $0 (depleted) to starting balance * 2 (excellent growth).
    /// </summary>
    public class MedianBalanceRating : ISimRating
    {
        private readonly double? _minRequired;
        private readonly MedianBalanceConfig _config;

        public MedianBalanceRating(MedianBalanceConfig config)
        {
            _config = config ?? new MedianBalanceConfig();
            _minRequired = config?.MinRequired;
        }

        public string Name => "MedianBalanceScore";
        
        public override string ToString() => _config.ToString();
        
        public SimScore10 Score(SimResult result)
        {
            double median = result.Percentile(0.50).EndingBalance;
            
            // Constraint violation
            if (_minRequired.HasValue && median < _minRequired.Value)
                return NinthBall.SimScore10.Zero;
            
            // Scale based on starting balance
            // 0 = depleted (score 1), 2x starting = excellent (score 10)
            double maxReasonable = result.InitialBalance * 2.0;
            double normalized = Math.Min(median / maxReasonable, 1.0);
            
            return new SimScore10(1.0 + (normalized * 9.0));
        }
    }

    /// <summary>
    /// Rates withdrawal rate by extracting from simulation objectives.
    /// Domain bounds: 2% (very conservative) to 6% (aggressive).
    /// </summary>
    public class WithdrawalRateRating : ISimRating
    {
        private const double MIN_CONSERVATIVE = 0.02; // 2% is very safe
        private const double MAX_AGGRESSIVE = 0.06;   // 6% is risky
        private readonly WithdrawalRateConfig _config;
        
        public WithdrawalRateRating(WithdrawalRateConfig config)
        {
            _config = config ?? new WithdrawalRateConfig();
        }
        
        public string Name => "WithdrawalRateScore";
        
        public override string ToString() => _config.ToString();
        
        public SimScore10 Score(SimResult result)
        {
            var withdrawalObj = result.Objectives.OfType<PCTWithdrawalObjective>().SingleOrDefault();
            
            if (withdrawalObj == null)
                return NinthBall.SimScore10.Unknown; // Not applicable
            
            double rate = withdrawalObj.FirstYearPct;
            
            // Outside reasonable bounds
            if (rate < MIN_CONSERVATIVE || rate > MAX_AGGRESSIVE)
                return NinthBall.SimScore10.Zero;
            
            // Linear scale: higher rate = higher score (1-10)
            // 2% = score 1, 6% = score 10
            double normalized = (rate - MIN_CONSERVATIVE) / (MAX_AGGRESSIVE - MIN_CONSERVATIVE);
            return new SimScore10(1.0 + (normalized * 9.0));
        }
    }

    /// <summary>
    /// Rates mean ending balance.
    /// Domain bounds: $0 (depleted) to starting balance * 2 (excellent).
    /// </summary>
    public class MeanBalanceRating : ISimRating
    {
        private readonly double? _minRequired;
        private readonly MeanBalanceConfig _config;

        public MeanBalanceRating(MeanBalanceConfig config)
        {
            _config = config ?? new MeanBalanceConfig();
            _minRequired = config?.MinRequired;
        }

        public string Name => "MeanBalanceScore";
        
        public override string ToString() => _config.ToString();
        
        public SimScore10 Score(SimResult result)
        {
            double mean = result.Iterations.Average(i => i.EndingBalance);
            
            // Constraint violation
            if (_minRequired.HasValue && mean < _minRequired.Value)
                return NinthBall.SimScore10.Zero;
            
            // Scale based on starting balance (1-10)
            double maxReasonable = result.InitialBalance * 2.0;
            double normalized = Math.Min(mean / maxReasonable, 1.0);
            
            return new SimScore10(1.0 + (normalized * 9.0));
        }
    }

*/
