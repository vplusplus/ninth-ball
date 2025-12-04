
namespace NinthBall
{
    /// <summary>
    /// Immutable structure that represents results of a simulation.
    /// </summary>
    public record SimResult
    (
        double InitialBalance, 
        double InitialStockAllocation, 
        int NoOfYears, 
        IReadOnlyList<ISimObjective> Objectives, 
        IReadOnlyList<SimIteration> Iterations
    )
    {
        public IReadOnlyDictionary<string, SimScore10> Scores { get; init; } =  new Dictionary<string, SimScore10>();
        
        public double SurvivalRate => (double)Iterations.Count(x => x.Success) / (double)Iterations.Count;
        
        public SimIteration Percentile(double percentile) =>
            percentile < 0.0 || percentile > 1.0 ? throw new ArgumentOutOfRangeException(nameof(percentile), "Percentile must be between 0.0 and 1.0") :
            Iterations.Count == 0 ? throw new InvalidOperationException("No results available") :
            Iterations[(int)(percentile * (Iterations.Count - 1))];
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

    /// <summary>
    /// A number ranging from 0 to 10 that represents quality of the simulation result.
    /// </summary>
    public readonly record struct SimScore10(double valueZeroToTen) : IComparable<SimScore10>
    {
        public static readonly SimScore10 Zero = new SimScore10(0);
        public static readonly SimScore10 Unknown = new(double.NaN);

        public readonly double Value = double.IsNaN(valueZeroToTen) || (valueZeroToTen >= 0.0 && valueZeroToTen <= 10.0) 
            ? valueZeroToTen 
            : throw new ArgumentOutOfRangeException(nameof(valueZeroToTen), valueZeroToTen, "Score must be between 0 and 10");

        public bool IsUnknown => double.IsNaN(Value);

        public static implicit operator double(SimScore10 score) => score.Value;

        public int CompareTo(SimScore10 other) => Value.CompareTo(other.Value);

        public override string ToString() => IsUnknown ? "N/A" : Math.Abs(Value - Math.Round(Value)) < 0.001 ? $"{Value:F0} of 10" : $"{Value:F1} of 10";
    }

}
