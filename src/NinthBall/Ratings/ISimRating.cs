
namespace NinthBall
{
    /// <summary>
    /// Evaluates simulation quality on a specific criterion.
    /// Returns absolute score [0.0, 1.0] based on domain knowledge:
    ///   - 0.0 = Unacceptable (constraint violation or worst case)
    ///   - 1.0 = Ideal (best possible outcome)
    ///   - Unknown = Not applicable or cannot be measured
    ///   - Values in between represent degrees of acceptability
    /// 
    /// Scores are stable and interpretable, independent of other simulations.
    /// Can be used for quality assessment, reporting, or as input to optimization.
    /// 
    /// DESIGN PRINCIPLE:
    /// ISimRating's job: Measure quality on one dimension.
    /// Must be idempotent - same SimResult always produces same Score.
    /// No weights, no priorities - those belong in optimization layer.
    /// Ratings provide objective measurements; optimization applies preferences.
    /// </summary>
    public interface ISimRating
    {
        /// <summary>
        /// Name of the rating criterion for display and logging.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Scores the simulation result on an absolute scale.
        /// Returns 0.0 (unacceptable) to 1.0 (ideal) based on domain knowledge,
        /// or Score.Unknown if not applicable.
        /// </summary>
        /// <param name="result">Simulation result to score.</param>
        /// <returns>Absolute score where higher is always better.</returns>
        Score Score(SimResult result);
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
        
        public string Name => "CapitalRequirement";
        
        public override string ToString() => _config.ToString();
        
        public Score Score(SimResult result)
        {
            double capital = result.StartingBalance;
            
            // Add buffer if used
            var bufferObj = result.Objectives.OfType<UseBufferCashAfterBadYears>().SingleOrDefault();
            if (bufferObj != null)
                capital += bufferObj.Amount;
            
            // Outside reasonable bounds = unacceptable
            if (capital < MIN_VIABLE || capital > MAX_REASONABLE)
                return NinthBall.Score.Zero;
            
            // Linear scale: lower capital = higher score
            return new Score(1.0 - (capital - MIN_VIABLE) / (MAX_REASONABLE - MIN_VIABLE));
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

        public string Name => "SurvivalRate";
        
        public override string ToString() => _config.ToString();
        
        public Score Score(SimResult result)
        {
            double survivalRate = result.SurvivalRate;
            
            // Constraint violation
            if (_minRequired.HasValue && survivalRate < _minRequired.Value)
                return NinthBall.Score.Zero;
            
            // Already on natural [0, 1] scale
            return new Score(survivalRate);
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

        public string Name => "MedianBalance";
        
        public override string ToString() => _config.ToString();
        
        public Score Score(SimResult result)
        {
            double median = result.Percentile(0.50).EndingBalance;
            
            // Constraint violation
            if (_minRequired.HasValue && median < _minRequired.Value)
                return NinthBall.Score.Zero;
            
            // Scale based on starting balance
            // 0 = depleted, 2x starting = excellent
            double maxReasonable = result.StartingBalance * 2.0;
            double score = Math.Min(median / maxReasonable, 1.0);
            
            return new Score(score);
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
        
        public string Name => "WithdrawalRate";
        
        public override string ToString() => _config.ToString();
        
        public Score Score(SimResult result)
        {
            var withdrawalObj = result.Objectives.OfType<PCTWithdrawalObjective>().SingleOrDefault();
            
            if (withdrawalObj == null)
                return NinthBall.Score.Unknown; // Not applicable
            
            double rate = withdrawalObj.FirstYearPct;
            
            // Outside reasonable bounds
            if (rate < MIN_CONSERVATIVE || rate > MAX_AGGRESSIVE)
                return NinthBall.Score.Zero;
            
            // Linear scale: higher rate = higher score
            return new Score((rate - MIN_CONSERVATIVE) / (MAX_AGGRESSIVE - MIN_CONSERVATIVE));
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

        public string Name => "MeanBalance";
        
        public override string ToString() => _config.ToString();
        
        public Score Score(SimResult result)
        {
            double mean = result.Iterations.Average(i => i.EndingBalance);
            
            // Constraint violation
            if (_minRequired.HasValue && mean < _minRequired.Value)
                return NinthBall.Score.Zero;
            
            // Scale based on starting balance
            double maxReasonable = result.StartingBalance * 2.0;
            double score = Math.Min(mean / maxReasonable, 1.0);
            
            return new Score(score);
        }
    }
}
