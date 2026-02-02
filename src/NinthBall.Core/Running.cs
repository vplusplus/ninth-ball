
namespace NinthBall.Core
{
    /// <summary>
    /// Running inflation multipliers
    /// </summary>
    public readonly record struct InflationIndex
    (
        double Consumer,        // Cumulative inflation multiplier since year #0
        double Federal,         // Cumulative Federal tax indexing (with lag)
        double State            // Cumulative State tax indexing (with lag)
    )
    {
        public InflationIndex() : this(1.0, 1.0, 1.0) { }   // IMP: Multipliers start with 1.0

        public readonly InflationIndex Update(double cyInflationRate, double federalTaxInflationLagHaircut, double stateTaxInflationLagHaircut)
        {
            // WHY:
            // Running multiplier that represents cumulative inflation impact since year #0
            // Tracked at full precision, not quantized.
            // C-CPI generally rises slower than the standard CPI (Apply small haircut)
            // The IRS uses the Chained CPI (C-CPI-U) to measure inflation.
            // NJ, for example, doesn't index based on CPI or C-CPI each year.
            // Eventually they adjust, still fall behind (Apply larger haircut)
            // Fed and state index never goes back (Apply floor)

            return new
            (
                Consumer: Math.Max(0.01,    Consumer * (1 + cyInflationRate )),
                Federal:  Math.Max(Federal, Federal  * (1 + cyInflationRate - federalTaxInflationLagHaircut )),
                State:    Math.Max(State,   State    * (1 + cyInflationRate - stateTaxInflationLagHaircut   ))
            );
        }
    }

    /// <summary>
    /// Current year portfolio return and annualized returns (nonimal and real) since year #0
    /// </summary>
    public readonly record struct Growth(double PortfolioReturn, double AnnualizedReturn, double RealAnnualizedReturn)
    {
        private double _RunningGrowthMultiplier    { get; init; } = 1.0;

        public Growth Update(int yearIndex, InflationIndex cyInflationIndex, double cyPortfolioReturn)
        {
            // Running multiplier that represents cumulative portfolio growth since year #0
            double newGrowthMultiplier = _RunningGrowthMultiplier * (1 + cyPortfolioReturn);

            // Annualized nominal return since year #0
            double annualizedReturn = Math.Pow(newGrowthMultiplier, 1.0 / (yearIndex + 1)) - 1.0;

            // Real annualized return since year #0
            double realAnnualizedReturn = Math.Pow(newGrowthMultiplier / cyInflationIndex.Consumer, 1.0 / (yearIndex + 1)) - 1.0;

            return new
            (
                PortfolioReturn: cyPortfolioReturn,
                AnnualizedReturn: annualizedReturn,
                RealAnnualizedReturn: realAnnualizedReturn
            )
            {
                _RunningGrowthMultiplier    = newGrowthMultiplier
            };
        }
    }
}
