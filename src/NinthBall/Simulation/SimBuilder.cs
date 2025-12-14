
using System.Collections.Immutable;

namespace NinthBall
{
    internal static class SimBuilder
    {
        /// <summary>
        /// Returns simulation objectives, sorted by execution order and then by config order.
        /// </summary>
        public static IReadOnlyList<ISimObjective> CreateObjectives(this SimConfig simConfig)
        {
            ArgumentNullException.ThrowIfNull(simConfig);

            // Create instances of ISimObjective(s)
            // Ignore NULLs - The ObjectiveOrNull() helper may return nulls.
            // Sort by execution order suggested by the objectives.
            // Break ties using their original config order.
            var objectives = new List<ISimObjective?> {
                simConfig.ObjectiveOrNull(simConfig.InitPortfolio),
                simConfig.ObjectiveOrNull(simConfig.YearlyRebalance),
                simConfig.ObjectiveOrNull(simConfig.AdditionalIncomes),
                simConfig.ObjectiveOrNull(simConfig.LivingExpenses),
                simConfig.ObjectiveOrNull(simConfig.PrecalculatedLivingExpenses),
                simConfig.ObjectiveOrNull(simConfig.Taxes),
                simConfig.ObjectiveOrNull(simConfig.PreTaxWithdrawal),
                simConfig.ObjectiveOrNull(simConfig.ReduceWithdrawal),
                simConfig.ObjectiveOrNull(simConfig.UseBufferCash),
                simConfig.ObjectiveOrNull(simConfig.FlatGrowth),
                simConfig.ObjectiveOrNull(simConfig.HistoricalGrowth),
                simConfig.ObjectiveOrNull(simConfig.Fees),
            }
            .Where(x => null != x)
            .Select((objective, index) => (Objective: objective!, ConfigOrder: index))
            .OrderBy(x => x.Objective.Order)
            .ThenBy(x => x.ConfigOrder)
            .Select(x => x.Objective)
            .ToList()
            .AsReadOnly();

            return objectives;
        }

        /// <summary>
        /// Returns instance ISimObjective.
        /// Returns null if config ection is null or the section is not recognized.
        /// </summary>
        private static ISimObjective? ObjectiveOrNull(this SimConfig cfg, object section) => 
            null != section && SimObjectiveMap.TryGetValue(section.GetType(), out var ctor) 
                ? ctor(cfg) 
                : null;

        /// <summary>
        /// Map of config section type and a factory that provides corresponsing ISimObjective.
        /// </summary>
        private static readonly IReadOnlyDictionary<Type, Func<SimConfig, ISimObjective>> SimObjectiveMap =
            new Dictionary<Type, Func<SimConfig, ISimObjective>>
            {
                [typeof(InitPortfolio)]                 = cfg => new InitPortfolioObjective(cfg),
                [typeof(YearlyRebalance)]               = cfg => new RebalancingObjective(cfg),
                [typeof(AdditionalIncomes)]             = cfg => new AdditionalIncomeObjective(cfg),
                [typeof(LivingExpenses)]                = cfg => new LivingExpensesObjective(cfg),
                [typeof(PrecalculatedLivingExpenses)]   = cfg => new PrecalculatedLivingExpensesObjective(cfg),
                [typeof(Taxes)]                         = cfg => new TaxStrategy(cfg),
                [typeof(PreTaxWithdrawal)]              = cfg => new PreTaxWithdrawalObjective(cfg),
                //[typeof(ReduceWithdrawal)]            = cfg => new ReduceWithdrawalAfterBadYears(cfg),
                //[typeof(UseBufferCash)]               = cfg => new UseBufferCashAfterBadYears(cfg),
                [typeof(FlatGrowth)]                    = cfg => new FlatGrowthObjective(cfg),
                [typeof(HistoricalGrowth)]              = cfg => new HistoricalGrowthObjective(cfg),
                [typeof(FeesPCT)]                       = cfg => new AnnualFeesObjective(cfg),
            }
            .ToImmutableDictionary();
    }
}
