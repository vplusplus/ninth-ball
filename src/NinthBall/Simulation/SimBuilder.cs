
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
            return new List<ISimObjective?> {
                simConfig.ObjectiveOrNull(simConfig.PCTWithdrawal),
                simConfig.ObjectiveOrNull(simConfig.PrecalculatedWithdrawal),
                simConfig.ObjectiveOrNull(simConfig.ReduceWithdrawal),
                simConfig.ObjectiveOrNull(simConfig.UseBufferCash),
                simConfig.ObjectiveOrNull(simConfig.FlatGrowth),
                simConfig.ObjectiveOrNull(simConfig.HistoricalGrowth),
                simConfig.ObjectiveOrNull(simConfig.Fees),
                new AllInOneStrategy(simConfig)
            }
            .Where(x => null != x)
            .Select((objective, index) => (Objective: objective!, ConfigOrder: index))
            .OrderBy(x => x.Objective.Order)
            .ThenBy(x => x.ConfigOrder)
            .Select(x => x.Objective)
            .ToList()
            .AsReadOnly();
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
                //[typeof(PCTWithdrawal)]             = cfg => new PCTWithdrawalObjective(cfg),
                //[typeof(PrecalculatedWithdrawal)]   = cfg => new PrecalculatedWithdrawalObjective(cfg),
                //[typeof(ReduceWithdrawal)]          = cfg => new ReduceWithdrawalAfterBadYears(cfg),
                //[typeof(UseBufferCash)]             = cfg => new UseBufferCashAfterBadYears(cfg),
                [typeof(FlatGrowth)]                = cfg => new FlatGrowthObjective(cfg),
                [typeof(HistoricalGrowth)]          = cfg => new HistoricalGrowthObjective(cfg),
                //[typeof(Fees)]                      = cfg => new AnnualFeesObjective(cfg),
            }
            .ToImmutableDictionary();
    }
}
