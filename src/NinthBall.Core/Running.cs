
namespace NinthBall.Core
{
    /// <summary>
    /// Running inflation multipliers.
    /// </summary>
    public readonly record struct InflationIndex
    {
        // Cumulative inflation multiplier since year #0
        public double Consumer
        {
            get  => 0 == field ? 1.0 : field;
            init => field = 0 == value ? 1e-100 : value;
        }

        // Cumulative Federal tax bracket and deduction indexing (includes C-CPI lag)
        public double Federal
        {
            get => 0 == field ? 1.0 : field;
            init => field = 0 == value ? 1e-100 : value;
        }

        // Cumulative State tax threshold and exemption indexing (includes legislative lag)
        public double State
        {
            get  => 0 == field ? 1.0 : field;
            init => field = 0 == value ? 1e-100 : value;
        }

        public InflationIndex Update(double cyInflationRate, double federalTaxInflationLagHaircut, double stateTaxInflationLagHaircut)
        {
            // WHY:
            // CPI multiplier represents cumulative inflation impact since year #0
            // Tracked at full precision, not quantized.
            // Impossible to hit zero (floor to 0.01)
            // The IRS uses the Chained CPI (C-CPI-U) to measure inflation.
            // C-CPI generally rises slower than the standard CPI (Apply small haircut)
            // NJ, for example, doesn't index based on CPI or C-CPI each year.
            // Eventually they adjust, still fall behind (Apply larger haircut)
            // Fed and state index never goes back (Apply floor)

            return new()
            {
                Consumer = Math.Max(0.01,    Consumer * (1 + cyInflationRate)),
                Federal  = Math.Max(Federal, Federal  * (1 + cyInflationRate - federalTaxInflationLagHaircut)),
                State    = Math.Max(State,   State    * (1 + cyInflationRate - stateTaxInflationLagHaircut)),
            };
        }

        public static InflationIndex Invalid => new() { Consumer = double.NaN, Federal = double.NaN, State = double.NaN };
    }


    /// <summary>
    /// Current year portfolio return and annualized returns (nominal and real) since year #0
    /// </summary>
    public readonly record struct Growth
    {
        private double GrowthMultiplier 
        {
            get     => 0 == field ? 1.0 : field;
            init    => field = 0 == value ? 1e-100 : value;
        }

        public double PortfolioReturn { get; init; }

        public double AnnualizedReturn { get; init; }

        public double RealAnnualizedReturn { get; init; }

        public Growth Update(int yearIndex, double cyPortfolioReturn, InflationIndex cyInflationIndex)
        {
            double newGrowthMultiplier = GrowthMultiplier * (1 + cyPortfolioReturn);

            return new()
            {
                GrowthMultiplier     = newGrowthMultiplier,
                PortfolioReturn      = cyPortfolioReturn,
                AnnualizedReturn     = Math.Pow(newGrowthMultiplier, 1.0 / (yearIndex + 1)) - 1.0,
                RealAnnualizedReturn = Math.Pow(newGrowthMultiplier / cyInflationIndex.Consumer, 1.0 / (yearIndex + 1)) - 1.0
            };
        }

        public static Growth Invalid => new() { GrowthMultiplier = double.NaN, PortfolioReturn = double.NaN, AnnualizedReturn = double.NaN, RealAnnualizedReturn = double.NaN };
    }

}
