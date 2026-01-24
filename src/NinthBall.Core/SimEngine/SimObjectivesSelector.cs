using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using static NinthBall.Core.AdditionalIncomes;

namespace NinthBall.Core
{
    internal sealed class SimObjectivesSelector(SimParams MySimParams, IServiceProvider AvailableServices)
    {
        public IReadOnlyList<ISimObjective> GetOrderedActiveObjectives()
        {
            if (null == MySimParams.Strategies) throw new FatalWarning($"Invalid input | List of simulation objectives was NULL.");
            if (0 == MySimParams.Strategies.Count) throw new FatalWarning($"Invalid input | Specify one or more simulation objectives.");

            List<ObjectiveInfo> chosenObjectives = new();

            foreach (var name in MySimParams.Strategies.Distinct())
            {
                // Cosmetic: Name as provided in the input can skip the Strategy(s) or Objevctive(s) suffix.
                var found = TryFindSimulationObjective(name, out var objectiveInfo);

                // Remeber chosen objective information
                chosenObjectives.Add( found 
                    ? objectiveInfo
                    : throw new FatalWarning($"Invalid input | Unknown simulation objective/strategy | '{name}'")
                );
            }

            ValidateRequiredObjectives( chosenObjectives );
            ValidateConflictingObjectives( chosenObjectives );

            // Resolve (activate) ISimObjective instance.
            // Sort by the execution order prescribed by the implementation.
            var orderedObjectives = chosenObjectives
                .Distinct()
                .Select(x => AvailableServices.GetRequiredService(x.Type))
                .OfType<ISimObjective>()
                .OrderBy(x => x.Order)
                .ToList();

            return 0 == orderedObjectives.Count 
                ? throw new FatalWarning($"Invalid input | Specify one or more simulation objectives.") 
                : orderedObjectives.AsReadOnly();
        }


        // Meta-data of ISimObjective implementation.
        private readonly record struct ObjectiveInfo(Type Type, StrategyFamily Family);

        // Discover ONCE all available implementations of ISimObjective.
        static readonly Lazy<IReadOnlyDictionary<string, ObjectiveInfo>> KnownObjectives = new(() =>
        {
            // Discover ISimObjective implementations
            var allObjectives = typeof(Simulation).Assembly
                .GetTypes()
                .Where(t => typeof(ISimObjective).IsAssignableFrom(t))      // Implements ISimObjective
                .Where(t => t != typeof(ISimObjective))                     // Is not ISimObjective itself
                .Where(t => !t.IsInterface)                                 // Is not just another interface
                .Where(t => !t.IsAbstract)                                  // We can create instances
                ;

            var map = new Dictionary<string, ObjectiveInfo>();

            foreach (var type in allObjectives)
            {
                var attr = type.GetCustomAttribute<StrategyFamilyAttribute>(inherit: false) ?? throw new Exception($"Fix the code | {type.FullName} has not declared its StrategyFamily.");
                map[type.Name] = new ObjectiveInfo(type, attr.Family);
            }

            // Lock it down.
            return map.AsReadOnly();
        });

        // Helper to register all known Simulation objectives to DI container.
        public static IServiceCollection AddSimulationObjectives(IServiceCollection services)
        {
            foreach ((string friendlyName, ObjectiveInfo objectiveInfo) in KnownObjectives.Value) services.AddSingleton(objectiveInfo.Type);
            return services;
        }

        // Cosmetic: Name as provided in the input can skip the Strategy(s) or Objevctive(s) suffix.
        // TODO: Review the naming convention of the ISumStrategy implementation, remove the guess work.
        static bool TryFindSimulationObjective(string friendlyName, out ObjectiveInfo objectiveInfo)
        {
            return KnownObjectives.Value.TryGetValue(friendlyName, out objectiveInfo)
                || KnownObjectives.Value.TryGetValue(friendlyName + "Strategy", out objectiveInfo)
                || KnownObjectives.Value.TryGetValue(friendlyName + "Strategies", out objectiveInfo)
                || KnownObjectives.Value.TryGetValue(friendlyName + "Objective", out objectiveInfo)
                || KnownObjectives.Value.TryGetValue(friendlyName + "Objectives", out objectiveInfo);
        }

        // Some strategies should not be ignored.
        // Throws an exception if a required strategy is not specified.
        static void ValidateRequiredObjectives(IList<ObjectiveInfo> chosenObjectives)
        {
            var requiredFamilies = new[]
            {
                StrategyFamily.Income,
                StrategyFamily.Expenses,
                StrategyFamily.Fees,
                StrategyFamily.Taxes,
                StrategyFamily.Withdrawals,
                StrategyFamily.Growth,
            };

            foreach (var family in requiredFamilies)
            {
                var specified = chosenObjectives.Any(x  => x.Family == family);
                if (!specified) throw new FatalWarning($"Please specify a '{family}' objective.");
            }
        }

        // For some strategy families, only one of its kind is allowed.
        // Throws an exception if more than one strategy is activated with-in each exclusive-families.
        static void ValidateConflictingObjectives(IList<ObjectiveInfo> chosenObjectives)
        {
            var exclusiveFamilies = new[]
            {
                StrategyFamily.Income,
                StrategyFamily.Expenses,
                StrategyFamily.Fees,
                StrategyFamily.Taxes,
                StrategyFamily.Withdrawals,
                StrategyFamily.Growth,
            };

            foreach (var family in exclusiveFamilies)
            {
                var conflicting = chosenObjectives.Where(s => s.Family == family).ToList();
                if (conflicting.Count > 1)
                {
                    var names = string.Join(", ", conflicting.Select(s => s.Type.Name));
                    throw new FatalWarning($"Conflicting strategies detected in family '{family}' [{names}] | Only one strategy from this family can be active at a time.");
                }
            }
        }

    }
}
