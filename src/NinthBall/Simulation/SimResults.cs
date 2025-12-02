
namespace NinthBall
{
    public readonly record struct Score(double value) : IComparable<Score>
    {
        public static readonly Score Zero = new Score(0);
        public static readonly Score Unknown = new(double.NaN);

        public readonly double Value = double.IsNaN(value) ? value : value >= 0.0 && value <= 10.0 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "Score must be between 0 and 10");
        public bool IsUnknown => double.IsNaN(Value);
        public int CompareTo(Score other) => Value.CompareTo(other.Value);
        public override string ToString() => IsUnknown ? "N/A" : Math.Abs(Value - Math.Round(Value)) < 0.001 ? $"{Value:F0} of 10" : $"{Value:F1} of 10";

        public static implicit operator double(Score score) => score.Value;
    }

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
        /// </summary>
        public IReadOnlyDictionary<string, Score> Scores { get; init; } =  new Dictionary<string, Score>();
        
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
