
namespace NinthBall
{
    /// <summary>
    /// Immutable structure that describe result of a simulation.
    /// </summary>
    public record SimResult
    (
        double StartingBalance, 
        double InitialAllocation, 
        int NoOfYears, 
        IReadOnlyList<ISimObjective> Objectives, 
        IReadOnlyList<SimIteration> Results)
    {
        public double SurvivalRate => (double)Results.Count(x => x.Success) / (double)Results.Count;
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
        public bool IsBadYear(double minimumExpectedGrowthPct = 0.0) => (Change / (JanBalance - Fees - ActualWithdrawal + 0.000001)) < minimumExpectedGrowthPct;

        public override string ToString() => $"{Year,4} | {JanBalance,6:C0} | {Fees,6:C0} | {ActualWithdrawal,6:C0} | {DecBalance,6:C0}";
    };
}
