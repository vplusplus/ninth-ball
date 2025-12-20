
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NinthBall
{
    internal sealed class SimBuilder(IConfiguration config, IServiceProvider services)
    {
        private readonly Lazy<IReadOnlyList<ISimObjective>> LazyObjectives = new( () => CreateObjectivesOnce(config, services) );

        /// <summary>
        /// Immutable list of simulation objectives, sorted by execution order, prepared once.
        /// </summary>
        public IReadOnlyList<ISimObjective> SimulationObjectives => LazyObjectives.Value;

        /// <summary>
        /// Consult ConfigSectionName to SimObjective map.
        /// Create simulation objective if corresponding config section is defined.
        /// I do not like this. Discover a better and elegant way.
        /// </summary>
        private static IReadOnlyList<ISimObjective> CreateObjectivesOnce(IConfiguration config, IServiceProvider services)
        {
            List<ISimObjective> objectives = new();

            foreach (var pair in StrategyMap)
            {
                var sectionName = pair.Key;
                var sectionExists = config.GetSection(sectionName).Exists();
                if (sectionExists)
                {
                    var strategyType = pair.Value;
                    var simObjective = services.GetRequiredService(strategyType) as ISimObjective ?? throw new Exception($"{strategyType.Name} is not an ISimObjective | Section: '{sectionName}'");
                    objectives.Add(simObjective);
                }
            }

            // Validate
            // There can be only one choice for some family of strategies.

            // Sort by preferred execution order suggested by each strategy
            return objectives.OrderBy(x => x.Order).ToList();
        }

        /// <summary>
        /// Map of config section name, and a type that implements corresponding Simulation Objective/Strategy.
        /// I do not like this. Discover a better and elegant way.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, Type> StrategyMap = new Dictionary<string, Type>
        {
            [nameof(Rebalance)] = typeof(RebalancingStrategy),
            [nameof(Reallocate)] = typeof(ReallocationStrategy),
            [nameof(AdditionalIncomes)] = typeof(AdditionalIncomeStrategy),
            [nameof(FeesPCT)] = typeof(AnnualFeesStrategy),
            [nameof(Taxes)] = typeof(TaxStrategy),
            [nameof(LivingExpenses)] = typeof(LivingExpensesStrategy),
            [nameof(PrecalculatedLivingExpenses)] = typeof(PrecalculatedLivingExpensesStrategy),

            [nameof(FixedWithdrawal)] = typeof(FixedWithdrawalStrategy),
            [nameof(PercentageWithdrawal)] = typeof(PercentageWithdrawalStrategy),
            [nameof(VariablePercentageWithdrawal)] = typeof(VariablePercentageWithdrawalStrategy),

            [nameof(UseBufferCash)] = typeof(UseBufferCashStrategy),
            [nameof(BufferRefill)] = typeof(BufferRefillStrategy),

            [nameof(Growth)] = typeof(GrowthStrategy),
        };
    }
}
