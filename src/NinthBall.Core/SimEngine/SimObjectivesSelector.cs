    
using Microsoft.Extensions.DependencyInjection;

namespace NinthBall.Core
{
    /// <summary>
    /// Provides active simulation objectives selected for current simulation session.
    /// </summary>
    internal sealed class SimObjectivesSelector(SimParams MySimParams, IServiceProvider AvailableServices)
    {
        public IReadOnlyList<ISimObjective> GetOrderedActiveObjectives()
        {
            if (null == MySimParams.Objectives) throw new FatalWarning($"Invalid input | List of simulation objectives was NULL.");
            if (0 == MySimParams.Objectives.Count) throw new FatalWarning($"Invalid input | Specify one or more simulation objectives.");

            List<SimObjectiveInfo> chosenObjectives = new();

            foreach (var name in MySimParams.Objectives.Distinct())
            {
                // Cosmetic: Name as provided in the input can skip the Strategy(s) or Objective(s) suffix.
                var found = TryFindSimulationObjective(name, out var objectiveInfo);

                // Remember chosen objective information
                chosenObjectives.Add( found 
                    ? objectiveInfo
                    : throw new FatalWarning($"Invalid input | Unknown simulation objective. | '{name}'")
                );
            }

            // Check for missing or conflicting objectives
            ValidateRequiredObjectives( chosenObjectives );
            ValidateConflictingObjectives( chosenObjectives );

            // IMPORTANT: Sort chosen objectives by the execution order of the family.
            // Resolve (activate) ISimObjective instance.
            var orderedObjectives = chosenObjectives
                .Distinct()                                                     // Just in case...
                .OrderBy(x => GetStrategyExecutionOrder(x.Family))              // IMP: Sort by execution order
                .Select(x => AvailableServices.GetRequiredService(x.Type))      // Activate
                .Cast<ISimObjective>()                                          // Should not fail here.
                .ToList();

            return 0 == orderedObjectives.Count 
                ? throw new FatalWarning($"Invalid input | Specify one or more simulation objectives.") 
                : orderedObjectives.AsReadOnly();

            // Cosmetic: Name as provided in the input can skip the Strategy(s) or Objective(s) suffix.
            static bool TryFindSimulationObjective(string friendlyName, out SimObjectiveInfo objectiveInfo)
            {
                var KnownObjectives = SimObjectives.AllObjectives;

                return KnownObjectives.TryGetValue(friendlyName, out objectiveInfo)
                    || KnownObjectives.TryGetValue($"{friendlyName}Objective", out objectiveInfo)
                    || KnownObjectives.TryGetValue($"{friendlyName}Objectives", out objectiveInfo)
                    || KnownObjectives.TryGetValue($"{friendlyName}Strategy", out objectiveInfo)
                    || KnownObjectives.TryGetValue($"{friendlyName}Strategies", out objectiveInfo)
                    ;
            }

            static int GetStrategyExecutionOrder(StrategyFamily family)
            {
                return family switch
                {
                    StrategyFamily.Rebalance    => 000,     // Changes Jan view of the portfolio
                    StrategyFamily.Fees         => 111,     // Fees are on Jan balance
                    StrategyFamily.Taxes        => 222,     // Since taxes work on prior year
                    StrategyFamily.Income       => 333,     // External source, doesn't depend on CY info
                    StrategyFamily.Expenses     => 444,     // Useful to know for withdrawal optimizations if any.
                    StrategyFamily.Withdrawals  => 555,     // CRITICAL: Park this before RMD
                    StrategyFamily.RMD          => 666,     // CRITICAL: RMD adjustments after planned withdrawals are known.
                    StrategyFamily.Growth       => 777,     // We know only at the end of the year.
                    _ => int.MaxValue                       // Doesn't matter
                };
            }

        }

        //......................................................................
        // Validate chosen strategies - Required & Conflicts
        //......................................................................
        // Some strategies should not be ignored.
        // Throws an exception if a required strategy is not specified.
        static void ValidateRequiredObjectives(IList<SimObjectiveInfo> chosenObjectives)
        {
            var requiredFamilies = new[]
            {
                StrategyFamily.Income,
                StrategyFamily.Expenses,
                StrategyFamily.Fees,
                StrategyFamily.Taxes,
                StrategyFamily.Withdrawals,
                StrategyFamily.Growth,
                StrategyFamily.Rebalance,
            };

            foreach (var family in requiredFamilies)
            {
                var specified = chosenObjectives.Any(x  => x.Family == family);
                if (!specified) throw new FatalWarning($"Please specify a '{family}' objective.");
            }
        }

        // For some strategy families, only one of its kind is allowed.
        // Throws an exception if more than one strategy is activated within each exclusive family.
        static void ValidateConflictingObjectives(IList<SimObjectiveInfo> chosenObjectives)
        {
            var exclusiveFamilies = new[]
            {
                StrategyFamily.Income,
                StrategyFamily.Expenses,
                StrategyFamily.Fees,
                StrategyFamily.Taxes,
                StrategyFamily.Withdrawals,
                StrategyFamily.Growth,
                StrategyFamily.Rebalance,
            };

            foreach (var family in exclusiveFamilies)
            {
                var conflicting = chosenObjectives.Where(s => s.Family == family).ToList();
                if (conflicting.Count > 1)
                {
                    var names = string.Join(", ", conflicting.Select(s => s.Type.Name));
                    throw new FatalWarning($"Conflicting objectives: [{names}]");
                }
            }
        }
    }
}
