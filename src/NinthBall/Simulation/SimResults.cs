
namespace NinthBall
{
    /// <summary>
    /// Represents a rating score on an absolute scale from 0.0 (unacceptable) to 1.0 (ideal).
    /// Enforces valid range and provides type safety.
    /// Can also represent unknown/undefined scores using double.NaN.
    /// 
    /// DESIGN PRINCIPLE:
    /// Score's job: Measure quality on one dimension.
    /// Scores are absolute and independent - no weights or preferences.
    /// Weights/preferences belong in higher layers (optimization), not core data.
    /// </summary>
    public readonly record struct Score : IComparable<Score>
    {
        /// <summary>
        /// Minimum possible score (completely unacceptable).
        /// </summary>
        public static readonly Score Zero = new(0.0);

        /// <summary>
        /// Maximum possible score (ideal/perfect).
        /// </summary>
        public static readonly Score Perfect = new(10.0);

        /// <summary>
        /// Unknown or not applicable score.
        /// </summary>
        public static readonly Score Unknown = new(double.NaN);

        private readonly double _value;

        /// <summary>
        /// Creates a new score with validation.
        /// </summary>
        /// <param name="value">Score value, must be between 1.0 and 10.0 inclusive (or 0.0 for failure), or NaN for unknown.</param>
        /// <exception cref="ArgumentOutOfRangeException">Value is outside valid range and not NaN.</exception>
        public Score(double value)
        {
            // Allow NaN for unknown/undefined scores
            // Allow 0.0 for constraint violations/failures
            // Allow 1.0-10.0 for quality ratings
            if (!double.IsNaN(value) && value != 0.0 && (value < 1.0 || value > 10.0))
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Score must be 0.0 (failure), 1.0-10.0 (quality rating), or NaN for unknown");

            _value = value;
        }

        /// <summary>
        /// The numeric value of the score [0.0, 1.0-10.0] or NaN.
        /// </summary>
        public double Value => _value;

        /// <summary>
        /// True if this score is unknown/undefined.
        /// </summary>
        public bool IsUnknown => double.IsNaN(_value);

        /// <summary>
        /// Compares scores for ordering.
        /// Unknown scores are considered less than any valid score.
        /// </summary>
        public int CompareTo(Score other)
        {
            // NaN handling: Unknown < any valid score
            if (IsUnknown && other.IsUnknown) return 0;
            if (IsUnknown) return -1;
            if (other.IsUnknown) return 1;
            
            return _value.CompareTo(other._value);
        }

        /// <summary>
        /// Implicit conversion to double for calculations and formatting.
        /// </summary>
        public static implicit operator double(Score score) => score._value;

        /// <summary>
        /// Greater than operator.
        /// Unknown scores are not greater than anything.
        /// </summary>
        public static bool operator >(Score left, Score right)
        {
            if (left.IsUnknown || right.IsUnknown) return false;
            return left._value > right._value;
        }

        /// <summary>
        /// Less than operator.
        /// Unknown scores are less than any valid score.
        /// </summary>
        public static bool operator <(Score left, Score right)
        {
            if (left.IsUnknown && !right.IsUnknown) return true;
            if (!left.IsUnknown && right.IsUnknown) return false;
            if (left.IsUnknown && right.IsUnknown) return false;
            return left._value < right._value;
        }

        /// <summary>
        /// Greater than or equal operator.
        /// </summary>
        public static bool operator >=(Score left, Score right)
        {
            if (left.IsUnknown || right.IsUnknown) return left.IsUnknown == right.IsUnknown;
            return left._value >= right._value;
        }

        /// <summary>
        /// Less than or equal operator.
        /// </summary>
        public static bool operator <=(Score left, Score right)
        {
            if (left.IsUnknown) return true;
            if (right.IsUnknown) return false;
            return left._value <= right._value;
        }

        /// <summary>
        /// Formats score as 'x.y of 10' with one decimal place (or 'x of 10' for whole numbers), or 'N/A' for unknown, or '0 of 10' for failure.
        /// </summary>
        public override string ToString()
        {
            if (IsUnknown) return "N/A";
            
            // Format whole numbers without decimal point (e.g., "10 of 10" not "10.0 of 10")
            bool isWholeNumber = Math.Abs(_value - Math.Round(_value)) < 0.001;
            string formattedValue = isWholeNumber ? _value.ToString("F0") : _value.ToString("F1");
            
            return $"{formattedValue} of 10";
        }
    }

    /// <summary>
    /// Immutable structure that describe result of a simulation.
    /// 
    /// DESIGN PRINCIPLE:
    /// SimResult's job: Capture what happened.
    /// Contains pure data - no weights, no preferences, no decision-making.
    /// Same configuration always produces same result (idempotent).
    /// Scores are absolute measurements, independent of optimization goals.
    /// </summary>
    public record SimResult
    (
        double StartingBalance, 
        double InitialAllocation, 
        int NoOfYears, 
        IReadOnlyList<ISimObjective> Objectives, 
        IReadOnlyList<SimIteration> Results)
    {
        /// <summary>
        /// Rating scores for this simulation result.
        /// Empty by default (never null), populated via WithScores() extension method.
        /// Scores are idempotent - same rating always produces same score for this result.
        /// </summary>
        public IReadOnlyDictionary<string, Score> Scores { get; init; } = 
            new Dictionary<string, Score>();
        
        public double SurvivalRate => (double)Results.Count(x => x.Success) / (double)Results.Count;
        
        /// <summary>
        /// Convenience property for accessing iterations.
        /// </summary>
        public IReadOnlyList<SimIteration> Iterations => Results;
        
        /// <summary>
        /// Gets the iteration at the specified percentile by ending balance.
        /// Results are assumed to be sorted worst-to-best.
        /// </summary>
        /// <param name="percentile">Percentile (0.0 to 1.0).</param>
        /// <returns>Iteration at the specified percentile.</returns>
        public SimIteration Percentile(double percentile)
        {
            if (percentile < 0.0 || percentile > 1.0)
                throw new ArgumentOutOfRangeException(nameof(percentile), "Percentile must be between 0.0 and 1.0");
            
            if (Results.Count == 0)
                throw new InvalidOperationException("No results available");
            
            int index = (int)(percentile * (Results.Count - 1));
            return Results[index];
        }
    }

    /// <summary>
    /// Immutable structure that describe result of an iteration.
    /// </summary>
    public record SimIteration(int Index, bool Success, IReadOnlyList<SimYear> ByYear)
    {
        public  double StartingBalance => ByYear[0].JanBalance;
        public double EndingBalance => ByYear[^1].DecBalance;
        public int SurvivedYears => Success ? ByYear.Count : ByYear.Count - 1;
    }

    /// <summary>
    /// Immutable structure that describe result of a single year in an iteration.
    /// </summary>
    public record SimYear
    (
        int    Year,
        double JanBalance,
        double JanStockPct,
        double Fees,
        double PlannedWithdrawal,
        double ActualWithdrawal,
        double Change,
        double DecBalance,
        double DecStockPct,
        double StockROI,
        double BondROI,
        int    LikeYear
    )
    {
        public double JanBondPct => 1 - JanStockPct;
        public double DecBondPct => 1 - DecStockPct;
        public double PCTChange => Change / (JanBalance - Fees - ActualWithdrawal);
        public bool IsBadYear(double minimumExpectedGrowthPct = 0.0) => (Change / (JanBalance - Fees - ActualWithdrawal + 0.000001)) < minimumExpectedGrowthPct;
        public override string ToString() => $"{Year,4} | {JanBalance,6:C0} | {Fees,6:C0} | {ActualWithdrawal,6:C0} | {DecBalance,6:C0}";
    };
}
