
namespace NinthBall
{
    /// <summary>
    /// Immutable structure that represents results of a simulation.
    /// </summary>
    public record SimResult
    (
        IReadOnlyList<ISimObjective> Objectives, 
        IReadOnlyList<SimIteration> Iterations
    )
    {
        public int NoOfYears { get; init; } = Iterations.Max(x => x.ByYear.Count);

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
        double Jan401K,
        double JanInv,
        double JanSav,
        double PYTaxes,
        double PYFees,
        double CYExp,
        double SS,
        double ANN,
        double X401K,
        double XInv,
        double XSav,
        double Surplus,
        double Change401K,
        double ChangeInv,
        double ChangeSav,
        double Dec401K,
        double DecInv,
        double DecSav,
        int    LikeYear
    )
    {
        public double JanBalance => Jan401K + JanInv + JanSav;
        public double Withdrawals => X401K + XInv + XSav;
        public double Change => Change401K + ChangeInv + ChangeSav;
        public double DecBalance => Dec401K + DecInv + DecSav;
        public double PCTChange => Change / (JanBalance - Withdrawals + 0.0000001);
        public bool IsBadYear(double minimumExpectedGrowthPct = 0.0) => PCTChange < minimumExpectedGrowthPct;
    };
}
